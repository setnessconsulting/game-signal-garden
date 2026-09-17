using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
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
        private Texture2D panelTexture;
        private Texture2D buttonTexture;
        private Texture2D buttonHoverTexture;
        private Texture2D overlayTexture;
        private GUIStyle eyebrowStyle;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle statusStyle;
        private GUIStyle buttonStyle;
        private int styleHeight = -1;
        private int optionHeight = -1;

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
            var guiPoint = new Vector2(pointerPosition.x, Screen.height - pointerPosition.y);
            var scale = GetUiScale();
            var margin = 26f * scale;
            var titlePanel = new Rect(margin, margin, 560f * scale, 164f * scale);
            var soundButton = new Rect(Screen.width - 378f * scale, margin, 168f * scale, 54f * scale);
            var motionButton = new Rect(Screen.width - 198f * scale, margin, 172f * scale, 54f * scale);
            var statusPanel = new Rect(margin, Screen.height - 104f * scale, Screen.width - margin * 2f, 76f * scale);
            return titlePanel.Contains(guiPoint) ||
                   soundButton.Contains(guiPoint) ||
                   motionButton.Contains(guiPoint) ||
                   statusPanel.Contains(guiPoint) ||
                   runState.phase == GardenPhase.Paused ||
                   runState.phase == GardenPhase.Verified;
        }

        private static float GetUiScale()
        {
            var heightScale = Mathf.Clamp(Screen.height / 1080f, 0.62f, 1.15f);
            var widthScale = Mathf.Clamp(Screen.width / 1680f, 0.62f, 1.15f);
            return Mathf.Min(heightScale, widthScale);
        }

        private void OnGUI()
        {
            if (Screen.width < 500 || Screen.height < 320)
            {
                return;
            }

            EnsureUiTextures();
            var scale = GetUiScale();
            EnsureGuiStyles(scale);
            DrawTopHud(scale);
            DrawStatusHud(scale);

            if (runState.phase == GardenPhase.Paused)
            {
                DrawPauseOverlay(scale);
            }
            else if (runState.phase == GardenPhase.Verified)
            {
                DrawSuccessOverlay(scale);
            }
        }

        private void DrawTopHud(float scale)
        {
            var margin = 26f * scale;
            var card = new Rect(margin, margin, 560f * scale, 164f * scale);
            DrawPanel(card);
            GUI.color = new Color(0.42f, 0.86f, 0.74f, 0.92f);
            GUI.DrawTexture(new Rect(card.x + 1f, card.y + 20f * scale, 4f * scale, 116f * scale), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(card.x + 22f * scale, card.y + 14f * scale, card.width - 42f * scale, 26f * scale),
                "SIGNAL GARDEN  /  FIELD STUDY 01", eyebrowStyle);
            GUI.Label(new Rect(card.x + 22f * scale, card.y + 45f * scale, card.width - 42f * scale, 40f * scale),
                "Wake the garden with one clear line.", titleStyle);
            GUI.Label(new Rect(card.x + 22f * scale, card.y + 91f * scale, card.width - 42f * scale, 54f * scale),
                "Drag coral to blue along the gold stones. WASD pans. Esc cancels or pauses.", bodyStyle);

            var soundRect = new Rect(Screen.width - 378f * scale, margin, 168f * scale, 54f * scale);
            var motionRect = new Rect(Screen.width - 198f * scale, margin, 172f * scale, 54f * scale);
            if (GUI.Button(soundRect, soundEnabled ? "Sound cues: On" : "Sound cues: Off", buttonStyle))
            {
                ToggleSound();
            }

            if (GUI.Button(motionRect, reducedMotion ? "Motion: Reduced" : "Motion: Full", buttonStyle))
            {
                ToggleReducedMotion();
            }
        }

        private void DrawStatusHud(float scale)
        {
            var margin = 26f * scale;
            var card = new Rect(margin, Screen.height - 104f * scale, Screen.width - margin * 2f, 76f * scale);
            DrawPanel(card);
            var retryWidth = runState.phase == GardenPhase.Recovery ? 164f * scale : 0f;
            GUI.Label(new Rect(card.x + 22f * scale, card.y + 11f * scale, card.width - retryWidth - 48f * scale, 54f * scale),
                statusText, statusStyle);

            if (runState.phase == GardenPhase.Recovery)
            {
                var retry = new Rect(card.xMax - 176f * scale, card.y + 12f * scale, 158f * scale, 52f * scale);
                if (GUI.Button(retry, "Try again", buttonStyle))
                {
                    suppressPointerUntilRelease = true;
                    statusText = "Begin at the coral source and follow the gold trail.";
                    Announce(statusText);
                }
            }
        }

        private void DrawPauseOverlay(float scale)
        {
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), overlayTexture);
            var width = 440f * scale;
            var height = 310f * scale;
            var card = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            DrawPanel(card);
            GUI.Label(new Rect(card.x + 34f * scale, card.y + 28f * scale, width - 68f * scale, 34f * scale),
                "GARDEN AT REST", eyebrowStyle);
            GUI.Label(new Rect(card.x + 34f * scale, card.y + 72f * scale, width - 68f * scale, 52f * scale),
                "Paused", titleStyle);
            GUI.Label(new Rect(card.x + 34f * scale, card.y + 132f * scale, width - 68f * scale, 56f * scale),
                "Your turn is safe. Resume, or reset the garden for the next player.", bodyStyle);
            var resume = new Rect(card.x + 34f * scale, card.y + 204f * scale, width - 68f * scale, 44f * scale);
            var replay = new Rect(card.x + 34f * scale, card.y + 258f * scale, width - 68f * scale, 36f * scale);
            if (GUI.Button(resume, "Resume  /  Esc", buttonStyle))
            {
                suppressPointerUntilRelease = true;
                ResumeGame();
            }

            if (GUI.Button(replay, "Restart this turn", buttonStyle))
            {
                suppressPointerUntilRelease = true;
                ResetGame();
            }
        }

        private void DrawSuccessOverlay(float scale)
        {
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), overlayTexture);
            var width = 480f * scale;
            var height = 292f * scale;
            var card = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            DrawPanel(card);
            GUI.Label(new Rect(card.x + 32f * scale, card.y + 28f * scale, width - 64f * scale, 34f * scale),
                "A SIGNAL TAKES ROOT", eyebrowStyle);
            GUI.Label(new Rect(card.x + 32f * scale, card.y + 70f * scale, width - 64f * scale, 52f * scale),
                "The garden is awake.", titleStyle);
            GUI.Label(new Rect(card.x + 32f * scale, card.y + 128f * scale, width - 64f * scale, 42f * scale),
                "A new player can take the next turn here.", bodyStyle);
            var replay = new Rect(card.x + 32f * scale, card.y + 192f * scale, width - 64f * scale, 56f * scale);
            if (GUI.Button(replay, "Play again", buttonStyle))
            {
                suppressPointerUntilRelease = true;
                ResetGame();
            }

            GUI.Label(new Rect(card.x + 32f * scale, card.y + 250f * scale, width - 64f * scale, 28f * scale),
                "Esc pauses  ·  use the page controls to leave", bodyStyle);
        }

        private void DrawPanel(Rect rect)
        {
            GUI.DrawTexture(rect, panelTexture);
            GUI.color = new Color(0.66f, 0.88f, 0.80f, 0.16f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void EnsureGuiStyles(float scale)
        {
            var scaleKey = Mathf.RoundToInt(scale * 1000f);
            if (styleHeight == Screen.height && optionHeight == scaleKey)
            {
                return;
            }

            styleHeight = Screen.height;
            optionHeight = scaleKey;
            eyebrowStyle = CreateStyle(13, scale, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.66f, 0.88f, 0.80f));
            titleStyle = CreateStyle(25, scale, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.96f, 0.97f, 0.87f));
            bodyStyle = CreateStyle(15, scale, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.81f, 0.87f, 0.81f));
            statusStyle = CreateStyle(16, scale, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.94f, 0.93f, 0.82f));
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.RoundToInt(15f * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false
            };
            buttonStyle.normal.textColor = new Color(0.93f, 0.97f, 0.90f);
            buttonStyle.hover.textColor = Color.white;
            buttonStyle.active.textColor = Color.white;
            buttonStyle.normal.background = buttonTexture;
            buttonStyle.hover.background = buttonHoverTexture;
            buttonStyle.active.background = buttonHoverTexture;
            buttonStyle.border = new RectOffset(12, 12, 12, 12);
        }

        private GUIStyle CreateStyle(int fontSize, float scale, FontStyle weight, TextAnchor alignment, Color color)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(fontSize * scale),
                fontStyle = weight,
                alignment = alignment,
                wordWrap = true,
                richText = false
            };
            style.normal.textColor = color;
            return style;
        }

        private void EnsureUiTextures()
        {
            if (panelTexture != null)
            {
                return;
            }

            panelTexture = CreateRoundedTexture(new Color(0.035f, 0.085f, 0.092f, 0.94f), new Color(0.66f, 0.88f, 0.80f, 0.30f));
            buttonTexture = CreateRoundedTexture(new Color(0.10f, 0.23f, 0.22f, 0.98f), new Color(0.44f, 0.82f, 0.70f, 0.68f));
            buttonHoverTexture = CreateRoundedTexture(new Color(0.16f, 0.36f, 0.31f, 1f), new Color(0.69f, 0.94f, 0.77f, 0.96f));
            overlayTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            overlayTexture.SetPixel(0, 0, new Color(0.015f, 0.035f, 0.044f, 0.72f));
            overlayTexture.Apply();
            panelTexture.hideFlags = HideFlags.HideAndDontSave;
            buttonTexture.hideFlags = HideFlags.HideAndDontSave;
            buttonHoverTexture.hideFlags = HideFlags.HideAndDontSave;
            overlayTexture.hideFlags = HideFlags.HideAndDontSave;
        }

        private static Texture2D CreateRoundedTexture(Color fill, Color edge)
        {
            const int size = 64;
            const float radius = 11f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = Mathf.Max(radius - x, 0f, x - (size - 1f - radius));
                    var dy = Mathf.Max(radius - y, 0f, y - (size - 1f - radius));
                    var outside = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                    if (outside > 1.5f)
                    {
                        pixels[y * size + x] = new Color(0f, 0f, 0f, 0f);
                    }
                    else if (x < 2 || y < 2 || x >= size - 2 || y >= size - 2)
                    {
                        pixels[y * size + x] = edge;
                    }
                    else
                    {
                        pixels[y * size + x] = fill;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private void OnEnable()
        {
            EnsureUiTextures();
        }

        private void OnDestroy()
        {
            if (panelTexture != null) Destroy(panelTexture);
            if (buttonTexture != null) Destroy(buttonTexture);
            if (buttonHoverTexture != null) Destroy(buttonHoverTexture);
            if (overlayTexture != null) Destroy(overlayTexture);
        }
    }
}
