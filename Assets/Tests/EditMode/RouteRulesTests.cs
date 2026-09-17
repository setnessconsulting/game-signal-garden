using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SignalGarden.Tests
{
    public sealed class RouteRulesTests
    {
        [Test]
        public void StandardTrailIsAValidCompleteRoute()
        {
            var route = new List<Vector2>(RouteRules.StandardTrail);

            Assert.That(RouteRules.Validate(route, RouteRules.StandardTrail), Is.EqualTo(RouteFailure.None));
        }

        [Test]
        public void ShortBlindSpurCannotVerify()
        {
            var route = new List<Vector2>
            {
                RouteRules.StandardTrail[0],
                RouteRules.StandardTrail[1],
                RouteRules.StandardTrail[2],
                RouteRules.DeadEndSpur[1],
                RouteRules.DeadEndSpur[2]
            };

            Assert.That(RouteRules.Validate(route, RouteRules.StandardTrail), Is.EqualTo(RouteFailure.LeavesTrail));
        }

        [Test]
        public void PartialRouteOnTheTrailCanBeRecovered()
        {
            var route = new List<Vector2>
            {
                RouteRules.StandardTrail[0],
                RouteRules.StandardTrail[1],
                RouteRules.StandardTrail[2]
            };

            Assert.That(RouteRules.Validate(route, RouteRules.StandardTrail), Is.EqualTo(RouteFailure.EndsBeforeReceiver));
        }

        [Test]
        public void RouteThatCutsAwayFromTrailIsRejected()
        {
            var route = new List<Vector2>(RouteRules.StandardTrail);
            route[4] += new Vector2(0f, 0.75f);

            Assert.That(RouteRules.Validate(route, RouteRules.StandardTrail), Is.EqualTo(RouteFailure.LeavesTrail));
        }

        [Test]
        public void BacktrackingAlongTrailIsRejected()
        {
            var route = new List<Vector2>
            {
                RouteRules.StandardTrail[0],
                RouteRules.StandardTrail[1],
                RouteRules.StandardTrail[2],
                RouteRules.StandardTrail[1],
                RouteRules.StandardTrail[2]
            };
            for (var i = 3; i < RouteRules.StandardTrail.Length; i++)
            {
                route.Add(RouteRules.StandardTrail[i]);
            }

            Assert.That(RouteRules.Validate(route, RouteRules.StandardTrail), Is.EqualTo(RouteFailure.ReversesDirection));
        }

        [Test]
        public void SourceEndpointToleranceIncludesNearBoundaryAndRejectsOutside()
        {
            var routeAtBoundary = new List<Vector2>(RouteRules.StandardTrail);
            routeAtBoundary[0] += Vector2.right * 0.45f;
            Assert.That(RouteRules.Validate(routeAtBoundary, RouteRules.StandardTrail), Is.EqualTo(RouteFailure.None));

            var routeOutsideBoundary = new List<Vector2>(RouteRules.StandardTrail);
            routeOutsideBoundary[0] += Vector2.right * 0.54f;
            Assert.That(RouteRules.Validate(routeOutsideBoundary, RouteRules.StandardTrail), Is.EqualTo(RouteFailure.StartsAwayFromSource));
        }

        [Test]
        public void CancelledAndFocusInterruptedRoutesReturnToRecoveryAndAllowRetry()
        {
            var cancelled = new GardenRunState();
            Assert.That(cancelled.BeginRoute(RouteRules.StandardTrail[0]), Is.True);
            Assert.That(cancelled.AppendRoutePoint(RouteRules.StandardTrail[1]), Is.True);
            Assert.That(cancelled.CancelRoute(), Is.True);
            Assert.That(cancelled.phase, Is.EqualTo(GardenPhase.Recovery));
            Assert.That(cancelled.routePoints, Is.Empty);
            Assert.That(cancelled.BeginRoute(RouteRules.StandardTrail[0]), Is.True);

            var interrupted = new GardenRunState();
            Assert.That(interrupted.BeginRoute(RouteRules.StandardTrail[0]), Is.True);
            Assert.That(interrupted.CancelRoute(RouteFailure.FocusInterrupted), Is.True);
            Assert.That(interrupted.lastFailure, Is.EqualTo(RouteFailure.FocusInterrupted));
            Assert.That(interrupted.phase, Is.EqualTo(GardenPhase.Recovery));
            Assert.That(interrupted.BeginRoute(RouteRules.StandardTrail[0]), Is.True);
        }

        [Test]
        public void VerificationIsDeterministicAndCannotBeCountedTwice()
        {
            var state = NewRoutingState();

            Assert.That(state.ResolveAttempt(RouteFailure.None), Is.True);
            Assert.That(state.phase, Is.EqualTo(GardenPhase.Verified));
            Assert.That(state.verificationCount, Is.EqualTo(1));
            Assert.That(state.ResolveAttempt(RouteFailure.None), Is.False);
            Assert.That(state.verificationCount, Is.EqualTo(1));
            Assert.That(state.BeginRoute(RouteRules.StandardTrail[0]), Is.False);
        }

        [Test]
        public void ResetRestoresInitialGameplayAndKeepsOnlyLocalResetEvidence()
        {
            var state = NewRoutingState();
            state.ResolveAttempt(RouteFailure.None);

            state.Reset();

            Assert.That(state.phase, Is.EqualTo(GardenPhase.Observe));
            Assert.That(state.routePoints, Is.Empty);
            Assert.That(state.attemptCount, Is.Zero);
            Assert.That(state.verificationCount, Is.Zero);
            Assert.That(state.wasReset, Is.True);
            Assert.That(state.resetCount, Is.EqualTo(1));
            Assert.That(state.sourceId, Is.EqualTo("source-coral"));
            Assert.That(state.receiverId, Is.EqualTo("receiver-glass"));
            Assert.That(state.BeginRoute(RouteRules.StandardTrail[0]), Is.True);
        }

        [Test]
        public void PauseResumeRestoresTheExactPriorPhase()
        {
            var state = new GardenRunState();
            Assert.That(state.Pause(), Is.True);
            Assert.That(state.phase, Is.EqualTo(GardenPhase.Paused));
            Assert.That(state.Resume(), Is.True);
            Assert.That(state.phase, Is.EqualTo(GardenPhase.Observe));

            state.BeginRoute(RouteRules.StandardTrail[0]);
            Assert.That(state.Pause(), Is.True);
            Assert.That(state.Resume(), Is.True);
            Assert.That(state.phase, Is.EqualTo(GardenPhase.Routing));
        }

        [Test]
        public void RunStateSurvivesJsonRoundTripAndSceneReloadStartsFresh()
        {
            var state = new GardenRunState();
            state.BeginRoute(RouteRules.StandardTrail[0]);
            state.AppendRoutePoint(RouteRules.StandardTrail[1]);
            var json = JsonUtility.ToJson(state);
            var restored = JsonUtility.FromJson<GardenRunState>(json);

            Assert.That(restored.sourceId, Is.EqualTo(state.sourceId));
            Assert.That(restored.receiverId, Is.EqualTo(state.receiverId));
            Assert.That(restored.phase, Is.EqualTo(GardenPhase.Routing));
            Assert.That(restored.attemptCount, Is.EqualTo(1));
            Assert.That(restored.routePoints, Has.Count.EqualTo(2));

            var reloaded = new GardenRunState();
            Assert.That(reloaded.phase, Is.EqualTo(GardenPhase.Observe));
            Assert.That(reloaded.routePoints, Is.Empty);
            Assert.That(reloaded.attemptCount, Is.Zero);
            Assert.That(reloaded.verificationCount, Is.Zero);
        }

        private static GardenRunState NewRoutingState()
        {
            var state = new GardenRunState();
            state.BeginRoute(RouteRules.StandardTrail[0]);
            for (var i = 1; i < RouteRules.StandardTrail.Length; i++)
            {
                state.AppendRoutePoint(RouteRules.StandardTrail[i]);
            }

            return state;
        }
    }
}
