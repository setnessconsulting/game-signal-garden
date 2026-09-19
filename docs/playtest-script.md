# Signal Garden — fresh-player playtest

Use this script with a fresh desktop browser session at 1920×1080. Do not coach the player. Clear the route and restart the scene before each participant. The selected formative sample is five first-time desktop players; the target is at least four of five meeting both time bars, with recovery and restart/quit succeeding in every session. This script records evidence but is not statistical release certification. Enter only anonymous session codes in the [SG-11 evidence record](sg-11-playtest-evidence.json).

## Record

For each session, note the browser and resolution, time to state the objective, time to first meaningful action, time to completion, invalid-route count, recovery outcome, restart/quit outcome, pause/focus behavior, replay choice, and perceived clarity from 1–5. Record the player's exact objective wording and short observations. Do not collect names or other personal information.

## Steps

1. Open /signal-garden/play/ and start the game. Start the timer when the garden first becomes interactive.
2. Ask: “What do you think you are trying to do?” Do not explain the controls. Record the time and exact understanding.
3. Let the player act. Observe whether they discover that the route begins at the coral source and ends at the blue receiver. Record the first meaningful action, even if it is a camera pan or pause rather than a route.
4. After a first success or after 45 seconds, ask the player to try the short side spur. If they do not find it, point at the branch without explaining the recovery.
5. During a route, press Esc once to check that the partial line cancels safely. Press Esc again to resume.
6. Start a route and switch focus to another window or tab. Return and check that the partial route is canceled with a clear retry instruction.
7. Use replay. Confirm the objective returns and the next player can begin from a known state. If the player chooses to quit, record whether the restart/quit path is clear.
8. Ask for a 1–5 perceived-clarity score and one short observation. Classify any finding against the Mini Metro, Dorfromantik, or The Room rubric with severity, evidence, remediation, and residual owner decision.
9. Toggle sound cues and reduced motion. Confirm both settings are legible and have immediate effect.

## Expected outcome

- The player can describe “connect the coral source to the blue receiver along the gold trail” within 30 seconds.
- Once the gesture is understood, the player completes a valid route in under one minute.
- An invalid, partial, canceled, or focus-interrupted attempt explains how to recover and accepts a new attempt without reloading.
- Success visibly activates the receiver. Replay restores the initial state.
- Esc cancels during a drag and pauses/resumes when not dragging. WASD moves the bounded camera only.
- The cursor remains visible, and game status is available through the focusable screen-reader live region.

The full SG-11 rubric, threshold values, and current qualification status are in [docs/sg-11-human-benchmark.md](sg-11-human-benchmark.md). Automated route tests and agent activity do not count as fresh-player sessions.
