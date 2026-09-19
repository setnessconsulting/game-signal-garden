# SG-10 clean-checkout record

Validated revision: `34122315f6f31718ffa616517495392be5c91a2b` (merged `main`)

This snapshot was checked before SG-10 implementation files were added. It is
the clean source baseline for the evidence lane; the SG-10 branch reruns the
same checks in GitHub Actions on every pull request and push.

| Check | Result |
| --- | --- |
| `git status --porcelain=v1 --untracked-files=all` | `PASS` — empty |
| Tracked generated output scan | `PASS` — no `Build/`, `Builds/`, or `Artifacts/` paths |
| SG-05 repository validator | `PASS` |
| Unity input tree identity | `PASS` — recorded in `docs/sg-10-build-identity.json` |
| Unity editor, WebGL build, and browser | `NOT_RUN` in this credential-free check |

The generated WebGL player remains ignored under `Builds/WebGL/`. A local
artifact verification must pass `--build-dir Builds/WebGL/Build` to
`scripts/ci/validate_release_evidence.py`; the hosted job intentionally does
not claim that lane.
