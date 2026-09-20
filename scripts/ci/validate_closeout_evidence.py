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
EXPECTED_BASE_MAIN_COMMIT = "318647a4143f22cd4da4e16b6cb4d9bcea023eb4"
EXPECTED_RUNTIME_COMMIT = "47fd09f8567d02bca8a4f4d38404b451ede5e0c6"
EXPECTED_RUNTIME_MANIFEST_HASH = "C48C96FEA42463FAFCD3D74C1FB50C2036E5BBDAD29897A404D7FE78D6DADA9C"
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
OWNER_QUALIFICATION_STATUSES = {"NOT_RUN", "PARTIAL", "READY_FOR_OWNER_REVIEW", "READY_FOR_PROMOTION"}
OWNER_GATE_STATUSES = {"NOT_RUN", "PASS", "FAIL", "BLOCKED", "ACCEPTED"}
OWNER_GATE_KEYS = (
    "desktopChromeForeground",
    "desktopEdgeForeground",
    "localColdTimeToInteractive",
    "browserWorkingSet",
    "physicalFocusLoss",
    "screenReader",
    "humanBenchmark",
)
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


def validate_release_candidate(
    candidate: Any,
    sg10: dict[str, Any] | None,
    sg11: dict[str, Any] | None,
    closeout_status: str,
) -> None:
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
        location_status = location.get("status")
        if closeout_status == "READY_FOR_PROMOTION":
            if location_status not in {"LOCAL_ONLY", "PRODUCTION_READBACK"}:
                fail("releaseCandidate.artifactLocation.status must be LOCAL_ONLY or PRODUCTION_READBACK before promotion")
            if location_status == "PRODUCTION_READBACK" and location.get("uploadedToProductionR2") is not True:
                fail("PRODUCTION_READBACK requires uploadedToProductionR2 true")
        else:
            if location_status != "LOCAL_ONLY":
                fail("releaseCandidate.artifactLocation.status must remain LOCAL_ONLY before promotion")
            if location.get("uploadedToProductionR2") is not False:
                fail("releaseCandidate.artifactLocation.uploadedToProductionR2 must be false before promotion")
        if location.get("prefixPattern") != "signal-garden/<version>/Build/":
            fail("releaseCandidate.artifactLocation.prefixPattern must preserve the immutable R2 contract")
        if location.get("promotionPrefixPattern") != "signal-garden/<UTC-date>-<first-7-chars-of-runtime-source-commit>/Build/":
            fail("releaseCandidate.artifactLocation.promotionPrefixPattern must preserve the Git-derived release prefix")
        if location.get("assetBasePattern") != "<assetBase>/Build/<filename>":
            fail("releaseCandidate.artifactLocation.assetBasePattern must preserve the host contract")

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
    if updates.get("status") == "SUPERSEDED":
        previous = updates.get("previousCandidateRuntimeSourceCommit")
        if not isinstance(previous, str) or not HEX_40.fullmatch(previous):
            fail("qualificationUpdates.previousCandidateRuntimeSourceCommit must be a full commit SHA")
        if updates.get("replacementRequired") is not True:
            fail("qualificationUpdates.replacementRequired must be true for superseded evidence")
        require_string(updates, "reason", "qualificationUpdates.reason")
        evidence = require_string(updates, "evidence", "qualificationUpdates.evidence")
        if evidence is not None:
            repo_file(evidence)
        smoke = updates.get("localRenderSmoke")
        if not isinstance(smoke, dict):
            fail("qualificationUpdates.localRenderSmoke must be an object")
        else:
            if smoke.get("status") != "PASS_LOCAL":
                fail("qualificationUpdates.localRenderSmoke.status must be PASS_LOCAL")
            if smoke.get("render") != "1920x1080":
                fail("qualificationUpdates.localRenderSmoke.render must be 1920x1080")
            require_string(smoke, "route", "qualificationUpdates.localRenderSmoke.route")
            require_string(smoke, "method", "qualificationUpdates.localRenderSmoke.method")
            require_string(smoke, "classification", "qualificationUpdates.localRenderSmoke.classification")
        return
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

    diagnostics = updates.get("localDiagnostics")
    if not isinstance(diagnostics, dict):
        fail("qualificationUpdates.localDiagnostics must be an object")
        return
    if diagnostics.get("status") != "RECORDED_NOT_OWNER_QUALIFICATION":
        fail("qualificationUpdates.localDiagnostics.status must preserve the non-owner classification")
    if diagnostics.get("classification") != "AUTOMATED_LOCAL_HEADLESS_ONLY":
        fail("qualificationUpdates.localDiagnostics.classification must preserve the headless-only classification")
    if diagnostics.get("recordedAt") != "2026-09-19":
        fail("qualificationUpdates.localDiagnostics.recordedAt must identify the diagnostic observation")
    if diagnostics.get("runtimeSourceCommit") != EXPECTED_RUNTIME_COMMIT:
        fail("qualificationUpdates.localDiagnostics.runtimeSourceCommit must match the runtime candidate")
    if diagnostics.get("route") != "/signal-garden/play/?sg-render=1920x1080&sg-stats=1":
        fail("qualificationUpdates.localDiagnostics.route must preserve the reference diagnostic route")
    require_string(diagnostics, "host", "qualificationUpdates.localDiagnostics.host")
    cache_key = diagnostics.get("assetCacheKey")
    if not isinstance(cache_key, str) or not re.fullmatch(r"[0-9a-f]{16}", cache_key):
        fail("qualificationUpdates.localDiagnostics.assetCacheKey must be a 16-character lowercase hex key")
    if diagnostics.get("render") != "1920x1080":
        fail("qualificationUpdates.localDiagnostics.render must be 1920x1080")
    if diagnostics.get("sampleSeconds") != 30 or diagnostics.get("sampleCount") != 30:
        fail("qualificationUpdates.localDiagnostics must record 30 seconds and 30 samples")
    browsers = diagnostics.get("browsers")
    if not isinstance(browsers, list) or {record.get("browser") for record in browsers if isinstance(record, dict)} != {"Chrome", "Edge"}:
        fail("qualificationUpdates.localDiagnostics.browsers must contain Chrome and Edge")
    else:
        for index, record in enumerate(browsers):
            label = f"qualificationUpdates.localDiagnostics.browsers[{index}]"
            if not isinstance(record, dict):
                fail(f"{label} must be an object")
                continue
            require_string(record, "browser", f"{label}.browser")
            require_string(record, "version", f"{label}.version")
            if record.get("mode") != "headless":
                fail(f"{label}.mode must be headless")
            if record.get("freshProfile") is not True:
                fail(f"{label}.freshProfile must be true")
            if record.get("result") != "PASS_DIAGNOSTIC":
                fail(f"{label}.result must be PASS_DIAGNOSTIC")
            tti = record.get("timeToInteractiveMilliseconds")
            if not isinstance(tti, (int, float)) or tti <= 0 or tti > 10000:
                fail(f"{label}.timeToInteractiveMilliseconds must be between 0 and 10000")
            mean = record.get("meanFps")
            if not isinstance(mean, (int, float)) or mean < 60:
                fail(f"{label}.meanFps must be at least 60 for a diagnostic pass")
            if not isinstance(record.get("minimumOneSecondSampleFps"), (int, float)):
                fail(f"{label}.minimumOneSecondSampleFps must be numeric")
    if diagnostics.get("focusQualified") is not False:
        fail("qualificationUpdates.localDiagnostics.focusQualified must be false")
    if diagnostics.get("ownerQualificationSubstitute") is not False:
        fail("qualificationUpdates.localDiagnostics.ownerQualificationSubstitute must be false")
    limitations = diagnostics.get("limitations")
    if not isinstance(limitations, list) or not limitations or not all(isinstance(item, str) and item for item in limitations):
        fail("qualificationUpdates.localDiagnostics.limitations must document diagnostic boundaries")
    repo_file("docs/sg-12-local-diagnostics.md")


