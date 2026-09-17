# Signal Garden SG-05 — receiver shrine asset record

**Issue:** [GAME-283 / SG-05](https://setnessconsulting.atlassian.net/browse/GAME-283)  
**Branch:** `codex/signal-garden-sg05`  
**Asset ID:** `signal-garden.receiver-shrine`  
**License/provenance:** Original Setness Consulting work; no third-party content.

This record binds the static receiver visual through the production chain:

```text
SourceArt/Blender/SignalGardenReceiver/SignalGardenReceiver.blend
  -> Assets/Art/SignalGarden/SignalGardenReceiver.fbx
  -> Assets/Art/SignalGarden/SignalGardenReceiver.prefab
  -> Assets/Scenes/SignalGarden.unity / Blue Receiver
```

## Source and export

- Authoring script: `SourceArt/Blender/SignalGardenReceiver/create_receiver_shrine.py`
- Blender: `5.2.1 LTS`, build hash `9e2066aef7ef`
- Source scene: `SignalGardenReceiver`, metric units, scale length `1.0` meters
- Source file: `106,993` bytes, SHA-256 `8D008A257324682A891DDC69DBE046BAD41DE34BCD817FF417ED79DC74B82D6A`
- Runtime export: FBX, `53,116` bytes, SHA-256 `A1EA560763D1ED19DC5078E9960CB05CC2EF4BBDFC79E08D433FF7A261D1D168`
- FBX options: mesh selection only; mesh modifiers applied; unit scale applied; `FBX_SCALE_ALL`; `axis_forward=-Z`; `axis_up=Y`; no animation or leaf bones; custom properties kept; textures not embedded
- Coordinate convention: Blender right-handed Z-up source, exported with the documented `-Z` forward / `Y` up conversion for Unity
- Geometry: 1,024 triangles, 3 material slots, no textures, no rig, animation, or embedded collision
- Budgets: at most 5,000 triangles, 4 material slots, 1,024px textures, 10 MB source, 2 MB FBX
- Naming and transforms: mesh objects use `SM_`; materials use `MAT_`; scale and rotation are applied in Blender; every mesh has a UV map

## Unity import and scene consumption

- FBX GUID: `63f683010e9b41b42b041a64ed0de3af`
- Prefab: `Assets/Art/SignalGarden/SignalGardenReceiver.prefab`
- Prefab GUID: `f099b6efa9b18a14381474f8d29e8bb8`
- Importer: scale factor `1`, file scale enabled, mesh compression `Off`, read/write disabled, animation/cameras/lights/colliders disabled, authored hierarchy preserved
- Existing URP material mapping: Base → `Receiver-Pedestal`; Crystal → `Receiver-Sea-Glass`; inlay/crown/fins → `Trail-Thread-Gold`
- The generated scene keeps the `Blue Receiver` marker, receiver light, route endpoint, and gameplay references. The prefab root is the `SignalGardenReceiver` child under that marker.
- `SignalGarden.Editor.SignalGardenAssetProvenanceEditor.ValidateSignalGardenAssetProvenance` is the editor/CLI gate. `BuildWebGL` invokes the same gate and fails closed when the chain is missing or stale.

## API-10 evidence

API-10 was consumed read-only from `project-blender-api` at commit
`9783b64379c5bb3220cabda7018056d3fbef2a0c` using the `prop` validation profile.

- Scene inspection: `PASS`; source unchanged before/after inspection
- `asset validate --profile prop`: `PASS` (0 errors, 0 warnings)
- Auxiliary GLB export: `PASS`, `Artifacts/SG-05/SignalGardenReceiver.glb`, 77,812 bytes, SHA-256 `B941FCE8F428B32000A8DFaf7F47F593080CFD8774A23FA8FCF327E0C248FCFC`
- Handoff manifest: `PASS`, `release_ready=true`; the GLB is an API-10 validation artifact only. The Unity runtime artifact remains the FBX.
- Machine-readable command strings and evidence paths are in `SignalGardenReceiver.provenance.json`.

## Review boundary

The asset is static and intentionally keeps the SG-01 interaction rules unchanged. Human visual approval, 1920×1080 frame-rate timing, browser focus-loss qualification, WebGL CDN metadata, production R2 upload, site catalog promotion, deployment, and Jira status changes remain separate review gates.
