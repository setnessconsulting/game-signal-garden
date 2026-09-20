# Signal Garden — SG-14 visual polish

**Issue:** Visual quality follow-up after owner preview feedback  
**Date:** 2026-09-19  
**Runtime source:** `47fd09f8567d02bca8a4f4d38404b451ede5e0c6`  
**Unity:** `6000.6.0f1` (`f7f8ed4d1e24`)  
**Route:** `/signal-garden/play/`  
**Reference render:** `1920x1080`

## Bounded change

The owner preview showed that the first rendered diorama felt too flat and
procedural. This pass improves presentation while preserving the frozen puzzle
loop, scene footprint, controls, and deterministic route rules. No new puzzle
content, runtime service, package, or external asset was introduced.

The generator now uses a smoother shared island surface with a restrained moss
palette, rounded capsule route stones with a shorter dead end, coral bloom lobes
around the source, a beacon collar and ring around the imported receiver,
subtle grounded contact shadows for major props, and a small set of emissive
fireflies to add depth. The existing source and receiver markers, endpoint,
lights, HUD, pause/replay, mute, reduced-motion, and recovery behavior remain
in place.

## Local verification

- Unity `6000.6.0f1` WebGL build completed with `Build Finished, Result:
  Success`.
- The ordered four-file artifact total is `10,471,760` bytes and remains below
  the provisional 12 MiB compressed budget. Exact hashes, Brotli encoding, and
  MIME metadata are recorded in [the SG-10 build identity](sg-10-build-identity.json).
- EditMode tests: **24/24 passed**.
- PlayMode tests: **8/8 passed**.
- The local nested preview at
  `http://127.0.0.1:4186/signal-garden/play/?sg-render=1920x1080&sg-stats=1`
  rendered the complete polished diorama at the exact reference canvas.
- The browser smoke route dragged from the source through the visible winding
  path to the receiver and reached the readable verified state with replay.
  No console errors were observed in the patched preview.
- A screenshot was captured locally as review evidence at
  `C:\Users\setne\Desktop\AI Projects\_tmp-sg-visual-polish3.png`.

The headless SwiftShader sample is diagnostic only and is not a foreground
Chrome or Edge performance qualification. Fresh owner runs are still required
for frame rate, cold TTI, working set, physical focus loss, screen reader use,
and the five-session benchmark.

## Requalification and release boundary

This visual revision supersedes the SG-13 rendering-fix candidate for release
qualification. SG-11 and SG-12 source identities were updated to this runtime
commit, while owner gates remain `NOT_RUN`, closeout remains `NOT_READY`, and
`promotionAllowed` remains `false`. No R2 object was uploaded, the games-site
catalog and deployment were not changed, and Jira was not updated. Generated
WebGL output remains ignored and uncommitted.
