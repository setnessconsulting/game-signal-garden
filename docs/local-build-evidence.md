# Local build evidence

## Current state (2026-09-16)

There is no Unity-generated WebGL build yet. Unity `6000.6.0f1` is installed with the WebGL module, but the Editor cannot start a project import or batch build because this machine has no valid Unity Editor license. Batch mode exited with code 198 and reported `No valid Unity Editor license`; launching the desktop Editor opened its licensing screen. No account activation or license change was attempted.

The source project pins these packages in `Packages/packages-lock.json`:

- Universal Render Pipeline: `17.6.0`
- Input System: `1.19.0`
- Unity Test Framework: `1.8.0`

## Checks completed without the Unity Editor

- C# runtime, editor, and EditMode test sources compiled with the .NET SDK bundled with Unity, against installed Unity Editor module assemblies and package assemblies: 0 warnings, 0 errors. This is a source compilation check; it does not validate Unity asset import, scene serialization, IL2CPP, or WebGL output.
- A temporary standalone runner exercised 12 routing and state assertions against the project’s `RouteRules` and `GardenRunState` sources: 12 passed. These are not Unity Test Runner EditMode results.
- `node --check` passed for `tests/host-preview/serve.mjs`.

## Evidence still pending

Unity project import and clean Editor compile; the actual EditMode suite; WebGL build; browser preview at `/signal-garden/play/`; mouse/WASD/Esc, focus-loss, pause and replay checks; objective and completion timings; 1920×1080 frame rate; and local time-to-interactive all require a licensed Unity Editor and a generated build.

Until then, there is no source commit/build SHA pair, WebGL loader/data/framework/WASM filename set, or observed output compression/MIME header record. Project settings request Brotli compression; the build artifact's bytes and HTTP `Content-Encoding`/`Content-Type` have not been verified. No generated build is tracked, and no production R2 upload or site-catalog change was made.

## Record after a successful local build

- Source commit SHA and SHA-256 fingerprints for the exact `Builds/WebGL/Build` artifact set.
- Unity Editor and resolved package versions.
- Exact loader, data, framework, and WASM filenames.
- Compression mode and HTTP `Content-Encoding` / `Content-Type` read-back.
- Unity compile/test/build results and browser/version/resolution.
- Control/recovery/pause/replay results, objective and completion timings, observed frame rate, and local time-to-interactive.

Production R2 upload and site-catalog promotion are separate reviewed release steps and are outside this local evidence record.
