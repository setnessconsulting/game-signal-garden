# Signal Garden — Game Vision, Game Bible, and first-slice GDD

**Scope authority:** [GAME-278](https://setnessconsulting.atlassian.net/browse/GAME-278) and its first four children, with the latest platform decision recorded in this repository.

**Implementation repository:** [setnessconsulting/game-signal-garden](https://github.com/setnessconsulting/game-signal-garden)

**Release destination:** https://games.setnessconsulting.com/signal-garden/play/
**Document status:** SG-01 scope record for the first playable slice. Jira statuses remain unchanged.

## Vision

Signal Garden is a quiet, tactile puzzle about noticing a living signal and giving it a path through a tiny world. The player should understand the goal by looking at the diorama, make one deliberate gesture, and see the garden answer.

The first release is a single-player browser game. A parent and child can take turns on the same computer. Each turn is self-contained; there is no shared session, account, network service, or saved progression.

## Game Bible

### Promise and loop

**Observe → route → verify.** Notice the coral source, the blue receiver, and the warm stone trail between them. Drag one continuous signal route from the source to the receiver. The receiver lights when the route follows the garden trail. If the line misses the trail, release early, or enters the blind spur, the attempt gives a specific recovery prompt and the player can try again immediately.

### Player and platform

- **Primary player:** a first-time desktop player who receives no verbal instructions.
- **Shared-computer use:** two people may take turns; only one player acts at a time.
- **Platform:** Unity WebGL in desktop Chrome or Edge at https://games.setnessconsulting.com/signal-garden/play/.
- **Reference display:** 1920×1080 desktop; responsive layout keeps the board and controls readable at nearby desktop sizes.
- **Input:** left mouse drag draws a route; WASD pans the bounded camera when not drawing; Esc cancels a route or pauses/resumes. The pointer remains visible and is not locked. A route begins only at the source.
- **Runtime:** Unity 6000.6.0f1, URP 17.6.0, Input System 1.19.0, UGUI 2.6.0, Test Framework 1.8.0; WebGL build with Brotli compression. Exact resolved versions are in Packages/packages-lock.json.
- **Asset pipeline:** first-slice geometry and materials are original procedural Unity assets. Later authored Blender assets use FBX and will record source, export hash, import identity, scale, coordinate convention, license/provenance, and human-modification status under SG-05.
- **Distribution:** the game repository owns Unity source and generated builds. Release builds use immutable private-R2 prefixes signal-garden/<version>/Build/; the site supplies an asset base and exact artifact filenames. Generated builds are not checked into Git or the site repository.

### One-scene challenge

SignalGarden contains one coral source, one blue receiver, one winding gold-stone route, and one short, visible dead-end spur. The camera presents a small floating garden as a calm low-poly diorama with a clear horizontal route silhouette, warm/cool endpoint contrast, and restrained signal feedback. The dead end is a recoverable hint, not a penalty or a new level.

The interaction model has explicit, serializable source identity, receiver identity, route points, phase, attempt count, verification count, and reset count. Legal phases are:

| Phase | Entry and behavior | Next phase |
| --- | --- | --- |
| Observe | Objective is visible; route input begins only at the source. | Routing or Paused |
| Routing | Mouse points are sampled on the board and checked against the route corridor. | Verified, Recovery, or Paused |
| Recovery | A specific retry instruction is visible; a new route can begin at the source. | Routing or Paused |
| Verified | Receiver and garden react; replay returns the challenge to its initial state. | Observe or Paused |
| Paused | Input and camera movement stop; resume restores the preceding phase. | Previous phase |

Invalid, partial, canceled, or focus-interrupted routes never require a page reload. Esc cancels the current route; outside an active drag it toggles pause. Browser focus loss cancels a partial route and leaves a clear restart instruction. The cursor remains visible in all phases. Reset clears route, attempts, and verification state. There is no disk persistence or network dependency.

### Presentation and accessibility

- Stylized, original, compact 3D diorama; readable silhouettes and contrast carry the puzzle.
- Objective, controls, status, pause, and replay text are presented in the game and mirrored into a focusable polite live region for screen readers. The spatial route itself remains visual.
- Reduced-motion toggle freezes the small ambient source/receiver pulse; camera panning is direct, bounded, and has no inertia.
- No music. Optional success and recovery tones are off by default and can be muted or enabled in play.
- English only. No Rive dependency in this slice. No Figma or Blender integration is required to begin SG-02–04.

### Quality bars

These are observable goals for the first playable, not claims of completed human qualification:

- **Objective clarity:** a first-time player can state the goal within 30 seconds without coaching.
- **Completion:** after understanding the gesture, a first-time player completes the route in under one minute.
- **Recovery:** every invalid, partial, canceled, or focus-interrupted route returns to a state from which another attempt can begin without restarting the page.
- **Performance:** target 60 fps at 1920×1080 on the reference desktop; the visual style favors clear geometry and stable frame time over effects.
- **Local load:** target 10 seconds or less to interactive in a local HTTP preview at 1920×1080 in desktop Chrome/Edge. Production CDN time is measured only after a reviewed release.
- **Formative playtest:** run five first-time desktop sessions before release. Record each objective-understanding time, completion time, route errors, recoveries, pauses, and replay. The slice target is at least four of five players understanding and completing within the time bars, with every invalid route recoverable; this small sample is directional rather than statistical release certification.

### Reference-game rubric

These references set qualities to check, not designs to copy:

- **Mini Metro — route clarity and state feedback:** source, receiver, and intended corridor are distinguishable at a glance; a successful route visibly reaches the receiver; a miss names the recovery action.
- **Dorfromantik — calm pacing and compact composition:** one focused island, no timer, no fail state that pressures the player, and enough negative space to read the winding route.
- **The Room — tactile 3D interaction:** endpoint objects have distinct materials and silhouettes; dragging leaves a visible signal trace; the receiver responds immediately on success.

## First-release scope and exclusions

Included: one SignalGarden scene, one source, one receiver, one route, one short dead end, one player, desktop browser input, accessible status text, pause/cancel/recovery/reset, local automated rules tests, one repeatable playtest script, and a local WebGL build.

Excluded: persistent progression, multiple puzzles or a campaign, multiplayer, accounts, monetization, leaderboards, remote analytics, LevelBest integration, localization, music, Rive, Figma integration, Blender source production, and production upload/catalog promotion.

## Technical and release notes

The implementation consumes Unity and Unity package APIs directly in the game repository; it does not copy or reimplement project-unity-api, project-game-maker, or the games site. The local host-preview harness mirrors the games site's asset-base contract: loader/data/framework/WASM requests are constructed from <assetBase>/Build/<exact-filename>, even when the play page is below /signal-garden/play/. Brotli metadata, MIME types, filenames, and local build identity are recorded in local-build-evidence.md after a build is available.

The project-unity-api integration is not qualified or required for this first implementation pass; its current runtime/API qualification remains a separate evidence lane. The production R2 bucket and site catalog are intentionally not modified by this work.
