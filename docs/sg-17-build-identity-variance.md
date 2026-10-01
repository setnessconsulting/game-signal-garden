# SG-17 — WebGL build-identity variance is caused by Unity's per-build `build-guid`

Issue: [GAME-287 / SG-09](https://setnessconsulting.atlassian.net/browse/GAME-287),
[GAME-315 / Wave 2](https://setnessconsulting.atlassian.net/browse/GAME-315)

Status: **finding explained**. The recorded release identity is unchanged and remains
authoritative for the promoted release. This record does not approve a promotion.

## Why this matters

Several earlier sessions recorded a fresh local WebGL build whose `data`/`wasm`
hashes did not match the recorded production identity, and treated the mismatch as
possible evidence of a render or source regression. That reading is wrong, and it
should stop being repeated.

## Root cause

`WebGL.data.br` is **not byte-reproducible**, because Unity writes a random
`build-guid` into the data archive on every build. Everything else in the archive is
deterministic.

Proof, on 2026-10-01, at reviewed main `2cf7d1a41c635147d8674edf151a760a2ab7996c`
with Unity `6000.6.0f1 (f7f8ed4d1e24)`:

1. `git diff --name-only da74c6a 2cf7d1a -- Assets/ Packages/ ProjectSettings/ SourceArt/`
   is **empty**. No Unity input changed between the readability source commit and
   reviewed main, so the two commits cannot legitimately produce different bytes.
2. Two consecutive `BuildWebGL` runs from that identical tree produced different
   `WebGL.data.br` files: `3,594,208` bytes (`7A63A889…`) and `3,593,674` bytes
   (`D434AF5F…`).
3. `WebGL.loader.js`, `WebGL.framework.js.br`, and `WebGL.wasm.br` were **byte-identical
   across both builds**.
4. Both data archives decompress to exactly `11,085,097` bytes and differ in exactly
   **28 bytes**, all inside one 32-character field:

   ```text
   build-guid=57499ef3267741e691dc35d70272bd30   (build A)
   build-guid=2240423ce6ae409d9fc36b3a10cc1095   (build B)
   ```

## Reproduce

```text
# Build twice from the same tree, then compare the two data archives.
python3 scripts/ci/compare_webgl_build_identity.py <buildA>/WebGL.data.br <buildB>/WebGL.data.br
```

The tool normalizes `build-guid` before comparing. It prints `PASS` when the
decompressed archives are otherwise identical and `FAIL` when there is a real
content change. It requires a Brotli decoder; without one it reports `NOT_RUN` and
exits 0 rather than guessing. It is a local diagnostic and is deliberately not part
of the credential-free CI lane, which installs no Brotli dependency and builds no
artifacts.

## Consequence for the release gate

`scripts/ci/validate_release_evidence.py --build-dir` compares the compressed
`WebGL.data.br` byte-for-byte against `docs/sg-10-build-identity.json`. That check
**cannot pass on any rebuild**, by Unity's design, and its failure is not evidence of
a source or render change. Treat a `data`/`wasm` mismatch as follows:

* Loader, framework, and wasm mismatches remain meaningful and are not excused.
* A `data` mismatch is expected on a fresh build. Confirm it with
  `compare_webgl_build_identity.py` before investigating further.

## What was deliberately not changed

`docs/sg-10-build-identity.json` was **not** edited. Its hash is bound into the
SG-12 closeout state machine (`validate_closeout_evidence.py` pins
`runtimeBuildIdentitySha256`), so changing it is an owner-gated release-identity
reconciliation belonging to SG-12 / GAME-290, not to SG-09. Recording a normalized
hash in the manifest, or relaxing the validator, is proposed there and is not taken
here.
