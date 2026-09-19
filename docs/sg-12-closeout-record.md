# Signal Garden — SG-12 release-candidate closeout record

**Issue:** [GAME-290 / SG-12](https://setnessconsulting.atlassian.net/browse/GAME-290)
**Status:** closeout evidence assembled; `NOT_READY` for promotion
**Epic:** [GAME-278](https://setnessconsulting.atlassian.net/browse/GAME-278)

This is the final reconciliation layer for the first playable. It records the
exact runtime candidate, child-story evidence, technology decisions, known
limitations, and owner gates. It deliberately does not mark Jira issues Done,
upload anything to R2, change the games-site catalog, deploy, or treat local
evidence as an owner release approval.

## Candidate identity

The recorded WebGL candidate is the local build from runtime source commit
`34122315f6f31718ffa616517495392be5c91a2b`, identified by
`docs/sg-10-build-identity.json` (SHA-256
`6886B4CD48F3B5CB1ABBCAC3D983428DBF3F6159E5398E794FE2EC00999CA161`). Its
ordered loader, data, framework, and WASM files, exact hashes, Brotli encoding,
MIME types, and immutable R2 prefix pattern remain authoritative in that
manifest. The closeout record is based on the current merged `main`
`318647a4143f22cd4da4e16b6cb4d9bcea023eb4`. Earlier SG-12 evidence was
assembled against `c388226fb02b66ea4084a0e52d4ce97fe844d784`; this metadata
refresh preserves that evidence while making the current repository baseline
explicit. Neither documentation commit changes the Unity input tree or the
recorded runtime artifact.

The artifact location is **local only**. A future reviewed release will use
`signal-garden/<version>/Build/` in private R2 and the host-supplied
`<assetBase>/Build/<exact-filename>` pattern. No production object or catalog
entry exists as part of SG-12. An approved promotion will use the deterministic
Git-derived prefix
`signal-garden/<UTC-date>-<first-7-chars-of-runtime-source-commit>/Build/`.

## Qualification update — 2026-09-19

The existing local candidate was opened in a visible desktop Chrome session
(Chrome `153.0.8010.52`) through the nested host preview. At the exact
1920×1080 render, the harness completed 30/30 focused seconds with a `101.9`
fps mean and `27.6` fps minimum one-second sample (`PASS` under the mean-at-least
60 fps gate). The minimum is retained as an informational dip; it is not
converted into a sustained frame-rate failure. Escape produced the readable
Paused state and a second Escape restored the Ready state.

A warm-cache reload reached the readable Ready state in `681 ms`, below the
10-second local target. This is local warm-cache evidence only. It does not
qualify a cold load, independent Edge run, production CDN, browser working set,
physical focus loss, screen reader, or human benchmark.

Automated local headless diagnostics are recorded in the
[SG-12 local diagnostics record](sg-12-local-diagnostics.md). Fresh-profile
Chrome and Edge runs reached the exact `1920x1080` render with 30 samples and
mean frame rates of `60.99` and `60.88` fps, and local time to interactive of
`1,833.4 ms` and `1,533.5 ms`. Because these were headless CDP sessions, they
remain diagnostic and do not replace independently observed foreground browser
qualification.

The exact observation is recorded under `qualificationUpdates` in the
[release-candidate manifest](sg-12-release-candidate.json). The closeout
decision remains `NOT_READY` until the remaining owner and human gates are
resolved.

The owner-observed qualification procedure is in [the SG-12 runbook](sg-12-owner-qualification-runbook.md).
Its machine-readable gate record is `ownerQualification` in the closeout
manifest. The record starts at `NOT_RUN`, may advance to
`READY_FOR_OWNER_REVIEW` only after all required observations and findings are
complete, and cannot enable promotion until owner approval and Jira
reconciliation are recorded.

## Scope agreement

The public repository describes one single-player `SignalGarden` scene for
desktop Chrome/Edge, with mouse drag routing, WASD camera pan, Esc cancel/pause,
recovery, replay, mute, reduced motion, readable UGUI HUD state, and no network,
account, multiplayer, progression, or analytics dependency. The game repository
owns Unity source, tests, source-art provenance, and build evidence; the games
site owns the catalog and release pointer.

The machine-readable reconciliation is in
[`docs/sg-12-release-candidate.json`](sg-12-release-candidate.json). It is
validated by `scripts/ci/validate_closeout_evidence.py` and cross-checks the
SG-10 build identity and SG-11 benchmark record instead of duplicating their
artifact or session data.

## Child-story evidence and disposition

| Story | Repository evidence | Current disposition |
| --- | --- | --- |
| SG-01 through SG-04 | GDD, local build evidence, route/HUD/feedback records | Implemented evidence is recorded; Jira statuses remain unchanged. |
| SG-05 | Blender source, FBX, prefab, provenance manifest, editor validator | Chain is recorded; owner visual review remains open. |
| SG-06 | Normalized Figma handoff, UGUI HUD, browser review | UGUI exception is explicit; independent screen-reader review remains open. |
| SG-07 | Motion decision | Rive is declined for v1; Unity-native motion remains. |
| SG-08 | Feedback/audio record and browser review | Sound is optional and muted by default; human listening and owner review remain open. |
| SG-09 | Qualification matrix and build evidence | Local/automated evidence is recorded; desktop, accessibility, and human gates remain open. |
| SG-10 | Build identity, clean checkout, credential-free workflow | Repository/evidence lane passes; production and owner gates remain open. |
| SG-11 | Benchmark protocol and anonymous evidence manifest | `NOT_RUN`, zero sessions, owner review pending. No human result is claimed. |
| SG-12 | This record and the closeout manifest | `NOT_READY` until the listed owner and human gates are resolved. |

## Technology and provenance reconciliation

| Technology or dependency | v1 disposition | Evidence |
| --- | --- | --- |
| Unity 6000.6.0f1 + URP/Input System/UGUI | Used and versioned | SG-10 build identity |
| Blender 5.2.1 LTS | Used for the SG-05 receiver source chain | SG-05 asset record |
| Figma/API-37 | Normalized handoff recorded; UGUI runtime exception explicit | SG-06 UI record |
| Rive | Declined for v1; no package or `.riv` asset | SG-07 motion decision |
| GitHub Actions | Credential-free repository/evidence validation only | CI validation record |
| Sentry, Hermes, browser telemetry, LevelBest | Deferred; no silent dependency | Closeout manifest and GDD |

All authored geometry, code, UI handoff content, and runtime feedback are
repository-owned Setness Consulting work. No third-party game assets, code,
branding, or protected content are copied.

## Known limitations and owner gates

The following remain open and are not inferred from automated or in-app
evidence:

- five fresh-player sessions with objective, first-action, completion, error,
  recovery, restart/quit, clarity, and dispositioned finding records;
- independently observed foreground Chrome and Edge performance;
- time-to-interactive, browser-tab working set, physical focus loss, and
  external screen-reader operation;
- owner visual, audio, accessibility, and provisional 12 MiB / 512 MiB budget
  approval; and
- Jira Epic/story status reconciliation before any release promotion.

The closeout manifest keeps `releaseDecision.status` at `NOT_READY` and
`promotionAllowed` false until those gates have owner-backed evidence. A future
closeout revision may change that classification only after the evidence is
added and reviewed. Production R2, the games-site catalog, deployment, and
Jira statuses remain untouched by this branch.

## Verification commands

From a clean checkout, run:

```text
python3 scripts/ci/check_clean_checkout.py
python3 scripts/ci/validate_repository.py
python3 scripts/ci/validate_release_evidence.py
python3 scripts/ci/validate_playtest_evidence.py
python3 scripts/ci/validate_closeout_evidence.py
```

The closeout validator checks that the exact runtime build identity, SG-11
record, required evidence links, technology dispositions, provenance boundary,
and `NOT_READY` release decision remain internally consistent. It does not
claim Unity, browser, human, or production qualification.
