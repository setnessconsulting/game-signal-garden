# SG-10 CI and release evidence

Issue: [GAME-288 / SG-10](https://setnessconsulting.atlassian.net/browse/GAME-288)

Status: credential-free evidence lane complete; the readability build is deployed
for production testing. Deployment and automated browser smoke do not replace
the owner-gated desktop, accessibility, or human qualification work.

The original SG-10 candidate, the SG-13 WebGL compatibility replacement, the
SG-14 visual polish replacement, and the SG-15 terrain refinement are retained
as historical evidence. The current runtime identity is the readability
candidate from source `da74c6a544066a538e7f999f18972d2b6ebe33fb`; it preserves
the puzzle and controls while raising WebGL render scale and enlarging the HUD.
Formal owner qualification remains separate.

## What is checked in

- `docs/sg-10-build-identity.json` is the machine-readable build identity.
  It records the Unity input revision and tree hash, Unity/package versions,
  SG-05 provenance identity, exact WebGL filenames, sizes, SHA-256 values,
  Brotli and MIME metadata, the immutable R2 prefix pattern, and the proposed
  compressed-build budget.
- `scripts/ci/validate_release_evidence.py` validates that record in a clean
  checkout and hashes the tracked Unity inputs from the manifest's declared
  source.commit. This keeps later Editor-only test files from changing the
  identity of an older immutable build. With `--build-dir <path>` it also
  hashes the generated loader, data, framework, and WASM files. Without that
  option it explicitly reports generated-artifact verification as `NOT_RUN`.
- `scripts/ci/check_clean_checkout.py` fails on any tracked or untracked
  checkout change and rejects tracked `Build/`, `Builds/`, or `Artifacts/`
  output.
- The credential-free Actions workflow runs both validators and writes a
  summary that keeps Unity compilation, Unity tests, WebGL generation, and
  browser qualification classified as `NOT_RUN` in hosted CI.

The current recorded local candidate is the readability build from runtime
source revision `da74c6a544066a538e7f999f18972d2b6ebe33fb`. Its ordered
four-file artifact total is `10,508,368` bytes, below the 12 MiB budget. The
manifest verifies the tracked Unity input tree hash and the generated files
were checked locally with `--build-dir`, while keeping those generated files
ignored. The production readback is recorded in
[Signal Garden readability release](signal-garden-readability-release.md).

## Local execution record

Unity `6000.6.0f1` with URP `17.6.0`, Input System `1.19.0`, UGUI `2.6.0`, and
Unity Test Framework `1.8.0` was run from the SG-10 worktree. EditMode passed
`24/24` and PlayMode passed `12/12`. The WebGL build completed with `Build
Finished, Result: Success`; its ordered four artifacts totalled `10,506,795`
bytes and the Unity build report total was `10,527,304` bytes. The release
validator passed both metadata-only mode and `--build-dir Builds/WebGL/Build`.

The Wave 2 local nested preview completed the exact 1920×1080 30-sample check
at `61.0` mean FPS with a `60.3` one-second minimum, and the local route-smoke
harness covers `canvas -> Observe -> Verified`. These are local/in-app checks;
foreground Chrome and Edge performance, time to interactive, and all owner
gates remain open. The prior Wave 2 production readback is preserved in
[SG-16](sg-16-production-closeout.md); the current clarity deployment is
recorded in [Signal Garden readability release](signal-garden-readability-release.md).

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
provisional and pending owner sign-off. Wave 2 was published under the
immutable R2 prefix and promoted through the reviewed games-site catalog path;
Jira status and resolution were not modified.
