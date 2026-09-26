# Local build evidence

## Maintenance validation (2026-09-25)

The authoritative checkout was fetched at origin/main commit
276ac44ca636c0ff6d2a10aa56e7dcc0f0bd982b. Validation began in a separate
worktree. Unity 6000.6.0f1 passed **97/97 EditMode** and **12/12
PlayMode** tests. The WebGL build completed with Build Finished, Result:
Success; Unity reported 10,524,693 bytes.

Unity reserialized two tracked receiver materials during the run. Their
generated contents matched the preserved SG-06 material-reserialization branch
at `39b8d68`; both were restored to the validation base afterward.

The generated loader and framework matched the recorded production identity.
The data and WebAssembly files did not:

| Artifact | Local bytes | Local SHA-256 | Recorded release bytes |
| --- | ---: | --- | ---: |
| WebGL.data.br | 3,589,087 | 5a369e968d30f8567f9110a16b7556c88a3843ded45136b598f4d69e10e60453 | 3,596,130 |
| WebGL.wasm.br | 6,820,549 | a2b56f9e227dbf8fcf4234fa224f00062b25e25bd687d7d9c25bfc12c53cef00 | 6,816,117 |

python scripts/ci/validate_release_evidence.py --build-dir Builds/WebGL/Build
therefore fails on those two artifacts. The generated directory remains
ignored; this output is not the immutable production artifact.

The local nested preview reached Ready with a 1920x1080 canvas, but the
garden geometry was absent. The WebGL context reported INVALID_OPERATION
(1282), and the ?sg-render=1920x1080&sg-smoke=route run timed out waiting
for a pointer drag to begin. This is a local end-to-end failure. A graphics-
enabled repeat stopped at a Unity Burst compiler-server queue timeout and
produced no second build result.

The deployed route at
https://games.setnessconsulting.com/signal-garden/play/ loaded the current
2026-09-21-58f2c29 release and rendered the garden. In browser automation,
an invalid route entered Recovery with the blind-spur retry message, a valid
source-to-receiver route reached Verified, Play again reset the turn, and Esc
paused and resumed. No blocking production browser errors were recorded. This
verifies the deployed artifact only; it does not qualify the new local build or
close the independent desktop-browser, screen-reader, focus-loss, working-set,
human benchmark, or owner-approval gates.

## Current production reconciliation (2026-09-20)

The current runtime candidate is deployed at
`https://games.setnessconsulting.com/signal-garden/play/` under the immutable
prefix `signal-garden/2026-09-20-1b3586f/Build/`. The production route, catalog
pointer, Pages deployment, four artifact hashes, MIME types, Brotli metadata,
and cache policy are recorded in [the SG-16 production closeout](sg-16-production-closeout.md)
and the SG-10 build identity. This readback records deployment state only; it
does not claim the remaining foreground browser, accessibility, working-set,
focus-loss, owner-approval, or human benchmark gates.

The historical local captures below are preserved as evidence for the builds
and environments in which they were observed. Their statements that no
production upload or catalog change had occurred describe those capture dates,
not the current deployment.

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
games-site, deployment, or Jira mutation was made at capture time. The next
focused issue was GAME-285 / SG-07; its v1 Rive decision is now recorded in
[sg-07-motion-decision.md](sg-07-motion-decision.md).

## SG-06 1920×1080 follow-up (2026-09-17)

Unity `6000.6.0f1` built the SG-06 WebGL candidate successfully with URP
`17.6.0`, Input System `1.19.0`, UGUI `2.6.0`, and Unity Test Framework
`1.8.0`. The local Unity build reported `10,480,240` bytes and remains ignored
under `Builds/WebGL/`. The local preview's cache identity is
`e9867b39eceb012f`, the first 16 hexadecimal characters of the SHA-256 over
the ordered four-artifact hash manifest.

