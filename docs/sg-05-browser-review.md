# SG-05 browser and visual review record

**Date:** 2026-09-17  
**Build:** Unity `6000.6.0f1`, local Brotli WebGL build from `codex/signal-garden-sg05`  
**Preview:** `http://127.0.0.1:4175/signal-garden/play/`

## Observed result

A fresh Chrome origin loaded the nested play route and reached the accessible ready state. The WebGL 2 canvas rendered the floating garden, coral source, gold trail and blind spur, and the imported turquoise receiver shrine. The browser console contained no runtime errors; Unity only reported optional URP post-processing shaders that are stripped on WebGL.

The local preview accepted a mouse drag at the source and displayed the live route. A drag that entered the blind spur resolved to the readable recovery message, “The blind spur has no receiver.” `Esc` displayed the pause overlay and a second `Esc` resumed the turn. The pause overlay’s `Restart this turn` control returned the state to `Fresh turn. Drag from the coral source to begin.` The standard complete route remains covered by the EditMode route rule and imported-prefab chain tests.

The review screenshot was captured from the fresh Chrome origin at a 1920-pixel-wide desktop viewport (the game canvas was 1920×797 in this browser shell); the editor render capture is retained at `Artifacts/SG-05/editor-render.png` for owner visual review. This is evidence for review, not owner approval.

## Separate gates

Human timing for objective clarity and completion, sustained 60 fps at 1920×1080, ten-second time to interactive, physical focus-loss interruption, and production CDN/R2 metadata remain separate qualification gates. No production object, catalog entry, deployment, or Jira status was changed.
