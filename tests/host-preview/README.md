# Local host preview

Build the Unity WebGL player first. Then run:

    node tests/host-preview/serve.mjs --port 4173

Open http://127.0.0.1:4173/signal-garden/play/. The harness deliberately starts the play page below a nested path and supplies the same asset base shape as the games site: /game-assets/signal-garden/local/Build/<exact-file>.

Compressed Unity files are served with Content-Encoding: br and matching MIME types. The Unity loader, data, framework, and WASM names are read from the local Build directory and passed exactly to createUnityInstance. The helper does not copy files, upload anything, or modify the games site.
