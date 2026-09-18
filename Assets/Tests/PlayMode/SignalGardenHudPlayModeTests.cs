using System.Collections;
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
            var motionRect = hud.transform.Find("Reduced Motion Button").GetComponent<RectTransform>();
            AssertTopRightControl(soundRect, 210f, 26f);
            AssertTopRightControl(motionRect, 26f, 26f);
            Assert.That(Application.targetFrameRate, Is.GreaterThanOrEqualTo(60));

            var eventSystem = EventSystem.current;
            Assert.That(eventSystem, Is.Not.Null);
            hud.MoveKeyboardFocus(false);
            Assert.That(eventSystem.currentSelectedGameObject.name, Is.EqualTo("Sound Cues Button"));
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
