# SG-08 feedback, audio, and game-feel record

**Issue:** [GAME-286 / SG-08](https://setnessconsulting.atlassian.net/browse/GAME-286)
**Base:** `214b5cf4c0e4148ef9e5dd8db1586afca119ae12` (SG-07 merge commit)
**Runtime:** Unity 6000.6.0f1, Unity WebGL, one `SignalGarden` scene
**Status:** implementation and local functional verification complete. Foreground 60 fps qualification remains open; Jira status unchanged.

## Bounded change

SG-08 adds concise visual and audio feedback to the existing observe → route → verify loop. The source, receiver, trail, route rules, input, run-state phases, pause, replay, and one-scene scope remain unchanged.

| Event | Visual and text response | Optional sound cue |
| --- | --- | --- |
| Begin tracing | Gold route line appears; coral source gives one short pulse; status text explains the active gesture. | Soft rising interaction tone. |
| Invalid or interrupted route | The attempted trace remains visible with the recovery line material; phase label says `RECOVERY / TRY AGAIN`; status text names the error and the retry action; coral source gives one short pulse. | Brief descending recovery tone. |
| Verified route | The route switches to its success material; the receiver brightens and gives one restrained pulse; phase and completion overlay say the signal is verified. | Short two-note ascending success cue. |

Text and shape/state labels carry the meaning independently from hue and sound. The recovery trace stays on screen as evidence of the attempt; it does not cover the garden. Effects are short and confined to the endpoint lights.

## Audio behavior and provenance

- Music and third-party sound libraries are excluded.
- Waveforms are synthesized at runtime by the original `SignalGardenGame.PlayFeedback` implementation. No external recordings, samples, sound files, or generated audio assets are loaded.
- Source and license provenance: original Setness Consulting implementation in this repository; no third-party audio license applies.
- Cues default to muted. The HUD offers a separate mute toggle and a keyboard-focusable volume slider from 0–100%, initialized to 55%. Setting volume to 0 silences cues while leaving the mute state independent.
- If the optional `AudioSource`, clip creation, or playback is unavailable, the audio call is skipped or caught after the deterministic run-state transition. Route recovery and completion continue through their normal visual and text path.
- Audio is never needed to infer the objective, diagnose an invalid route, or complete the challenge.

## Motion and accessibility

- With reduced motion enabled, ambient and one-shot light pulses stop and the verified receiver does not rotate. The route material, phase label, recovery/status text, and verified completion overlay remain visible.
- The volume slider is part of the HUD's explicit Tab focus order. Its live label includes the current percentage and is announced by the existing WebGL accessible-status bridge when focused or adjusted.
- The muted/unmuted button label and volume percentage are textual. Recovery and verified states use words and phase labels alongside their color/material changes.
- The feedback motion is short and nonessential. No particle effect or full-screen flash is added.

## Acceptance evidence

| GAME-286 criterion | Implementation/evidence |
| --- | --- |
| Receiver and route states have clear visual feedback. | Route material changes on resolve; phase/status text distinguishes routing, recovery, and verification; source/receiver light feedback is localized. |
| Verification has restrained pulse/animation and a cue. | Receiver light gets a short pulse; existing slow verified rotation remains optional under reduced motion; synthesized success cue is muteable. |
| Invalid routes explain the event and recovery. | Existing failure-specific status messages remain; recovery phase label and visible failed trace accompany the retry instruction. |
| Mute and reduced motion work. | Sound defaults off, has independent mute and volume controls; reduced motion removes optional animation while retaining state text and route appearance. |
| Feedback works for color-blind and low-attention play. | Phase, status, mute, volume, and recovery use explicit words. Hue is supplemental. |
| Effects remain bounded and do not hide the puzzle. | Only endpoint light intensity and existing route material change; there is no camera shake, overlay flash, or new puzzle content. |
| Audio provenance and failure handling are recorded. | Original runtime synthesis; no external files or libraries; null source and playback errors do not gate state resolution. |

### Local checks

Unity `6000.6.0f1` compiled the project and passed 24/24 EditMode tests and 5/5 PlayMode tests. The PlayMode cases cover scene/HUD binding and focus, muted-by-default volume control, left/right keyboard volume adjustment, missing optional audio, and reduced-motion behavior.

The local WebGL build completed successfully with Brotli compression. Exact artifact names, SHA-256 values, MIME/compression headers, and the nested asset-base cache key are in [docs/local-build-evidence.md](local-build-evidence.md). The browser interaction and frame-rate observations are in [docs/sg-08-browser-review.md](sg-08-browser-review.md).

The nested preview rendered the source, winding trail, receiver, and HUD. Browser checks exercised pause/resume, mute, mouse and keyboard volume adjustment, reduced motion, blind-spur recovery, and retry. On the final build, Tab announced the volume slider's left/right arrow-key instructions; ArrowLeft and ArrowRight changed the value from 55% to 45% and back. The browser reported no runtime errors; it emitted the known optional URP FSR upscaling shader warning.

The final visible in-app browser run completed a 30-second sample at an exact 1920×1080 render: 74.6 fps minimum, 86.9 fps average, 30/30 seconds (`PASS`). This passes the local preview target for the captured run. Independent foreground desktop Chrome/Edge and production CDN qualification remain open; the earlier unpresented/background-clamped readings are retained as environment caveats. Human audio listening, screen-reader operation, focus-loss qualification, and fresh-player timing remain outside these local checks. Hosted repository integrity does not run Unity or WebGL checks; those lanes remain separate.
