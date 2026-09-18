# Credential-free validation

The `Credential-free validation` GitHub Actions workflow runs on pull requests to `main`, pushes to `main`, and manual dispatch. It checks changed-line whitespace, the tracked Unity project identity, required SG-05 source and runtime files, source/FBX hashes and recorded size budgets, Unity GUID references, the receiver prefab reference in the scene, and that generated build outputs remain untracked.

The workflow does **not** run Unity. Unity editor compilation, EditMode and PlayMode tests, the WebGL build, browser preview, and runtime performance checks are classified as **NOT_RUN** in the hosted job. Their local results remain recorded in [local build evidence](local-build-evidence.md); a green repository-integrity check must not be read as a Unity or WebGL pass.

This is the minimal credential-free CI prerequisite needed for SG-07 review. GAME-288 / SG-10 still owns the broader qualification work, including a durable clean-checkout record and explicit Unity/build identity reporting. No Unity license or other repository secret is needed by this workflow.
