# Temporary Python 3.12 shadow probe — 2026-10-03

This documentation-only change requests the repository's existing `credential-free-validation.yml` workflow on an exact pull-request head for the `game-signal-garden-python312` shadow profile.

The workflow grants `contents: read` and runs one Ubuntu `repository-integrity` job. Its Python validators check repository and asset identity, clean checkout, release evidence, playtest evidence schema, closeout evidence, and the production-readback contract. The workflow does not install or license Unity, build WebGL, use repository secrets, or access live production services. Unity/WebGL, generated-artifact, browser, working-set, accessibility, human-benchmark, owner-evidence, and release-promotion lanes remain separate Actions boundaries.

This file has no runtime effect and does not change the workflow or its credentials. It is a temporary source probe and will be closed unmerged after exact-head Jenkins readback.
