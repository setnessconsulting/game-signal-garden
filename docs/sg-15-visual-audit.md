# Signal Garden — SG-15 visual audit and refinement

**Issue:** Visual refinement after the SG-14 preview
**Date:** 2026-09-20
**Runtime source:** `1b3586f26f0c81410cbbbe33fc908e2dee2f1079`
**Unity:** `6000.6.0f1` (`f7f8ed4d1e24`)
**Route:** `/signal-garden/play/`
**Reference render:** `1920x1080`

## Audit findings

The full-size preview showed two presentation issues that were safe to address
without changing the puzzle: the island center had a strong radial fan pattern,
and the long signal trail read as a repeated strip of identical stones.

The refinement keeps the route points, dead end, source and receiver markers,
camera, HUD, input mapping, and deterministic phases unchanged. The island
center now uses calmer terrain shading and reduced vertical variation. The
standard trail uses a restrained alternate stone material on selected segments,
while the center thread, waypoint markers, and blind spur remain unchanged.
This adds visual rhythm without changing route geometry or hit testing.

## Verification

- Unity `6000.6.0f1` WebGL build completed with `Build Finished, Result:
  Success`.
- The ordered four-file artifact total is `10,468,381` bytes and remains below
  the provisional 12 MiB compressed budget. Exact hashes, Brotli encoding, and
  MIME metadata are recorded in [the SG-10 build identity](sg-10-build-identity.json).
- EditMode tests: **24/24 passed**.
- PlayMode tests: **8/8 passed**.
- The local nested preview at
  `http://127.0.0.1:4186/signal-garden/play/?sg-render=1920x1080&sg-stats=1`
  rendered the full diorama at the reference canvas.
- A source-to-receiver drag reached `Signal received. The garden is awake.`
  and offered replay. An invalid route produced readable recovery guidance,
  and Esc produced the readable paused state.
- No browser console errors were observed in the local preview.

Headless SwiftShader frame samples remain diagnostic only and do not satisfy
foreground Chrome or Edge qualification. Owner FPS, cold TTI, working set,
physical focus loss, screen-reader use, five-session benchmark, approvals, and
production checks remain open. No R2 object, catalog change, deployment, or
Jira mutation was made.
