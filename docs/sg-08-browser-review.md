# SG-08 local browser review

**Date:** 2026-09-18
**Build:** Unity `6000.6.0f1`; exact artifact identity and cache key are in [docs/local-build-evidence.md](local-build-evidence.md).
**Preview:** `http://127.0.0.1:4182/signal-garden/play/` with assets beneath `/game-assets/signal-garden/local/Build/`.

## Visual and interaction review

- The WebGL player loaded below the nested play path. The 1920×1080 reference render showed the coral source, gold winding route and blind spur, blue receiver shrine, objective/status panels, and upper-right sound, volume, and motion controls without cropping.
- Pressing Esc paused the garden and displayed the Resume and Replay actions; Esc resumed the game.
- The sound button changed the accessible status from muted to on and back to muted. The volume slider changed with the mouse. On the final build, Tab announced `CUE VOLUME 55%. Use the left and right arrow keys to adjust.` ArrowLeft changed the value to 45%, then ArrowRight restored 55%; both adjustments were announced.
- Reduced motion toggled on and off with an explicit live-region message.
- A mouse drag into the blind spur displayed the failure-specific recovery text and retained the attempted pink route. Try again returned the game to its initial observe state.
- Browser DOM accessibility exposed the game's status/live-region messages. No external screen reader was used. Route completion is covered by Unity PlayMode tests; this browser pass did not draw the complete winding route.
- No browser runtime errors appeared. The only warning was that the optional URP Edge Adaptive Spatial Upsampling shader was stripped for WebGL; the scene rendered normally.

## 1920×1080 performance status

The final visible in-app browser run produced an exact 1920×1080 render and completed all `30/30s` samples at a minimum of `74.6 fps` and an average of `86.9 fps` (`PASS`). The preview tab was opened visibly and inspected at the reference resolution. This verifies the local nested preview's performance target for this run; it is not an independent production CDN or desktop Chrome/Edge qualification.

An earlier unpresented in-app tab reported `50.2 min / 58.9 average fps`, and the in-app visibility capability reported `false` even while the page reported `visibilityState: visible`. A separate agent-controlled Chrome run read 1.0 fps under an unverified foreground/occlusion condition. Those measurements are retained as background/visibility caveats rather than representative foreground failures. The declared desktop browser qualification remains open until Chrome and Edge are tested in a verified foreground desktop session.
