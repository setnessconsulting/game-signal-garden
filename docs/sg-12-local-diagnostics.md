# Signal Garden — SG-12 local diagnostics

**Issue:** [GAME-290 / SG-12](https://setnessconsulting.atlassian.net/browse/GAME-290)

This record captures automated local diagnostics that can be run before the
owner-observed qualification session. It does not advance
`ownerQualification.status`, does not replace foreground browser evidence, and
does not provide owner approval.

## Candidate and host

- Runtime source: `34122315f6f31718ffa616517495392be5c91a2b`
- Unity: `6000.6.0f1`
- Build identity: `docs/sg-10-build-identity.json`
- Candidate artifacts: the four SG-10 artifacts, with cache key `7201692b3f2f2f53`
- Local host: `http://127.0.0.1:4183`
- Nested route: `/signal-garden/play/`
- Diagnostic route: `/signal-garden/play/?sg-render=1920x1080&sg-stats=1`
- Render: exact `1920x1080`

The host served the frozen artifact identity from a clean SG-10 candidate
checkout. The loader returned `text/javascript; charset=utf-8`; the data file
returned `application/octet-stream` with Brotli; the framework returned
`text/javascript; charset=utf-8` with Brotli; and the WASM returned
`application/wasm` with Brotli.

## Automated results

Each browser used a fresh temporary profile and a local Chrome DevTools
Protocol session. The diagnostic waited for the page's existing interactive
signal, then collected the harness's five-second warmup and 30 one-second
samples at the exact reference render.

| Browser | Version | Mode | TTI | Mean FPS | Minimum one-second sample | Result |
| --- | --- | --- | ---: | ---: | ---: | --- |
| Chrome | 153.0.8010.52 | Headless CDP | 1,833.4 ms | 60.99 | 60.10 | PASS_DIAGNOSTIC |
| Edge | 153.0.4234.32 | Headless CDP | 1,533.5 ms | 60.88 | 59.70 | PASS_DIAGNOSTIC |

The local TTI observations are below the 10-second target. The Edge minimum
sample is retained as diagnostic information; the gate is the 30-sample mean.

## Classification and limits

The results are classified `AUTOMATED_LOCAL_HEADLESS_ONLY`. Headless browser
execution has no independently observed foreground window, so these results
remain diagnostic and do not populate the SG-12 owner gates. They do not
measure tab working set, physical focus interruption, screen-reader operation,
or fresh-player behavior. Production CDN timing remains a separate release
step. The manifest therefore remains `NOT_READY` with
`promotionAllowed: false` and all owner gates `NOT_RUN`.
