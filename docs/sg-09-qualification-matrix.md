# SG-09 qualification matrix

Issue: [GAME-287 / SG-09](https://setnessconsulting.atlassian.net/browse/GAME-287)

Status: in progress. This record distinguishes automated and local-browser evidence from owner approval and physical accessibility review. It is not a production release sign-off.

Latest run: 2026-10-01 against reviewed main `2cf7d1a41c635147d8674edf151a760a2ab7996c`, Unity `6000.6.0f1 (f7f8ed4d1e24)`. The machine-readable record is [sg-09-foreground-qualification.json](sg-09-foreground-qualification.json) and the browser driver is [tests/host-preview/qualify.mjs](../tests/host-preview/qualify.mjs).

## Target and budgets

| Item | Qualification target | Approval / measurement status |
| --- | --- | --- |
| Runtime | Windows desktop, Chrome and Edge, Unity WebGL | Chrome 153.0.8010.53 and Edge 154.0.4258.48 were driven as real foreground windows on 2026-10-01. |
| Display | 16:9 desktop layouts from 1280×720 through the 1920×1080 reference composition; exact reference render for performance sampling | Exact 1920×1080 canvas buffer confirmed in both browsers. The 1280×720 clipping and legibility sweep is still open. |
| Frame rate | 30 seconds of visible, focused samples at an exact 1920×1080 Unity canvas; arithmetic mean at least 60 fps (16.67 ms average frame interval). Use the harness's 5-second warmup and 30 one-second samples. | Chrome foreground: 30/30 samples, mean **123.99 fps**, one-second minimum 106.57. Edge foreground: the sampler could not hold continuous focus in the automated session, so the Edge gate is still `NOT_RUN`. |
| Local load | At most 10 seconds from the harness's `signalGardenLoadStartedAt` to `signalGardenInteractiveAt` at 1920×1080 | Cold-profile Chrome run reached Ready in **2,865 ms**. Cold/warm split and production CDN timing remain open. |
| Compressed WebGL artifacts | At most 12 MiB (12,582,912 bytes) for the exact shipped loader, data, framework, and WASM files combined | Fresh build measured **10,525,424 bytes**. Budget still explicitly pending owner sign-off. |
| Browser-tab working set | At most 512 MiB for the game tab | Proposed by the owner and explicitly pending owner sign-off. Not measured in this run. Do not infer tab working set from JavaScript heap or shared browser-process memory. |
| Console errors | Zero unexpected game/runtime errors; known optional URP FSR shader-stripped warning is allow-listed if it remains the only warning and the scene renders | Chrome foreground run: **0 game console errors, 0 failed game requests, 0 page errors.** Only `/favicon.ico` 404s, which is preview-host noise. |

The game targets a 60 fps application frame rate and has an existing visible-preview test. The harness pauses sampling when the page is hidden or unfocused; background/occluded measurements are not valid foreground performance evidence. The harness only advances a one-second sample while the page is active, so a completed 30/30 sample is itself evidence of at least 30 seconds of continuous visible, focused running.

## Acceptance and evidence map

| GAME-287 criterion | Test procedure and evidence | Result |
| --- | --- | --- |
| Mouse/keyboard input and reset | In a fresh nested preview, drag from coral along the gold trail to blue; pan with WASD; press Esc during a route and while idle; use Replay. Confirm cursor stays visible and route recovery permits a new attempt. | PASS_FOREGROUND_AUTOMATED — Chrome foreground run drove the real pointer path from coral to blue and reached `Signal received. The garden is awake.` Deterministic EditMode/PlayMode coverage remains in place. Physical WASD/Esc and Replay on a desktop remain an owner check. |
| Keyboard access decision | Tab and Shift+Tab through reachable controls; activate buttons with Enter/Space; adjust the volume slider with arrow keys. Record focus order and accessible names. | PARTIAL — `lang=en`, a `<main>` landmark, a `role=status` `aria-live=polite` region and a labelled focusable canvas are present. The only DOM-focusable elements are the canvas and the status region, because the HUD is drawn inside the canvas. |
| Critical/serious accessible UI issues | Inspect reachable text, focus indication, keyboard operation, and text/shape state cues at each qualified resolution. External screen-reader operation is a separate manual gate and is not claimed from DOM or live-region inspection alone. | OPEN FINDING — see the accessibility finding below. Not claimed as defect-free. |
| Reduced motion and mute | Toggle each control in Observe and Verified; confirm the labels/status change, no route rule changes, and reduced motion stops pulsing/rotation while preserving readable success/recovery state. Sound starts muted. | PASS_RECORDED — automated PlayMode coverage (`SoundStartsMutedAndTheVolumeControlChangesOnlyAudioPreference`, `ReducedMotionKeepsVerificationVisibleAndStopsReceiverMotion`, `SuccessReactionWakesFirefliesAndReducedMotionRestoresTheirMaterialState`, `KeyboardVolumeNavigationAdjustsInBothDirections`) and the SG-08 browser review. In-canvas toggling still needs an owner-visible check. |
| Frame time | Open `/signal-garden/play/?sg-render=1920x1080&sg-stats=1`; keep the window foreground and visible through 5-second warmup and all 30 samples. Record the reported exact buffer, minimum and mean, build identity, and browser. | CHROME PASS (123.99 mean, 106.57 minimum, 30/30). EDGE NOT_RUN — the Edge window did not maintain continuous focus under automation, measured as 820/820 unfocused polls. The Edge gate stays open for a human or a less intrusive run. |
| Local time to interactive | Measure from the local host's load start to the readable Ready state at the reference render. Keep cold/warm cache and production CDN classifications separate. | PASS_LOCAL_COLD — 2,865 ms on a fresh profile, within the 10 s target. Warm and production CDN measurements remain open. |
| Missing assets and invalid state | Run the SG-05 provenance validation and negative tests; run route/state EditMode coverage for invalid, partial, canceled, reset, and verified transitions. Confirm the release build fails closed when required provenance assets are absent. | PASS_RECORDED — SG-05 provenance suite, 97/97 EditMode, and 12/12 PlayMode including fail-closed provenance cases. |
| Interrupted input and application pause | PlayMode-invoke Unity focus-loss and pause callbacks during a partial route; verify route points clear, Recovery is shown, and a valid retry succeeds. Physical window/tab focus interruption remains a browser check. | PASS_RECORDED — PlayMode covers focus interruption and application pause. A real browser focus interruption was also observed during the Edge run: the game cancelled the partial route and reported `Focus changed, so the partial route was canceled. Start again at the coral source.` |
| Scene reload | Reload `SignalGarden` during an active partial route; verify fresh Observe state, empty route/counters, default mute/motion settings, and complete HUD binding. | PASS_RECORDED — PlayMode `ReloadDuringRoutingStartsACompletelyFreshSession`. |
| Evidence tied to source/build | Record full implementation commit SHA, Unity/package versions, browser and environment, exact commands, build cache identity, artifact names/sizes/SHA-256, and HTTP metadata. Link the evidence from GAME-287 without changing its status. | PASS_RECORDED — [sg-09-foreground-qualification.json](sg-09-foreground-qualification.json) and [SG-10 build identity](sg-10-build-identity.json). |
| Build identity reproducibility | Confirm whether a rebuild reproduces the recorded `data` artifact hash. | EXPLAINED — `WebGL.data.br` embeds a random per-build `build-guid`, so it is not byte-reproducible. Two builds of an identical tree differ in 28 bytes inside that field only. See [SG-17](sg-17-build-identity-variance.md). |
| Five-session fresh-player benchmark | Run five uncoached first-time desktop sessions against the Mini Metro, Dorfromantik, and The Room rubric. Record objective-understanding time, first meaningful action, completion time, invalid routes, recovery, restart/quit success, perceived clarity, and dispositioned findings. | NOT_RUN — protocol and fail-closed anonymous evidence record are in [SG-11](sg-11-human-benchmark.md); owner/participant sessions remain required |

## Accessibility finding (open)

The Unity HUD — sound, reduced motion, volume, and try again — is rendered inside the
WebGL canvas. It does not appear in the browser accessibility tree: the recorded run
found **0 DOM-reachable interactive controls** (no `button`, `slider`, or `link`
roles) and only two focusable elements, the labelled canvas and the `role=status`
live region.

What is already satisfied:

* Keyboard operation works inside Unity through the UGUI event system, covered by PlayMode.
* State changes are announced through the `role=status` `aria-live=polite` region, and the run confirmed the full status sequence `Ready.` → `Tracing the signal…` → `Signal received. The garden is awake.`
* The document exposes `lang=en` and a `<main>` landmark, and the canvas carries `tabindex="0"` with an accessible name.

What is not satisfied:

* A DOM-based screen reader (NVDA, JAWS) cannot reach or operate the in-canvas HUD controls.

This needs an **explicit owner accessibility decision** for v1, either accepting the
canvas-surface model with the keyboard-and-live-region mitigations as the documented
conformance target, or scheduling a DOM mirror of the HUD. It is deliberately not
recorded as "no critical or serious defect", and an external screen-reader session
remains a separate manual gate.

## Recovery and failure-state observation

A deliberate invalid route was driven in the browser along `RouteRules.DeadEndSpur`.
Releasing there produced an understandable failure trace and a specific retry
instruction:

```text
Tracing the signal. Stay on the gold stones and release at the blue receiver. Esc cancels.
The blind spur has no receiver. Follow the gold stones from coral to blue and try again.
```

This satisfies the Wave 2 requirement that an invalid release always enters Recovery
with a readable failure trace and a named retry action.

## Commands and local procedures

The Unity test and WebGL build commands, nested host harness, and resolution/performance query are documented in [the repository README](../README.md) and [host-preview instructions](../tests/host-preview/README.md). The exact SG-09 build hashes and HTTP results are in [the SG-09 build record](sg-09-build-evidence.md); the SG-10 machine-readable identity and clean-checkout checks are in [the SG-10 evidence record](sg-10-ci-and-release-evidence.md). The qualification run must use the host harness at `/signal-garden/play/`, which serves exact artifacts below the nested asset base. Generated builds remain ignored under `Builds/WebGL/`.

Foreground desktop runs are driven by `tests/host-preview/qualify.mjs`, which launches a real non-headless Chrome or Edge window on the exact reference render, waits for the page to become visible and focused, drives the real pointer path, and records readiness, load time, frame samples, WebGL state, console output, failed requests, and the accessibility tree. It records observations and never decides PASS/FAIL; the validators and owner qualification own those classifications. A run that cannot hold a genuinely visible and focused window is reported as not-run rather than as a failure.

The objective-under-30-seconds and completion-under-one-minute targets are assessed with the existing [fresh-player playtest script](playtest-script.md) and the [SG-12 owner qualification runbook](sg-12-owner-qualification-runbook.md). They require human participants and are not inferred from automated route tests or Unity's deterministic state transitions.

