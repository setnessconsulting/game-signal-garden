#!/usr/bin/env python3
"""Validate the fail-closed Signal Garden SG-12 closeout record."""

from __future__ import annotations

import hashlib
import json
import re
import sys
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[2]
MANIFEST_RELATIVE_PATH = "docs/sg-12-release-candidate.json"
SG10_RELATIVE_PATH = "docs/sg-10-build-identity.json"
SG11_RELATIVE_PATH = "docs/sg-11-playtest-evidence.json"
EXPECTED_GAME = "signal-garden"
EXPECTED_ISSUE = "GAME-290"
EXPECTED_RUNTIME_COMMIT = "34122315f6f31718ffa616517495392be5c91a2b"
EXPECTED_RUNTIME_MANIFEST_HASH = "6886B4CD48F3B5CB1ABBCAC3D983428DBF3F6159E5398E794FE2EC00999CA161"
EXPECTED_ARTIFACTS = (
    "WebGL.loader.js",
    "WebGL.data.br",
    "WebGL.framework.js.br",
    "WebGL.wasm.br",
)
EXPECTED_CHILDREN = {f"GAME-{number}" for number in range(279, 291)}
HEX_40 = re.compile(r"^[0-9a-fA-F]{40}$")
HEX_64 = re.compile(r"^[0-9a-fA-F]{64}$")
VALID_STATUSES = {"NOT_READY", "READY_FOR_OWNER_REVIEW", "READY_FOR_PROMOTION"}
errors: list[str] = []


def fail(message: str) -> None:
    errors.append(message)


def repo_file(relative_path: str) -> Path | None:
    candidate = (ROOT / relative_path).resolve()
    try:
        candidate.relative_to(ROOT)
    except ValueError:
        fail(f"Path escapes the repository: {relative_path}")
        return None
    if not candidate.is_file():
        fail(f"Required closeout file is missing: {relative_path}")
        return None
    return candidate


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest().upper()


def read_json(relative_path: str) -> dict[str, Any] | None:
    path = repo_file(relative_path)
    if path is None:
        return None
    try:
        value = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, UnicodeError, json.JSONDecodeError) as exc:
        fail(f"Invalid JSON in {relative_path}: {exc}")
        return None
    if not isinstance(value, dict):
        fail(f"Expected a JSON object in {relative_path}")
        return None
    return value


def require_string(parent: dict[str, Any], key: str, label: str) -> str | None:
    value = parent.get(key)
    if not isinstance(value, str) or not value:
        fail(f"{label} must be a non-empty string")
        return None
    return value


def validate_repository_identity(repository: Any) -> None:
    if not isinstance(repository, dict):
        fail("repository must be an object")
        return
    for key in ("baseMainCommit", "readme", "gdd", "closeoutRecord", "scope"):
        require_string(repository, key, f"repository.{key}")
    base = repository.get("baseMainCommit")
    if isinstance(base, str) and not HEX_40.fullmatch(base):
        fail("repository.baseMainCommit must be a full commit SHA")
    for key in ("readme", "gdd", "closeoutRecord"):
        path = repository.get(key)
        if isinstance(path, str):
            repo_file(path)


