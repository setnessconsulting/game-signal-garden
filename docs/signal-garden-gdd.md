# Signal Garden — Game Vision, Game Bible, and first-slice GDD

**Scope authority:** [GAME-278](https://setnessconsulting.atlassian.net/browse/GAME-278) and its SG-01–SG-06 children, with the latest platform and UI decisions recorded in this repository.

**Implementation repository:** [setnessconsulting/game-signal-garden](https://github.com/setnessconsulting/game-signal-garden)

**Release destination:** https://games.setnessconsulting.com/signal-garden/play/
**Document status:** SG-06 UI/HUD handoff and implementation record layered onto the first playable slice; SG-09 qualification evidence, SG-10 credential-free release evidence, SG-11 human-benchmark protocol, and SG-12 closeout reconciliation are recorded separately in [the qualification matrix](sg-09-qualification-matrix.md), [the SG-10 evidence record](sg-10-ci-and-release-evidence.md), and [the SG-12 closeout record](sg-12-closeout-record.md). Jira statuses remain unchanged.

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
- **Asset pipeline:** the SG-01 diorama remains authored from deterministic Unity scene code, while the receiver visual now has the SG-05 Blender source → FBX → Unity prefab provenance chain. Later authored Blender assets follow the same FBX, hash, import identity, scale, coordinate, license/provenance, and human-modification record.
- **UI pipeline:** SG-06 uses a repository-owned normalized Figma/API-37 HUD handoff with a recorded file/version/node/component/token identity. The runtime implementation is a serialized UGUI Canvas; the project-figma-api `unity-ui-toolkit` consumer profile is an explicit exception because the existing runtime is UGUI and the UI Toolkit lane is not qualified.
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
- The UGUI HUD covers objective/start, active routing, invalid/recovery, verified completion, reset/replay, pause/resume, mute, and reduced-motion states. Buttons use automatic keyboard navigation with readable labels and desktop targets of at least 40 px.
- Phase text, shaped panels, and status copy accompany accent colors; no state depends on color or audio alone. The normalized snapshot and design tokens are in `SourceArt/Figma/SignalGardenHud/`, with the evidence classification in [docs/sg-06-ui-record.md](sg-06-ui-record.md).
- Reduced-motion toggle freezes the ambient source/receiver pulse, one-shot feedback pulses, and verified receiver rotation; phase text, route feedback, and completion state remain visible. Camera panning is direct, bounded, and has no inertia.
- No music. Original optional interaction, recovery, and success cues are generated in code, off by default, muteable, and adjustable from 0–100% volume. Visual and text feedback always communicate the same state; cue playback can fail without blocking routing or completion. See [the SG-08 feedback record](sg-08-feedback-record.md).
- English only. Rive is explicitly declined for v1; the existing Unity-native source/receiver pulse and verified receiver rotation remain, with reduced-motion behavior. No `.riv` asset or Rive runtime is in the v1 release dependency list; see [the SG-07 motion decision](sg-07-motion-decision.md). Live Figma plugin import and UI Toolkit consumption remain deferred; SG-06's normalized handoff and UGUI implementation are in scope. Blender production beyond the SG-05 receiver remains deferred.

### Quality bars

These are observable goals for the first playable, not claims of completed human qualification:

- **Objective clarity:** a first-time player can state the goal within 30 seconds without coaching.
- **Completion:** after understanding the gesture, a first-time player completes the route in under one minute.
- **Recovery:** every invalid, partial, canceled, or focus-interrupted route returns to a state from which another attempt can begin without restarting the page.
- **Performance:** target a visible-preview mean of at least 60 fps at an exact 1920×1080 render on the reference desktop; the qualification harness uses a five-second warmup and 30 one-second samples. The visual style favors clear geometry and stable frame time over effects.
- **Local load:** target 10 seconds or less to interactive in a local HTTP preview at 1920×1080 in desktop Chrome/Edge. Production CDN time is measured only after a reviewed release.
- **Formative playtest (SG-01 selection):** run five first-time desktop sessions before release. Record objective-understanding time, first meaningful action, completion time, route errors, recovery, restart/quit success, and perceived clarity. The slice target is at least four of five players understanding the objective within 30 seconds and completing within one minute, with recovery and restart/quit succeeding in every session; this small sample is directional rather than statistical release certification. The protocol and anonymous evidence record are in [SG-11](sg-11-human-benchmark.md).

### Reference-game rubric

These references set qualities to check, not designs to copy:

- **Mini Metro — route clarity and state feedback:** source, receiver, and intended corridor are distinguishable at a glance; a successful route visibly reaches the receiver; a miss names the recovery action.
- **Dorfromantik — calm pacing and compact composition:** one focused island, no timer, no fail state that pressures the player, and enough negative space to read the winding route.
- **The Room — tactile 3D interaction:** endpoint objects have distinct materials and silhouettes; dragging leaves a visible signal trace; the receiver responds immediately on success.

## First-release scope and exclusions

Included: one SignalGarden scene, one source, one receiver, one route, one short dead end, one player, desktop browser input, accessible status text, pause/cancel/recovery/reset, local automated rules tests, one repeatable playtest script, and a local WebGL build.

Excluded: persistent progression, multiple puzzles or a campaign, multiplayer, accounts, monetization, leaderboards, remote analytics, LevelBest integration, localization, music, Rive runtime and `.riv` production assets (declined for v1 in SG-07), live Figma plugin import, UI Toolkit consumption, and further Blender source production beyond the SG-05 receiver. Production release administration is recorded separately from the runtime scope in [the SG-16 production closeout](sg-16-production-closeout.md).

## Technical and release notes

The implementation consumes Unity and Unity package APIs directly in the game repository; it does not copy or reimplement project-unity-api, project-game-maker, or the games site. The local host-preview harness mirrors the games site's asset-base contract: loader/data/framework/WASM requests are constructed from <assetBase>/Build/<exact-filename>, even when the play page is below /signal-garden/play/. Brotli metadata, MIME types, filenames, and local build identity are recorded in local-build-evidence.md after a build is available.

The project-unity-api integration is not qualified or required for this first implementation pass; its current runtime/API qualification remains a separate evidence lane. The production R2 readback and games-site catalog promotion are recorded separately from this runtime document in [the SG-16 production closeout](sg-16-production-closeout.md). Formal owner qualification remains independent of that deployment record.
