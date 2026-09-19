#!/usr/bin/env python3
"""Validate the anonymous SG-11 human-playtest evidence record.

The validator checks the protocol and prevents a claimed benchmark pass from
being recorded without complete, threshold-matching session evidence. It does
not invent sessions and it does not treat automated gameplay or agent activity
as human qualification.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[2]
DEFAULT_MANIFEST = ROOT / "docs" / "sg-11-playtest-evidence.json"
EXPECTED_GAME = "signal-garden"
EXPECTED_ISSUE = "GAME-289"
EXPECTED_SAMPLE_SIZE = 5
HEX_40 = re.compile(r"^[0-9a-fA-F]{40}$")
HEX_64 = re.compile(r"^[0-9a-fA-F]{64}$")
SESSION_ID = re.compile(r"^P0[1-5]$")
STATUSES = {"NOT_RUN", "PARTIAL", "PASS_RECORDED", "BLOCKED"}
SEVERITIES = {"blocker", "high", "medium", "low", "note"}
DISPOSITIONS = {"accepted", "deferred", "not-a-bug", "fixed", "open"}
FORBIDDEN_KEYS = {
    "name",
    "email",
    "emailaddress",
    "phone",
    "address",
    "age",
    "dateofbirth",
    "participantname",
}
errors: list[str] = []


def fail(message: str) -> None:
    errors.append(message)


def repository_file(relative_path: str) -> Path | None:
    candidate = (ROOT / relative_path).resolve()
    try:
        candidate.relative_to(ROOT)
    except ValueError:
        fail(f"Path escapes the repository: {relative_path}")
        return None
    if not candidate.is_file():
        fail(f"Required evidence file is missing: {relative_path}")
        return None
    return candidate


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest().upper()


def read_json(path: Path) -> dict[str, Any] | None:
    try:
        value = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, UnicodeError, json.JSONDecodeError) as exc:
        fail(f"Invalid JSON in {path}: {exc}")
        return None
    if not isinstance(value, dict):
        fail("The SG-11 evidence file must contain a JSON object")
        return None
    return value


def require_string(parent: dict[str, Any], key: str, label: str) -> str | None:
    value = parent.get(key)
    if not isinstance(value, str) or not value:
        fail(f"{label} must be a non-empty string")
        return None
    return value


def require_bool(parent: dict[str, Any], key: str, label: str) -> bool | None:
    value = parent.get(key)
    if type(value) is not bool:
        fail(f"{label} must be a boolean")
        return None
    return value


def require_number(parent: dict[str, Any], key: str, label: str, *, minimum: float = 0) -> float | None:
    value = parent.get(key)
    if type(value) not in (int, float) or isinstance(value, bool) or value < minimum:
        fail(f"{label} must be a number >= {minimum}")
        return None
    return float(value)


def check_forbidden_keys(value: Any, path: str = "root") -> None:
    if isinstance(value, dict):
        for key, child in value.items():
            if str(key).casefold().replace("_", "") in FORBIDDEN_KEYS:
                fail(f"Personal-data field is not allowed in {path}.{key}")
            check_forbidden_keys(child, f"{path}.{key}")
    elif isinstance(value, list):
        for index, child in enumerate(value):
            check_forbidden_keys(child, f"{path}[{index}]")


def validate_source(source: Any) -> None:
    if not isinstance(source, dict):
        fail("source must be an object")
        return
    for key in ("candidateReviewCommit", "runtimeBuildSourceCommit"):
        value = require_string(source, key, f"source.{key}")
        if value is not None and not HEX_40.fullmatch(value):
            fail(f"source.{key} must be a full 40-character commit SHA")
    path_value = require_string(source, "runtimeBuildIdentity", "source.runtimeBuildIdentity")
    hash_value = require_string(source, "runtimeBuildIdentitySha256", "source.runtimeBuildIdentitySha256")
    if path_value is not None and hash_value is not None:
        if not HEX_64.fullmatch(hash_value):
            fail("source.runtimeBuildIdentitySha256 must be a SHA-256")
        else:
            path = repository_file(path_value)
            if path is not None and sha256_file(path) != hash_value.upper():
                fail("source.runtimeBuildIdentitySha256 does not match the tracked SG-10 manifest")


def validate_target(target: Any) -> None:
    if not isinstance(target, dict):
        fail("target must be an object")
        return
    if target.get("browser") != "Desktop Chrome or Edge":
        fail("target.browser must be Desktop Chrome or Edge")
    if target.get("display") != "1920x1080":
        fail("target.display must be 1920x1080")
    if target.get("locale") != "English":
        fail("target.locale must be English")
    if target.get("coaching") != "none":
        fail("target.coaching must be none")
    if target.get("freshPlayer") is not True:
        fail("target.freshPlayer must be true")
    if target.get("sampleSize") != EXPECTED_SAMPLE_SIZE:
        fail(f"target.sampleSize must be {EXPECTED_SAMPLE_SIZE}")


def validate_thresholds(thresholds: Any) -> tuple[float, float, int] | None:
    if not isinstance(thresholds, dict):
        fail("thresholds must be an object")
        return None
    objective = require_number(thresholds, "objectiveUnderSeconds", "thresholds.objectiveUnderSeconds")
    completion = require_number(thresholds, "completionUnderSeconds", "thresholds.completionUnderSeconds")
    minimum = thresholds.get("minimumPlayersMeetingBothTimeBars")
    if type(minimum) is not int or not 1 <= minimum <= EXPECTED_SAMPLE_SIZE:
        fail("thresholds.minimumPlayersMeetingBothTimeBars must be an integer from 1 to 5")
        minimum = 0
    if thresholds.get("recoverySuccessRequiredForEverySession") is not True:
        fail("thresholds.recoverySuccessRequiredForEverySession must be true")
    if thresholds.get("restartQuitSuccessRequiredForEverySession") is not True:
        fail("thresholds.restartQuitSuccessRequiredForEverySession must be true")
    if objective is None or completion is None:
        return None
    return objective, completion, minimum


def validate_session(session: Any, index: int, objective_limit: float, completion_limit: float) -> bool:
    label = f"sessions[{index}]"
    if not isinstance(session, dict):
        fail(f"{label} must be an object")
        return False
    code = require_string(session, "sessionCode", f"{label}.sessionCode")
    if code is not None and not SESSION_ID.fullmatch(code):
        fail(f"{label}.sessionCode must be one of P01 through P05")
    browser = require_string(session, "browser", f"{label}.browser")
    if browser is not None and browser not in {"Chrome", "Edge"}:
        fail(f"{label}.browser must be Chrome or Edge")
    if session.get("resolution") != "1920x1080":
        fail(f"{label}.resolution must be 1920x1080")
    if session.get("freshPlayer") is not True:
        fail(f"{label}.freshPlayer must be true")
    for key in (
        "timeToUnderstandSeconds",
        "timeToFirstMeaningfulActionSeconds",
        "timeToCompleteSeconds",
    ):
        require_number(session, key, f"{label}.{key}")
    invalid_count = session.get("invalidRouteCount")
    if type(invalid_count) is not int or invalid_count < 0:
        fail(f"{label}.invalidRouteCount must be a non-negative integer")
    recovery = require_bool(session, "recoverySuccess", f"{label}.recoverySuccess")
    restart = require_bool(session, "restartQuitSuccess", f"{label}.restartQuitSuccess")
    clarity = session.get("perceivedClarity")
    if type(clarity) is not int or not 1 <= clarity <= 5:
        fail(f"{label}.perceivedClarity must be an integer from 1 to 5")
    observations = session.get("observations")
    if not isinstance(observations, list) or not all(isinstance(item, str) and item for item in observations):
        fail(f"{label}.observations must be a list of non-empty strings")
    objective = session.get("timeToUnderstandSeconds")
    completion = session.get("timeToCompleteSeconds")
    if type(objective) not in (int, float) or isinstance(objective, bool):
        return False
    if type(completion) not in (int, float) or isinstance(completion, bool):
        return False
    return (
        objective <= objective_limit
        and completion <= completion_limit
        and recovery is True
        and restart is True
    )


def validate_findings(findings: Any, session_codes: set[str]) -> None:
    if not isinstance(findings, list):
        fail("findings must be an array")
        return
    seen: set[str] = set()
    for index, finding in enumerate(findings):
        label = f"findings[{index}]"
        if not isinstance(finding, dict):
            fail(f"{label} must be an object")
            continue
        finding_id = require_string(finding, "id", f"{label}.id")
        if finding_id is not None and finding_id in seen:
            fail(f"Duplicate finding id: {finding_id}")
        if finding_id is not None:
            seen.add(finding_id)
        if finding.get("rubricDimension") not in {"miniMetro", "dorfromantik", "theRoom"}:
            fail(f"{label}.rubricDimension must identify a reference rubric")
        severity = require_string(finding, "severity", f"{label}.severity")
        if severity is not None and severity not in SEVERITIES:
            fail(f"{label}.severity is unsupported")
        evidence = finding.get("evidenceSessionCodes")
        if not isinstance(evidence, list) or not evidence or not all(code in session_codes for code in evidence):
            fail(f"{label}.evidenceSessionCodes must reference recorded session codes")
        for key in ("observation", "remediation", "residualOwnerDecision"):
            require_string(finding, key, f"{label}.{key}")
        disposition = require_string(finding, "disposition", f"{label}.disposition")
        if disposition is not None and disposition not in DISPOSITIONS:
            fail(f"{label}.disposition is unsupported")


def validate_manifest(manifest: dict[str, Any]) -> None:
    if manifest.get("schemaVersion") != 1:
        fail("schemaVersion must be 1")
    if manifest.get("game") != EXPECTED_GAME:
        fail("game must be signal-garden")
    if manifest.get("issue") != EXPECTED_ISSUE:
        fail("issue must be GAME-289")
    status = manifest.get("status")
    if status not in STATUSES:
        fail("status must be NOT_RUN, PARTIAL, PASS_RECORDED, or BLOCKED")
    validate_source(manifest.get("source"))
    validate_target(manifest.get("target"))
    threshold_values = validate_thresholds(manifest.get("thresholds"))
    measures = manifest.get("measures")
    expected_measures = {
        "timeToUnderstandSeconds",
        "timeToFirstMeaningfulActionSeconds",
        "timeToCompleteSeconds",
        "invalidRouteCount",
        "recoverySuccess",
        "restartQuitSuccess",
        "perceivedClarity",
    }
    if not isinstance(measures, list) or set(measures) != expected_measures:
        fail("measures must list the seven SG-11 metrics exactly")

    rubric = manifest.get("rubric")
    if not isinstance(rubric, dict):
        fail("rubric must be an object")
    else:
        for key in ("miniMetro", "dorfromantik", "theRoom"):
            if not isinstance(rubric.get(key), list) or not rubric[key]:
                fail(f"rubric.{key} must contain measurable questions")

    sessions = manifest.get("sessions")
    if not isinstance(sessions, list):
        fail("sessions must be an array")
        sessions = []
    if len(sessions) > EXPECTED_SAMPLE_SIZE:
        fail("sessions cannot exceed the selected sample size of five")
    objective_limit, completion_limit, minimum = threshold_values or (0, 0, 0)
    session_codes: set[str] = set()
    qualifying = 0
    for index, session in enumerate(sessions):
        if isinstance(session, dict) and isinstance(session.get("sessionCode"), str):
            if session["sessionCode"] in session_codes:
                fail(f"Duplicate session code: {session['sessionCode']}")
            session_codes.add(session["sessionCode"])
        if validate_session(session, index, objective_limit, completion_limit):
            qualifying += 1

    findings = manifest.get("findings")
    validate_findings(findings, session_codes)
    owner_review = manifest.get("ownerReview")
    if not isinstance(owner_review, dict):
        fail("ownerReview must be an object")
    elif owner_review.get("status") not in {"PENDING", "APPROVED", "REJECTED"}:
        fail("ownerReview.status must be PENDING, APPROVED, or REJECTED")

    if status == "NOT_RUN" and sessions:
        fail("status NOT_RUN cannot contain recorded sessions")
    if status == "PARTIAL" and not (0 < len(sessions) < EXPECTED_SAMPLE_SIZE):
        fail("status PARTIAL requires between one and four recorded sessions")
    if status == "PASS_RECORDED":
        if len(sessions) != EXPECTED_SAMPLE_SIZE:
            fail("status PASS_RECORDED requires all five sessions")
        if qualifying < minimum:
            fail("status PASS_RECORDED does not meet the selected time/recovery thresholds")
        if any(
            not isinstance(session, dict)
            or session.get("recoverySuccess") is not True
            or session.get("restartQuitSuccess") is not True
            for session in sessions
        ):
            fail("status PASS_RECORDED requires recovery and restart/quit success in every session")
        if isinstance(findings, list):
            for finding in findings:
                if not isinstance(finding, dict):
                    continue
                if finding.get("severity") in {"blocker", "high"} and finding.get("disposition") == "open":
                    fail("status PASS_RECORDED cannot leave a blocker or high finding open")
        if isinstance(owner_review, dict) and owner_review.get("status") != "APPROVED":
            fail("status PASS_RECORDED requires ownerReview.status APPROVED")

    provenance = manifest.get("provenance")
    if not isinstance(provenance, dict):
        fail("provenance must be an object")
    else:
        for key in ("originalWork", "thirdPartyAssetsCopied", "thirdPartyCodeCopied", "thirdPartyBrandingCopied"):
            if provenance.get(key) is not (key == "originalWork"):
                fail(f"provenance.{key} has an invalid value")

    check_forbidden_keys(manifest)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST)
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    manifest_path = args.manifest.resolve()
    try:
        manifest_path.relative_to(ROOT)
    except ValueError:
        print("SG-11 evidence validation failed: manifest must be inside the repository", file=sys.stderr)
        return 1
    manifest = read_json(manifest_path)
    if manifest is not None:
        validate_manifest(manifest)
    if errors:
        print("SG-11 playtest evidence validation failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1
    status = manifest.get("status") if manifest else "UNKNOWN"
    print(f"PASS: SG-11 protocol and anonymous evidence schema are valid; human benchmark status {status}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
