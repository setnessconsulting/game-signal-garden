# Credential-free validation

The `Credential-free validation` GitHub Actions workflow runs on pull requests to `main`, pushes to `main`, and manual dispatch. It checks changed-line whitespace, the tracked Unity project identity, required SG-05 source and runtime files, source/FBX hashes and recorded size budgets, Unity GUID references, the receiver prefab reference in the scene, generated-build hygiene, the clean-checkout contract, and the SG-10 build identity manifest.

The workflow does **not** run Unity or human sessions. Unity editor compilation, EditMode and PlayMode tests, the WebGL build, browser preview, runtime performance checks, the SG-11 fresh-player benchmark, and the SG-12 owner closeout are classified as **NOT_RUN** or **PENDING** in the hosted job. Their local results remain recorded in [SG-09 build evidence](sg-09-build-evidence.md), the [SG-11 benchmark record](sg-11-human-benchmark.md), and the [SG-12 closeout record](sg-12-closeout-record.md); the owner procedure is in [the SG-12 runbook](sg-12-owner-qualification-runbook.md). A green repository-integrity or evidence-manifest check must not be read as a Unity, WebGL, human-playtest, or release-approval pass.

GAME-288 / SG-10 adds the durable clean-checkout record and explicit Unity/build identity reporting in [the SG-10 evidence record](sg-10-ci-and-release-evidence.md). No Unity license or other repository secret is needed by this workflow.

GAME-289 / SG-11 adds the anonymous playtest schema and fail-closed evidence
validator. An empty `NOT_RUN` record is structurally valid while sessions await
owner-approved participants; the validator rejects a claimed pass without all
five threshold-checked sessions and dispositioned findings.

GAME-290 / SG-12 adds the cross-record closeout validator. It verifies the
recorded runtime source/build identity, child-story evidence links, technology
decisions, known limitations, Jira boundary, and `NOT_READY` promotion gate.
