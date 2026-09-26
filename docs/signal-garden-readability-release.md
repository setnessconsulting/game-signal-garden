# Signal Garden readability release

**Recorded:** 2026-09-26
**Runtime source:** `da74c6a544066a538e7f999f18972d2b6ebe33fb`
**Game main merge:** `5eb5fc0e2b1ca1545db662ca9a40fe6c6d4c9062`
**Games-site merge:** `bf33bfbc98dcf856c4ee6c3ff685f1930ce22926`
**Cloudflare Pages deployment:** `9f8f7b53-1912-4c25-b335-27669de09eea`
**Release:** `2026-09-26-da74c6a`
**R2 prefix:** `signal-garden/2026-09-26-da74c6a/Build/`
**Production route:** https://games.setnessconsulting.com/signal-garden/play/

## Reported issue and fix

The game looked blurry and its words were hard to read. The WebGL internal
render scale is now capped at `0.80` instead of `0.60`, while still respecting
the Mobile URP asset's configured `0.80` ceiling. The objective panel widened
from 560 to 650 units. The objective title grew from 25 to 32 points, the
instruction text from 15 to 22, small labels from 12/13 to 18, status text from
16 to 22, and button labels from 15 to 20. Pause overlay text was enlarged too.
These changes improve the rendered detail and the size of the key words without
changing the puzzle or its controls.

## Build identity

Unity `6000.6.0f1`; resolved packages remain recorded in
[`sg-10-build-identity.json`](sg-10-build-identity.json). The four WebGL build
artifacts total **10,508,368 bytes**, below the 12 MiB budget.

| Artifact | Bytes | SHA-256 |
| --- | ---: | --- |
| `WebGL.loader.js` | 27,914 | `D63926545C10A77F1556D19BCBCB9F6AD9EF1D4A45D6160698CA3F742F84C90F` |
| `WebGL.data.br` | 3,593,229 | `27AD4EFEE4947E4D7A9D1E41F95E88E1E981B1CBABCB1CD7503F568A322EE1D6` |
| `WebGL.framework.js.br` | 66,634 | `1186EB45CBF5D43B13C3EEB050FD4B5849B184C3BF1E5C2CAEED5639F32449C3` |
| `WebGL.wasm.br` | 6,820,591 | `BBD5A40ACE03C784E30E087EA02225CC2E4B68C6B6291D073F50E7FDD642F95A` |

## Production readback

The site catalog now points to `2026-09-26-da74c6a`. The Pages deployment check
passed; the [versioned Pages deployment](https://9f8f7b53.games-site-7pn.pages.dev)
returns HTTP 200 and serves the same release. The live play route loads asset base
`/game-assets/signal-garden/2026-09-26-da74c6a`. All four production asset
requests returned HTTP 200 with the expected MIME type, `Content-Encoding: br`,
and `Cache-Control: public, max-age=31536000, immutable`. The downloaded
production bodies for the three precompressed `.br` files match the build
SHA-256 values above byte-for-byte; the loader's browser-decoded size is 27,914
bytes.

The production browser rendered a visible `1178x589` WebGL canvas with
`gl.getError() == 0`. It reported “Ready. Drag from the coral source to begin.”
A coral-to-blue pointer route completed and returned “Signal received. The
garden is awake. Choose Play again for the next turn.” This is a production
smoke check to confirm the deployed version works; it is not owner qualification.

A separate local nested-preview sample at `1920x1080` completed 30 focused
seconds at 61.18 mean FPS and 60.46 minimum one-second FPS. This is local
performance evidence only.

## Qualification and rollback

[`sg-12-release-candidate.json`](sg-12-release-candidate.json) remains
`NOT_READY` with `promotionAllowed: false`. Foreground Chrome and Edge, cold
load timing, browser working set, physical focus loss, screen-reader operation,
five uncoached player sessions, and owner approval remain open. The production
deployment lets the owner test the readability fix while those gates stay
truthfully unqualified.

The prior immutable release prefix remains
`signal-garden/2026-09-21-58f2c29/Build/`. The games-site repository's
[`signal-garden-rollback.md`](https://github.com/setnessconsulting/games-site/blob/main/docs/signal-garden-rollback.md)
records the previous catalog pointer and Pages rollback path.
