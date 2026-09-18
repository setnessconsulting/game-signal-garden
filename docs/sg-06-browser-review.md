# SG-06 browser review record

**Date:** 2026-09-17
**Build:** local Unity WebGL build from `codex/signal-garden-sg06`
**URL:** `http://127.0.0.1:4176/signal-garden/play/`
**Viewport:** 667×912 browser viewport; the Unity canvas was 667×854. The
1920×1080 reference viewport remains a separate desktop qualification target.

## Visual and interaction evidence

The Codex in-app Chromium preview reached the playable scene and rendered the
floating garden, coral source, winding gold route with its blind spur, imported
blue receiver shrine, objective panel, controls, and bottom status panel. A
visual screenshot was captured during the review; this short record is the
committed evidence artifact rather than a generated image file.

The following state changes were observed in the browser:

- Initial state showed `OBSERVE / FIND THE PATH` and the source-to-receiver
  objective.
- `Esc` opened the pause overlay with `Paused`, `Resume / Esc`, and
  `Restart this turn`; `Esc` again returned to the objective state.
- The sound control changed the live region to `Sound cues on. Use the Sound
  button to mute them.`
- The reduced-motion control changed the live region to `Reduced motion on.
  Ambient pulses are now still.` and the button label to `Motion: Reduced`.
- In the final keyboard-focus recheck, `Tab` from the canvas announced `Sound
  cues: Off`, a second `Tab` announced `Motion: Full`, and `Shift+Tab` returned
  to `Sound cues: Off`.
- While paused, `Tab` announced `Restart this turn`, `Shift+Tab` returned to
  `Resume / Esc`, and `Enter` resumed the scene to `Ready. Drag from the coral
  source to begin.`
- The accessible live region remained available as the state/status channel;
  spatial route drawing remained on the canvas.

The browser reported no runtime errors. It did report that the optional URP
Edge Adaptive Spatial Upsampling shader was stripped for WebGL, so that FSR
post-processing pass does not run. The scene rendered normally. Existing
deterministic route, recovery, reset, replay, and focus-interruption coverage
remains in the Unity EditMode suite; this visual pass did not claim human
timing, sustained 60 fps, or physical focus-loss qualification.

## Nested hosting read-back

The local harness supplied the same nested route and asset-base shape used by
the games site. The loader and all compressed artifacts returned `200` with the
expected MIME and compression metadata:

- `WebGL.loader.js`: `text/javascript; charset=utf-8`, no content encoding
- `WebGL.data.br`: `application/octet-stream`, `Content-Encoding: br`
- `WebGL.framework.js.br`: `text/javascript; charset=utf-8`, `Content-Encoding: br`
- `WebGL.wasm.br`: `application/wasm`, `Content-Encoding: br`

No production R2 object, games-site catalog entry, deployment, or Jira status
was changed.
