#!/usr/bin/env python3
"""Credential-free checks for the tracked Signal Garden Unity project."""

from __future__ import annotations

import hashlib
import json
import re
import subprocess
import sys
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[2]
EXPECTED_UNITY_VERSION = "6000.6.0f1"
errors: list[str] = []


def fail(message: str) -> None:
    errors.append(message)


def repository_file(relative_path: str) -> Path | None:
    candidate = (ROOT / relative_path).resolve()
    try:
        candidate.relative_to(ROOT)
    except ValueError:
        fail(f"Manifest path escapes the repository: {relative_path}")
        return None
    if not candidate.is_file():
        fail(f"Required file is missing: {relative_path}")
        return None
    return candidate


def read_json(relative_path: str) -> dict[str, Any] | None:
    path = repository_file(relative_path)
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


def validate_unity_project() -> None:
    version_path = repository_file("ProjectSettings/ProjectVersion.txt")
    if version_path is not None:
        text = version_path.read_text(encoding="utf-8")
        match = re.search(r"^m_EditorVersion:\s*(\S+)\s*$", text, flags=re.MULTILINE)
        if match is None or match.group(1) != EXPECTED_UNITY_VERSION:
            fail(f"Project must target Unity {EXPECTED_UNITY_VERSION}")

    for relative_path in (
        "Assets/Scenes/SignalGarden.unity",
        "Packages/packages-lock.json",
        "Assets/Art/SignalGarden/SignalGardenReceiver.prefab",
    ):
        repository_file(relative_path)

    package_manifest = read_json("Packages/manifest.json")
    if package_manifest is not None and not isinstance(package_manifest.get("dependencies"), dict):
        fail("Packages/manifest.json must contain a dependencies object")


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest().upper()


def validate_recorded_file(
    record: dict[str, Any],
    *,
    label: str,
    max_bytes: int | None = None,
) -> Path | None:
    relative_path = record.get("relativePath")
    expected_hash = record.get("sha256")
    expected_length = record.get("byteLength")
    if not isinstance(relative_path, str):
        fail(f"{label} has no relativePath")
        return None
    path = repository_file(relative_path)
    if path is None:
        return None
    if (
        not isinstance(expected_hash, str)
        or re.fullmatch(r"[0-9a-fA-F]{64}", expected_hash) is None
        or sha256_file(path) != expected_hash.upper()
    ):
        fail(f"{label} SHA-256 does not match the provenance manifest")
    byte_length = path.stat().st_size
    if type(expected_length) is not int or byte_length != expected_length:
        fail(f"{label} byteLength does not match the provenance manifest")
    if max_bytes is not None and byte_length > max_bytes:
        fail(f"{label} exceeds its recorded byte budget ({byte_length} > {max_bytes})")
    return path


def read_meta_guid(relative_path: str) -> str | None:
    path = repository_file(relative_path)
    if path is None:
        return None
    text = path.read_text(encoding="utf-8")
    match = re.search(r"^guid:\s*([0-9a-fA-F]{32})\s*$", text, flags=re.MULTILINE)
    if match is None:
        fail(f"Unity metadata has no valid GUID: {relative_path}")
        return None
    return match.group(1).lower()


