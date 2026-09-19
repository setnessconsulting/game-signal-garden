# Credential-free validation

The `Credential-free validation` GitHub Actions workflow runs on pull requests to `main`, pushes to `main`, and manual dispatch. It checks changed-line whitespace, the tracked Unity project identity, required SG-05 source and runtime files, source/FBX hashes and recorded size budgets, Unity GUID references, the receiver prefab reference in the scene, generated-build hygiene, the clean-checkout contract, and the SG-10 build identity manifest.

The workflow does **not** run Unity. Unity editor compilation, EditMode and PlayMode tests, the WebGL build, browser preview, and runtime performance checks are classified as **NOT_RUN** in the hosted job. Their local results remain recorded in [SG-09 build evidence](sg-09-build-evidence.md); a green repository-integrity or evidence-manifest check must not be read as a Unity or WebGL pass.

GAME-288 / SG-10 adds the durable clean-checkout record and explicit Unity/build identity reporting in [the SG-10 evidence record](sg-10-ci-and-release-evidence.md). No Unity license or other repository secret is needed by this workflow.
