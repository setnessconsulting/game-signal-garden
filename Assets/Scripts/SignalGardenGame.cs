using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SignalGarden
{
    public sealed class SignalGardenGame : MonoBehaviour
    {
        private const float RouteDrawHeight = 0.60f;
        private const float StartRadius = 0.68f;
        private const float CameraPanSpeed = 3.0f;

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
        private Vector3 cameraHomePosition;
        private Vector3 cameraHomeFocus;
        private Vector3 cameraPanOffset;
        private Vector2 lastPointerWorld;
        private bool suppressPointerUntilRelease;
        private float measurementSeconds;
        private int measurementFrames;
        private bool soundEnabled;
        private bool reducedMotion;
        private string statusText = "Ready. Drag from the coral source to begin.";

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void SignalGardenSetAccessibleStatus(string message);

        [DllImport("__Internal")]
        private static extern void SignalGardenInstallPointerCapture();

        [DllImport("__Internal")]
        private static extern void SignalGardenReportFrameRate(int framesPerSecond);
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

        public bool ReducedMotionEnabled
        {
            get { return reducedMotion; }
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

            Application.targetFrameRate = 60;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            if (feedbackAudio != null)
            {
                feedbackAudio.playOnAwake = false;
                feedbackAudio.spatialBlend = 0f;
            }

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
            SignalGardenReportFrameRate(Mathf.RoundToInt(measurementFrames / measurementSeconds));
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
                PlayFeedback(880f, 0.18f);
            }
            else
            {
                statusText = RecoveryMessage(result);
                SetLineMaterial(routeFailureMaterial);
                PlayFeedback(190f, 0.10f);
            }

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
                runState.CancelRoute(RouteFailure.Cancelled);
                statusText = RecoveryMessage(RouteFailure.Cancelled);
                SetLineMaterial(routeActiveMaterial);
                UpdateRouteVisual();
                Announce(statusText);
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

            cameraPanOffset += movement.normalized * CameraPanSpeed * Time.unscaledDeltaTime;
            cameraPanOffset.x = Mathf.Clamp(cameraPanOffset.x, -1.45f, 1.45f);
            cameraPanOffset.z = Mathf.Clamp(cameraPanOffset.z, -0.95f, 0.95f);
            gardenCamera.transform.position = cameraHomePosition + cameraPanOffset;
            gardenCamera.transform.LookAt(cameraHomeFocus + cameraPanOffset);
        }

        private void UpdatePulse()
        {
            var pulse = reducedMotion ? 0.92f : 0.78f + Mathf.Sin(Time.unscaledTime * 2.1f) * 0.16f;
            if (sourceGlow != null)
            {
                sourceGlow.intensity = 1.35f * pulse;
            }

            if (receiverGlow != null)
            {
                var completedBoost = runState.phase == GardenPhase.Verified ||
                                     (runState.phase == GardenPhase.Paused && runState.phaseBeforePause == GardenPhase.Verified)
                    ? 1.4f
                    : 0.72f;
                receiverGlow.intensity = completedBoost * pulse;
            }

            if (!reducedMotion && receiverMarker != null &&
                (runState.phase == GardenPhase.Verified ||
                 (runState.phase == GardenPhase.Paused && runState.phaseBeforePause == GardenPhase.Verified)))
            {
                receiverMarker.Rotate(Vector3.up, 12f * Time.unscaledDeltaTime, Space.World);
            }
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

        public void AnnounceHudFocusFromHud(string label)
        {
            Announce("Focused " + label + ". Press Enter or Space to activate.");
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

        public void ToggleReducedMotionFromHud()
        {
            suppressPointerUntilRelease = true;
            ToggleReducedMotion();
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
            statusText = soundEnabled ? "Sound cues on. Use the Sound button to mute them." : "Sound cues muted.";
            Announce(statusText);
        }

        private void ToggleReducedMotion()
        {
            reducedMotion = !reducedMotion;
            statusText = reducedMotion ? "Reduced motion on. Ambient pulses are now still." : "Reduced motion off. Ambient pulses are enabled.";
            Announce(statusText);
        }

        private void PlayFeedback(float frequency, float duration)
        {
            if (!soundEnabled || feedbackAudio == null)
            {
                return;
            }

            var sampleRate = 22050;
            var sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var samples = new float[sampleCount];
            for (var i = 0; i < sampleCount; i++)
            {
                var envelope = 1f - i / (float)sampleCount;
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * envelope * 0.11f;
            }

            var clip = AudioClip.Create("Garden signal cue", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            feedbackAudio.PlayOneShot(clip, 0.55f);
            Destroy(clip, duration + 0.25f);
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
                statusText = RecoveryMessage(RouteFailure.FocusInterrupted);
                SetLineMaterial(routeActiveMaterial);
                UpdateRouteVisual();
                Announce(statusText);
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && runState.phase == GardenPhase.Routing && runState.CancelRoute(RouteFailure.FocusInterrupted))
            {
                statusText = RecoveryMessage(RouteFailure.FocusInterrupted);
                SetLineMaterial(routeActiveMaterial);
                UpdateRouteVisual();
                Announce(statusText);
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