def validate_provenance() -> None:
    manifest = read_json("SourceArt/Blender/SignalGardenReceiver/SignalGardenReceiver.provenance.json")
    if manifest is None:
        return

    source = manifest.get("source")
    exported = manifest.get("export")
    unity = manifest.get("unity")
    budgets = manifest.get("budgets")
    if not all(isinstance(value, dict) for value in (source, exported, unity, budgets)):
        fail("SG-05 provenance must include source, export, unity, and budgets objects")
        return

    if manifest.get("schemaVersion") != 1:
        fail("SG-05 provenance schemaVersion must be 1")

    expected_paths = (
        (source, "relativePath", "SourceArt/Blender/SignalGardenReceiver/SignalGardenReceiver.blend"),
        (exported, "relativePath", "Assets/Art/SignalGarden/SignalGardenReceiver.fbx"),
        (unity, "prefabRelativePath", "Assets/Art/SignalGarden/SignalGardenReceiver.prefab"),
        (unity, "sceneRelativePath", "Assets/Scenes/SignalGarden.unity"),
    )
    for record, key, expected in expected_paths:
        if record.get(key) != expected:
            fail(f"SG-05 provenance {key} must identify {expected}")

    max_source_bytes = budgets.get("maxSourceBytes")
    max_export_bytes = budgets.get("maxExportBytes")
    if type(max_source_bytes) is not int or max_source_bytes <= 0:
        fail("SG-05 provenance maxSourceBytes must be a positive integer")
        max_source_bytes = -1
    if type(max_export_bytes) is not int or max_export_bytes <= 0:
        fail("SG-05 provenance maxExportBytes must be a positive integer")
        max_export_bytes = -1

    validate_recorded_file(source, label="Blender source", max_bytes=max_source_bytes)
    fbx_path = validate_recorded_file(
        exported,
        label="Unity FBX export",
        max_bytes=max_export_bytes,
    )

    for measured_key, budget_key in (
        ("measuredTriangles", "maxTriangles"),
        ("measuredMaterialSlots", "maxMaterialSlots"),
        ("measuredMaxTextureDimension", "maxTextureDimension"),
    ):
        measured = budgets.get(measured_key)
        limit = budgets.get(budget_key)
        if type(measured) not in (int, float) or type(limit) not in (int, float):
            fail(f"SG-05 provenance is missing numeric budget fields {measured_key}/{budget_key}")
        elif measured > limit:
            fail(f"SG-05 recorded {measured_key} exceeds {budget_key}")

    fbx_meta = unity.get("fbxMetaRelativePath")
    fbx_guid = unity.get("fbxGuid")
    prefab_path = unity.get("prefabRelativePath")
    prefab_guid = unity.get("prefabGuid")
    scene_path = unity.get("sceneRelativePath")
    if not all(isinstance(value, str) for value in (fbx_meta, fbx_guid, prefab_path, prefab_guid, scene_path)):
        fail("SG-05 provenance is missing Unity metadata, prefab, or scene identity")
        return

    if fbx_path is not None and read_meta_guid(fbx_meta) != fbx_guid.lower():
        fail("FBX metadata GUID does not match the SG-05 provenance manifest")

    prefab_meta = read_meta_guid(f"{prefab_path}.meta")
    if prefab_meta is not None and prefab_meta != prefab_guid.lower():
        fail("Prefab metadata GUID does not match the SG-05 provenance manifest")

    scene = repository_file(scene_path)
    if scene is not None and prefab_guid.lower() not in scene.read_text(encoding="utf-8").lower():
        fail("SignalGarden scene does not reference the prefab GUID from the provenance manifest")


def validate_build_hygiene() -> None:
    ignore_path = repository_file(".gitignore")
    if ignore_path is not None:
        ignored = {line.strip() for line in ignore_path.read_text(encoding="utf-8").splitlines()}
        for required_pattern in ("/Build/", "/Builds/", "/Artifacts/"):
            if required_pattern not in ignored:
                fail(f".gitignore must keep generated output out of Git with {required_pattern}")

    try:
        result = subprocess.run(
            ["git", "ls-files", "-z"],
            cwd=ROOT,
            check=True,
            capture_output=True,
        )
    except (OSError, subprocess.CalledProcessError) as exc:
        fail(f"Could not inspect tracked files for generated builds: {exc}")
        return

    tracked = result.stdout.decode("utf-8", errors="replace").split("\0")
    forbidden = [
        path
        for path in tracked
        if path and path.replace("\\", "/").casefold().startswith(("build/", "builds/", "artifacts/"))
    ]
    if forbidden:
        fail("Generated build/output files must not be tracked: " + ", ".join(forbidden[:5]))


def main() -> int:
    validate_unity_project()
    validate_provenance()
    validate_build_hygiene()

    if errors:
        print("Repository validation failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1

    print("PASS: project identity, SG-05 file hashes/GUIDs/budgets, scene link, and build hygiene")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
