#!/usr/bin/env python3
"""Explain WebGL build-identity differences for the Signal Garden release lane.

Unity writes a random ``build-guid`` into the WebGL data archive on every
build, so ``WebGL.data.br`` is not byte-reproducible even when every Unity
input is unchanged. This tool decompresses two data archives and reports
whether their differences are confined to that field.

It is a local diagnostic, not a release gate: it needs a Brotli decoder and
two builds, so it is intentionally not part of the credential-free CI lane.
Without a Brotli decoder it reports NOT_RUN and exits 0.

Usage:
    python3 scripts/ci/compare_webgl_build_identity.py A.br B.br
    python3 scripts/ci/compare_webgl_build_identity.py --build-dir Builds/WebGL/Build \
        --recorded-sha256 27AD4E... --recorded-bytes 3593229
"""

from __future__ import annotations

import argparse
import hashlib
import re
import sys
from pathlib import Path
from typing import Any

BUILD_GUID_PATTERN = re.compile(rb"build-guid=([0-9a-f]{32})")
# Unity writes other per-build fields; these are recorded here so a future
# difference can be triaged without re-deriving the offset by hand.
KNOWN_VOLATILE_KEYS = (b"build-guid",)


def fail(message: str) -> None:
    print(f"ERROR: {message}", file=sys.stderr)
    raise SystemExit(1)


def load_brotli() -> Any:
    try:
        import brotli  # type: ignore[import-not-found]

        return brotli
    except ImportError:
        return None


def read_build_guid(payload: bytes) -> bytes | None:
    match = BUILD_GUID_PATTERN.search(payload)
    return match.group(1) if match else None


def normalize(payload: bytes) -> bytes:
    """Replace the per-build GUID with a fixed placeholder for comparison."""
    return BUILD_GUID_PATTERN.sub(b"build-guid=" + b"0" * 32, payload)


def describe_difference(left: bytes, right: bytes) -> dict[str, Any]:
    differing = [index for index in range(min(len(left), len(right))) if left[index] != right[index]]
    runs: list[tuple[int, int]] = []
    for index in differing:
        if runs and index == runs[-1][1] + 1:
            runs[-1] = (runs[-1][0], index)
        else:
            runs.append((index, index))
    return {
        "decompressedBytesLeft": len(left),
        "decompressedBytesRight": len(right),
        "sameLength": len(left) == len(right),
        "differingByteCount": len(differing),
        "contiguousRunCount": len(runs),
        "firstRunOffset": runs[0][0] if runs else None,
        "lastRunOffset": runs[-1][0] if runs else None,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("left", nargs="?", type=Path, help="First WebGL.data.br")
    parser.add_argument("right", nargs="?", type=Path, help="Second WebGL.data.br")
    parser.add_argument(
        "--build-dir",
        type=Path,
        help="Build directory holding WebGL.data.br to compare against the recorded identity",
    )
    parser.add_argument("--recorded-sha256", help="SHA-256 recorded in the SG-10 build identity")
    parser.add_argument("--recorded-bytes", type=int, help="Byte count recorded in the SG-10 build identity")
    args = parser.parse_args()

    brotli = load_brotli()
    if brotli is None:
        print(
            "NOT_RUN: no Brotli decoder available (pip install brotli, or add the vendored "
            "wheel directory to PYTHONPATH). Nothing was compared."
        )
        return 0

    left_path = args.left
    right_path = args.right
    if args.build_dir is not None:
        candidate = args.build_dir / "WebGL.data.br"
        if not candidate.is_file():
            fail(f"WebGL.data.br not found under {args.build_dir}")
        if right_path is None:
            fail("pass a second archive to compare against")
        right_path = candidate

    if left_path is None or right_path is None:
        fail("provide two WebGL.data.br archives, or --build-dir plus a comparison archive")

    left_raw = left_path.read_bytes()
    right_raw = right_path.read_bytes()
    left = brotli.decompress(left_raw)
    right = brotli.decompress(right_raw)

    left_guid = read_build_guid(left)
    right_guid = read_build_guid(right)
    left_normalized = normalize(left)
    right_normalized = normalize(right)
    normalized_match = left_normalized == right_normalized

    print(f"left:  {left_path} ({len(left_raw)} compressed -> {len(left)} decompressed bytes)")
    print(f"       sha256(compressed)   = {hashlib.sha256(left_raw).hexdigest().upper()}")
    print(f"       build-guid           = {left_guid.decode() if left_guid else 'ABSENT'}")
    print(f"right: {right_path} ({len(right_raw)} compressed -> {len(right)} decompressed bytes)")
    print(f"       sha256(compressed)   = {hashlib.sha256(right_raw).hexdigest().upper()}")
    print(f"       build-guid           = {right_guid.decode() if right_guid else 'ABSENT'}")

    if args.recorded_sha256 or args.recorded_bytes is not None:
        print("recorded identity (SG-10):")
        if args.recorded_sha256:
            print(f"       sha256(compressed)   = {args.recorded_sha256.upper()}")
        if args.recorded_bytes is not None:
            print(f"       bytes                 = {args.recorded_bytes}")

    print("decompressed difference:", describe_difference(left, right))

    if normalized_match:
        print(
            "PASS: the decompressed data archives are identical once build-guid is normalized. "
            "The compressed SHA-256 differs only because Unity randomizes build-guid per build."
        )
        return 0

    print(
        "FAIL: the decompressed data archives differ beyond build-guid. This is a real content "
        "change, not Unity build non-determinism."
    )
    for key in KNOWN_VOLATILE_KEYS:
        print(f"       volatile key checked: {key.decode()}")
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
