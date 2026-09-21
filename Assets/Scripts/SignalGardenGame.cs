using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SignalGarden
{
    public sealed class SignalGardenGame : MonoBehaviour
    {
        private const float RouteDrawHeight = 0.60f;
        private const float StartRadius = 0.68f;
        private const float CameraPanSpeed = 3.0f;
        private const float DefaultSoundVolume = 0.55f;
        private const float InteractionPulseDuration = 0.42f;
        private const float VerifiedPulseDuration = 0.72f;
        private const float GardenReactionDuration = 1.80f;
        private const float GardenReactionLift = 0.16f;
        private const float GardenReactionScale = 0.42f;
        private const float WebGlRenderScale = 0.60f;
        private const int FeedbackSampleRate = 22050;

        private enum FeedbackCue
        {
            Interaction,
            Recovery,
            Success
        }

        [SerializeField] private Camera gardenCamera;
        [SerializeField] private Transform cameraFocus;
        [SerializeField] private Transform sourceMarker;
        [SerializeField] private Transform receiverMarker;
        [SerializeField] private LineRenderer playerRouteLine;
        [SerializeField] private Light sourceGlow;
        [SerializeField] private Light receiverGlow;
        [SerializeField] private Material routeActiveMaterial;
        [SerializeField] private Material routeFailureMaterial;
        [SerializeField] private Material routeSuccessMaterial;
        [SerializeField] private AudioSource feedbackAudio;
        [SerializeField] private SignalGardenHud hud;

        private readonly GardenRunState runState = new GardenRunState();
        private readonly List<Vector2> routeTrail = new List<Vector2>(RouteRules.StandardTrail);
        private readonly List<Transform> fireflyReactors = new List<Transform>(12);
        private readonly List<Vector3> fireflyBasePositions = new List<Vector3>(12);
        private readonly List<Vector3> fireflyBaseScales = new List<Vector3>(12);
        private Vector3 cameraHomePosition;
        private Vector3 cameraHomeFocus;
        private Vector3 cameraPanOffset;
        private Vector2 lastPointerWorld;
        private bool suppressPointerUntilRelease;
        private float measurementSeconds;
        private int measurementFrames;
        private bool soundEnabled;
        [SerializeField, Range(0f, 1f)] private float soundVolume = DefaultSoundVolume;
        private bool reducedMotion;
        private float sourceFeedbackPulseSeconds;
        private float receiverFeedbackPulseSeconds;
        private float gardenReactionSeconds;
        private bool audioFailureReported;
        private bool panHintShown;
        private bool performanceTuningApplied;
        private bool fireflyReactionApplied;
        private string statusText = "Ready. Drag from the coral source to begin.";

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void SignalGardenSetAccessibleStatus(string message);

        [DllImport("__Internal")]
        private static extern void SignalGardenInstallPointerCapture();

        [DllImport("__Internal")]
        private static extern void SignalGardenReportFrameRate(float framesPerSecond);
#endif

        public GardenRunState RunState
        {
            get { return runState; }
        }

        public GardenPhase Phase
        {
            get { return runState.phase; }
        }

        public string StatusText
        {
            get { return statusText; }
        }

        public bool SoundEnabled
        {
            get { return soundEnabled; }
        }

        public float SoundVolume
        {
            get { return soundVolume; }
        }

        public bool ReducedMotionEnabled
        {
            get { return reducedMotion; }
        }

        public bool PanHintShown
        {
            get { return panHintShown; }
        }

        public bool SuccessReactionActive
        {
            get { return gardenReactionSeconds > 0f; }
        }

        public int SuccessReactionCount
        {
            get { return fireflyReactors.Count; }
        }

        public bool PerformanceTuningApplied
        {
            get { return performanceTuningApplied; }
        }

        public void Configure(
            Camera sceneCamera,
            Transform sceneFocus,
            Transform source,
            Transform receiver,
            LineRenderer routeLine,
            Light sourceLight,
            Light receiverLight,
            Material activeMaterial,
            Material failureMaterial,
            Material successMaterial,
            AudioSource audioSource)
        {
            gardenCamera = sceneCamera;
            cameraFocus = sceneFocus;
            sourceMarker = source;
            receiverMarker = receiver;
            playerRouteLine = routeLine;
            sourceGlow = sourceLight;
            receiverGlow = receiverLight;
            routeActiveMaterial = activeMaterial;
            routeFailureMaterial = failureMaterial;
            routeSuccessMaterial = successMaterial;
            feedbackAudio = audioSource;
        }

        private void Awake()
        {
            if (gardenCamera == null || sourceMarker == null || receiverMarker == null || playerRouteLine == null)
            {
                SignalGardenRuntimeBuilder.Build(this);
            }

            if (gardenCamera == null)
            {
                gardenCamera = Camera.main;
            }

            if (gardenCamera != null)
            {
                cameraHomePosition = gardenCamera.transform.position;
            }

            if (cameraFocus != null)
            {
                cameraHomeFocus = cameraFocus.position;
            }

            if (hud == null)
            {
                hud = FindAnyObjectByType<SignalGardenHud>();
            }

            if (hud != null)
            {
                hud.Bind(this);
            }

            CollectFireflyReactors();
            ApplyPerformanceTuning();

            Application.targetFrameRate = 60;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            if (feedbackAudio != null)
            {
                feedbackAudio.playOnAwake = false;
                feedbackAudio.spatialBlend = 0f;
            }
            soundVolume = Mathf.Clamp01(soundVolume);

#if UNITY_WEBGL && !UNITY_EDITOR
            SignalGardenInstallPointerCapture();
#endif
            UpdateRouteVisual();
            Announce(statusText);
        }

        private void Update()
        {
            MeasureFrameRate();
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                HandleEscape();
            }

            if (runState.phase == GardenPhase.Paused || runState.phase == GardenPhase.Verified)
            {
                UpdatePulse();
                return;
            }

            if (runState.phase != GardenPhase.Routing)
            {
                UpdateCameraPan();
            }

            UpdatePointerRoute();
            UpdatePulse();
        }

        private void MeasureFrameRate()
        {
            measurementFrames++;
            measurementSeconds += Time.unscaledDeltaTime;
            if (measurementSeconds < 1f)
            {
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            SignalGardenReportFrameRate(measurementFrames / measurementSeconds);
#endif
            measurementFrames = 0;
            measurementSeconds = 0f;
        }

        private void UpdatePointerRoute()
        {
            var mouse = Mouse.current;
            if (mouse == null || gardenCamera == null || sourceMarker == null)
            {
                return;
            }

            if (suppressPointerUntilRelease)
            {
                if (!mouse.leftButton.isPressed)
                {
                    suppressPointerUntilRelease = false;
                    return;
                }

                return;
            }

            var screenPoint = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (IsPointerOverHud(screenPoint))
                {
                    return;
                }

                if (runState.phase == GardenPhase.Observe || runState.phase == GardenPhase.Recovery)
                {
                    if (!TryGetGardenPoint(screenPoint, out var gardenPoint))
                    {
                        return;
                    }

                    var sourcePoint = new Vector2(sourceMarker.position.x, sourceMarker.position.z);
                    if (Vector2.Distance(gardenPoint, sourcePoint) > StartRadius)
                    {
                        statusText = "Start on the glowing coral source. The gold trail leads to the blue receiver.";
                        Announce(statusText);
                        return;
                    }

                    runState.BeginRoute(sourcePoint);
                    lastPointerWorld = sourcePoint;
                    statusText = "Tracing the signal. Stay on the gold stones and release at the blue receiver. Esc cancels.";
                    sourceFeedbackPulseSeconds = InteractionPulseDuration;
                    PlayFeedback(FeedbackCue.Interaction);
                    SetLineMaterial(routeActiveMaterial);
                    UpdateRouteVisual();
                    Announce(statusText);
                }
            }

            if (runState.phase != GardenPhase.Routing)
            {
                return;
            }

            if (mouse.leftButton.isPressed && TryGetGardenPoint(screenPoint, out var currentPoint))
            {
                if (Vector2.Distance(lastPointerWorld, currentPoint) >= 0.045f)
                {
                    runState.AppendRoutePoint(currentPoint);
                    lastPointerWorld = currentPoint;
                    UpdateRouteVisual();
                }
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                if (TryGetGardenPoint(screenPoint, out var finalPoint))
                {
                    runState.AppendRoutePoint(finalPoint);
                }

                ResolveCurrentRoute();
            }
        }

        private bool TryGetGardenPoint(Vector2 screenPoint, out Vector2 gardenPoint)
        {
            var ray = gardenCamera.ScreenPointToRay(new Vector3(screenPoint.x, screenPoint.y, 0f));
            var ground = new Plane(Vector3.up, new Vector3(0f, RouteDrawHeight, 0f));
            if (ground.Raycast(ray, out var distance))
            {
                var worldPoint = ray.GetPoint(distance);
                gardenPoint = new Vector2(worldPoint.x, worldPoint.z);
                return true;
            }

            gardenPoint = Vector2.zero;
            return false;
        }

        private void ResolveCurrentRoute()
        {
            var result = RouteRules.Validate(runState.routePoints, routeTrail);
            if (!runState.ResolveAttempt(result))
            {
                return;
            }

            if (result == RouteFailure.None)
            {
                statusText = "Signal received. The garden is awake. Choose Play again for the next turn.";
                SetLineMaterial(routeSuccessMaterial);
                receiverFeedbackPulseSeconds = VerifiedPulseDuration;
                StartSuccessReaction();
                PlayFeedback(FeedbackCue.Success);
                UpdateRouteVisual();
                Announce(statusText);
            }
            else
            {
                EnterRecovery(result, true);
                return;
            }

            UpdateRouteVisual();
            Announce(statusText);
        }

        private void EnterRecovery(RouteFailure failure, bool preserveFailureTrace)
        {
            statusText = RecoveryMessage(failure);
            SetLineMaterial(preserveFailureTrace ? routeFailureMaterial : routeActiveMaterial);
            sourceFeedbackPulseSeconds = InteractionPulseDuration;
            PlayFeedback(FeedbackCue.Recovery);
            UpdateRouteVisual();
            Announce(statusText);
        }

        private static string RecoveryMessage(RouteFailure failure)
        {
            switch (failure)
            {
                case RouteFailure.EndsBeforeReceiver:
                    return "The line stopped short. Reach the blue receiver, then release. Try again from the coral source.";
                case RouteFailure.LeavesTrail:
                    return "The blind spur has no receiver. Follow the gold stones from coral to blue and try again.";
                case RouteFailure.ReversesDirection:
                    return "Follow the trail forward from coral to blue. Start a fresh line at the source.";
                case RouteFailure.StartsAwayFromSource:
                    return "A signal must begin at the coral source. Try again from the glowing orb.";
                case RouteFailure.Cancelled:
                    return "Route canceled. Start again at the coral source when you are ready.";
                case RouteFailure.FocusInterrupted:
                    return "Focus changed, so the partial route was canceled. Start again at the coral source.";
                default:
                    return "No complete signal reached the receiver. Start at coral, follow the gold stones, and try again.";
            }
        }

        private void HandleEscape()
        {
            if (runState.phase == GardenPhase.Routing)
            {
                if (runState.CancelRoute(RouteFailure.Cancelled))
                {
                    EnterRecovery(RouteFailure.Cancelled, false);
                }
                return;
            }

            if (runState.phase == GardenPhase.Paused)
            {
                ResumeGame();
                return;
            }

            runState.Pause();
            statusText = "Paused. Press Esc or choose Resume to continue. Replay starts a fresh turn.";
            Announce(statusText);
        }

        private void UpdateCameraPan()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || gardenCamera == null)
            {
                return;
            }

            var movement = Vector3.zero;
            if (keyboard.wKey.isPressed) movement += Vector3.forward;
            if (keyboard.sKey.isPressed) movement += Vector3.back;
            if (keyboard.aKey.isPressed) movement += Vector3.left;
            if (keyboard.dKey.isPressed) movement += Vector3.right;
            if (movement.sqrMagnitude <= 0.001f)
            {
                return;
            }

            if (!panHintShown)
            {
                panHintShown = true;
                statusText = "Optional pan: WASD moves the garden view. Route tracing still uses drag.";
                Announce(statusText);
            }

            cameraPanOffset += movement.normalized * CameraPanSpeed * Time.unscaledDeltaTime;
            cameraPanOffset.x = Mathf.Clamp(cameraPanOffset.x, -1.45f, 1.45f);
            cameraPanOffset.z = Mathf.Clamp(cameraPanOffset.z, -0.95f, 0.95f);
            gardenCamera.transform.position = cameraHomePosition + cameraPanOffset;
            gardenCamera.transform.LookAt(cameraHomeFocus + cameraPanOffset);
        }

        private void UpdatePulse()
        {
            var pulse = reducedMotion ? 0.92f : 0.78f + Mathf.Sin(Time.unscaledTime * 2.1f) * 0.16f;
            var sourceFeedback = ReadFeedbackPulse(ref sourceFeedbackPulseSeconds, InteractionPulseDuration, 0.46f);
            if (sourceGlow != null)
            {
                sourceGlow.intensity = 1.35f * pulse + sourceFeedback;
            }

            if (receiverGlow != null)
            {
                var completedBoost = runState.phase == GardenPhase.Verified ||
                                     (runState.phase == GardenPhase.Paused && runState.phaseBeforePause == GardenPhase.Verified)
                    ? 1.4f
                    : 0.72f;
                var receiverFeedback = ReadFeedbackPulse(ref receiverFeedbackPulseSeconds, VerifiedPulseDuration, 0.65f);
                receiverGlow.intensity = completedBoost * pulse + receiverFeedback;
            }

            UpdateGardenReaction();

            if (!reducedMotion && receiverMarker != null &&
                (runState.phase == GardenPhase.Verified ||
                 (runState.phase == GardenPhase.Paused && runState.phaseBeforePause == GardenPhase.Verified)))
            {
                receiverMarker.Rotate(Vector3.up, 12f * Time.unscaledDeltaTime, Space.World);
            }
        }

        private float ReadFeedbackPulse(ref float secondsRemaining, float duration, float strength)
        {
            if (secondsRemaining <= 0f)
            {
                return 0f;
            }

            var progress = 1f - secondsRemaining / duration;
            secondsRemaining = Mathf.Max(0f, secondsRemaining - Time.unscaledDeltaTime);
            if (reducedMotion)
            {
                return 0f;
            }

            return Mathf.Sin(Mathf.PI * Mathf.Clamp01(progress)) * strength;
        }

        private void CollectFireflyReactors()
        {
            fireflyReactors.Clear();
            fireflyBasePositions.Clear();
            fireflyBaseScales.Clear();

            for (var index = 1; index <= 12; index++)
            {
                var firefly = GameObject.Find("Garden firefly " + index);
                if (firefly == null)
                {
                    continue;
                }

                fireflyReactors.Add(firefly.transform);
                fireflyBasePositions.Add(firefly.transform.localPosition);
                fireflyBaseScales.Add(firefly.transform.localScale);
            }
        }

        private void StartSuccessReaction()
        {
            gardenReactionSeconds = GardenReactionDuration;
            fireflyReactionApplied = false;
            RestoreFireflyReactors();
        }

        private void UpdateGardenReaction()
        {
            if (fireflyReactors.Count == 0)
            {
                return;
            }

            if (gardenReactionSeconds <= 0f)
            {
                if (fireflyReactionApplied)
                {
                    RestoreFireflyReactors();
                    fireflyReactionApplied = false;
                }
                return;
            }

            gardenReactionSeconds = Mathf.Max(0f, gardenReactionSeconds - Time.unscaledDeltaTime);
            if (reducedMotion)
            {
                RestoreFireflyReactors();
                fireflyReactionApplied = false;
                return;
            }

            var progress = 1f - gardenReactionSeconds / GardenReactionDuration;
            var envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(progress));
            for (var index = 0; index < fireflyReactors.Count; index++)
            {
                var reactor = fireflyReactors[index];
                if (reactor == null)
                {
                    continue;
                }

                var phase = index * 0.73f;
                var lift = GardenReactionLift * envelope * (0.70f + 0.30f * Mathf.Sin(phase + 1f));
                reactor.localPosition = fireflyBasePositions[index] + Vector3.up * lift;
                reactor.localScale = fireflyBaseScales[index] * (1f + GardenReactionScale * envelope);
            }

            fireflyReactionApplied = true;
        }

        private void RestoreFireflyReactors()
        {
            for (var index = 0; index < fireflyReactors.Count; index++)
            {
                var reactor = fireflyReactors[index];
                if (reactor == null)
                {
                    continue;
                }

                reactor.localPosition = fireflyBasePositions[index];
                reactor.localScale = fireflyBaseScales[index];
            }
        }

        private void ApplyPerformanceTuning()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL uses the Mobile quality profile, but enforce that selection at runtime as
            // well. This keeps an editor's active desktop quality level from leaking into the
            // browser build and retains the exact canvas size while avoiding desktop-only HDR,
            // MSAA, and shadow costs.
            QualitySettings.SetQualityLevel(0, true);
            QualitySettings.antiAliasing = 0;
            var pipeline = QualitySettings.renderPipeline as UniversalRenderPipelineAsset ??
                           GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline != null)
            {
                // Keep the browser canvas at 1920x1080 while reducing only the internal
                // color/depth buffers. Route markers and UI remain at the exact target size.
                pipeline.renderScale = Mathf.Min(pipeline.renderScale, WebGlRenderScale);
            }
            QualitySettings.shadows = UnityEngine.ShadowQuality.Disable;
            QualitySettings.softParticles = false;
            QualitySettings.realtimeReflectionProbes = false;
            if (gardenCamera != null)
            {
                gardenCamera.allowHDR = false;
                gardenCamera.allowMSAA = false;
            }
