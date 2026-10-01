# Local host preview

Build the Unity WebGL player first. Then run:

    node tests/host-preview/serve.mjs --port 4173

Open http://127.0.0.1:4173/signal-garden/play/. The harness deliberately starts the play page below a nested path and supplies the same asset base shape as the games site: /game-assets/signal-garden/local/Build/<exact-file>.

Compressed Unity files are served with Content-Encoding: br and matching MIME types. The Unity loader, data, framework, and WASM names are read from the local Build directory and passed exactly to createUnityInstance. The helper does not copy files, upload anything, or modify the games site.

The preview calculates a SHA-256 cache key from the exact four WebGL artifacts and adds it to each Unity asset URL and product version. Rebuilt files therefore cannot reuse an older Unity browser-cache entry at the same local path.

For the 1920×1080 composition and sustained frame-rate check, open
`http://127.0.0.1:4173/signal-garden/play/?sg-render=1920x1080&sg-stats=1`.
This local-only mode letterboxes the scene to 16:9, asks Unity for an exact
1920×1080 canvas buffer, waits for a five-second visible/focused warmup, then
collects 30 one-second frame-rate samples. The sample pauses while the page is
hidden or unfocused and passes only when the buffer is 1920×1080 and the
30-second mean is at least 60 fps. The minimum one-second sample remains in the
readout to show brief dips without treating normal frame timing variation as a
sustained failure. Optional `sg-warmup-seconds` and
`sg-sample-seconds` query parameters shorten checks during harness development;
leave their defaults for release evidence. This diagnostic does not affect the
game build or production site.

For a local-only route smoke, open
`http://127.0.0.1:4173/signal-garden/play/?sg-render=1920x1080&sg-smoke=route`.
After the Unity canvas is interactive, the harness waits for a tester to make a
real browser pointer drag through the gold route and records
`window.signalGardenSmokeResult`. A passing result proves the local
`canvas -> Observe -> Verified` path; it does not add a production API or
auto-solve behavior to the game.

## Foreground qualification driver

`qualify.mjs` performs the desktop foreground checks that a tester would otherwise
have to run by hand. It uses only the Node standard library and drives a real,
non-headless browser window through the Chrome DevTools Protocol.

    $env:SG_BROWSER = "C:\Program Files\Google\Chrome\Application\chrome.exe"
    node tests/host-preview/qualify.mjs --url="http://127.0.0.1:4173/signal-garden/play/?sg-render=1920x1080&sg-stats=1&sg-smoke=route" --out=chrome.json --label="Chrome-desktop-foreground"

Point `SG_BROWSER` at `chrome.exe` or `msedge.exe`. The driver waits until the page is
genuinely visible and focused, records readiness and time-to-interactive, drives the
real pointer path from coral to blue through the documented route, and then reads the
harness frame-rate result. It also records WebGL state, console output, failed
requests, and the accessibility tree, and separates `/favicon.ico` noise from real
failures.

Useful flags:

* `--invalid-route` drives the blind dead-end spur to observe Recovery behaviour.
* `--skip-drag` omits the pointer route.
* `--cdp-port=<n>` avoids a port collision.

Two behaviours are worth knowing. The harness only advances a sample while the page
is active, so a completed 30-sample run is itself evidence of continuous visible and
focused running. If the window cannot hold focus under automation the driver reports
the sample as not-run and records how many polls were unfocused, rather than
reporting a failure. The driver records observations only; `scripts/ci/` validators
and owner qualification decide PASS/FAIL.
