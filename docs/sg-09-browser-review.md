# SG-09 browser review

Date: 2026-09-19 UTC

Source under test: `7b890e81cb2d41665dfc67f5ac704ead31b45ea0`

Build identity: local host cache key `e815d4dee6f719cc`; Unity `6000.6.0f1` WebGL build from `Builds/WebGL/Build/`. No generated build files are tracked.

## Nested route and headers

The local harness served `http://127.0.0.1:4183/signal-garden/play/` and the four exact files beneath `/game-assets/signal-garden/local/Build/`. Every request returned `200`. The loader returned `text/javascript; charset=utf-8`; the data file returned `application/octet-stream` with `Content-Encoding: br`; the framework returned `text/javascript; charset=utf-8` with `Content-Encoding: br`; and the WASM returned `application/wasm` with `Content-Encoding: br`.

## Browser interaction

The visible Codex in-app Chromium preview loaded the nested route and exposed the focusable canvas and live status region. The status began with `Ready. Drag from the coral source to begin.` Pressing `Escape` changed the live status to `Paused. Press Esc or choose Resume to continue. Replay starts a fresh turn.` Pressing `Escape` again returned the status to `Ready. Drag from the coral source to begin.` This verifies the pause/resume input path and live-region recovery in the local preview. The Unity PlayMode suite covers route verification, focus interruption, application pause, and scene reload; this browser interaction did not claim a physical window focus loss or external screen-reader pass.

The preview canvas and HUD were present in the captured browser state, but the computer-use screenshot did not provide reliable scene pixels for visual asset approval. Treat owner visual review and external screen-reader operation as open SG-09 gates.

## Performance observations

Using `?sg-render=1920x1080&sg-stats=1` in a kept-foreground in-app preview, the harness completed `30/30s` with an average of `63.1 fps` and an informational minimum of `51.8 fps`; the harness displayed `PASS` because its gate is mean ≥60 fps and exact 1920×1080 buffer. A shorter 5-second diagnostic also completed, but it is not used as release evidence. Earlier unattended/occluded reads produced lower values and are retained as nonrepresentative caveats.

This is a local in-app browser result. An independent foreground desktop Chrome run and Edge run were not completed in this slice. The installed browser versions observed on the Windows machine were Chrome `153.0.8010.52` and Edge `153.0.4234.32`; neither version is claimed as a completed SG-09 performance pass here.

Time to interactive was not directly captured from the harness timestamps in this review. Browser-tab working set was not measured. The compressed artifact total is recorded in [local build evidence](local-build-evidence.md), but the proposed 12 MiB and 512 MiB budgets remain pending owner sign-off.