def validate_owner_gate(record: Any, label: str) -> str | None:
    if not isinstance(record, dict):
        fail(f"{label} must be an object")
        return None
    status = record.get("status")
    if status not in OWNER_GATE_STATUSES:
        fail(f"{label}.status must be one of {sorted(OWNER_GATE_STATUSES)}")
    require_string(record, "classification", f"{label}.classification")
    if status in {"NOT_RUN", "FAIL", "BLOCKED"}:
        require_string(record, "reason", f"{label}.reason")
    if status in {"PASS", "ACCEPTED"}:
        require_string(record, "method", f"{label}.method")
    return status if isinstance(status, str) else None


def validate_owner_qualification(qualification: Any, sg11: dict[str, Any] | None, closeout_status: str) -> None:
    if not isinstance(qualification, dict):
        fail("ownerQualification must be an object")
        return
    if qualification.get("schemaVersion") != 1:
        fail("ownerQualification.schemaVersion must be 1")
    status = qualification.get("status")
    if status not in OWNER_QUALIFICATION_STATUSES:
        fail(f"ownerQualification.status must be one of {sorted(OWNER_QUALIFICATION_STATUSES)}")
    runtime_commit = require_string(
        qualification,
        "runtimeSourceCommit",
        "ownerQualification.runtimeSourceCommit",
    )
    if runtime_commit is not None:
        if not HEX_40.fullmatch(runtime_commit):
            fail("ownerQualification.runtimeSourceCommit must be a full commit SHA")
        if runtime_commit != EXPECTED_RUNTIME_COMMIT:
            fail("ownerQualification.runtimeSourceCommit must match the frozen SG-10 runtime")

    target = qualification.get("target")
    if not isinstance(target, dict):
        fail("ownerQualification.target must be an object")
    else:
        expected_target = {
            "render": "1920x1080",
            "meanFpsMinimum": 60,
            "localTimeToInteractiveMaximumMilliseconds": 10000,
            "compressedArtifactBudgetMiB": 12,
            "browserWorkingSetBudgetMiB": 512,
        }
        if target != expected_target:
            fail("ownerQualification.target must preserve the approved qualification targets")

    gate_statuses: dict[str, str | None] = {}
    for key in OWNER_GATE_KEYS:
        gate_statuses[key] = validate_owner_gate(qualification.get(key), f"ownerQualification.{key}")

    for key in ("desktopChromeForeground", "desktopEdgeForeground"):
        record = qualification.get(key)
        if not isinstance(record, dict) or gate_statuses[key] not in {"PASS", "ACCEPTED"}:
            continue
        if record.get("render") != "1920x1080":
            fail(f"ownerQualification.{key}.render must be 1920x1080")
        if record.get("sampleSeconds") != 30 or record.get("sampleCount") != 30:
            fail(f"ownerQualification.{key} must record 30 focused seconds and 30 samples")
        if not isinstance(record.get("meanFps"), (int, float)) or record["meanFps"] < 60:
            fail(f"ownerQualification.{key}.meanFps must be at least 60")
        if not isinstance(record.get("minimumOneSecondSampleFps"), (int, float)):
            fail(f"ownerQualification.{key}.minimumOneSecondSampleFps must be numeric")
        require_string(record, "browser", f"ownerQualification.{key}.browser")

    cold_tti = qualification.get("localColdTimeToInteractive")
    if isinstance(cold_tti, dict) and gate_statuses["localColdTimeToInteractive"] in {"PASS", "ACCEPTED"}:
        value = cold_tti.get("milliseconds")
        if not isinstance(value, (int, float)) or value > 10000:
            fail("ownerQualification.localColdTimeToInteractive.milliseconds must be at most 10000")

    working_set = qualification.get("browserWorkingSet")
    if isinstance(working_set, dict) and gate_statuses["browserWorkingSet"] in {"PASS", "ACCEPTED"}:
        peak = working_set.get("peakMiB")
        if not isinstance(peak, (int, float)) or peak > 512:
            fail("ownerQualification.browserWorkingSet.peakMiB must be at most 512")
        if working_set.get("budgetMiB") != 512:
            fail("ownerQualification.browserWorkingSet.budgetMiB must be 512")
        require_string(working_set, "measurementTool", "ownerQualification.browserWorkingSet.measurementTool")

    focus = qualification.get("physicalFocusLoss")
    if isinstance(focus, dict) and gate_statuses["physicalFocusLoss"] in {"PASS", "ACCEPTED"}:
        if focus.get("recoverySuccess") is not True:
            fail("ownerQualification.physicalFocusLoss.recoverySuccess must be true")

    screen_reader = qualification.get("screenReader")
    if isinstance(screen_reader, dict) and gate_statuses["screenReader"] in {"PASS", "ACCEPTED"}:
        require_string(screen_reader, "reader", "ownerQualification.screenReader.reader")
        announcements = screen_reader.get("announcements")
        if not isinstance(announcements, list) or not announcements:
            fail("ownerQualification.screenReader.announcements must list observed state announcements")

    human = qualification.get("humanBenchmark")
    if isinstance(human, dict):
        if human.get("evidence") != SG11_RELATIVE_PATH:
            fail("ownerQualification.humanBenchmark.evidence must point to SG-11 evidence")
        if gate_statuses["humanBenchmark"] in {"PASS", "ACCEPTED"}:
            if not isinstance(sg11, dict) or sg11.get("status") != "PASS_RECORDED":
                fail("ownerQualification.humanBenchmark PASS requires SG-11 PASS_RECORDED")
        elif gate_statuses["humanBenchmark"] == "NOT_RUN" and isinstance(sg11, dict) and sg11.get("status") != "NOT_RUN":
            fail("ownerQualification.humanBenchmark NOT_RUN must match SG-11 status")

    complete = all(value in {"PASS", "ACCEPTED"} for value in gate_statuses.values())
    any_recorded = any(value not in {None, "NOT_RUN"} for value in gate_statuses.values())
    if status == "NOT_RUN" and any_recorded:
        fail("ownerQualification.status NOT_RUN cannot contain recorded gate results")
    if status == "PARTIAL" and (not any_recorded or complete):
        fail("ownerQualification.status PARTIAL requires incomplete recorded gate results")
    if status in {"READY_FOR_OWNER_REVIEW", "READY_FOR_PROMOTION"} and not complete:
        fail(f"ownerQualification.status {status} requires every owner gate to pass or be accepted")
    if closeout_status == "NOT_READY" and status in {"READY_FOR_OWNER_REVIEW", "READY_FOR_PROMOTION"}:
        fail("closeout status NOT_READY cannot claim completed owner qualification")
    if closeout_status == "READY_FOR_OWNER_REVIEW" and status != "READY_FOR_OWNER_REVIEW":
        fail("READY_FOR_OWNER_REVIEW closeout must match ownerQualification.status")
    if closeout_status == "READY_FOR_PROMOTION" and status != "READY_FOR_PROMOTION":
        fail("READY_FOR_PROMOTION closeout must match ownerQualification.status")


