using System.Collections;
using NUnit.Framework;
using SignalGarden;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

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
    }
}
