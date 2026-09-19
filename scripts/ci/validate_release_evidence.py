#!/usr/bin/env python3
"""Validate Signal Garden's checked-in SG-10 build identity and evidence.

The repository intentionally does not track generated WebGL output.  This
validator therefore checks the recorded identity, hashes, budgets, and
classification fields in a clean checkout.  Passing without ``--build-dir``
means the evidence record is structurally valid; it does not mean that Unity
or a browser ran in the current process.  Supplying ``--build-dir`` verifies
the four generated files against the recorded hashes as well.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import subprocess
import sys
from pathlib import Path
from typing import Any, Iterable


ROOT = Path(__file__).resolve().parents[2]
MANIFEST_RELATIVE_PATH = "docs/sg-10-build-identity.json"
EXPECTED_UNITY_VERSION = "6000.6.0f1"
EXPECTED_UNITY_REVISION = "f7f8ed4d1e24"
EXPECTED_ISSUE = "GAME-288"
EXPECTED_GAME = "signal-garden"
EXPECTED_PACKAGE_VERSIONS = {
    "com.unity.render-pipelines.universal": "17.6.0",
    "com.unity.inputsystem": "1.19.0",
    "com.unity.ugui": "2.6.0",
    "com.unity.test-framework": "1.8.0",
}
EXPECTED_ARTIFACT_ROLES = ("loader", "data", "framework", "wasm")
HEX_40 = re.compile(r"^[0-9a-fA-F]{40}$")
HEX_64 = re.compile(r"^[0-9a-fA-F]{64}$")
VALID_STATUSES = {
    "PASS",
    "PASS_LOCAL",
    "PASS_RECORDED",
    "NOT_RUN",
    "PARTIAL",
    "BLOCKED",
    "PENDING",
}
errors: list[str] = []


def fail(message: str) -> None:
    errors.append(message)


def repo_file(relative_path: str, *, required: bool = True) -> Path | None:
    candidate = (ROOT / relative_path).resolve()
    try:
        candidate.relative_to(ROOT)
    except ValueError:
        fail(f"Path escapes the repository: {relative_path}")
        return None
    if not candidate.is_file():
        if required:
            fail(f"Required evidence file is missing: {relative_path}")
        return None
    return candidate


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


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest().upper()


def git(*arguments: str) -> str | None:
    try:
        result = subprocess.run(
            ["git", *arguments],
            cwd=ROOT,
            check=True,
            capture_output=True,
            text=True,
        )
    except (OSError, subprocess.CalledProcessError) as exc:
        fail(f"Git command failed ({' '.join(arguments)}): {exc}")
        return None
    return result.stdout.strip()


def tracked_unity_inputs() -> list[str]:
    try:
        result = subprocess.run(
            [
                "git",
                "ls-files",
                "-z",
                "--",
                "Assets",
                "Packages",
                "ProjectSettings",
                "SourceArt",
            ],
            cwd=ROOT,
            check=True,
            capture_output=True,
        )
    except (OSError, subprocess.CalledProcessError) as exc:
        fail(f"Could not list Unity input files: {exc}")
        return []
    return sorted(path for path in result.stdout.decode("utf-8").split("\0") if path)


def unity_input_tree_sha256() -> str:
    """Hash tracked Unity inputs as ``path NUL bytes NUL`` records."""

    digest = hashlib.sha256()
    for relative_path in tracked_unity_inputs():
        path = repo_file(relative_path)
        if path is None:
            continue
        digest.update(relative_path.replace("\\", "/").encode("utf-8"))
        digest.update(b"\0")
        with path.open("rb") as stream:
            for block in iter(lambda: stream.read(1024 * 1024), b""):
                digest.update(block)
        digest.update(b"\0")
    return digest.hexdigest().upper()


def require_string(parent: dict[str, Any], key: str, label: str) -> str | None:
    value = parent.get(key)
    if not isinstance(value, str) or not value:
        fail(f"{label} must be a non-empty string")
        return None
    return value


def require_status(parent: dict[str, Any], key: str, label: str) -> str | None:
    value = require_string(parent, key, label)
    if value is not None and value not in VALID_STATUSES:
        fail(f"{label} has unsupported classification {value!r}")
    return value


def validate_source(manifest: dict[str, Any]) -> None:
    source = manifest.get("source")
    if not isinstance(source, dict):
        fail("Build identity must include a source object")
        return

    commit = require_string(source, "commit", "source.commit")
    if commit is not None:
        if not HEX_40.fullmatch(commit):
            fail("source.commit must be a full 40-character SHA-1")
        elif git("cat-file", "-e", f"{commit}^{{commit}}") is None:
            fail("source.commit is not present in the checkout")

    tree_hash = require_string(source, "unityInputTreeSha256", "source.unityInputTreeSha256")
    if tree_hash is not None:
        if not HEX_64.fullmatch(tree_hash):
            fail("source.unityInputTreeSha256 must be a SHA-256")
        elif tree_hash.upper() != unity_input_tree_sha256():
            fail("source.unityInputTreeSha256 does not match tracked Unity inputs")

    scene = require_string(source, "scene", "source.scene")
    if scene is not None:
        repo_file(scene)

    packages_lock = require_string(source, "packagesLock", "source.packagesLock")
    packages_hash = require_string(
        source,
        "packagesLockSha256",
        "source.packagesLockSha256",
    )
    if packages_lock is not None and packages_hash is not None:
        path = repo_file(packages_lock)
        if path is not None and (
            not HEX_64.fullmatch(packages_hash)
            or sha256_file(path) != packages_hash.upper()
        ):
            fail("source.packagesLockSha256 does not match packages-lock.json")

    recorded_packages = source.get("packages")
    if not isinstance(recorded_packages, dict):
        fail("source.packages must record the resolved Unity package versions")
    else:
        for package_name, expected_version in EXPECTED_PACKAGE_VERSIONS.items():
            if recorded_packages.get(package_name) != expected_version:
                fail(f"source.packages.{package_name} must be {expected_version}")
    if packages_lock is not None:
        lock = read_json(packages_lock)
        dependencies = lock.get("dependencies") if isinstance(lock, dict) else None
        if not isinstance(dependencies, dict):
            fail("Packages/packages-lock.json must contain dependencies")
        else:
            for package_name, expected_version in EXPECTED_PACKAGE_VERSIONS.items():
                package_record = dependencies.get(package_name)
                if not isinstance(package_record, dict) or package_record.get("version") != expected_version:
                    fail(f"Packages/packages-lock.json does not resolve {package_name}@{expected_version}")

    unity = source.get("unityEditor")
    if not isinstance(unity, dict):
        fail("source.unityEditor must be an object")
    else:
        if unity.get("version") != EXPECTED_UNITY_VERSION:
            fail(f"source.unityEditor.version must be {EXPECTED_UNITY_VERSION}")
        if unity.get("revision") != EXPECTED_UNITY_REVISION:
            fail(f"source.unityEditor.revision must be {EXPECTED_UNITY_REVISION}")

    provenance = source.get("provenanceManifest")
    if not isinstance(provenance, dict):
        fail("source.provenanceManifest must be an object")
    else:
        provenance_path = require_string(provenance, "path", "source.provenanceManifest.path")
        provenance_hash = require_string(
            provenance,
            "sha256",
            "source.provenanceManifest.sha256",
        )
        if provenance_path is not None and provenance_hash is not None:
            path = repo_file(provenance_path)
            if path is not None and (
                not HEX_64.fullmatch(provenance_hash)
                or sha256_file(path) != provenance_hash.upper()
            ):
                fail("source.provenanceManifest.sha256 does not match the tracked manifest")
        if provenance.get("status") != "PASS":
            fail("source.provenanceManifest.status must be PASS")


def validate_artifacts(manifest: dict[str, Any]) -> list[dict[str, Any]]:
    build = manifest.get("build")
    if not isinstance(build, dict):
        fail("Build identity must include a build object")
        return []

    if build.get("target") != "WebGL":
        fail("build.target must be WebGL")
    compression = build.get("compression")
    if not isinstance(compression, dict):
        fail("build.compression must be an object")
    else:
        if compression.get("format") != "Brotli":
            fail("build.compression.format must be Brotli")
        if compression.get("decompressionFallback") is not False:
            fail("build.compression.decompressionFallback must be false")
    if build.get("threading") != "single":
        fail("build.threading must be single")
    if build.get("outputPrefix") != "signal-garden/<version>/Build/":
        fail("build.outputPrefix must preserve the immutable R2 Build prefix contract")
    if build.get("assetBasePattern") != "<assetBase>/Build/<filename>":
        fail("build.assetBasePattern must be <assetBase>/Build/<filename>")

    total = build.get("totalBytes")
    budget = build.get("compressedBudgetBytes")
    if type(total) is not int or total <= 0:
        fail("build.totalBytes must be a positive integer")
        total = 0
    if type(budget) is not int or budget <= 0:
        fail("build.compressedBudgetBytes must be a positive integer")
        budget = 0

    artifacts = build.get("artifacts")
    if not isinstance(artifacts, list):
        fail("build.artifacts must be an array")
        return []
    by_role: dict[str, dict[str, Any]] = {}
    for artifact in artifacts:
        if not isinstance(artifact, dict):
            fail("Every build artifact record must be an object")
            continue
        role = require_string(artifact, "role", "build artifact role")
        if role is None:
            continue
        if role in by_role:
            fail(f"Duplicate build artifact role: {role}")
        by_role[role] = artifact

        filename = require_string(artifact, "filename", f"artifact {role}.filename")
        relative_path = require_string(artifact, "relativePath", f"artifact {role}.relativePath")
        sha = require_string(artifact, "sha256", f"artifact {role}.sha256")
        byte_count = artifact.get("bytes")
        content_type = require_string(artifact, "contentType", f"artifact {role}.contentType")
        content_encoding = artifact.get("contentEncoding")
        if filename is not None and relative_path is not None:
            if relative_path != f"Build/{filename}":
                fail(f"artifact {role}.relativePath must be Build/{{filename}}")
            if filename.endswith(".br") and content_encoding != "br":
                fail(f"artifact {role} must record Content-Encoding br")
            if not filename.endswith(".br") and content_encoding is not None:
                fail(f"artifact {role} must not record Content-Encoding")
        if sha is not None and not HEX_64.fullmatch(sha):
            fail(f"artifact {role}.sha256 must be a SHA-256")
        if type(byte_count) is not int or byte_count <= 0:
            fail(f"artifact {role}.bytes must be a positive integer")
        if content_type is not None:
            expected_type = {
                "loader": "text/javascript; charset=utf-8",
                "data": "application/octet-stream",
                "framework": "text/javascript; charset=utf-8",
                "wasm": "application/wasm",
            }.get(role)
            if expected_type is not None and content_type != expected_type:
                fail(f"artifact {role}.contentType must be {expected_type}")

    if tuple(by_role) != EXPECTED_ARTIFACT_ROLES:
        # Preserve a deterministic error even if a malicious manifest changes
        # array ordering.  The set is checked separately for useful detail.
        if set(by_role) != set(EXPECTED_ARTIFACT_ROLES):
            fail(
                "build.artifacts must contain exactly the roles "
                + ", ".join(EXPECTED_ARTIFACT_ROLES)
            )
    if set(by_role) == set(EXPECTED_ARTIFACT_ROLES):
        expected_filenames = {
            "loader": "WebGL.loader.js",
            "data": "WebGL.data.br",
            "framework": "WebGL.framework.js.br",
            "wasm": "WebGL.wasm.br",
        }
        for role, expected in expected_filenames.items():
            if by_role[role].get("filename") != expected:
                fail(f"artifact {role}.filename must be {expected}")

    measured_total = sum(
        artifact.get("bytes", 0)
        for artifact in artifacts
        if isinstance(artifact, dict) and type(artifact.get("bytes")) is int
    )
    if total and measured_total != total:
        fail(f"build.totalBytes {total} does not equal artifact total {measured_total}")
    if total and budget and total > budget:
        fail(f"build.totalBytes exceeds compressed budget ({total} > {budget})")
    if build.get("status") not in {"PASS_LOCAL", "PASS_RECORDED"}:
        fail("build.status must classify the recorded local build as PASS_LOCAL or PASS_RECORDED")
    return artifacts


def validate_verification(manifest: dict[str, Any]) -> None:
    verification = manifest.get("verification")
    if not isinstance(verification, dict):
        fail("Build identity must include a verification object")
        return
    required = (
        "provenance",
        "unityEditMode",
        "unityPlayMode",
        "webglBuild",
        "nestedBrowserPreview",
        "hostedCredentialFree",
        "cleanCheckout",
    )
    for key in required:
        record = verification.get(key)
        if not isinstance(record, dict):
            fail(f"verification.{key} must be an object")
            continue
        require_status(record, "status", f"verification.{key}.status")
        if not isinstance(record.get("evidence"), str) or not record["evidence"]:
            fail(f"verification.{key}.evidence must identify its evidence record")

    release_gates = manifest.get("releaseGates")
    if not isinstance(release_gates, dict):
        fail("Build identity must include releaseGates classifications")
        return
    for key, record in release_gates.items():
        if not isinstance(record, dict):
            fail(f"releaseGates.{key} must be an object")
            continue
        require_status(record, "status", f"releaseGates.{key}.status")
        if not isinstance(record.get("reason"), str) or not record["reason"]:
            fail(f"releaseGates.{key}.reason must explain its current classification")


def verify_build_files(artifacts: Iterable[dict[str, Any]], build_dir: Path | None) -> bool:
    if build_dir is None:
        return False
    build_dir = build_dir.resolve()
    if not build_dir.is_dir():
        fail(f"Build directory does not exist: {build_dir}")
        return False
    all_match = True
    for artifact in artifacts:
        filename = artifact.get("filename")
        if not isinstance(filename, str):
            continue
        path = build_dir / filename
        if not path.is_file():
            fail(f"Generated WebGL artifact is missing: {path}")
            all_match = False
            continue
        actual_bytes = path.stat().st_size
        actual_sha = sha256_file(path)
        if actual_bytes != artifact.get("bytes"):
            fail(f"Generated {filename} size does not match build identity")
            all_match = False
        if actual_sha != str(artifact.get("sha256", "")).upper():
            fail(f"Generated {filename} SHA-256 does not match build identity")
            all_match = False
    return all_match


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--build-dir",
        type=Path,
        help="Verify generated WebGL artifacts under this Build directory",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    manifest = read_json(MANIFEST_RELATIVE_PATH)
    artifacts: list[dict[str, Any]] = []
    if manifest is not None:
        if manifest.get("schemaVersion") != 1:
            fail("SG-10 build identity schemaVersion must be 1")
        if manifest.get("game") != EXPECTED_GAME:
            fail("SG-10 build identity game must be signal-garden")
        if manifest.get("issue") != EXPECTED_ISSUE:
            fail("SG-10 build identity issue must be GAME-288")
        validate_source(manifest)
        artifacts = validate_artifacts(manifest)
        validate_verification(manifest)
        verify_build_files(artifacts, args.build_dir)

    if errors:
        print("SG-10 release evidence validation failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1

    if args.build_dir is None:
        print(
            "PASS: SG-10 build identity, artifact hashes, budgets, provenance, "
            "and classifications; generated artifact verification NOT_RUN (no --build-dir)"
        )
    else:
        print("PASS: SG-10 build identity and generated WebGL artifact hashes match")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
