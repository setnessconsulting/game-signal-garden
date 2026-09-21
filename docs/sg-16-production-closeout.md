# Signal Garden — production promotion and artifact readback

**Runtime source:** `58f2c2917ae15fb2299bd13a60a139420bf79690`
**Game main merge:** `58f2c2917ae15fb2299bd13a60a139420bf79690` (source PR merge identity will be reconciled after the game PR lands)
**Games-site merge:** `b7adfb44e4bd6989f1b2cee8bcb894fe1c325f65`
**Pages deployment:** `aa4cc88e-ef6c-4349-8f36-b530c150ec8f`
**Route:** <https://games.setnessconsulting.com/signal-garden/play/>
**R2 prefix:** `signal-garden/2026-09-21-58f2c29/Build/`

## Promotion record

The owner-authorized Wave 2 production promotion is complete. The games-site catalog
marks Signal Garden `playable`, and the nested play page references the exact
immutable release prefix above. This record describes the deployment and
readback; it is not a substitute for the SG-09 foreground/accessibility gates,
the SG-11 human benchmark, or owner approval.

The release uses the four files required by the games-site contract. The
recorded source hashes and sizes match `docs/sg-10-build-identity.json`.

| File | Bytes | SHA-256 | R2 type | R2 encoding | Browser response |
| --- | ---: | --- | --- | --- | --- |
| `WebGL.loader.js` | 27,914 | `D63926545C10A77F1556D19BCBCB9F6AD9EF1D4A45D6160698CA3F742F84C90F` | `text/javascript; charset=utf-8` | none | `200`, JavaScript, Brotli negotiated |
| `WebGL.data.br` | 3,596,130 | `EE0353821921BB4341179F556C79E98811DB29011AE9867F5B327557C9848235` | `application/octet-stream` | `br` | `200`, Brotli |
| `WebGL.framework.js.br` | 66,634 | `1186EB45CBF5D43B13C3EEB050FD4B5849B184C3BF1E5C2CAEED5639F32449C3` | `text/javascript; charset=utf-8` | `br` | `200`, Brotli |
| `WebGL.wasm.br` | 6,816,117 | `53571DE49A16115595AAB3390F3A36FBBD8EFDCCCECB8EF7EA5D8F08BF2BA0D8` | `application/wasm` | `br` | `200`, Brotli |

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
