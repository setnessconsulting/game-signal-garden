# Local build evidence

## Current state (2026-09-16)

The first playable WebGL slice was imported, compiled, tested, built, and served locally with Unity `6000.6.0f1`. The project uses the assigned Unity Personal entitlement. No production R2 object was uploaded and the games-site catalog remains unchanged.

Source commit: `cb7a8ff` (`Build SG-01 first playable slice`).

## Unity and packages

- Unity Editor: `6000.6.0f1` (`f7f8ed4d1e24`)
- Build target: WebGL, URP, Brotli compression, single threaded
- Universal Render Pipeline: `17.6.0`
- Input System: `1.19.0`
- Unity Test Framework: `1.8.0`
- UGUI: `2.6.0`

The batch import and script compilation probe exited `0`. The scene generator command also exited `0` and reported `Signal Garden scene generated at Assets/Scenes/SignalGarden.unity.`

## EditMode tests

The final Unity Test Framework run exited `0` using the test runner without `-quit` (Unity's test runner owns shutdown for command-line runs).

`C:\Users\setne\AppData\Local\Temp\signal-garden-editmode-final.xml` reports:

- 11 test cases
- 11 passed
- 0 failed, skipped, or inconclusive

The suite covers valid, invalid, partial, cancelled, repeated, reset, JSON round-trip, scene reload, and focus interruption state behavior. The final source change also guards the first pointer release after a pause/replay button click so a UI action cannot become an accidental route start.

## WebGL build

The final clean build command used Unity's CLI build method:

```powershell
$unity = "C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe"
$project = (Get-Location).Path
& $unity -batchmode -nographics -quit -projectPath $project `
  -executeMethod SignalGarden.Editor.SignalGardenProjectSetup.BuildWebGL `
  -logFile "$project\Temp\webgl-build.log"
```

The final log is `C:\Users\setne\AppData\Local\Temp\signal-garden-webgl-final4-build.log`. Unity reported `Build Finished, Result: Success`, a total build size of `10,448,932` bytes, and return code `0`. Generated output remains under ignored `Builds/WebGL/` and is not committed.

Exact files in `Builds/WebGL/Build`:

| File | Bytes | SHA-256 |
| --- | ---: | --- |
| `WebGL.loader.js` | 27,914 | `BA79BFB08C7D380942BC0A322538515ED98E5554777A3B88263904DB375D3214` |
| `WebGL.data.br` | 3,611,762 | `090AAD13FD1E7BA979F8AC6DB6ECE355F42C5CC184517BBFD723722641368E3F` |
| `WebGL.framework.js.br` | 66,565 | `7A3B10FF77AA44F32185F084B9EE41AA21EE01A106446223893D1AF7B44207D9` |
| `WebGL.wasm.br` | 6,722,182 | `6356855AD8F7978CB8A142788933824C325208F1012562EDAB9152884856131F` |

The loader references the exact four files above. The project setting is persisted as Brotli (`webGLCompressionFormat: 2`) and the build method also sets `PlayerSettings.WebGL.compressionFormat` to Brotli.

## Host harness and HTTP headers

`tests/host-preview/serve.mjs` was run at `http://127.0.0.1:4173`. It served the play route at `/signal-garden/play/` and the asset base at `/game-assets/signal-garden/local/Build/`, matching the site's nested-path and asset-base shape.

Raw `curl -I` read-back returned `200` for every artifact:

- `WebGL.loader.js`: `text/javascript; charset=utf-8`, no content encoding
- `WebGL.data.br`: `application/octet-stream`, `Content-Encoding: br`
- `WebGL.framework.js.br`: `text/javascript; charset=utf-8`, `Content-Encoding: br`
- `WebGL.wasm.br`: `application/wasm`, `Content-Encoding: br`

The harness also sends `Cache-Control: public, max-age=0, must-revalidate` and `X-Content-Type-Options: nosniff` for build files.

## Browser preview

The final build loaded at the nested local route in the Codex in-app Chromium-compatible browser. The page reached `readyState: complete`, hid its loading and failure panels, created a WebGL 2 context, and rendered the complete diorama: floating garden, coral source, gold route with blind spur, blue receiver, HUD, and status bar. The browser console contained no runtime errors; the only messages were URP warnings for optional post-processing shaders that are stripped on WebGL.

Observed controls and recovery:

- Mouse drag begins a route at the coral source and renders the live line.
- An interrupted or invalid route enters recovery with a readable blind-spur message.
- `Esc` pauses/resumes and cancels an active route.
- `Restart this turn` clears the route and returns to `Fresh turn. Drag from the coral source to begin.`
- The accessible status text mirrors objective, recovery, pause, and replay states; spatial route drawing remains visual.

The IAB viewport was `1280x662` for the Unity canvas. The target `1920x1080` presentation, 60 fps qualification, human objective-under-30-seconds and completion-under-one-minute timing, focus-loss behavior with a physically interrupted browser window, and ten-second time-to-interactive target remain open for a visible desktop Chrome/Edge qualification pass. The background IAB frame-rate report read `2 fps`, so it is not used as a 60 fps claim.

## Scope and release boundary

The local result is the SG-01 vertical slice only: one scene, one source, one receiver, one winding route, one dead end, deterministic observe/route/recovery/verify/pause phases, and the confirmed mouse/WASD/Esc mapping. No Rive, music, Blender/Figma integration, Unity API qualification, production upload, catalog promotion, merge, or deploy was performed.

The next issue after review is [GAME-283 / SG-05](https://setnessconsulting.atlassian.net/browse/GAME-283), the Blender source and Unity import provenance chain.
