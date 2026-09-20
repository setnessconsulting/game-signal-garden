# Signal Garden — production promotion and artifact readback

**Runtime source:** `1b3586f26f0c81410cbbbe33fc908e2dee2f1079`
**Game main merge:** `89e7afc161ce16404929a394b2ba89cbd2c136ec`
**Games-site merge:** `42acc2aa2bec48861d9bc9ae335027507b66f4a2`
**Pages deployment:** `d55fc64e-07af-4aec-99f6-c1ce32d82b26`
**Route:** <https://games.setnessconsulting.com/signal-garden/play/>
**R2 prefix:** `signal-garden/2026-09-20-1b3586f/Build/`

## Promotion record

The owner-authorized production promotion is complete. The games-site catalog
marks Signal Garden `playable`, and the nested play page references the exact
immutable release prefix above. This record describes the deployment and
readback; it is not a substitute for the SG-09 foreground/accessibility gates,
the SG-11 human benchmark, or owner approval.

The release uses the four files required by the games-site contract. The
recorded source hashes and sizes match `docs/sg-10-build-identity.json`.

| File | Bytes | SHA-256 | R2 type | R2 encoding | Browser response |
| --- | ---: | --- | --- | --- | --- |
| `WebGL.loader.js` | 27,914 | `8E62AFD3D90B709331D8223954F6C3906287B9E8E8C7D852FD85435CD8A74AE3` | `text/javascript; charset=utf-8` | none | `200`, JavaScript, Brotli negotiated |
| `WebGL.data.br` | 3,563,617 | `3C6D90D094804E9F1DB7F139F9CA7C7CD0ACED3C060F681AE0E81526A41D3DDC` | `application/octet-stream` | `br` | `200`, Brotli |
| `WebGL.framework.js.br` | 66,617 | `BF630E2ED2DF06748BEF43376CD3B9CF39F5CA7F00023B1D28646448E1899255` | `text/javascript; charset=utf-8` | `br` | `200`, Brotli |
| `WebGL.wasm.br` | 6,810,233 | `492B0F4228524D7814AF51168D5A78C6005A7E1DE1099371F45667E04DA2FC7F` | `application/wasm` | `br` | `200`, Brotli |

All four objects use `public, max-age=31536000, immutable`. The browser-like
readback requested `Accept-Encoding: br`; the loader may be Brotli-compressed
by the edge even though its R2 object is stored without a content-encoding
metadata value. The `.br` artifacts retain explicit `Content-Encoding: br` in
R2 and in the response.

## Qualification boundary

The SG-12 manifest intentionally remains `NOT_READY` with
`promotionAllowed: false`. The following still require owner-observed or human
evidence against this candidate: foreground Chrome and Edge performance, cold
and warm time-to-interactive, browser working set, physical focus loss,
external screen-reader operation, five fresh-player sessions, visual/audio/
accessibility approval, and budget sign-off. No result is inferred from this
deployment readback.

The public deployment does not change the Unity gameplay rules or the scope of
SG-01 through SG-12. Jira status reconciliation is a separate owner action
after the evidence PR is reviewed and merged.
