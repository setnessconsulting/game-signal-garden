# SG-06 UI/HUD handoff record

**Issue:** [GAME-284 / SG-06](https://setnessconsulting.atlassian.net/browse/GAME-284)
**Repository:** [setnessconsulting/game-signal-garden](https://github.com/setnessconsulting/game-signal-garden)
**Runtime:** Unity `6000.6.0f1`, UGUI `2.6.0`, WebGL at `/signal-garden/play/`
**Branch:** `codex/signal-garden-sg06`

## Delivered slice

The first playable scene now presents its deterministic `Observe → Routing → Recovery → Verified → Paused` loop through a serialized UGUI Canvas. The route rules, mouse drag, WASD camera pan, Esc cancel/pause, focus-loss recovery, and `GardenRunState` transitions remain unchanged.

The scene contains the following reachable controls and states:

| Jira state | Unity presentation | Player action |
| --- | --- | --- |
| Objective / start | `Objective Panel` and `OBSERVE / FIND THE PATH` | Read the source-to-receiver instruction and begin at the coral source |
| Active routing | `ROUTING / HOLD TO TRACE` and live status text | Hold the mouse and trace the gold stones |
| Invalid / recovery | `RECOVERY / TRY AGAIN`, specific status copy, `Try Again Button` | Start a fresh attempt at the coral source |
| Verified completion | `State Overlay`, `The garden is awake.`, `Play again` | Return to a fresh turn for the next player |
| Reset / replay | `Restart this turn` or `Play again` | Clear the line and reset the turn without a reload |
| Pause / resume | `State Overlay`, `Paused`, `Resume / Esc` | Resume or restart safely |
| Mute | `Sound cues: Off/On` | Toggle optional feedback cues; off is the default |
| Reduced motion | `Motion: Full/Reduced` | Freeze ambient pulses while keeping state feedback immediate |

The persistent status copy is also announced through the existing WebGL `#signal-garden-accessible-status` live region (`role=status`, `aria-live=polite`, `tabindex=0`). Spatial route tracing remains visual by design. Every state has an uppercase phase label, readable status text, and shaped panel/action so state does not rely on color or sound alone.

## Design handoff and provenance

The repository-owned handoff is intentionally normalized and contains no private Figma payload:

- [HUD snapshot](../SourceArt/Figma/SignalGardenHud/SignalGardenHud.snapshot.json)
- [Design tokens and component record](../SourceArt/Figma/SignalGardenHud/SignalGardenHud.design-system.json)
- [API-37 export manifest](../SourceArt/Figma/SignalGardenHud/SignalGardenHud.export-manifest.json)
- [API-37 Unity handoff manifest](../SourceArt/Figma/SignalGardenHud/SignalGardenHud.handoff.json)
- [Machine-readable provenance](../SourceArt/Figma/SignalGardenHud/SignalGardenHud.provenance.json)

The snapshot identifies file key `fixture-signal-garden-sg06-hud-001`, version `1`, HUD node `1:1`, four component identities, the eight bounded state frames, focus order, typography, spacing, color tokens, contrast intent, and the 40 px minimum interactive target. The design reference viewport is `1920×1080`; runtime buttons are 52–56 px high.

This is an offline authored fixture because no Figma connector or credential was available for this implementation. It is evidence of the design contract and is not presented as a live Figma file. The fixture is original Setness Consulting work and remains subject to owner visual review.

## API-37 validation

The read-only `project-figma-api` checkout was `914d8891c87d213f2f964d374ca30ea0da732d4a` (`figma-api 0.1.2`). The `hud` profile validation passed with zero blocking findings. The profile reports contrast calculation as unsupported for colors that are not determinate in the normalized node payload; token values and contrast intent are recorded separately in the design-system file.

The deterministic fixture export produced ten hashed PNG placeholder records, and the API handoff command produced a `manifest_only` Unity handoff. The handoff still names the current API-29/API-30 UI Toolkit consumer blockers; it is retained for traceability and is not used to claim a UI Toolkit import.

The live lane was attempted with `figma-api doctor --live` and classified **BLOCKED** because no Figma credential was present in Credential Manager or `FIGMA_*` environment variables. Live REST, plugin, MCP, and Unity UI Toolkit read-back evidence was not inferred from the fixture.

## UGUI exception

The project-figma-api handoff profile is named `unity-ui-toolkit`, but SG-06 uses a UGUI Canvas. This is an explicit exception because the existing Signal Garden runtime is already UGUI-based and the UI Toolkit lane is not qualified for this project. The normalized snapshot remains the design source of truth; `SignalGardenHud` is the serialized runtime implementation.

The scene root `Signal Garden HUD` contains:

- `Canvas` (`ScreenSpaceOverlay`, sorting order `50`)
- `CanvasScaler` with reference resolution `1920×1080`
- `GraphicRaycaster`
- `SignalGardenHud` with serialized game, objective, status, retry, sound, motion, and modal references
- An `EventSystem` with `InputSystemUIInputModule`, visible selected-button tint, and explicit `Tab` / `Shift+Tab` cycling in the documented order; the live region announces the focused action

Phase changes select the recovery button or modal primary button when one is reachable. Button clicks suppress the next pointer release so a UI action cannot become an accidental route start.

## Boundaries and next issue

This record does not qualify sustained 60 fps, human objective/completion timing, physical focus-loss behavior, production CDN load, or owner visual approval; those remain SG-09/SG-11 and release gates. The SG-07 decision to decline Rive for v1 is recorded in [docs/sg-07-motion-decision.md](sg-07-motion-decision.md); SG-08 feedback polish follows. No R2 object, games-site catalog entry, deployment, or Jira status was changed.
