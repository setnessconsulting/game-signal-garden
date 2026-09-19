# Signal Garden — SG-11 human benchmark and playtest evidence

**Issue:** [GAME-289 / SG-11](https://setnessconsulting.atlassian.net/browse/GAME-289)
**Status:** protocol ready; human sessions `NOT_RUN`
**Scope:** five fresh-player desktop sessions against the existing first playable

This record turns the named reference games into observable evidence. It does
not copy their assets, code, branding, or protected content. The benchmark is
formative and directional; it is not a statistical release certification.

## Selected sample and time bars

SG-01 selected a sample of **five** first-time desktop players before testing.
The target is at least **four of five** players who can state the objective in
30 seconds and complete the route in under one minute after understanding the
gesture. Every session must recover from an invalid route and successfully
restart or quit. The sample size and thresholds are recorded in
`docs/sg-11-playtest-evidence.json` so a later result cannot silently change the
qualification bar.

Use a fresh browser session at 1920×1080 with the local nested preview or the
owner-approved candidate build. Do not coach, explain controls, or point at the
source before the first meaningful action. Record only anonymous session codes
(`P01` through `P05`), never names, contact details, ages, or other personal
information.

## Session procedure

1. Start the timer when the garden becomes interactive. Ask, “What do you think
   you are trying to do?” Record the time to an accurate objective statement.
2. Without explanation, observe the first meaningful action: a source-directed
   route attempt, camera pan, pause, or other deliberate interaction. Record
   its time and what the player tried.
3. Let the player route the signal. Record completion time, invalid-route count,
   and the exact recovery behavior after a miss or partial route.
4. During an active route, press Esc once and verify cancellation; press Esc
   again while idle to verify pause/resume. Switch focus away and return once,
   then record whether the player can recover without a reload.
5. Use Replay and record whether a second player can identify a known initial
   state. If the player chooses to quit, record whether the quit/restart path is
   clear; do not pressure a player to quit.
6. Ask for perceived clarity on a 1–5 scale and one short observation. Record
   findings using the rubric below, including severity, evidence, remediation,
   and the residual owner decision.

The existing [playtest script](playtest-script.md) is the operator checklist.
The machine-readable result file is the only place to enter session metrics.
The validator accepts an empty `sessions` array while the benchmark is
`NOT_RUN`; it rejects a claimed pass without complete evidence.

## Reference rubric

| Reference | Measurable question | Evidence to record |
| --- | --- | --- |
| Mini Metro | Can the player distinguish the objective, coral source, blue receiver, and gold route without coaching? Do valid and invalid routes produce readable feedback? | Objective wording, first meaningful action, invalid-route count, recovery observation, and severity if any state is misunderstood. |
| Dorfromantik | Does the small garden feel calm and visually organized? Is there distracting clutter or a hierarchy problem? | Perceived clarity score, composition observation, and a finding with evidence if clutter or pacing interferes. |
| The Room | Is the drag gesture discoverable? Does the trace and receiver response feel immediate and satisfying? | Time to first route, time to complete, tactile-response observation, and any remediation or residual decision. |

Finding severities are `blocker`, `high`, `medium`, `low`, or `note`.
Every finding must include:

- `id` and one rubric dimension;
- the session codes that provide evidence;
- a plain observation and severity;
- a remediation or an explicit `accepted`, `deferred`, or `not-a-bug`
  disposition; and
- the residual owner decision when the finding remains open.

Release-blocking findings (`blocker` or `high`) must be fixed or explicitly
dispositioned before SG-12 closeout. A green repository check does not replace
that human decision.

## Current result

No human sessions have been recorded yet. The evidence file intentionally has
`status: "NOT_RUN"`, an empty session list, and `ownerReview.status: "PENDING"`.
This is an honest qualification boundary: automated route tests and an agent
playing the build cannot stand in for a fresh human benchmark.

When sessions are complete, update the JSON record, run:

```text
python3 scripts/ci/validate_playtest_evidence.py
```

Then append the exact candidate build, browser, date, session count, threshold
result, findings, remediation, and owner decision here. Keep the original
protocol and threshold values unchanged.

## Boundaries

- This record does not qualify production CDN load, desktop foreground frame
  rate, browser working set, physical focus loss, screen-reader operation, or
  owner visual/audio approval.
- No R2 upload, games-site catalog change, deployment, or Jira status update is
  part of SG-11.
- Participant identity and private notes stay outside the repository.
