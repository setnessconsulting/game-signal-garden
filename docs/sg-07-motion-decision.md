# SG-07 motion and Rive decision

**Decision:** Rive is declined for Signal Garden v1. The game keeps its existing Unity-native ambient pulse and verified receiver rotation. No `.riv` asset, Rive Unity runtime, or Rive bridge belongs in the v1 release dependency list.

- Jira: [GAME-285 / SG-07](https://setnessconsulting.atlassian.net/browse/GAME-285)
- Reviewed base: `6fb79d4638bdd069c6d5ad5bdbb829f09f862371` (SG-06 merge commit)
- Target: single-player Unity WebGL scene at `/signal-garden/play/`
- Decision scope: animation middleware for the v1 game; this does not change the game's motion behavior or content scope.

## Why Rive is declined

The one-scene v1 needs simple source and receiver feedback, not a separate authored state machine. Existing Unity-native effects already provide that feedback and obey the reduced-motion setting. Adding Rive would add an authoring and runtime dependency, asset import, state-machine contract, WebGL compatibility and build surface, and another behavior to qualify without meeting an unmet v1 acceptance criterion.

This is a production-game decision. `project-rive-api` remains a separate tooling repository; it is not a Signal Garden runtime dependency or a source of production game assets.

## Existing Unity-native motion

`Assets/Scripts/SignalGardenGame.cs` owns the current effects in `UpdatePulse()`:

| Effect | Existing behavior | Reduced motion |
| --- | --- | --- |
| Source light | Ambient intensity pulses with unscaled time | Holds a steady intensity |
| Receiver light | Pulses and gains intensity after verification | Holds a steady intensity while retaining state feedback |
| Receiver shrine marker | Rotates slowly after verification | Rotation is suppressed |

`Update()` continues to update presentation in the existing game phases. The reduced-motion preference is already represented by the UGUI control and does not depend on Rive. Route rules, phase transitions, input, pause, reset, replay, and HUD behavior are unchanged by this decision.

## V1 release dependency and asset disposition

- Motion uses the existing Unity C# implementation and Unity render pipeline; no external animation runtime is required.
- `Packages/manifest.json` and `Packages/packages-lock.json` contain no Rive package.
- `Assets/` contains no `.riv` asset, Rive component, or Rive integration bridge.
- No Rive file, runtime package, web module, remote runtime fetch, or production asset is included in the v1 release.
- No dependency removal from `Packages/manifest.json` was necessary: inspection found no previously declared Rive dependency to remove. The release disposition is now explicit here and in the Game GDD.

Rive runtime qualification and `.riv` read-back are `NOT_RUN` for this game because Rive is declined and no game asset or runtime is selected. This is not a Rive compatibility or runtime qualification result.

## Verification

- Unity `6000.6.0f1` clean-worktree import and script compilation completed; EditMode passed `24/24` and PlayMode passed `1/1`.
- The existing SG-06 runtime rendered at `/signal-garden/play/` in the in-app browser at `1920×1080`; the visible 30-sample readout showed `60.0 fps` minimum and `60.1 fps` mean. SG-07 changes no runtime files, so this is a smoke check of the unchanged build, not a new WebGL build qualification.
- `git diff --check` passed.

## Acceptance and boundaries

- The decision is explicitly recorded as **Rive declined for v1**.
- Unity-native effects remain sufficient for the current receiver/status feedback; reduced motion freezes ambient pulsing and suppresses receiver rotation.
- Rive is explicitly excluded from the v1 release dependency list, with no Rive package or asset left in the game project.
- This documentation-only slice does not alter the scene, add puzzle content, change gameplay, or alter the 1920×1080 / 60 fps performance target.
- SG-08 / GAME-286 is the next bounded issue: polish success and recovery visuals and optional cues within the existing native motion and audio decisions.

## Evidence inspected

- `Assets/Scripts/SignalGardenGame.cs`: `Update()` and `UpdatePulse()`
- `Assets/Scripts/SignalGardenHud.cs`: existing reduced-motion control
- `Packages/manifest.json` and `Packages/packages-lock.json`: no Rive package
- `Assets/` and `Packages/`: no `.riv` asset or Rive Unity bridge
- Jira acceptance criteria: [GAME-285](https://setnessconsulting.atlassian.net/browse/GAME-285)
