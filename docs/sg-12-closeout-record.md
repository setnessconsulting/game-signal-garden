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

The recorded WebGL candidate is the visual-refinement local build from runtime
source commit `1b3586f26f0c81410cbbbe33fc908e2dee2f1079`, identified by
`docs/sg-10-build-identity.json` (SHA-256
`8B882A25376564B9706A2D89710A79C43F2B5721A0391C618FB41CBE81925B12`). Its
ordered loader, data, framework, and WASM files, exact hashes, Brotli encoding,
MIME types, and immutable R2 prefix pattern remain authoritative in that
manifest. The closeout record is based on the current merged `main`
`318647a4143f22cd4da4e16b6cb4d9bcea023eb4`. Earlier SG-12 evidence was
assembled against `c388226fb02b66ea4084a0e52d4ce97fe844d784`; this metadata
refresh preserves that evidence while making the current repository baseline
explicit. The WebGL compatibility replacement is recorded in [the SG-13 rendering
fix record](sg-13-webgl-rendering-fix.md), and the current authored presentation
change is recorded in [SG-14](sg-14-visual-polish.md), and the current refinement
is recorded in [SG-15](sg-15-visual-audit.md). The visual changes the Unity input
tree and produces a replacement data bundle; the prior
qualification observations therefore require a fresh run.

The artifact location is **local only**. A future reviewed release will use
`signal-garden/<version>/Build/` in private R2 and the host-supplied
`<assetBase>/Build/<exact-filename>` pattern. No production object or catalog
entry exists as part of SG-12. An approved promotion will use the deterministic
Git-derived prefix
`signal-garden/<UTC-date>-<first-7-chars-of-runtime-source-commit>/Build/`.

## Qualification update — 2026-09-20

The earlier local browser and timing observations were made against the
superseded blank-world candidate. They remain in [the historical diagnostics
record](sg-12-local-diagnostics.md) for traceability but are marked
`SUPERSEDED` in the machine-readable closeout manifest. The replacement
candidate has passed the local rendering and route smoke checks documented in
[SG-15](sg-15-visual-audit.md); all performance and owner gates must be rerun
against it.

The replacement was opened in the local nested host preview at the exact
`1920x1080` render. The full diorama rendered, and a source-to-receiver drag
reached the readable verified state and offered replay. This is a visual and
functional smoke check only. The earlier frame-rate, timing, and headless
diagnostic observations belong to the superseded candidate and are not carried
forward. Fresh focused Chrome and Edge samples, cold and warm timing, and all
owner gates must be recorded against the replacement.

The replacement observation is recorded under `qualificationUpdates` in the
[release-candidate manifest](sg-12-release-candidate.json) with status
`SUPERSEDED` for the prior measurements. The closeout decision remains
`NOT_READY` until the replacement's owner and human gates are resolved.

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
