# Signal Garden — SG-12 owner qualification runbook

**Issue:** [GAME-290 / SG-12](https://setnessconsulting.atlassian.net/browse/GAME-290)

This runbook records owner qualification for the current SG-10 readability candidate.
The candidate raises the WebGL render scale to the configured 0.8 ceiling and
increases HUD text sizes; the observation steps below do not modify gameplay,
scenes, assets, packages, or runtime code. Automated checks and agent-controlled
play do not replace the manual gates below.

## Candidate and environment

- Runtime source: `da74c6a544066a538e7f999f18972d2b6ebe33fb`
- Unity: `6000.6.0f1`
- Build identity: `docs/sg-10-build-identity.json`
- Browser target: desktop Chrome and Edge
- Reference render: exact `1920x1080`
- Route: `/signal-garden/play/`
- Local harness: `tests/host-preview/serve.mjs`
- Candidate artifact budget: 12 MiB compressed, pending owner approval
- Browser-tab working-set budget: 512 MiB, pending owner approval

Use a clean browser profile or a deliberately documented cache state. Record the
browser version, operating-system version, render size, candidate source commit,
date, and whether the observation is local or production. Do not record names,
ages, contact details, or other participant identity.

Build identity must be captured from a clean project checkout before running
Unity's PlayMode performance tests. The Unity test framework can create ignored
`Assets/Resources/PerformanceTestRunInfo.json` and
`PerformanceTestRunSettings.json` files; remove those generated files or use a
separate clean build checkout before hashing the four release artifacts.

## Browser performance and load

For each browser, open:

`/signal-garden/play/?sg-render=1920x1080&sg-stats=1`

Keep the window visible and focused through the five-second warmup and all 30
one-second samples. Record the exact canvas buffer, every reported mean and
informational minimum, sample count, and the harness result. The gate is an
arithmetic mean of at least 60 fps; a one-second minimum is diagnostic.

Measure time to interactive twice locally: once with a cold cache in a fresh
profile and once with a warm cache. Use the harness timestamps from load start
to the readable `Ready` state. Production CDN timing is a separate observation
after an approved immutable upload.

## Working set and focus recovery

Measure the game tab's process working set with the browser's operating-system
process view while the game is interactive and during a route attempt. Record
the measurement tool, baseline, peak, browser process/tab identification, and
the provisional 512 MiB comparison. Do not substitute JavaScript heap size or
shared browser-process memory for a tab working-set measurement.

Start a route, switch the real browser window or tab away, and return. Record
whether the partial route was canceled, whether the Recovery message was
readable, and whether a valid retry completed without a page reload. Also verify
Esc cancels an active route and pauses/resumes while idle.

## Screen-reader and keyboard review

With the candidate at `1920x1080`, use the owner's available external screen
reader and record its name/version. Traverse the Canvas controls with `Tab` and
`Shift+Tab`, activate buttons with `Enter` or `Space`, and adjust the volume
control with the arrow keys. Record accessible names, focus order, visible focus
indication, and announcements for objective, routing, recovery, verified,
paused, replay, mute, and reduced-motion states. Confirm that state meaning is
available through text and shape, without relying on color or sound.

## Five fresh-player sessions

Run exactly five uncoached sessions using `docs/playtest-script.md` and
`docs/sg-11-human-benchmark.md`. Use anonymous codes `P01` through `P05` only.
Record objective-understanding time, first meaningful action, completion time,
invalid routes, recovery, pause/focus behavior, replay or restart/quit result,
clarity score, and findings with severity, evidence, remediation, and residual
owner disposition.

The target is at least four of five players stating the objective within 30
seconds and completing within one minute after understanding the gesture. Every
session must recover from an invalid or interrupted route and successfully
restart or quit. Do not change these thresholds after observing the sessions.

## Evidence update and state transitions

Enter the observations in `docs/sg-12-release-candidate.json` under
`ownerQualification` and update the SG-11 evidence record for the five sessions.
Use `PASS`, `FAIL`, `BLOCKED`, or `NOT_RUN` for each gate, with a method,
classification, and reason. A failed or blocked gate remains visible; it is not
converted into a pass by prose.

Keep the manifest at `NOT_READY` while any required gate is `NOT_RUN`, `FAIL`,
or `BLOCKED`. It may move to `READY_FOR_OWNER_REVIEW` only when all required
evidence is complete and the findings have an explicit disposition. It may move
to `READY_FOR_PROMOTION` only after owner approval, Jira reconciliation, and a
separate production release review. `promotionAllowed` remains false until
that final state.

Run the repository, release, playtest, and closeout validators before opening a
PR. Keep generated WebGL output ignored and leave R2, the games-site catalog,
deployment, and Jira unchanged during the evidence PR.