def validate_jira(jira: Any, closeout_status: str) -> None:
    if not isinstance(jira, dict):
        fail("jira must be an object")
        return
    if jira.get("epicKey") != "GAME-278":
        fail("jira.epicKey must be GAME-278")
    if jira.get("epicStatusReadback") != "Ready":
        fail("jira.epicStatusReadback must preserve the current Ready readback")
    if jira.get("statusChangedByThisRecord") is not False:
        fail("jira.statusChangedByThisRecord must be false")
    expected_agreement = "RECONCILED" if closeout_status == "READY_FOR_PROMOTION" else "PENDING_OWNER_RECONCILIATION"
    if jira.get("agreementStatus") != expected_agreement:
        fail(f"jira.agreementStatus must be {expected_agreement} for closeout status {closeout_status}")
    require_string(jira, "reason", "jira.reason")


def validate_owner_and_decision(owner: Any, decision: Any, closeout_status: str) -> None:
    if not isinstance(owner, dict):
        fail("ownerApprovals must be an object")
    else:
        expected_owner_status = "APPROVED" if closeout_status == "READY_FOR_PROMOTION" else "PENDING"
        if owner.get("status") != expected_owner_status:
            fail(f"ownerApprovals.status must be {expected_owner_status} for closeout status {closeout_status}")
        items = owner.get("items")
        if not isinstance(items, list):
            fail("ownerApprovals.items must be an array")
        elif expected_owner_status == "PENDING" and not items:
            fail("ownerApprovals.items must list the outstanding gates")
        elif expected_owner_status == "APPROVED" and items:
            fail("ownerApprovals.items must be empty after owner approval")
    if not isinstance(decision, dict):
        fail("releaseDecision must be an object")
    else:
        expected_decision = "READY_FOR_PROMOTION" if closeout_status == "READY_FOR_PROMOTION" else "NOT_READY"
        if decision.get("status") != expected_decision:
            fail(f"releaseDecision.status must be {expected_decision} for closeout status {closeout_status}")
        expected_promotion = closeout_status == "READY_FOR_PROMOTION"
        if decision.get("promotionAllowed") is not expected_promotion:
            fail(f"releaseDecision.promotionAllowed must be {str(expected_promotion).lower()} for closeout status {closeout_status}")
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
        repository = manifest.get("repository")
        if isinstance(repository, dict) and repository.get("baseMainCommit") != EXPECTED_BASE_MAIN_COMMIT:
            fail("repository.baseMainCommit must identify the current merged main commit")
        validate_repository_identity(manifest.get("repository"))
        validate_release_candidate(manifest.get("releaseCandidate"), sg10, sg11, status if isinstance(status, str) else "NOT_READY")
        validate_qualification_updates(manifest.get("qualificationUpdates"))
        validate_owner_qualification(manifest.get("ownerQualification"), sg11, status if isinstance(status, str) else "NOT_READY")
        validate_children(manifest.get("childStories"))
        validate_technology(manifest.get("technology"))
        validate_known_limitations(manifest.get("knownLimitations"))
        validate_jira(manifest.get("jira"), status if isinstance(status, str) else "NOT_READY")
        validate_owner_and_decision(manifest.get("ownerApprovals"), manifest.get("releaseDecision"), status if isinstance(status, str) else "NOT_READY")
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
