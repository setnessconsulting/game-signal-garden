# SG-10 CI and release evidence

Issue: [GAME-288 / SG-10](https://setnessconsulting.atlassian.net/browse/GAME-288)

Status: implementation complete for the credential-free evidence lane. This
record does not promote the game to playable production and does not replace
the owner-gated desktop, accessibility, or human qualification work.

The original SG-10 candidate, the SG-13 WebGL compatibility replacement, and
the SG-14 visual polish replacement are retained as historical evidence. The
current runtime identity is the bounded SG-15 terrain and trail refinement;
see [the SG-15 visual audit](sg-15-visual-audit.md). It preserves gameplay and
resets owner qualification because the runtime artifact changed.

## What is checked in

- `docs/sg-10-build-identity.json` is the machine-readable build identity.
  It records the Unity input revision and tree hash, Unity/package versions,
  SG-05 provenance identity, exact WebGL filenames, sizes, SHA-256 values,
  Brotli and MIME metadata, the immutable R2 prefix pattern, and the proposed
  compressed-build budget.
- `scripts/ci/validate_release_evidence.py` validates that record in a clean
  checkout. With `--build-dir <path>` it also hashes the generated loader,
  data, framework, and WASM files. Without that option it explicitly reports
  generated-artifact verification as `NOT_RUN`.
- `scripts/ci/check_clean_checkout.py` fails on any tracked or untracked
  checkout change and rejects tracked `Build/`, `Builds/`, or `Artifacts/`
  output.
- The credential-free Actions workflow runs both validators and writes a
  summary that keeps Unity compilation, Unity tests, WebGL generation, and
  browser qualification classified as `NOT_RUN` in hosted CI.

The current recorded local candidate is a fresh SG-10 build from runtime source
revision `1b3586f26f0c81410cbbbe33fc908e2dee2f1079`. The ordered four-file
artifact total is `10,468,381` bytes; Unity reported a `10,488,890` byte total
including its surrounding output. The manifest verifies the tracked Unity
input tree hash and the generated files were checked locally with
`--build-dir`, while keeping those generated files ignored.

## Local execution record

Unity `6000.6.0f1` with URP `17.6.0`, Input System `1.19.0`, UGUI `2.6.0`, and
Unity Test Framework `1.8.0` was run from the SG-10 worktree. EditMode passed
`24/24` and PlayMode passed `8/8`. The WebGL build completed with `Build
Finished, Result: Success`; its ordered four artifacts totalled `10,468,381`
bytes and the Unity build report total was `10,488,890` bytes. The release
validator passed both metadata-only mode and `--build-dir Builds/WebGL/Build`.

The current visual smoke result is recorded in [SG-15](sg-15-visual-audit.md)
and does not claim a new desktop browser or production CDN run. Foreground
Chrome and Edge performance, time to interactive, and all owner gates remain
open.

## Clean-checkout evidence

The release evidence lane was checked from a clean checkout of merged `main`
`34122315f6f31718ffa616517495392be5c91a2b` before the SG-10 files were added.
`git status --porcelain=v1 --untracked-files=all` was empty, the tracked-file
scan found no generated build output, and the existing repository validator
passed. The same checks are rerun by Actions for every pull request and push.
The repeatable command is:

```text
python3 scripts/ci/check_clean_checkout.py
python3 scripts/ci/validate_repository.py
python3 scripts/ci/validate_release_evidence.py
```

The SG-10 pull request's hosted **Repository integrity** check passed in
[Actions run 35412361707](https://github.com/setnessconsulting/game-signal-garden/actions/runs/35412361707)
(job `105814335542`). This is a credential-free repository/evidence pass; the
workflow summary still classifies Unity, generated WebGL, and browser lanes as
`NOT_RUN`.

## Classification boundary

| Lane | Classification | Evidence |
| --- | --- | --- |
| Repository structure, SG-05 provenance, build identity, and build hygiene | `PASS` | Credential-free Actions plus the two validators |
| Unity editor compilation and EditMode/PlayMode execution in hosted CI | `NOT_RUN` | No Unity license or editor is installed in the hosted job |
| WebGL generation in hosted CI | `NOT_RUN` | Local build identity is recorded; generated files remain ignored |
| Local Unity/WebGL/browser evidence | `PASS_LOCAL` / `PASS_RECORDED` | [SG-09 build evidence](sg-09-build-evidence.md) |
| Foreground Chrome and Edge qualification | `NOT_RUN` | Requires an independently observed desktop session |
| Browser-tab working set and time to interactive | `NOT_RUN` | Not measured by the clean-checkout lane |
| Physical focus loss and screen reader | `NOT_RUN` | Requires an independently observed desktop session |
| Five-session fresh-player benchmark | `NOT_RUN` | [SG-11 protocol and anonymous evidence record](sg-11-human-benchmark.md) are ready; owner/participant sessions remain open |

The 12 MiB compressed-build and 512 MiB browser-tab working-set limits remain
provisional and pending owner sign-off. No R2 object was uploaded, the
games-site catalog was not changed, no deployment was made, and Jira status was
not modified.