def validate_release_candidate(candidate: Any, sg10: dict[str, Any] | None, sg11: dict[str, Any] | None) -> None:
    if not isinstance(candidate, dict):
        fail("releaseCandidate must be an object")
        return
    runtime_commit = require_string(candidate, "runtimeSourceCommit", "releaseCandidate.runtimeSourceCommit")
    if runtime_commit is not None:
        if not HEX_40.fullmatch(runtime_commit):
            fail("releaseCandidate.runtimeSourceCommit must be a full commit SHA")
        if runtime_commit != EXPECTED_RUNTIME_COMMIT:
            fail("releaseCandidate.runtimeSourceCommit must remain the recorded SG-10 runtime source commit")
    identity_path = require_string(candidate, "runtimeBuildIdentity", "releaseCandidate.runtimeBuildIdentity")
    identity_hash = require_string(candidate, "runtimeBuildIdentitySha256", "releaseCandidate.runtimeBuildIdentitySha256")
    if identity_path is not None and identity_hash is not None:
        if identity_path != SG10_RELATIVE_PATH:
            fail("releaseCandidate.runtimeBuildIdentity must point to SG-10 build identity")
        if not HEX_64.fullmatch(identity_hash):
            fail("releaseCandidate.runtimeBuildIdentitySha256 must be a SHA-256")
        path = repo_file(identity_path)
        if path is not None and sha256_file(path) != identity_hash.upper():
            fail("releaseCandidate.runtimeBuildIdentitySha256 does not match the tracked manifest")
        if identity_hash.upper() != EXPECTED_RUNTIME_MANIFEST_HASH:
            fail("releaseCandidate.runtimeBuildIdentitySha256 is not the reviewed SG-10 identity")

    location = candidate.get("artifactLocation")
    if not isinstance(location, dict):
        fail("releaseCandidate.artifactLocation must be an object")
    else:
        if location.get("status") != "LOCAL_ONLY":
            fail("releaseCandidate.artifactLocation.status must remain LOCAL_ONLY")
        if location.get("prefixPattern") != "signal-garden/<version>/Build/":
            fail("releaseCandidate.artifactLocation.prefixPattern must preserve the immutable R2 contract")
        if location.get("assetBasePattern") != "<assetBase>/Build/<filename>":
            fail("releaseCandidate.artifactLocation.assetBasePattern must preserve the host contract")
        if location.get("uploadedToProductionR2") is not False:
            fail("releaseCandidate.artifactLocation.uploadedToProductionR2 must be false")

    artifacts = candidate.get("artifacts")
    if artifacts != list(EXPECTED_ARTIFACTS):
        fail("releaseCandidate.artifacts must list the exact SG-10 artifact filenames in order")
    if candidate.get("unity") != "6000.6.0f1":
        fail("releaseCandidate.unity must be 6000.6.0f1")

    packages = candidate.get("packages")
    expected_packages = {
        "com.unity.render-pipelines.universal": "17.6.0",
        "com.unity.inputsystem": "1.19.0",
        "com.unity.ugui": "2.6.0",
        "com.unity.test-framework": "1.8.0",
    }
    if packages != expected_packages:
        fail("releaseCandidate.packages must match the recorded Unity package versions")

    if isinstance(sg10, dict):
        source = sg10.get("source")
        if not isinstance(source, dict) or source.get("commit") != runtime_commit:
            fail("SG-10 source commit does not match the closeout runtime source commit")
        build = sg10.get("build")
        if not isinstance(build, dict) or [record.get("filename") for record in build.get("artifacts", [])] != list(EXPECTED_ARTIFACTS):
            fail("SG-10 build artifacts do not match the closeout artifact list")
    if isinstance(sg11, dict):
        source = sg11.get("source")
        if not isinstance(source, dict):
            fail("SG-11 evidence is missing its source identity")
        else:
            if source.get("runtimeBuildIdentity") != SG10_RELATIVE_PATH:
                fail("SG-11 evidence must point to the SG-10 build identity")
            if source.get("runtimeBuildIdentitySha256") != identity_hash:
                fail("SG-11 runtime build identity hash does not match SG-12")
            if source.get("runtimeBuildSourceCommit") != runtime_commit:
                fail("SG-11 runtime build source commit does not match SG-12")


def validate_children(children: Any) -> None:
    if not isinstance(children, list):
        fail("childStories must be an array")
        return
    keys: set[str] = set()
    for index, child in enumerate(children):
        label = f"childStories[{index}]"
        if not isinstance(child, dict):
            fail(f"{label} must be an object")
            continue
        key = require_string(child, "key", f"{label}.key")
        if key is not None:
            if key in keys:
                fail(f"Duplicate child story {key}")
            keys.add(key)
        require_string(child, "name", f"{label}.name")
        require_string(child, "evidenceStatus", f"{label}.evidenceStatus")
        require_string(child, "ownerDisposition", f"{label}.ownerDisposition")
        evidence = child.get("evidence")
        if not isinstance(evidence, list) or not evidence:
            fail(f"{label}.evidence must contain at least one path")
        else:
            for path in evidence:
                if not isinstance(path, str):
                    fail(f"{label}.evidence entries must be strings")
                else:
                    repo_file(path)
    if keys != EXPECTED_CHILDREN:
        fail("childStories must include GAME-279 through GAME-290 exactly once")


