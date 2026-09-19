#!/usr/bin/env python3
"""Fail if a release validation checkout has tracked or untracked changes."""

from __future__ import annotations

import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
FORBIDDEN_PREFIXES = ("build/", "builds/", "artifacts/")


def run_git(*arguments: str) -> str:
    result = subprocess.run(
        ["git", *arguments],
        cwd=ROOT,
        check=True,
        capture_output=True,
        text=True,
    )
    return result.stdout


def main() -> int:
    try:
        status = run_git("status", "--porcelain=v1", "--untracked-files=all")
        tracked = run_git("ls-files", "-z").split("\0")
    except (OSError, subprocess.CalledProcessError) as exc:
        print(f"Clean-checkout validation could not inspect Git: {exc}", file=sys.stderr)
        return 1

    forbidden = [
        path
        for path in tracked
        if path and path.replace("\\", "/").casefold().startswith(FORBIDDEN_PREFIXES)
    ]
    if status or forbidden:
        print("Clean-checkout validation failed:", file=sys.stderr)
        if status:
            print(status, file=sys.stderr, end="")
        if forbidden:
            print("Generated output is tracked: " + ", ".join(forbidden[:5]), file=sys.stderr)
        return 1

    print("PASS: clean checkout with no tracked generated builds or untracked files")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
