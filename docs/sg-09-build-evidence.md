# SG-09 build evidence

Tested source revision: `7b890e81cb2d41665dfc67f5ac704ead31b45ea0`

Unity: `6000.6.0f1` (`f7f8ed4d1e24`)

Packages: URP `17.6.0`, Input System `1.19.0`, UGUI `2.6.0`, Unity Test Framework `1.8.0`

## Automated checks

- EditMode: `24/24` passed.
- PlayMode: `8/8` passed, including focus interruption, application pause, and scene reload coverage.
- SG-05 provenance CLI: `PASS`.
- Credential-free repository validator: `PASS`.
- `git diff --check`: clean before the evidence record was added.

## WebGL artifacts

The clean Unity WebGL build completed with result `Success`. The ordered artifact total is `10,490,711` bytes (10.005 MiB), below the proposed 12 MiB compressed-build budget. The proposed 12 MiB build and 512 MiB browser-tab working-set limits are pending owner sign-off; the tab working set was not measured.

| File | Bytes | SHA-256 |
| --- | ---: | --- |
| `WebGL.loader.js` | 27,914 | `8E62AFD3D90B709331D8223954F6C3906287B9E8E8C7D852FD85435CD8A74AE3` |
| `WebGL.data.br` | 3,585,947 | `8934DAEDA5B4FCEEDED5BC2EEC44EECA670150B5A08CE1B45FFAFFC90F9A4148` |
| `WebGL.framework.js.br` | 66,617 | `BF630E2ED2DF06748BEF43376CD3B9CF39F5CA7F00023B1D28646448E1899255` |
| `WebGL.wasm.br` | 6,810,233 | `492B0F4228524D7814AF51168D5A78C6005A7E1DE1099371F45667E04DA2FC7F` |

Cache key from the local host manifest: `e815d4dee6f719cc`.

## HTTP and browser evidence

The local host at `http://127.0.0.1:4183` returned `200` for `/signal-garden/play/` and all four files below `/game-assets/signal-garden/local/Build/`. The loader and framework returned `text/javascript; charset=utf-8`; the data file returned `application/octet-stream`; and the WASM returned `application/wasm`. Each compressed `.br` file returned `Content-Encoding: br`.

The visible in-app browser completed the exact `1920×1080` render sample with `30/30s`, `63.1 fps` mean, `51.8 fps` informational minimum, and harness `PASS`. It also exercised Esc pause and resume and exposed the readable live status. Full interaction notes are in [sg-09-browser-review.md](sg-09-browser-review.md).

This is local in-app-browser evidence, not independent desktop Chrome or Edge qualification. Chrome `153.0.8010.52` and Edge `153.0.4234.32` were installed, but completed foreground runs in those desktop browsers were not captured. The 10-second time-to-interactive target, external screen-reader operation, physical focus-loss behavior, browser-tab working set, and five-session human benchmark remain open.

No R2 upload, games-site catalog change, deployment, or Jira status update was made.