#endif

            var renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
            var staticDecorativeObjects = new List<GameObject>(renderers.Length);
            for (var index = 0; index < renderers.Length; index++)
            {
                var renderer = renderers[index];
                if (!IsDecorativeRenderer(renderer.gameObject.name))
                {
                    continue;
                }

#if UNITY_WEBGL && !UNITY_EDITOR
                if (ShouldCullOptionalDecorative(renderer))
                {
                    renderer.enabled = false;
                    continue;
                }
#endif

                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

#if UNITY_WEBGL && !UNITY_EDITOR
                var decorativeLine = renderer as LineRenderer;
                if (decorativeLine != null)
                {
                    decorativeLine.numCornerVertices = 0;
                    decorativeLine.numCapVertices = 0;
                }
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
                if (!IsDynamicDecorativeRenderer(renderer.gameObject.name))
                {
                    renderer.gameObject.isStatic = true;
                    staticDecorativeObjects.Add(renderer.gameObject);
                }
#endif
            }

            var lights = FindObjectsByType<Light>(FindObjectsInactive.Exclude);
            for (var index = 0; index < lights.Length; index++)
            {
                var light = lights[index];
                if (light.type == LightType.Directional && light.gameObject.name == "Warm canopy light")
                {
                    light.shadows = LightShadows.None;
                }
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            ApplyDecorativeStaticBatching(staticDecorativeObjects);
#endif

            performanceTuningApplied = true;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private static void ApplyDecorativeStaticBatching(List<GameObject> decorativeObjects)
        {
            if (decorativeObjects.Count == 0)
            {
                return;
            }

            var batchRoot = new GameObject("Signal Garden Decorative Static Batch");
            batchRoot.hideFlags = HideFlags.HideAndDontSave;
            batchRoot.isStatic = true;
            StaticBatchingUtility.Combine(decorativeObjects.ToArray(), batchRoot);
        }

        private static bool ShouldCullOptionalDecorative(Renderer renderer)
        {
            var objectName = renderer.gameObject.name;
            if (objectName.Contains("Plant contact shadow"))
            {
                return true;
            }

            // Keep three petals per flower so the garden still reads as planted while
            // removing the redundant back-facing decorative overdraw on WebGL.
            return objectName == "Wildflower petal" && renderer.transform.GetSiblingIndex() % 2 == 1;
        }
#endif

        private static bool IsDecorativeRenderer(string objectName)
        {
            return objectName.StartsWith("Garden firefly", System.StringComparison.Ordinal) ||
                   objectName.Contains("Wildflower") ||
                   objectName.Contains("Fern") ||
                   objectName.Contains("Moss pebble") ||
                   objectName.Contains("Moss-polished pebble") ||
                   objectName.Contains("Plant contact shadow") ||
                   objectName.Contains("Dead end") ||
                   objectName.StartsWith("Wild growth", System.StringComparison.Ordinal) ||
                   objectName.StartsWith("Suspended basalt shard", System.StringComparison.Ordinal) ||
                   objectName == "Upper warm earth band" ||
                   objectName == "Rose clay stratum" ||
                   objectName == "Deep floating stone" ||
                   objectName == "Faceted moss surface" ||
                   objectName == "Glazed garden edge";
        }

        private static bool IsDynamicDecorativeRenderer(string objectName)
        {
            return objectName.StartsWith("Garden firefly", System.StringComparison.Ordinal);
        }

        private void UpdateRouteVisual()
        {
            if (playerRouteLine == null)
            {
                return;
            }

            var points = runState.routePoints;
            if (points.Count == 0)
            {
                playerRouteLine.positionCount = 0;
                playerRouteLine.enabled = false;
                return;
            }

            playerRouteLine.enabled = true;
            playerRouteLine.positionCount = Mathf.Max(2, points.Count);
            for (var i = 0; i < points.Count; i++)
            {
                playerRouteLine.SetPosition(i, new Vector3(points[i].x, RouteDrawHeight, points[i].y));
            }

            if (points.Count == 1)
            {
                playerRouteLine.SetPosition(1, new Vector3(points[0].x + 0.01f, RouteDrawHeight, points[0].y));
            }
        }

        private void SetLineMaterial(Material material)
        {
            if (playerRouteLine != null && material != null)
            {
                playerRouteLine.sharedMaterial = material;
            }
        }

        public void SuppressPointerUntilRelease()
        {
            suppressPointerUntilRelease = true;
        }

        public void AnnounceHudFocusFromHud(string label, bool isVolumeSlider = false)
        {
            var instruction = isVolumeSlider
                ? "Use the left and right arrow keys to adjust."
                : "Press Enter or Space to activate.";
            Announce("Focused " + label + ". " + instruction);
        }

        public void ResumeFromHud()
        {
            suppressPointerUntilRelease = true;
            ResumeGame();
        }

        public void ResetFromHud()
        {
            suppressPointerUntilRelease = true;
            ResetGame();
        }

        public void RetryFromHud()
        {
            suppressPointerUntilRelease = true;
            if (runState.phase == GardenPhase.Recovery)
            {
                statusText = "Begin at the coral source and follow the gold trail.";
                Announce(statusText);
            }
        }

        public void ToggleSoundFromHud()
        {
            suppressPointerUntilRelease = true;
            ToggleSound();
        }

        public void SetSoundVolumeFromHud(float value)
        {
            suppressPointerUntilRelease = true;
            soundVolume = Mathf.Clamp01(value);
            Announce("Cue volume " + Mathf.RoundToInt(soundVolume * 100f) + " percent. " +
                     (soundEnabled ? "Sound cues are on." : "Sound cues are muted."));
        }

        public void ToggleReducedMotionFromHud()
        {
            suppressPointerUntilRelease = true;
            ToggleReducedMotion();
        }

        public void HandleBrowserPointerCancel()
        {
            if (runState.phase == GardenPhase.Routing && runState.CancelRoute(RouteFailure.FocusInterrupted))
            {
                EnterRecovery(RouteFailure.FocusInterrupted, false);
            }
        }

        private void ResumeGame()
        {
            if (!runState.Resume())
            {
                return;
            }

            statusText = StatusForPhase(runState.phase);
            Announce(statusText);
        }

        private static string StatusForPhase(GardenPhase phase)
        {
            switch (phase)
            {
                case GardenPhase.Routing:
                    return "Tracing the signal. Stay on the gold stones and release at the blue receiver. Esc cancels.";
                case GardenPhase.Recovery:
                    return "Ready for another try. Begin on the coral source and follow the gold stones.";
                case GardenPhase.Verified:
                    return "Signal received. The garden is awake. Choose Play again for the next turn.";
                default:
                    return "Ready. Drag from the coral source to begin.";
            }
        }

        private void ResetGame()
        {
            runState.Reset();
            gardenReactionSeconds = 0f;
            fireflyReactionApplied = false;
            RestoreFireflyReactors();
            cameraPanOffset = Vector3.zero;
            if (gardenCamera != null)
            {
                gardenCamera.transform.position = cameraHomePosition;
                gardenCamera.transform.LookAt(cameraHomeFocus);
            }

            SetLineMaterial(routeActiveMaterial);
            UpdateRouteVisual();
            statusText = "Fresh turn. Drag from the coral source to begin.";
            Announce(statusText);
        }

        private void ToggleSound()
        {
            soundEnabled = !soundEnabled;
            statusText = soundEnabled
                ? "Sound cues on at " + Mathf.RoundToInt(soundVolume * 100f) + " percent volume. Use the Sound button to mute them."
                : "Sound cues muted. Visual and text feedback remain available.";
            Announce(statusText);
        }

        private void ToggleReducedMotion()
        {
            reducedMotion = !reducedMotion;
            statusText = reducedMotion ? "Reduced motion on. Ambient pulses are now still." : "Reduced motion off. Ambient pulses are enabled.";
            Announce(statusText);
        }

        private void PlayFeedback(FeedbackCue cue)
        {
            if (!soundEnabled || soundVolume <= 0f || feedbackAudio == null)
            {
                return;
            }

            var duration = CueDuration(cue);
            AudioClip clip = null;
            try
            {
                var sampleCount = Mathf.CeilToInt(FeedbackSampleRate * duration);
                var samples = new float[sampleCount];
                for (var i = 0; i < sampleCount; i++)
                {
                    var normalizedTime = i / (float)sampleCount;
                    var frequency = CueFrequency(cue, normalizedTime);
                    var attack = Mathf.Clamp01(normalizedTime / 0.035f);
                    var release = Mathf.Clamp01((1f - normalizedTime) / 0.24f);
                    var envelope = attack * release * (1f - normalizedTime * 0.25f);
                    samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / FeedbackSampleRate) * envelope * 0.12f;
                }

                clip = AudioClip.Create("Signal Garden " + cue + " cue", sampleCount, 1, FeedbackSampleRate, false);
                if (clip == null)
                {
                    return;
                }

                clip.SetData(samples, 0);
                feedbackAudio.PlayOneShot(clip, soundVolume);
            }
            catch (System.Exception exception)
            {
                if (!audioFailureReported)
                {
                    Debug.LogWarning("Optional Signal Garden audio cue unavailable: " + exception.Message, this);
                    audioFailureReported = true;
                }
            }
            finally
            {
                if (clip != null)
                {
                    Destroy(clip, duration + 0.25f);
                }
            }
        }

        private static float CueDuration(FeedbackCue cue)
        {
            switch (cue)
            {
                case FeedbackCue.Interaction:
                    return 0.075f;
                case FeedbackCue.Recovery:
                    return 0.15f;
                default:
                    return 0.22f;
            }
        }

        private static float CueFrequency(FeedbackCue cue, float normalizedTime)
        {
            switch (cue)
            {
                case FeedbackCue.Interaction:
                    return Mathf.Lerp(520f, 590f, normalizedTime);
                case FeedbackCue.Recovery:
                    return Mathf.Lerp(300f, 205f, normalizedTime);
                default:
                    return normalizedTime < 0.46f ? 659.25f : 880f;
            }
        }

        private void Announce(string message)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SignalGardenSetAccessibleStatus(message);
#endif
        }

        private void OnApplicationFocus(bool focus)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            if (!focus && runState.phase == GardenPhase.Routing && runState.CancelRoute(RouteFailure.FocusInterrupted))
            {
                EnterRecovery(RouteFailure.FocusInterrupted, false);
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && runState.phase == GardenPhase.Routing && runState.CancelRoute(RouteFailure.FocusInterrupted))
            {
                EnterRecovery(RouteFailure.FocusInterrupted, false);
            }
        }

        private bool IsPointerOverHud(Vector2 pointerPosition)
        {
            return (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) ||
                   runState.phase == GardenPhase.Paused ||
                   runState.phase == GardenPhase.Verified;
        }
    }
}
