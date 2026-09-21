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
