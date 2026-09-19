# SG-09 qualification matrix

Issue: [GAME-287 / SG-09](https://setnessconsulting.atlassian.net/browse/GAME-287)

Status: in progress. This record distinguishes automated and local-browser evidence from owner approval and physical accessibility review. It is not a production release sign-off.

## Target and budgets

| Item | Qualification target | Approval / measurement status |
| --- | --- | --- |
| Runtime | Windows desktop, Chrome and Edge, Unity WebGL | Chrome/Edge versions and exact environment will be recorded with the run. |
| Display | 16:9 desktop layouts from 1280×720 through the 1920×1080 reference composition; exact reference render for performance sampling | Provisional range; every listed size must be checked for clipping and legibility. |
| Frame rate | 30 seconds of visible, focused samples at an exact 1920×1080 Unity canvas; arithmetic mean at least 60 fps (16.67 ms average frame interval). Use the harness's 5-second warmup and 30 one-second samples. | 60 fps target established by the GDD; actual result pending the SG-09 build. The one-second minimum is informational. |
| Local load | At most 10 seconds from the harness's `signalGardenLoadStartedAt` to `signalGardenInteractiveAt` at 1920×1080 | Existing GDD target; actual result pending the SG-09 build. This is local HTTP preview evidence, not CDN evidence. |
| Compressed WebGL artifacts | At most 12 MiB (12,582,912 bytes) for the exact shipped loader, data, framework, and WASM files combined | Proposed by the owner and explicitly pending owner sign-off. Record the measured total without calling this an approved gate. |
| Browser-tab working set | At most 512 MiB for the game tab | Proposed by the owner and explicitly pending owner sign-off. Do not infer tab working set from JavaScript heap or shared browser-process memory. |
| Console errors | Zero unexpected game/runtime errors; known optional URP FSR shader-stripped warning is allow-listed if it remains the only warning and the scene renders | Record exact browser console observations. |

The game targets a 60 fps application frame rate and has an existing visible-preview test. The harness pauses sampling when the page is hidden or unfocused; background/occluded measurements are not valid foreground performance evidence.

## Acceptance and evidence map

| GAME-287 criterion | Test procedure and evidence | Result |
| --- | --- | --- |
| Mouse/keyboard input and reset | In a fresh nested preview, drag from coral along the gold trail to blue; pan with WASD; press Esc during a route and while idle; use Replay. Confirm cursor stays visible and route recovery permits a new attempt. | PASS_LOCAL — nested preview and deterministic route coverage; desktop foreground qualification remains open |
| Keyboard access decision | Tab and Shift+Tab through reachable controls; activate buttons with Enter/Space; adjust the volume slider with arrow keys. Record focus order and accessible names. | PASS_RECORDED — SG-06/SG-09 local HUD review; independent screen-reader review remains open |
| Critical/serious accessible UI issues | Inspect reachable text, focus indication, keyboard operation, and text/shape state cues at each qualified resolution. External screen-reader operation is a separate manual gate and is not claimed from DOM or live-region inspection alone. | Pending review |
| Reduced motion and mute | Toggle each control in Observe and Verified; confirm the labels/status change, no route rule changes, and reduced motion stops pulsing/rotation while preserving readable success/recovery state. Sound starts muted. | PASS_LOCAL — SG-08 browser review and automated PlayMode coverage |
| Frame time | Open `/signal-garden/play/?sg-render=1920x1080&sg-stats=1`; keep the window foreground and visible through 5-second warmup and all 30 samples. Record the reported exact buffer, minimum and mean, build identity, and browser. | PASS_LOCAL — in-app sample 63.1 fps mean; new visible desktop Chrome sample 30/30 at exact 1920×1080, 101.9 fps mean and 27.6 fps informational minimum. Independent Edge remains open. |
| Local time to interactive | Measure from the local host's load start to the readable Ready state at the reference render. Keep cold/warm cache and production CDN classifications separate. | PASS_LOCAL_WARM — visible Chrome reload reached Ready in 681 ms with the existing candidate cached; cold and production CDN measurements remain open |
| Missing assets and invalid state | Run the SG-05 provenance validation and negative tests; run route/state EditMode coverage for invalid, partial, canceled, reset, and verified transitions. Confirm the release build fails closed when required provenance assets are absent. | PASS_RECORDED — SG-05 provenance suite and SG-09 EditMode coverage |
| Interrupted input and application pause | PlayMode-invoke Unity focus-loss and pause callbacks during a partial route; verify route points clear, Recovery is shown, and a valid retry succeeds. Physical window/tab focus interruption remains a browser check. | PASS_RECORDED — 8/8 PlayMode, including focus interruption and application pause; physical interruption remains open |
| Scene reload | Reload `SignalGarden` during an active partial route; verify fresh Observe state, empty route/counters, default mute/motion settings, and complete HUD binding. | PASS_RECORDED — 8/8 PlayMode includes scene reload |
| Evidence tied to source/build | Record full implementation commit SHA, Unity/package versions, browser and environment, exact commands, build cache identity, artifact names/sizes/SHA-256, and HTTP metadata. Link the evidence from GAME-287 without changing its status. | PASS_RECORDED — [SG-09 build evidence](sg-09-build-evidence.md) and [SG-10 build identity](sg-10-build-identity.json) |
| Five-session fresh-player benchmark | Run five uncoached first-time desktop sessions against the Mini Metro, Dorfromantik, and The Room rubric. Record objective-understanding time, first meaningful action, completion time, invalid routes, recovery, restart/quit success, perceived clarity, and dispositioned findings. | NOT_RUN — protocol and fail-closed anonymous evidence record are in [SG-11](sg-11-human-benchmark.md); owner/participant sessions remain required |

## Commands and local procedures

The Unity test and WebGL build commands, nested host harness, and resolution/performance query are documented in [the repository README](../README.md) and [host-preview instructions](../tests/host-preview/README.md). The exact SG-09 build hashes and HTTP results are in [the SG-09 build record](sg-09-build-evidence.md); the SG-10 machine-readable identity and clean-checkout checks are in [the SG-10 evidence record](sg-10-ci-and-release-evidence.md). The qualification run must use the host harness at `/signal-garden/play/`, which serves exact artifacts below the nested asset base. Generated builds remain ignored under `Builds/WebGL/`.

The objective-under-30-seconds and completion-under-one-minute targets are assessed with the existing [fresh-player playtest script](playtest-script.md) and the [SG-12 owner qualification runbook](sg-12-owner-qualification-runbook.md). They require human participants and are not inferred from automated route tests or Unity's deterministic state transitions.
