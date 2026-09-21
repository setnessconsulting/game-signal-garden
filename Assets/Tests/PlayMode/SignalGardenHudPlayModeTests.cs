using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SignalGarden;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SignalGarden.Tests
{
    public sealed class SignalGardenHudPlayModeTests
    {
        [UnityTest]
        public IEnumerator SignalGardenSceneStartsWithACompleteHudBinding()
        {
            yield return SceneManager.LoadSceneAsync("SignalGarden");
            yield return null;

            var hud = UnityEngine.Object.FindAnyObjectByType<SignalGardenHud>();
            Assert.That(hud, Is.Not.Null);
            Assert.That(hud.HasCompleteBinding, Is.True);
            Assert.That(hud.DisplayedPhase, Is.EqualTo(GardenPhase.Observe));

            var canvasScaler = hud.GetComponent<CanvasScaler>();
            Assert.That(canvasScaler, Is.Not.Null);
            Assert.That(canvasScaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(canvasScaler.referenceResolution.x, Is.EqualTo(1920f));
            Assert.That(canvasScaler.referenceResolution.y, Is.EqualTo(1080f));

            var soundRect = hud.transform.Find("Sound Cues Button").GetComponent<RectTransform>();
            var volumeRect = hud.transform.Find("Sound Volume Slider").GetComponent<RectTransform>();
            var motionRect = hud.transform.Find("Reduced Motion Button").GetComponent<RectTransform>();
            AssertTopRightControl(soundRect, 210f, 26f);
            AssertTopRightControl(volumeRect, 26f, 116f);
            AssertTopRightControl(motionRect, 26f, 26f);
            Assert.That(volumeRect.rect.width, Is.GreaterThanOrEqualTo(340f));
            Assert.That(Application.targetFrameRate, Is.GreaterThanOrEqualTo(60));

            var eventSystem = EventSystem.current;
            Assert.That(eventSystem, Is.Not.Null);
            hud.MoveKeyboardFocus(false);
            Assert.That(eventSystem.currentSelectedGameObject.name, Is.EqualTo("Sound Cues Button"));
            hud.MoveKeyboardFocus(false);
            Assert.That(eventSystem.currentSelectedGameObject.name, Is.EqualTo("Sound Volume Slider"));
            hud.MoveKeyboardFocus(false);
            Assert.That(eventSystem.currentSelectedGameObject.name, Is.EqualTo("Reduced Motion Button"));
            hud.MoveKeyboardFocus(false);
            Assert.That(eventSystem.currentSelectedGameObject.name, Is.EqualTo("Sound Cues Button"));
            hud.MoveKeyboardFocus(true);
            Assert.That(eventSystem.currentSelectedGameObject.name, Is.EqualTo("Reduced Motion Button"));

            var game = UnityEngine.Object.FindAnyObjectByType<SignalGardenGame>();
            Assert.That(game, Is.Not.Null);
            game.RunState.Pause();
            yield return null;
            Assert.That(hud.DisplayedPhase, Is.EqualTo(GardenPhase.Paused));
            Assert.That(eventSystem.currentSelectedGameObject.name, Is.EqualTo("Overlay Primary Button"));
            hud.MoveKeyboardFocus(false);
            Assert.That(eventSystem.currentSelectedGameObject.name, Is.EqualTo("Overlay Secondary Button"));
            hud.MoveKeyboardFocus(true);
            Assert.That(eventSystem.currentSelectedGameObject.name, Is.EqualTo("Overlay Primary Button"));
        }

        [UnityTest]
        public IEnumerator SoundStartsMutedAndTheVolumeControlChangesOnlyAudioPreference()
        {
            yield return SceneManager.LoadSceneAsync("SignalGarden");
            yield return null;

            var game = UnityEngine.Object.FindAnyObjectByType<SignalGardenGame>();
            var hud = UnityEngine.Object.FindAnyObjectByType<SignalGardenHud>();
            Assert.That(game, Is.Not.Null);
            Assert.That(hud, Is.Not.Null);
            Assert.That(game.SoundEnabled, Is.False);
            Assert.That(game.Phase, Is.EqualTo(GardenPhase.Observe));

            var volumeSlider = hud.transform.Find("Sound Volume Slider").GetComponent<Slider>();
            var volumeLabel = hud.transform.Find("Sound Volume Label").GetComponent<Text>();
            Assert.That(volumeSlider.value, Is.EqualTo(game.SoundVolume).Within(0.001f));

            volumeSlider.value = 0.73f;
            Assert.That(game.SoundVolume, Is.EqualTo(0.73f).Within(0.001f));
            Assert.That(volumeLabel.text, Is.EqualTo("CUE VOLUME  73%"));
            Assert.That(game.SoundEnabled, Is.False);
            Assert.That(game.Phase, Is.EqualTo(GardenPhase.Observe));

            hud.OnSoundPressed();
            Assert.That(game.SoundEnabled, Is.True);
            Assert.That(hud.transform.Find("Sound Cues Button/Label").GetComponent<Text>().text, Is.EqualTo("Sound cues: On"));
            hud.OnSoundPressed();
            Assert.That(game.SoundEnabled, Is.False);
            Assert.That(game.Phase, Is.EqualTo(GardenPhase.Observe));

            game.SetSoundVolumeFromHud(1.5f);
            Assert.That(game.SoundVolume, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator KeyboardVolumeNavigationAdjustsInBothDirections()
        {
            yield return SceneManager.LoadSceneAsync("SignalGarden");
            yield return null;

            var game = UnityEngine.Object.FindAnyObjectByType<SignalGardenGame>();
            var hud = UnityEngine.Object.FindAnyObjectByType<SignalGardenHud>();
            var eventSystem = EventSystem.current;
            Assert.That(game, Is.Not.Null);
            Assert.That(hud, Is.Not.Null);
            Assert.That(eventSystem, Is.Not.Null);
            var slider = hud.transform.Find("Sound Volume Slider").GetComponent<Slider>();
            Assert.That(slider.navigation.mode, Is.EqualTo(Navigation.Mode.Explicit));
            Assert.That(slider.navigation.selectOnLeft, Is.Null);
            Assert.That(slider.navigation.selectOnRight, Is.Null);

            eventSystem.SetSelectedGameObject(slider.gameObject);
            var initial = slider.value;
            slider.OnMove(new AxisEventData(eventSystem) { moveDir = MoveDirection.Left });
            var lowered = slider.value;
            Assert.That(lowered, Is.LessThan(initial));
            Assert.That(game.SoundVolume, Is.EqualTo(lowered).Within(0.001f));

            slider.OnMove(new AxisEventData(eventSystem) { moveDir = MoveDirection.Right });
            Assert.That(slider.value, Is.EqualTo(initial).Within(0.001f));
            Assert.That(game.SoundVolume, Is.EqualTo(initial).Within(0.001f));
            Assert.That(game.Phase, Is.EqualTo(GardenPhase.Observe));
        }

        [UnityTest]
        public IEnumerator MissingOptionalAudioDoesNotBlockRecoveryOrRouteVerification()
        {
            yield return SceneManager.LoadSceneAsync("SignalGarden");
            yield return null;

            var game = UnityEngine.Object.FindAnyObjectByType<SignalGardenGame>();
            Assert.That(game, Is.Not.Null);
            game.ToggleSoundFromHud();
            typeof(SignalGardenGame)
                .GetField("feedbackAudio", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(game, null);

            game.RunState.BeginRoute(RouteRules.StandardTrail[0]);
            game.RunState.AppendRoutePoint(RouteRules.StandardTrail[1]);
            ResolveCurrentRoute(game);
            Assert.That(game.Phase, Is.EqualTo(GardenPhase.Recovery));
            Assert.That(game.StatusText, Does.Contain("stopped short"));

            game.ResetFromHud();
            game.RunState.BeginRoute(RouteRules.StandardTrail[0]);
            for (var i = 1; i < RouteRules.StandardTrail.Length; i++)
            {
                game.RunState.AppendRoutePoint(RouteRules.StandardTrail[i]);
            }

            Assert.DoesNotThrow(() => ResolveCurrentRoute(game));
            Assert.That(game.Phase, Is.EqualTo(GardenPhase.Verified));
            Assert.That(game.StatusText, Does.Contain("Signal received"));
        }

        [UnityTest]
        public IEnumerator InvalidReleaseKeepsFailureTraceAndNamesTheRetryAction()
        {
            yield return SceneManager.LoadSceneAsync("SignalGarden");
            yield return null;

            var game = UnityEngine.Object.FindAnyObjectByType<SignalGardenGame>();
            var routeLine = typeof(SignalGardenGame)
                .GetField("playerRouteLine", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(game) as LineRenderer;
            Assert.That(game, Is.Not.Null);
            Assert.That(routeLine, Is.Not.Null);

            game.RunState.BeginRoute(RouteRules.StandardTrail[0]);
            game.RunState.AppendRoutePoint(RouteRules.StandardTrail[1]);
            ResolveCurrentRoute(game);
            yield return null;

            Assert.That(game.Phase, Is.EqualTo(GardenPhase.Recovery));
            Assert.That(game.RunState.lastFailure, Is.EqualTo(RouteFailure.EndsBeforeReceiver));
            Assert.That(routeLine.enabled, Is.True);
            Assert.That(routeLine.positionCount, Is.GreaterThanOrEqualTo(2));
            Assert.That(routeLine.sharedMaterial.name, Does.Contain("Faded"));
            Assert.That(game.StatusText, Does.Contain("stopped short"));
            Assert.That(game.StatusText, Does.Contain("Try again"));
        }

        [UnityTest]
        public IEnumerator BrowserPointerCancelUsesDistinctFocusRecovery()
        {
            yield return SceneManager.LoadSceneAsync("SignalGarden");
            yield return null;

            var game = UnityEngine.Object.FindAnyObjectByType<SignalGardenGame>();
            Assert.That(game, Is.Not.Null);
            game.RunState.BeginRoute(RouteRules.StandardTrail[0]);
            game.RunState.AppendRoutePoint(RouteRules.StandardTrail[1]);

            game.HandleBrowserPointerCancel();
            yield return null;

            Assert.That(game.Phase, Is.EqualTo(GardenPhase.Recovery));
            Assert.That(game.RunState.lastFailure, Is.EqualTo(RouteFailure.FocusInterrupted));
            Assert.That(game.RunState.routePoints, Is.Empty);
            Assert.That(game.StatusText, Does.Contain("Focus changed"));
        }

        [UnityTest]
        public IEnumerator SuccessReactionWakesFirefliesAndReducedMotionRestoresTheirMaterialState()
        {
            yield return SceneManager.LoadSceneAsync("SignalGarden");
            yield return null;

            var game = UnityEngine.Object.FindAnyObjectByType<SignalGardenGame>();
            var firefly = GameObject.Find("Garden firefly 1");
            Assert.That(game, Is.Not.Null);
            Assert.That(firefly, Is.Not.Null);
            Assert.That(game.SuccessReactionCount, Is.GreaterThan(0));

            var basePosition = firefly.transform.localPosition;
            game.RunState.BeginRoute(RouteRules.StandardTrail[0]);
            for (var i = 1; i < RouteRules.StandardTrail.Length; i++)
            {
                game.RunState.AppendRoutePoint(RouteRules.StandardTrail[i]);
            }

            ResolveCurrentRoute(game);
            Assert.That(game.Phase, Is.EqualTo(GardenPhase.Verified));
            Assert.That(game.SuccessReactionActive, Is.True);
            yield return null;
            Assert.That(firefly.transform.localPosition, Is.Not.EqualTo(basePosition));

            game.ToggleReducedMotionFromHud();
            yield return null;
            Assert.That(firefly.transform.localPosition, Is.EqualTo(basePosition));
            Assert.That(game.StatusText, Does.Contain("Reduced motion on"));
        }

        [UnityTest]
        public IEnumerator PerformanceTuningRemovesDecorativeShadowCostWithoutHidingGameplayMarkers()
        {
            yield return SceneManager.LoadSceneAsync("SignalGarden");
            yield return null;

            var game = UnityEngine.Object.FindAnyObjectByType<SignalGardenGame>();
            var fireflyRenderer = GameObject.Find("Garden firefly 1").GetComponent<Renderer>();
            var sourceRenderer = GameObject.Find("Coral signal").GetComponent<Renderer>();
            Assert.That(game.PerformanceTuningApplied, Is.True);
            Assert.That(fireflyRenderer.shadowCastingMode, Is.EqualTo(UnityEngine.Rendering.ShadowCastingMode.Off));
            Assert.That(fireflyRenderer.receiveShadows, Is.False);
            Assert.That(sourceRenderer.shadowCastingMode, Is.Not.EqualTo(UnityEngine.Rendering.ShadowCastingMode.Off));
        }

        [UnityTest]
        public IEnumerator ReducedMotionKeepsVerificationVisibleAndStopsReceiverMotion()
        {
            yield return SceneManager.LoadSceneAsync("SignalGarden");
            yield return null;

            var game = UnityEngine.Object.FindAnyObjectByType<SignalGardenGame>();
            var hud = UnityEngine.Object.FindAnyObjectByType<SignalGardenHud>();
            var receiver = GameObject.Find("Blue Receiver").transform;
            var receiverGlow = GameObject.Find("Receiver glow").GetComponent<Light>();
            Assert.That(game, Is.Not.Null);

            game.RunState.BeginRoute(RouteRules.StandardTrail[0]);
            for (var i = 1; i < RouteRules.StandardTrail.Length; i++)
            {
                game.RunState.AppendRoutePoint(RouteRules.StandardTrail[i]);
            }
            ResolveCurrentRoute(game);
            game.ToggleReducedMotionFromHud();
            yield return null;

            Assert.That(game.Phase, Is.EqualTo(GardenPhase.Verified));
            Assert.That(game.ReducedMotionEnabled, Is.True);
            Assert.That(hud.transform.Find("Status Panel/Phase Label").GetComponent<Text>().text,
                Is.EqualTo("VERIFIED  /  SIGNAL ROOTED"));
            Assert.That(hud.transform.Find("State Overlay").gameObject.activeSelf, Is.True);

            var rotation = receiver.rotation;
            var glow = receiverGlow.intensity;
            yield return new WaitForSeconds(0.25f);
            Assert.That(receiver.rotation, Is.EqualTo(rotation));
            Assert.That(receiverGlow.intensity, Is.EqualTo(glow).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator FocusLossCancelsPartialRouteAndAllowsAnotherAttempt()
        {
            yield return SceneManager.LoadSceneAsync("SignalGarden");
            yield return null;

            var game = UnityEngine.Object.FindAnyObjectByType<SignalGardenGame>();
            var hud = UnityEngine.Object.FindAnyObjectByType<SignalGardenHud>();
            Assert.That(game, Is.Not.Null);
            Assert.That(hud, Is.Not.Null);

            game.RunState.BeginRoute(RouteRules.StandardTrail[0]);
            game.RunState.AppendRoutePoint(RouteRules.StandardTrail[1]);
            InvokeLifecycleCallback(game, "OnApplicationFocus", false);
            yield return null;

            Assert.That(game.Phase, Is.EqualTo(GardenPhase.Recovery));
            Assert.That(game.RunState.lastFailure, Is.EqualTo(RouteFailure.FocusInterrupted));
            Assert.That(game.RunState.routePoints, Is.Empty);
            Assert.That(game.StatusText,
                Is.EqualTo("Focus changed, so the partial route was canceled. Start again at the coral source."));
            Assert.That(hud.DisplayedPhase, Is.EqualTo(GardenPhase.Recovery));

            InvokeLifecycleCallback(game, "OnApplicationFocus", true);
            yield return null;
            Assert.That(game.Phase, Is.EqualTo(GardenPhase.Recovery));

            Assert.That(game.RunState.BeginRoute(RouteRules.StandardTrail[0]), Is.True);
            for (var i = 1; i < RouteRules.StandardTrail.Length; i++)
            {
                game.RunState.AppendRoutePoint(RouteRules.StandardTrail[i]);
            }

            ResolveCurrentRoute(game);
            yield return null;
            Assert.That(game.Phase, Is.EqualTo(GardenPhase.Verified));
            Assert.That(game.RunState.verificationCount, Is.EqualTo(1));
            Assert.That(hud.DisplayedPhase, Is.EqualTo(GardenPhase.Verified));
        }

        [UnityTest]
        public IEnumerator ApplicationPauseCancelsPartialRouteAndLeavesSessionRecoverable()
        {
            yield return SceneManager.LoadSceneAsync("SignalGarden");
            yield return null;

            var game = UnityEngine.Object.FindAnyObjectByType<SignalGardenGame>();
            var hud = UnityEngine.Object.FindAnyObjectByType<SignalGardenHud>();
            Assert.That(game, Is.Not.Null);
            Assert.That(hud, Is.Not.Null);

            game.RunState.BeginRoute(RouteRules.StandardTrail[0]);
            game.RunState.AppendRoutePoint(RouteRules.StandardTrail[1]);
            InvokeLifecycleCallback(game, "OnApplicationPause", true);
            yield return null;

            Assert.That(game.Phase, Is.EqualTo(GardenPhase.Recovery));
            Assert.That(game.RunState.lastFailure, Is.EqualTo(RouteFailure.FocusInterrupted));
            Assert.That(game.RunState.routePoints, Is.Empty);
            Assert.That(game.StatusText, Does.Contain("partial route was canceled"));
            Assert.That(hud.DisplayedPhase, Is.EqualTo(GardenPhase.Recovery));

            InvokeLifecycleCallback(game, "OnApplicationPause", false);
            yield return null;
            Assert.That(game.Phase, Is.EqualTo(GardenPhase.Recovery));
            Assert.That(game.RunState.BeginRoute(RouteRules.StandardTrail[0]), Is.True);
        }

        [UnityTest]
        public IEnumerator ReloadDuringRoutingStartsACompletelyFreshSession()
        {
            yield return SceneManager.LoadSceneAsync("SignalGarden");
            yield return null;

            var previousGame = UnityEngine.Object.FindAnyObjectByType<SignalGardenGame>();
            Assert.That(previousGame, Is.Not.Null);
            previousGame.RunState.BeginRoute(RouteRules.StandardTrail[0]);
            previousGame.RunState.AppendRoutePoint(RouteRules.StandardTrail[1]);
            previousGame.ToggleSoundFromHud();
            previousGame.ToggleReducedMotionFromHud();

            yield return SceneManager.LoadSceneAsync("SignalGarden");
            yield return null;

            var game = UnityEngine.Object.FindAnyObjectByType<SignalGardenGame>();
            var hud = UnityEngine.Object.FindAnyObjectByType<SignalGardenHud>();
            Assert.That(game, Is.Not.Null);
            Assert.That(hud, Is.Not.Null);
            Assert.That(game, Is.Not.SameAs(previousGame));
            Assert.That(game.Phase, Is.EqualTo(GardenPhase.Observe));
            Assert.That(game.RunState.routePoints, Is.Empty);
            Assert.That(game.RunState.attemptCount, Is.Zero);
            Assert.That(game.RunState.verificationCount, Is.Zero);
            Assert.That(game.StatusText, Is.EqualTo("Ready. Drag from the coral source to begin."));
            Assert.That(game.SoundEnabled, Is.False);
            Assert.That(game.ReducedMotionEnabled, Is.False);
            Assert.That(hud.DisplayedPhase, Is.EqualTo(GardenPhase.Observe));
        }

        private static void ResolveCurrentRoute(SignalGardenGame game)
        {
            typeof(SignalGardenGame)
                .GetMethod("ResolveCurrentRoute", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(game, null);
        }

        private static void InvokeLifecycleCallback(SignalGardenGame game, string methodName, bool value)
        {
            var callback = typeof(SignalGardenGame).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(callback, Is.Not.Null, "Expected Unity lifecycle callback " + methodName);
            callback.Invoke(game, new object[] { value });
        }

        private static void AssertTopRightControl(RectTransform rect, float right, float top)
        {
            Assert.That(rect.anchorMin, Is.EqualTo(Vector2.one));
            Assert.That(rect.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(rect.pivot, Is.EqualTo(Vector2.one));
            Assert.That(rect.anchoredPosition, Is.EqualTo(new Vector2(-right, -top)));
            Assert.That(rect.rect.width, Is.GreaterThanOrEqualTo(40f));
            Assert.That(rect.rect.height, Is.GreaterThanOrEqualTo(40f));
        }
    }
}