def validate_technology(technology: Any) -> None:
    if not isinstance(technology, dict):
        fail("technology must be an object")
        return
    expected = {
        "unity": "PASS_RECORDED",
        "blender": "PASS_RECORDED",
        "figma": "PASS_RECORDED_NORMALIZED_HANDOFF",
        "rive": "DECLINED_V1",
        "githubActions": "PASS_RECORDED_CREDENTIAL_FREE",
        "sentry": "DEFERRED",
        "hermes": "DEFERRED",
        "browserQualification": "OPEN_OWNER_GATE",
        "levelBest": "DEFERRED",
    }
    for key, expected_status in expected.items():
        record = technology.get(key)
        if not isinstance(record, dict) or record.get("status") != expected_status:
            fail(f"technology.{key}.status must be {expected_status}")
        if isinstance(record, dict) and key not in {"rive", "sentry", "hermes", "browserQualification", "levelBest"}:
            require_string(record, "evidence", f"technology.{key}.evidence")


def validate_known_limitations(limitations: Any) -> None:
    if not isinstance(limitations, list) or not limitations:
        fail("knownLimitations must contain the open owner gates")
        return
    seen: set[str] = set()
    for index, limitation in enumerate(limitations):
        label = f"knownLimitations[{index}]"
        if not isinstance(limitation, dict):
            fail(f"{label} must be an object")
            continue
        identifier = require_string(limitation, "id", f"{label}.id")
        if identifier is not None and identifier in seen:
            fail(f"Duplicate known limitation {identifier}")
        if identifier is not None:
            seen.add(identifier)
        if limitation.get("status") != "OPEN":
            fail(f"{label}.status must remain OPEN until owner evidence exists")
        require_string(limitation, "ownerAction", f"{label}.ownerAction")


def validate_qualification_updates(updates: Any) -> None:
    if not isinstance(updates, dict):
        fail("qualificationUpdates must be an object")
        return
    if updates.get("recordedAt") != "2026-09-19":
        fail("qualificationUpdates.recordedAt must identify the current qualification observation")
    if updates.get("candidateRuntimeSourceCommit") != EXPECTED_RUNTIME_COMMIT:
        fail("qualificationUpdates.candidateRuntimeSourceCommit must match the runtime candidate")
    chrome = updates.get("desktopChromeForeground")
    if not isinstance(chrome, dict):
        fail("qualificationUpdates.desktopChromeForeground must be an object")
    else:
        if chrome.get("status") != "PASS_LOCAL":
            fail("qualificationUpdates.desktopChromeForeground.status must be PASS_LOCAL")
        if chrome.get("browser") != "Chrome 153.0.8010.52":
            fail("qualificationUpdates.desktopChromeForeground.browser must record the observed Chrome version")
        if chrome.get("render") != "1920x1080":
            fail("qualificationUpdates.desktopChromeForeground.render must be 1920x1080")
        if chrome.get("sampleSeconds") != 30 or chrome.get("sampleCount") != 30:
            fail("qualificationUpdates.desktopChromeForeground must record 30 focused seconds and 30 samples")
        if chrome.get("meanFps") != 101.9 or chrome.get("minimumOneSecondSampleFps") != 27.6:
            fail("qualificationUpdates.desktopChromeForeground FPS values do not match the observed run")
        if chrome.get("result") != "PASS":
            fail("qualificationUpdates.desktopChromeForeground.result must be PASS")
        require_string(chrome, "method", "qualificationUpdates.desktopChromeForeground.method")
        require_string(chrome, "classification", "qualificationUpdates.desktopChromeForeground.classification")
    pause = updates.get("pauseResume")
    if not isinstance(pause, dict) or pause.get("status") != "PASS_LOCAL":
        fail("qualificationUpdates.pauseResume.status must be PASS_LOCAL")
    elif not isinstance(pause.get("method"), str) or not pause["method"]:
        fail("qualificationUpdates.pauseResume.method must describe the observed control path")
    warm_tti = updates.get("localWarmReloadTimeToInteractive")
    if not isinstance(warm_tti, dict):
        fail("qualificationUpdates.localWarmReloadTimeToInteractive must be an object")
    else:
        if warm_tti.get("status") != "PASS_LOCAL_WARM":
            fail("qualificationUpdates.localWarmReloadTimeToInteractive.status must be PASS_LOCAL_WARM")
        if warm_tti.get("milliseconds") != 681 or warm_tti.get("targetMilliseconds") != 10000:
            fail("qualificationUpdates.localWarmReloadTimeToInteractive timing does not match the observed run")
        require_string(warm_tti, "method", "qualificationUpdates.localWarmReloadTimeToInteractive.method")


