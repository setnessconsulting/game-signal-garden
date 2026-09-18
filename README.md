# Signal Garden

Signal Garden is a small, single-player Unity game about routing a living signal through a pocket-sized garden. Two people can take turns on the same computer; the game has no multiplayer, account, network, or progression system.

## First playable

The first playable is one WebGL scene, SignalGarden, built for desktop Chrome and Edge. A player drags a signal from the coral source to the blue receiver along the garden's gold trail. A short blind spur tests recovery. WASD pans the bounded camera; Esc cancels an active route or pauses/resumes the game. The cursor stays visible and is never locked.

The playable target is hosted at https://games.setnessconsulting.com/signal-garden/play/. This repository owns the Unity project, source, tests, and build evidence. The games site owns the catalog and release pointer.

## Open in Unity

1. Install Unity 6000.6.0f1 with the WebGL Build Support module.
2. In Unity Hub, add this repository directory as a project and open it with that editor.
3. Run **Signal Garden → Generate First Playable Scene** once to save the authored diorama, then open `Assets/Scenes/SignalGarden.unity` and press Play. The WebGL build method generates this scene automatically if it is missing.

The receiver visual is the SG-05 production chain. The Blender source and repeatable authoring script live under `SourceArt/Blender/SignalGardenReceiver/`; Unity consumes `Assets/Art/SignalGarden/SignalGardenReceiver.fbx` through the tracked prefab at `Assets/Art/SignalGarden/SignalGardenReceiver.prefab`. Run **Signal Garden → Validate SG-05 Asset Provenance** before review or a build. The validator checks the source/export hashes, Unity metadata and importer settings, naming/UV/budget rules, and prefab placement under `Blue Receiver`.

The SG-06 HUD is a serialized UGUI Canvas under `Signal Garden HUD`. Run **Signal Garden → Install UGUI HUD in SignalGarden Scene** to regenerate the authored Canvas bindings. The normalized Figma/API-37 handoff, design tokens, and explicit UGUI exception are recorded in [docs/sg-06-ui-record.md](docs/sg-06-ui-record.md) and `SourceArt/Figma/SignalGardenHud/`.

## Build and test

Run these from PowerShell after Unity has opened the project once and resolved packages:

    $unity = "C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe"
    $project = (Get-Location).Path
    & $unity -batchmode -projectPath $project -runTests -testPlatform EditMode -testResults "$project\Temp\editmode-results.xml" -logFile "$project\Temp\editmode.log"
    & $unity -batchmode -projectPath $project -runTests -testPlatform PlayMode -testResults "$project\Temp\playmode-results.xml" -logFile "$project\Temp\playmode.log"
    & $unity -batchmode -quit -projectPath $project -executeMethod SignalGarden.Editor.SignalGardenProjectSetup.BuildWebGL -logFile "$project\Temp\webgl-build.log"

The WebGL build is written under ignored Builds/WebGL/. Never commit the generated build. The tests/host-preview harness serves the build at /signal-garden/play/ and uses the same /game-assets/signal-garden/<version>/Build/ asset-base shape as the games site. See docs/local-build-evidence.md for the most recent local build identity and checks.

GitHub Actions runs credential-free repository checks on pull requests. See [docs/ci-validation.md](docs/ci-validation.md) for the hosted coverage and the Unity/WebGL checks it classifies as `NOT_RUN`.

## Scope and release contract

- [Game vision and GDD](docs/signal-garden-gdd.md)
- [SG-05 receiver asset record](docs/sg-05-asset-record.md)
- [SG-06 UI/HUD handoff record](docs/sg-06-ui-record.md)
- [SG-07 motion and Rive decision](docs/sg-07-motion-decision.md)
- [SG-08 feedback and audio record](docs/sg-08-feedback-record.md)
- [SG-09 qualification matrix](docs/sg-09-qualification-matrix.md)
- [Fresh-player playtest script](docs/playtest-script.md)
- [GAME-278 Epic](https://setnessconsulting.atlassian.net/browse/GAME-278)
- [GAME-279 SG-01](https://setnessconsulting.atlassian.net/browse/GAME-279) · [GAME-280 SG-02](https://setnessconsulting.atlassian.net/browse/GAME-280) · [GAME-281 SG-03](https://setnessconsulting.atlassian.net/browse/GAME-281) · [GAME-282 SG-04](https://setnessconsulting.atlassian.net/browse/GAME-282)
- [GAME-283 SG-05](https://setnessconsulting.atlassian.net/browse/GAME-283)
- [GAME-284 SG-06](https://setnessconsulting.atlassian.net/browse/GAME-284)
- [GAME-285 SG-07](https://setnessconsulting.atlassian.net/browse/GAME-285)
- [GAME-286 SG-08](https://setnessconsulting.atlassian.net/browse/GAME-286)
- [Games site release contract](https://github.com/setnessconsulting/games-site/blob/main/docs/game-release-contract.md)

The Unity WebGL files are released under the immutable private-R2 prefix signal-garden/<version>/Build/. The host supplies an asset base URL; the player must load its loader, data, framework, and WASM files from <assetBase>/Build/<exact-filename>. Brotli builds require Content-Encoding: br on compressed files and the correct Content-Type (in particular application/wasm for compressed WebAssembly). The production bucket and catalog stay unchanged until a separately reviewed release PR.
