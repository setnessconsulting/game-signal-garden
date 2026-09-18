# Local build evidence

## Current state (2026-09-16)

The first playable WebGL slice was imported, compiled, tested, built, and served locally with Unity `6000.6.0f1`. The project uses the assigned Unity Personal entitlement. No production R2 object was uploaded and the games-site catalog remains unchanged.

Source commit: `58fd28e` (`Merge SG-01 first playable slice`).

The earlier SG-01 evidence was captured from `cb7a8ff` before that work was
merged. The original evidence remains below; this correction identifies the
merged baseline used for SG-05.

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

The loader references the exact four files above. Unity `6000.6.0f1` persists Brotli as `webGLCompressionFormat: 0` (the enum probe confirms value `0` is `Brotli`), and the build method also sets `PlayerSettings.WebGL.compressionFormat` to Brotli.

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

## SG-05 asset-chain addendum (2026-09-17)

The SG-05 branch now carries the original receiver shrine through the tracked chain
`SignalGardenReceiver.blend → SignalGardenReceiver.fbx → SignalGardenReceiver.prefab → SignalGarden.unity`.
The source is 106,993 bytes (`8D008A257324682A891DDC69DBE046BAD41DE34BCD817FF417ED79DC74B82D6A`) and the FBX is 53,116 bytes (`A1EA560763D1ED19DC5078E9960CB05CC2EF4BBDFC79E08D433FF7A261D1D168`). The full provenance record is [docs/sg-05-asset-record.md](sg-05-asset-record.md).

The current clean Unity `6000.6.0f1` WebGL build completed with result `Success` and total build size `10,555,173` bytes. In this Unity version the serialized Brotli setting is `webGLCompressionFormat: 0`; the editor enum probe identifies value `0` as `Brotli`.

| File | Bytes | SHA-256 |
| --- | ---: | --- |
| `WebGL.loader.js` | 27,914 | `7A837D73235844DA886F6CA938EAEADB5096D9CDA00F5CBD80981775F75153FB` |
| `WebGL.data.br` | 3,640,545 | `F0B807FBE5E3A335DF016389FD28E36C61D00A2C7190F7CCD6BB72775A2542DC` |
| `WebGL.framework.js.br` | 66,565 | `7A3B10FF77AA44F32185F084B9EE41AA21EE01A106446223893D1AF7B44207D9` |
| `WebGL.wasm.br` | 6,799,640 | `83D17F42887772260D3EFA74ED9698E9A22C46EA7FFB7BBB579CC6250DCE1151` |

The loader names those exact four files. The local host harness served them beneath the nested asset base and returned `text/javascript; charset=utf-8` for the loader and framework, `application/octet-stream` for the data file, and `application/wasm` for the WebAssembly file. The `.br` files returned `Content-Encoding: br`.

The final EditMode run passed all 18 tests: 11 existing route/state cases and 7 SG-05 provenance cases, including missing and stale source, mismatched FBX, GUID mismatch, missing prefab, and budget failure. API-10 read-only inspection and `prop` validation passed at commit `9783b64379c5bb3220cabda7018056d3fbef2a0c`; the auxiliary GLB and handoff manifest remain ignored validation artifacts.

The fresh Chrome preview at `http://127.0.0.1:4175/signal-garden/play/` rendered the imported receiver, accepted source input, showed blind-spur recovery, and displayed pause/resume and restart controls. The [SG-05 browser review record](sg-05-browser-review.md) captures the visual evidence and keeps human timing, sustained 60 fps, focus-loss, and owner approval as separate gates. The generated build remains ignored under `Builds/WebGL/`; no production R2 object, site catalog entry, deployment, or Jira status changed.

## SG-06 UGUI HUD addendum (2026-09-17)

The SG-06 branch replaces the first slice's IMGUI presentation with a
serialized UGUI Canvas HUD. The normalized design handoff, explicit UGUI
exception, and API-37 evidence are recorded in
[docs/sg-06-ui-record.md](sg-06-ui-record.md). The browser visual review is
captured in [docs/sg-06-browser-review.md](sg-06-browser-review.md).

The Unity `6000.6.0f1` build used URP `17.6.0`, Input System `1.19.0`, UGUI
`2.6.0`, and Unity Test Framework `1.8.0`. Final EditMode coverage passed 24
of 24 tests (11 existing route/state cases, 5 HUD phase-label cases, 1 HUD
binding case, and 7 SG-05 provenance cases). Final PlayMode coverage passed 1
of 1 scene binding and keyboard focus test, including forward/reverse traversal
of the HUD and pause actions. The final WebGL build reported `Success`,
`10,557,900` bytes, and remains ignored under `Builds/WebGL/`. Its Unity log is
`Artifacts/SG-06/webgl-build-final2.log`.

| File | Bytes | SHA-256 |
| --- | ---: | --- |
| `WebGL.loader.js` | 27,914 | `FFE688F245D8A0F86A01AD97C69768AE5DA05534AC43657731CF5CB47C1C50FF` |
| `WebGL.data.br` | 3,633,110 | `EA4CCC8307200557A927333987763B3F194DCE44BC2CE741D9D987B96D5C51FB` |
| `WebGL.framework.js.br` | 66,565 | `7A3B10FF77AA44F32185F084B9EE41AA21EE01A106446223893D1AF7B44207D9` |
| `WebGL.wasm.br` | 6,809,802 | `4F7C5A208A63E64357F37CD3D276B50E98584EBEDB1689AEA00C1539E41DC3A6` |

The final local host at `http://127.0.0.1:4176/signal-garden/play/` returned `200`
for the nested play route and all four artifacts. The loader and framework
returned `text/javascript; charset=utf-8`, the data file returned
`application/octet-stream`, and the WASM file returned `application/wasm`;
each `.br` artifact returned `Content-Encoding: br`. The browser rendered the
new HUD and imported receiver at the nested route. At a `667×912` browser
viewport, Tab/Shift+Tab/Enter traversal announced the sound and motion controls,
traversed `Resume / Esc` and `Restart this turn` in both directions, resumed
the game, and toggled sound and reduced motion. The browser reported no runtime
errors; it reported the optional URP FSR upscaling shader unavailable/stripped
for WebGL, so that post-processing pass is skipped. The full observations and
remaining owner-gated qualifications are in the SG-06 browser record.

API-37 `hud` profile validation passed offline with zero blocking findings.
Live qualification was attempted and classified `BLOCKED` because no Figma
credential was available. No generated WebGL files are tracked, and no R2,
games-site, deployment, or Jira mutation was made. After SG-06 review, the
next focused issue is GAME-285 / SG-07 (record the v1 Rive decision).
