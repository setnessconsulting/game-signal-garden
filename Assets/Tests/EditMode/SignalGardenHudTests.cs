using NUnit.Framework;
using UnityEngine;

namespace SignalGarden.Tests
{
    public sealed class SignalGardenHudTests
    {
        [TestCase(GardenPhase.Observe, "OBSERVE  /  FIND THE PATH")]
        [TestCase(GardenPhase.Routing, "ROUTING  /  HOLD TO TRACE")]
        [TestCase(GardenPhase.Recovery, "RECOVERY  /  TRY AGAIN")]
        [TestCase(GardenPhase.Verified, "VERIFIED  /  SIGNAL ROOTED")]
        [TestCase(GardenPhase.Paused, "PAUSED  /  TURN SAFE")]
        public void EachRunPhaseHasAReadableTextLabel(GardenPhase phase, string expected)
        {
            Assert.That(SignalGardenHud.GetPhaseLabel(phase), Is.EqualTo(expected));
        }

        [Test]
        public void NewHudDoesNotClaimACompleteBindingSet()
        {
            var objectUnderTest = new GameObject("HUD test");
            try
            {
                var hud = objectUnderTest.AddComponent<SignalGardenHud>();
                Assert.That(hud.HasCompleteBinding, Is.False);
                Assert.That(hud.DisplayedPhase, Is.EqualTo(GardenPhase.Observe));
            }
            finally
            {
                Object.DestroyImmediate(objectUnderTest);
            }
        }
    }
}
