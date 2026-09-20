#!/usr/bin/env python3
"""Focused tests for the recorded SG-12 production readback contract."""

from __future__ import annotations

import copy
import json
import unittest
from pathlib import Path

import validate_closeout_evidence as validator


ROOT = Path(__file__).resolve().parents[2]


def load_fixtures() -> tuple[dict, dict]:
    manifest = json.loads((ROOT / "docs/sg-12-release-candidate.json").read_text(encoding="utf-8"))
    sg10 = json.loads((ROOT / "docs/sg-10-build-identity.json").read_text(encoding="utf-8"))
    return manifest["productionDeployment"], sg10


class ProductionReadbackTests(unittest.TestCase):
    def setUp(self) -> None:
        validator.errors.clear()
        self.deployment, self.sg10 = load_fixtures()

    def tearDown(self) -> None:
        validator.errors.clear()

    def errors_for(self, deployment: object) -> list[str]:
        validator.validate_production_deployment(deployment, self.sg10)
        return list(validator.errors)

    def test_current_readback_passes(self) -> None:
        self.assertEqual(self.errors_for(self.deployment), [])

    def test_missing_readback_fails_closed(self) -> None:
        errors = self.errors_for(None)
        self.assertIn("productionDeployment must be an object", errors)

    def test_stale_site_commit_fails(self) -> None:
        deployment = copy.deepcopy(self.deployment)
        deployment["siteMergeCommit"] = "0" * 40
        errors = self.errors_for(deployment)
        self.assertTrue(any("siteMergeCommit" in error for error in errors))

    def test_mismatched_artifact_hash_fails(self) -> None:
        deployment = copy.deepcopy(self.deployment)
        deployment["artifacts"][0]["sha256"] = "0" * 64
        errors = self.errors_for(deployment)
        self.assertTrue(any("artifacts[0].sha256" in error for error in errors))

    def test_wrong_wasm_metadata_fails(self) -> None:
        deployment = copy.deepcopy(self.deployment)
        deployment["artifacts"][3]["httpContentType"] = "application/octet-stream"
        errors = self.errors_for(deployment)
        self.assertTrue(any("artifacts[3].httpContentType" in error for error in errors))


if __name__ == "__main__":
    unittest.main()