def validate_jira(jira: Any) -> None:
    if not isinstance(jira, dict):
        fail("jira must be an object")
        return
    if jira.get("epicKey") != "GAME-278":
        fail("jira.epicKey must be GAME-278")
    if jira.get("epicStatusReadback") != "Ready":
        fail("jira.epicStatusReadback must preserve the current Ready readback")
    if jira.get("statusChangedByThisRecord") is not False:
        fail("jira.statusChangedByThisRecord must be false")
    if jira.get("agreementStatus") != "PENDING_OWNER_RECONCILIATION":
        fail("jira.agreementStatus must remain pending until the owner reconciles Jira")
    require_string(jira, "reason", "jira.reason")


def validate_owner_and_decision(owner: Any, decision: Any) -> None:
    if not isinstance(owner, dict):
        fail("ownerApprovals must be an object")
    else:
        if owner.get("status") != "PENDING":
            fail("ownerApprovals.status must remain PENDING")
        items = owner.get("items")
        if not isinstance(items, list) or not items:
            fail("ownerApprovals.items must list the outstanding gates")
    if not isinstance(decision, dict):
        fail("releaseDecision must be an object")
    else:
        if decision.get("status") != "NOT_READY":
            fail("releaseDecision.status must remain NOT_READY")
        if decision.get("promotionAllowed") is not False:
            fail("releaseDecision.promotionAllowed must be false")
        require_string(decision, "reason", "releaseDecision.reason")


def validate_provenance(provenance: Any) -> None:
    if not isinstance(provenance, dict):
        fail("provenance must be an object")
        return
    require_string(provenance, "license", "provenance.license")
    if provenance.get("generatedBuildTracked") is not False:
        fail("provenance.generatedBuildTracked must be false")
    if provenance.get("productionChanged") is not False:
        fail("provenance.productionChanged must be false")


def main() -> int:
    manifest = read_json(MANIFEST_RELATIVE_PATH)
    sg10 = read_json(SG10_RELATIVE_PATH)
    sg11 = read_json(SG11_RELATIVE_PATH)
    if manifest is not None:
        if manifest.get("schemaVersion") != 1:
            fail("schemaVersion must be 1")
        if manifest.get("game") != EXPECTED_GAME:
            fail("game must be signal-garden")
        if manifest.get("issue") != EXPECTED_ISSUE:
            fail("issue must be GAME-290")
        status = manifest.get("status")
        if status not in VALID_STATUSES:
            fail("status must be NOT_READY, READY_FOR_OWNER_REVIEW, or READY_FOR_PROMOTION")
        validate_repository_identity(manifest.get("repository"))
        validate_release_candidate(manifest.get("releaseCandidate"), sg10, sg11)
        validate_qualification_updates(manifest.get("qualificationUpdates"))
        validate_children(manifest.get("childStories"))
        validate_technology(manifest.get("technology"))
        validate_known_limitations(manifest.get("knownLimitations"))
        validate_jira(manifest.get("jira"))
        validate_owner_and_decision(manifest.get("ownerApprovals"), manifest.get("releaseDecision"))
        validate_provenance(manifest.get("provenance"))

    if errors:
        print("SG-12 closeout evidence validation failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1
    print("PASS: SG-12 closeout identity, child evidence, technology dispositions, and NOT_READY boundary are consistent")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
