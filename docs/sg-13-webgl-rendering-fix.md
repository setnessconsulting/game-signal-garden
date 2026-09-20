# Signal Garden — WebGL rendering compatibility replacement

**Record:** local rendering fix and replacement candidate
**Date:** 2026-09-19
**Runtime source:** `20bf9b0595b2b16bb341a3bed119e37e615491aa`
**Unity:** `6000.6.0f1` (`f7f8ed4d1e24`)
**Route:** `/signal-garden/play/`
**Reference render:** `1920x1080`

## Finding and bounded fix

The frozen SG-10 candidate loaded its HUD but rendered an empty world in the
current Chromium WebGL preview. The browser console reported repeated WebGL
sampler-type mismatch errors. The scene, route rules, input mapping, and
gameplay scripts were unchanged.

The replacement disables Mobile URP paths that are not needed by this scene and
that triggered the WebGL failure: main and additional light shadows,
additional-light rendering, light cookies, light layers, shadow prefiltering,
and the native render pass. This is a rendering compatibility change only; the
receiver, source, route, HUD, and deterministic phases remain the same.

## Local verification

- Unity `6000.6.0f1` WebGL build completed successfully.
- EditMode tests: **24/24 passed**.
- PlayMode tests: **8/8 passed**.
- The local nested host at `http://127.0.0.1:4186/signal-garden/play/` rendered
  the full floating garden, coral source, gold route, receiver shrine, trees,
  and flowers at the exact `1920x1080` canvas.
- A mouse drag from the source to the receiver reached the readable verified
  state and offered replay. No sampler mismatch errors appeared in the patched
  preview.
- The preview showed `60.1 fps` during the route smoke check. A fresh focused
  30-sample frame-rate run is still required; this smoke readout is not owner
  qualification.
- The generated four-file identity is recorded in
  `docs/sg-10-build-identity.json`. The data bundle changed; loader, framework,
  and WASM hashes remained unchanged. All `.br` files retain `Content-Encoding:
  br`; the WASM retains `application/wasm`.

## Requalification boundary

The earlier SG-12 browser, timing, and diagnostic observations were captured
against the blank-world candidate and are superseded by this replacement. The
closeout manifest marks that evidence `SUPERSEDED`, resets the owner gates to
`NOT_RUN`, and keeps `NOT_READY` with `promotionAllowed: false`. Chrome and Edge
foreground performance, cold and warm TTI, working set, physical focus loss,
screen reader behavior, five fresh-player sessions, owner approvals, and
production CDN checks must be rerun against this candidate.

No R2 object was uploaded. The games-site catalog, deployment, and Jira were
not changed. Generated WebGL files remain ignored and are not committed.
