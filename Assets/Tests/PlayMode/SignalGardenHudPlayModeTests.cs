using System.Collections;
using NUnit.Framework;
using SignalGarden;
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
        }
    }
}