| File | Bytes | SHA-256 |
| --- | ---: | --- |
| `WebGL.loader.js` | 27,914 | `6DA2C7385B7FD9A2EC4F634A4B40A32604A6BA8ED383494DA3EC6FFECD467B32` |
| `WebGL.data.br` | 3,632,706 | `716B7AE8685465B47A0F29CB25D93284B43AB4794796F32242388391CA3F642F` |
| `WebGL.framework.js.br` | 66,617 | `BF630E2ED2DF06748BEF43376CD3B9CF39F5CA7F00023B1D28646448E1899255` |
| `WebGL.wasm.br` | 6,732,494 | `4B3EB9D1AAD281B474D8572C8AF5B0821DF46C0D997392930FD6812364F82476` |

Final EditMode coverage passed 24/24 tests; PlayMode passed 1/1. The local
nested route and all four build files returned HTTP 200. The loader returned
`text/javascript; charset=utf-8` without compression; the data file returned
`application/octet-stream`, the framework returned
`text/javascript; charset=utf-8`, and the WASM returned `application/wasm`.
Each `.br` file returned `Content-Encoding: br`.

The visible in-app browser completed the five-second warmup and all 30
one-second samples at an exact 1920×1080 canvas: minimum 60.0 fps, mean 60.1
fps, `PASS`. A separate desktop Chrome automation run at an exact
1920×1080 viewport and canvas completed its samples but reported 1.0 fps
minimum/mean. No runtime error appeared; the only console warning was the
optional URP FSR shader being stripped. Edge was unavailable. Treat the
desktop Chrome/Edge 60 fps qualification as unresolved until the owner's
desktop browser confirms it; the in-app preview alone does not close that
browser-specific gate.

`git diff --check` passed, and generated builds remain ignored. No production
R2 upload, games-site catalog change, deployment, or Jira status update was
made.

## SG-08 feedback and audio addendum (2026-09-18)

Unity `6000.6.0f1` built the SG-08 candidate successfully with URP `17.6.0`,
Input System `1.19.0`, UGUI `2.6.0`, and Unity Test Framework `1.8.0`. The
Unity Editor passed 24/24 EditMode tests and 5/5 PlayMode tests. The WebGL
build report was `Success`, `10,560,659` bytes. Generated output remains
ignored under `Builds/WebGL/`.

The final local nested preview used cache key `63b4db6fcbfdd87e` and resolved these
exact artifacts:

| File | Bytes | SHA-256 |
| --- | ---: | --- |
| `WebGL.loader.js` | 27,914 | `8E62AFD3D90B709331D8223954F6C3906287B9E8E8C7D852FD85435CD8A74AE3` |
| `WebGL.data.br` | 3,635,386 | `8CAB72C9E89F55E08CABB66A93D2E1234FAFBEEFB5EDB9B4850F37517B9C9C3E` |
| `WebGL.framework.js.br` | 66,617 | `BF630E2ED2DF06748BEF43376CD3B9CF39F5CA7F00023B1D28646448E1899255` |
| `WebGL.wasm.br` | 6,810,233 | `492B0F4228524D7814AF51168D5A78C6005A7E1DE1099371F45667E04DA2FC7F` |

The local host returned `200` for the play route and each artifact. It served
the loader as `text/javascript; charset=utf-8` without compression, the data
as `application/octet-stream` with `Content-Encoding: br`, the framework as
`text/javascript; charset=utf-8` with `Content-Encoding: br`, and the WASM as
`application/wasm` with `Content-Encoding: br`. The browser interaction record
is [docs/sg-08-browser-review.md](sg-08-browser-review.md).

The visible in-app browser preview exercised pause/resume, mute, mouse and
keyboard volume changes, reduced motion, blind-spur recovery, and retry. On the
final build, the exact 1920×1080 render completed a 30-second frame-rate sample
with a 74.6 fps minimum and 86.9 fps average (`PASS`). The tab completed all
30/30 samples while visible in the in-app browser. This is a local preview pass;
it does not replace an independent desktop Chrome/Edge qualification or
production CDN measurement. Earlier unpresented/agent-controlled reads below 60
fps, including the background/occlusion-clamped Chrome run, remain recorded in
the browser review and are not treated as representative foreground failures.

The only browser console warning was the optional URP Edge Adaptive Spatial
Upsampling shader being stripped for WebGL; no runtime errors appeared.

The build and repository checks did not upload to production R2, change the
games-site catalog, deploy, or modify Jira status.
