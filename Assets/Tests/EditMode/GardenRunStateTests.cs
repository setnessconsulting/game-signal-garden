using System.Globalization;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace SignalGarden.Tests
{
    /// <summary>
    /// Deterministic transition coverage for the Signal Garden run state.
    ///
    /// GardenRunState is the canonical state model: the GDD's phase table plus Paused is implemented
    /// here and driven by SignalGardenGame. These tests pin the legal transitions, the rejection of
    /// every illegal one, the counters that are the run's only score, and the adversarial sequences
    /// (repeat, out-of-order, terminal, reset-during-play) that would otherwise corrupt state.
    ///
    /// Legal transitions, taken from the GDD phase table:
    ///   Observe  -> Routing (BeginRoute) or Paused
    ///   Routing  -> Verified (ResolveAttempt(None)) / Recovery (ResolveAttempt(failure))
    ///            -> Recovery (CancelRoute) / Paused
    ///   Recovery -> Routing (BeginRoute) or Paused
    ///   Verified -> Observe (Reset) or Paused
    ///   Paused   -> the exact phase it paused from (Resume)
    ///
    /// There is no level, stage or campaign progression in this game. Verified is terminal for the
    /// turn, and a new turn begins only through Reset, so no test invents a "next level" transition.
    /// </summary>
    public sealed class GardenRunStateTests
    {
        private const string SourceId = "source-coral";
        private const string ReceiverId = "receiver-glass";

        // ---------------------------------------------------------------------------------------
        // Initial state
        // ---------------------------------------------------------------------------------------

        [Test]
        public void NewStateStartsInObserveWithCleanCountersAndStableIdentities()
        {
            var state = new GardenRunState();

            Assert.That(state.phase, Is.EqualTo(GardenPhase.Observe));
            Assert.That(state.phaseBeforePause, Is.EqualTo(GardenPhase.Observe));
            Assert.That(state.lastFailure, Is.EqualTo(RouteFailure.None));
            Assert.That(state.routePoints, Is.Empty);
            Assert.That(state.attemptCount, Is.Zero);
            Assert.That(state.verificationCount, Is.Zero);
            Assert.That(state.resetCount, Is.Zero);
            Assert.That(state.wasReset, Is.False);
            Assert.That(state.sourceId, Is.EqualTo(SourceId));
            Assert.That(state.receiverId, Is.EqualTo(ReceiverId));
        }

        [Test]
        public void ObserveRejectsEveryRoutingActionAndStaysUnchangedUntilBeginRoute()
        {
            var state = new GardenRunState();
            var before = Snapshot(state);

            Assert.That(state.AppendRoutePoint(new Vector2(0f, 0f)), Is.False);
            Assert.That(state.ResolveAttempt(RouteFailure.None), Is.False);
            Assert.That(state.ResolveAttempt(RouteFailure.LeavesTrail), Is.False);
            Assert.That(state.CancelRoute(), Is.False);
            Assert.That(state.Resume(), Is.False);
            Assert.That(Snapshot(state), Is.EqualTo(before), "rejected actions must not change state");

            Assert.That(state.BeginRoute(RouteRules.StandardTrail[0]), Is.True);
            Assert.That(state.phase, Is.EqualTo(GardenPhase.Routing));
            Assert.That(state.attemptCount, Is.EqualTo(1));
        }

        // ---------------------------------------------------------------------------------------
        // Observe / Recovery -> Routing
        // ---------------------------------------------------------------------------------------

        [Test]
        public void BeginRouteFromObserveSeedsTheRouteWithExactlyTheSourcePoint()
        {
            var state = new GardenRunState();

            Assert.That(state.BeginRoute(RouteRules.StandardTrail[0]), Is.True);

            Assert.That(state.phase, Is.EqualTo(GardenPhase.Routing));
            Assert.That(state.routePoints, Has.Count.EqualTo(1));
            Assert.That(state.routePoints[0], Is.EqualTo(RouteRules.StandardTrail[0]));
            Assert.That(state.lastFailure, Is.EqualTo(RouteFailure.None));
            Assert.That(state.attemptCount, Is.EqualTo(1));
            Assert.That(state.verificationCount, Is.Zero);
            Assert.That(state.resetCount, Is.Zero);
        }

        [Test]
        public void BeginRouteFromRecoveryStartsAFreshAttemptAndClearsTheStaleFailure()
        {
            var state = InRouting();
            state.AppendRoutePoint(new Vector2(-2.90f, 2.20f));
            state.AppendRoutePoint(new Vector2(-2.90f, 1.00f));
            state.ResolveAttempt(RouteFailure.LeavesTrail);

            Assert.That(state.phase, Is.EqualTo(GardenPhase.Recovery));
            Assert.That(state.routePoints, Has.Count.EqualTo(3), "the failure trace is retained in recovery");

            Assert.That(state.BeginRoute(RouteRules.StandardTrail[0]), Is.True);

            Assert.That(state.phase, Is.EqualTo(GardenPhase.Routing));
            Assert.That(state.routePoints, Has.Count.EqualTo(1));
            Assert.That(state.routePoints[0], Is.EqualTo(RouteRules.StandardTrail[0]));
            Assert.That(state.lastFailure, Is.EqualTo(RouteFailure.None));
            Assert.That(state.attemptCount, Is.EqualTo(2));
            Assert.That(state.verificationCount, Is.Zero);
        }

        [TestCase(GardenPhase.Routing)]
        [TestCase(GardenPhase.Verified)]
        [TestCase(GardenPhase.Paused)]
        public void BeginRouteIsRejectedFromEveryPhaseThatIsNotObserveOrRecovery(GardenPhase phase)
        {
            var state = StateInPhase(phase);
            var before = Snapshot(state);

            Assert.That(state.BeginRoute(RouteRules.StandardTrail[0]), Is.False);
            Assert.That(Snapshot(state), Is.EqualTo(before), "a rejected BeginRoute must not change state");
        }

        // ---------------------------------------------------------------------------------------
        // Routing -> route point sampling
        // ---------------------------------------------------------------------------------------

        [Test]
        public void AppendAcceptsPointsAtOrBeyondTheMinimumSpacing()
        {
            var state = new GardenRunState();
            state.BeginRoute(Vector2.zero);

            // Exactly at the threshold: the guard rejects only points strictly closer than the spacing.
            Assert.That(state.AppendRoutePoint(new Vector2(0.25f, 0f), 0.25f), Is.True);
            Assert.That(state.AppendRoutePoint(new Vector2(0.75f, 0f), 0.25f), Is.True);

            Assert.That(state.routePoints, Has.Count.EqualTo(3));
            Assert.That(state.phase, Is.EqualTo(GardenPhase.Routing));
            Assert.That(state.attemptCount, Is.EqualTo(1), "sampling points is not a new attempt");
        }

        [Test]
        public void AppendRejectsPointsInsideTheMinimumSpacing()
        {
            var state = new GardenRunState();
            state.BeginRoute(Vector2.zero);
            var before = Snapshot(state);

            Assert.That(state.AppendRoutePoint(new Vector2(0.125f, 0f), 0.25f), Is.False);
            Assert.That(state.routePoints, Has.Count.EqualTo(1));
            Assert.That(Snapshot(state), Is.EqualTo(before));
        }

        [Test]
        public void AppendUsesTheDefaultSpacingWhenNoneIsGiven()
        {
            var state = new GardenRunState();
            state.BeginRoute(Vector2.zero);

            Assert.That(state.AppendRoutePoint(new Vector2(0.01f, 0f)), Is.False, "0.01 is inside the default 0.045 spacing");
            Assert.That(state.AppendRoutePoint(new Vector2(1.00f, 0f)), Is.True);
        }

        [Test]
        public void AppendMeasuresSpacingFromTheMostRecentPointOnly()
        {
            var state = new GardenRunState();
            state.BeginRoute(Vector2.zero);
            state.AppendRoutePoint(new Vector2(1f, 0f), 0.25f);

            // Close to the origin, far from the most recent point: the guard is about the last point.
            Assert.That(state.AppendRoutePoint(new Vector2(0.5f, 0f), 0.25f), Is.True);
            Assert.That(state.routePoints, Has.Count.EqualTo(3));
        }

        [TestCase(GardenPhase.Observe)]
        [TestCase(GardenPhase.Recovery)]
        [TestCase(GardenPhase.Verified)]
        [TestCase(GardenPhase.Paused)]
        public void AppendIsRejectedOutsideRouting(GardenPhase phase)
        {
            var state = StateInPhase(phase);
            var before = Snapshot(state);

            Assert.That(state.AppendRoutePoint(new Vector2(1f, 1f)), Is.False);
            Assert.That(Snapshot(state), Is.EqualTo(before));
        }

        // ---------------------------------------------------------------------------------------
        // Routing -> Verified / Recovery
        // ---------------------------------------------------------------------------------------

        [Test]
        public void ResolveSuccessEntersVerifiedAndCountsExactlyOneVerification()
        {
            var state = InRouting();

            Assert.That(state.ResolveAttempt(RouteFailure.None), Is.True);

            Assert.That(state.phase, Is.EqualTo(GardenPhase.Verified));
            Assert.That(state.lastFailure, Is.EqualTo(RouteFailure.None));
            Assert.That(state.verificationCount, Is.EqualTo(1));
            Assert.That(state.attemptCount, Is.EqualTo(1));
        }

        [TestCase(RouteFailure.TooFewPoints)]
        [TestCase(RouteFailure.StartsAwayFromSource)]
        [TestCase(RouteFailure.EndsBeforeReceiver)]
        [TestCase(RouteFailure.LeavesTrail)]
        [TestCase(RouteFailure.ReversesDirection)]
        [TestCase(RouteFailure.Cancelled)]
        [TestCase(RouteFailure.FocusInterrupted)]
        public void ResolveFailureEntersRecoveryWithoutCountingAVerification(RouteFailure failure)
        {
            var state = InRouting();
            state.AppendRoutePoint(new Vector2(1f, 0f), 0.25f);
            var sampledPoints = state.routePoints.Count;

            Assert.That(state.ResolveAttempt(failure), Is.True);

            Assert.That(state.phase, Is.EqualTo(GardenPhase.Recovery));
            Assert.That(state.lastFailure, Is.EqualTo(failure));
            Assert.That(state.verificationCount, Is.Zero, "only RouteFailure.None may count a verification");
            Assert.That(state.attemptCount, Is.EqualTo(1), "the attempt was already counted by BeginRoute");
            Assert.That(state.routePoints, Has.Count.EqualTo(sampledPoints), "the failure trace is retained");
            Assert.That(state.resetCount, Is.Zero);
            Assert.That(state.wasReset, Is.False);
        }

        [TestCase(GardenPhase.Observe)]
        [TestCase(GardenPhase.Recovery)]
        [TestCase(GardenPhase.Verified)]
        [TestCase(GardenPhase.Paused)]
        public void ResolveIsRejectedOutsideRouting(GardenPhase phase)
        {
            var state = StateInPhase(phase);
            var before = Snapshot(state);

            Assert.That(state.ResolveAttempt(RouteFailure.None), Is.False);
            Assert.That(state.ResolveAttempt(RouteFailure.LeavesTrail), Is.False);
            Assert.That(Snapshot(state), Is.EqualTo(before));
        }

        [Test]
        public void CompletionCannotBeCountedTwiceAndVerifiedIsTerminalForTheTurn()
        {
            var state = InRouting();
            Assert.That(state.ResolveAttempt(RouteFailure.None), Is.True);

            var verified = Snapshot(state);

            // Every routing or completion event after success is inert.
            Assert.That(state.ResolveAttempt(RouteFailure.None), Is.False, "a second completion must be ignored");
            Assert.That(state.ResolveAttempt(RouteFailure.LeavesTrail), Is.False, "failure cannot replace success");
            Assert.That(state.BeginRoute(RouteRules.StandardTrail[0]), Is.False, "no new attempt after success");
            Assert.That(state.AppendRoutePoint(new Vector2(1f, 1f)), Is.False);
            Assert.That(state.CancelRoute(), Is.False);

            Assert.That(state.verificationCount, Is.EqualTo(1), "completion happens exactly once");
            Assert.That(state.attemptCount, Is.EqualTo(1));
            Assert.That(Snapshot(state), Is.EqualTo(verified));
        }

        [Test]
        public void VerifiedTurnsAreOnlyLeftByPauseResumeOrReplayReset()
        {
            var state = InVerified();

            Assert.That(state.Pause(), Is.True);
            Assert.That(state.phase, Is.EqualTo(GardenPhase.Paused));
            Assert.That(state.Resume(), Is.True);
            Assert.That(state.phase, Is.EqualTo(GardenPhase.Verified));

            state.Reset();

            Assert.That(state.phase, Is.EqualTo(GardenPhase.Observe));
            Assert.That(state.verificationCount, Is.Zero);
        }

        // ---------------------------------------------------------------------------------------
        // Routing -> Recovery by cancellation
        // ---------------------------------------------------------------------------------------

        [Test]
        public void CancelFromRoutingClearsTheRouteAndDefaultReasonIsCancelled()
        {
            var state = InRouting();
            state.AppendRoutePoint(new Vector2(1f, 0f), 0.25f);
            state.AppendRoutePoint(new Vector2(2f, 0f), 0.25f);

            Assert.That(state.CancelRoute(), Is.True);

            Assert.That(state.phase, Is.EqualTo(GardenPhase.Recovery));
            Assert.That(state.lastFailure, Is.EqualTo(RouteFailure.Cancelled));
            Assert.That(state.routePoints, Is.Empty);
            Assert.That(state.attemptCount, Is.EqualTo(1), "cancelling does not count a second attempt");
            Assert.That(state.verificationCount, Is.Zero);
        }

        [Test]
        public void CancelAcceptsAnExplicitFocusInterruptedReason()
        {
            var state = InRouting();

            Assert.That(state.CancelRoute(RouteFailure.FocusInterrupted), Is.True);
            Assert.That(state.lastFailure, Is.EqualTo(RouteFailure.FocusInterrupted));
            Assert.That(state.phase, Is.EqualTo(GardenPhase.Recovery));
            Assert.That(state.BeginRoute(RouteRules.StandardTrail[0]), Is.True, "recovery allows an immediate retry");
        }

        [TestCase(GardenPhase.Observe)]
        [TestCase(GardenPhase.Recovery)]
        [TestCase(GardenPhase.Verified)]
        [TestCase(GardenPhase.Paused)]
        public void CancelIsRejectedOutsideRouting(GardenPhase phase)
        {
            var state = StateInPhase(phase);
            var before = Snapshot(state);

            Assert.That(state.CancelRoute(), Is.False);
            Assert.That(Snapshot(state), Is.EqualTo(before));
        }

        // ---------------------------------------------------------------------------------------
        // Pause / resume
        // ---------------------------------------------------------------------------------------

        [TestCase(GardenPhase.Observe)]
        [TestCase(GardenPhase.Routing)]
        [TestCase(GardenPhase.Recovery)]
        [TestCase(GardenPhase.Verified)]
        public void PauseRemembersTheExactPhaseItPausedFrom(GardenPhase phase)
        {
            var state = StateInPhase(phase);

            Assert.That(state.Pause(), Is.True);
            Assert.That(state.phase, Is.EqualTo(GardenPhase.Paused));
            Assert.That(state.phaseBeforePause, Is.EqualTo(phase));

            Assert.That(state.Resume(), Is.True);
            Assert.That(state.phase, Is.EqualTo(phase));
        }

        [Test]
        public void PauseTwiceIsRejectedAndKeepsTheOriginalMemory()
        {
            var state = InRouting();

            Assert.That(state.Pause(), Is.True);
            Assert.That(state.Pause(), Is.False, "a second pause must not overwrite the remembered phase");
            Assert.That(state.phaseBeforePause, Is.EqualTo(GardenPhase.Routing));

            Assert.That(state.Resume(), Is.True);
            Assert.That(state.phase, Is.EqualTo(GardenPhase.Routing));
        }

        [TestCase(GardenPhase.Observe)]
        [TestCase(GardenPhase.Routing)]
        [TestCase(GardenPhase.Recovery)]
        [TestCase(GardenPhase.Verified)]
        public void ResumeIsRejectedWhenNotPaused(GardenPhase phase)
        {
            var state = StateInPhase(phase);
            var before = Snapshot(state);

            Assert.That(state.Resume(), Is.False);
            Assert.That(Snapshot(state), Is.EqualTo(before));
        }

        [Test]
        public void PausedBlocksEveryGameplayTransitionUntilResumed()
        {
            var state = InRouting();
            state.Pause();
            var paused = Snapshot(state);

            Assert.That(state.BeginRoute(RouteRules.StandardTrail[0]), Is.False);
            Assert.That(state.AppendRoutePoint(new Vector2(1f, 1f)), Is.False);
            Assert.That(state.ResolveAttempt(RouteFailure.None), Is.False);
            Assert.That(state.CancelRoute(), Is.False);
            Assert.That(Snapshot(state), Is.EqualTo(paused), "input must be inert while paused");

            Assert.That(state.Resume(), Is.True);
            Assert.That(state.phase, Is.EqualTo(GardenPhase.Routing));
            Assert.That(state.CancelRoute(), Is.True, "the route is resumed intact");
            Assert.That(state.phase, Is.EqualTo(GardenPhase.Recovery));
        }

        // ---------------------------------------------------------------------------------------
        // Reset / replay
        // ---------------------------------------------------------------------------------------

        [TestCase(GardenPhase.Observe)]
        [TestCase(GardenPhase.Routing)]
        [TestCase(GardenPhase.Recovery)]
        [TestCase(GardenPhase.Verified)]
        [TestCase(GardenPhase.Paused)]
        public void ResetFromEveryPhaseRestoresTheCanonicalObserveState(GardenPhase phase)
        {
            var state = StateInPhase(phase);

            state.Reset();

            Assert.That(state.phase, Is.EqualTo(GardenPhase.Observe));
            Assert.That(state.phaseBeforePause, Is.EqualTo(GardenPhase.Observe));
            Assert.That(state.lastFailure, Is.EqualTo(RouteFailure.None));
            Assert.That(state.routePoints, Is.Empty);
            Assert.That(state.attemptCount, Is.Zero);
            Assert.That(state.verificationCount, Is.Zero);
            Assert.That(state.resetCount, Is.EqualTo(1));
            Assert.That(state.wasReset, Is.True);
            Assert.That(state.sourceId, Is.EqualTo(SourceId));
            Assert.That(state.receiverId, Is.EqualTo(ReceiverId));

            Assert.That(state.BeginRoute(RouteRules.StandardTrail[0]), Is.True, "reset leaves the game playable");
        }

        [Test]
        public void ResetClearsPausedMemorySoResumeCannotRevertToTheOldPhase()
        {
            var state = InVerified();
            state.Pause();

            state.Reset();

            Assert.That(state.phase, Is.EqualTo(GardenPhase.Observe));
            Assert.That(state.phaseBeforePause, Is.EqualTo(GardenPhase.Observe));
            Assert.That(state.Resume(), Is.False, "there is no paused phase to resume after reset");
            Assert.That(state.phase, Is.EqualTo(GardenPhase.Observe));
        }

        [Test]
        public void RepeatedResetIsIdempotentForGameplayAndOnlyCountsResets()
        {
            var state = InRouting();
            state.Reset();
            var once = Snapshot(state);

            state.Reset();

            Assert.That(state.resetCount, Is.EqualTo(2));
            Assert.That(state.wasReset, Is.True);
            Assert.That(state.phase, Is.EqualTo(GardenPhase.Observe));
            Assert.That(state.routePoints, Is.Empty);
            // Only resetCount differs between one reset and two; gameplay state is identical.
            Assert.That(Snapshot(state), Is.Not.EqualTo(once));
            Assert.That(state.attemptCount, Is.Zero);
            Assert.That(state.verificationCount, Is.Zero);
        }

        [Test]
        public void ResetThenPlayCountsFreshAttemptsAndVerifications()
        {
            var state = InVerified();

            state.Reset();
            Assert.That(state.BeginRoute(RouteRules.StandardTrail[0]), Is.True);
            Assert.That(state.ResolveAttempt(RouteFailure.None), Is.True);

            Assert.That(state.attemptCount, Is.EqualTo(1));
            Assert.That(state.verificationCount, Is.EqualTo(1));
            Assert.That(state.resetCount, Is.EqualTo(1));
        }

        // ---------------------------------------------------------------------------------------
        // End-to-end attempts and adversarial sequences
        // ---------------------------------------------------------------------------------------

        [Test]
        public void FailureThenRetryThenSuccessCountsTwoAttemptsAndOneVerification()
        {
            var state = new GardenRunState();

            Assert.That(state.BeginRoute(RouteRules.StandardTrail[0]), Is.True);
            state.AppendRoutePoint(RouteRules.StandardTrail[1]);
            Assert.That(state.ResolveAttempt(RouteFailure.EndsBeforeReceiver), Is.True);
            Assert.That(state.phase, Is.EqualTo(GardenPhase.Recovery));

            Assert.That(state.BeginRoute(RouteRules.StandardTrail[0]), Is.True);
            for (var i = 1; i < RouteRules.StandardTrail.Length; i++)
            {
                state.AppendRoutePoint(RouteRules.StandardTrail[i]);
            }

            Assert.That(state.ResolveAttempt(RouteFailure.None), Is.True);

            Assert.That(state.phase, Is.EqualTo(GardenPhase.Verified));
            Assert.That(state.attemptCount, Is.EqualTo(2));
            Assert.That(state.verificationCount, Is.EqualTo(1));
        }

        [Test]
        public void AnOutOfOrderEventBurstCannotMutateTheState()
        {
            var state = new GardenRunState();
            var before = Snapshot(state);

            // Every one of these is illegal from Observe.
            state.AppendRoutePoint(new Vector2(0.5f, 0.5f));
            state.ResolveAttempt(RouteFailure.None);
            state.ResolveAttempt(RouteFailure.LeavesTrail);
            state.CancelRoute();
            state.ResolveAttempt(RouteFailure.None);
            state.Resume();
            state.CancelRoute(RouteFailure.Cancelled);

            Assert.That(Snapshot(state), Is.EqualTo(before));
            Assert.That(state.phase, Is.EqualTo(GardenPhase.Observe));
            Assert.That(state.attemptCount, Is.Zero);
            Assert.That(state.verificationCount, Is.Zero);
            Assert.That(state.resetCount, Is.Zero);
        }

        [Test]
        public void SameEventRepeatedIsIdempotentAcrossEveryPhase()
        {
            // Append is rejected when the point repeats inside the spacing, and the phase never drifts.
            var routing = InRouting();
            routing.AppendRoutePoint(new Vector2(0.25f, 0f), 0.25f);
            var afterFirst = Snapshot(routing);
            Assert.That(routing.AppendRoutePoint(new Vector2(0.25f, 0f), 0.25f), Is.False);
            Assert.That(Snapshot(routing), Is.EqualTo(afterFirst));

            var verified = InVerified();
            var verifiedSnapshot = Snapshot(verified);
            for (var i = 0; i < 3; i++)
            {
                Assert.That(verified.ResolveAttempt(RouteFailure.None), Is.False);
            }

            Assert.That(Snapshot(verified), Is.EqualTo(verifiedSnapshot));
            Assert.That(verified.verificationCount, Is.EqualTo(1));
        }

        [Test]
        public void IdenticalActionSequencesProduceIdenticalStates()
        {
            var first = ScriptedRun();
            var second = ScriptedRun();

            Assert.That(JsonUtility.ToJson(second), Is.EqualTo(JsonUtility.ToJson(first)));
        }

        [Test]
        public void JsonRoundTripPreservesObserveRoutingRecoveryVerifiedAndPausedStates()
        {
            var phases = new[]
            {
                GardenPhase.Observe,
                GardenPhase.Routing,
                GardenPhase.Recovery,
                GardenPhase.Verified,
                GardenPhase.Paused
            };

            foreach (var phase in phases)
            {
                var state = StateInPhase(phase);
                var json = JsonUtility.ToJson(state);
                var restored = JsonUtility.FromJson<GardenRunState>(json);

                Assert.That(restored.phase, Is.EqualTo(state.phase), phase.ToString());
                Assert.That(restored.phaseBeforePause, Is.EqualTo(state.phaseBeforePause), phase.ToString());
                Assert.That(restored.lastFailure, Is.EqualTo(state.lastFailure), phase.ToString());
                Assert.That(restored.attemptCount, Is.EqualTo(state.attemptCount), phase.ToString());
                Assert.That(restored.verificationCount, Is.EqualTo(state.verificationCount), phase.ToString());
                Assert.That(restored.resetCount, Is.EqualTo(state.resetCount), phase.ToString());
                Assert.That(restored.wasReset, Is.EqualTo(state.wasReset), phase.ToString());
                Assert.That(restored.routePoints.Count, Is.EqualTo(state.routePoints.Count), phase.ToString());
                Assert.That(JsonUtility.ToJson(restored), Is.EqualTo(json), phase.ToString());
            }
        }

        // ---------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------

        private static GardenRunState InRouting()
        {
            var state = new GardenRunState();
            state.BeginRoute(RouteRules.StandardTrail[0]);
            return state;
        }

        private static GardenRunState InRecovery()
        {
            var state = InRouting();
            state.ResolveAttempt(RouteFailure.EndsBeforeReceiver);
            return state;
        }

        private static GardenRunState InVerified()
        {
            var state = InRouting();
            state.ResolveAttempt(RouteFailure.None);
            return state;
        }

        private static GardenRunState InPaused()
        {
            var state = InRouting();
            state.Pause();
            return state;
        }

        private static GardenRunState StateInPhase(GardenPhase phase)
        {
            switch (phase)
            {
                case GardenPhase.Observe:
                    return new GardenRunState();
                case GardenPhase.Routing:
                    return InRouting();
                case GardenPhase.Recovery:
                    return InRecovery();
                case GardenPhase.Verified:
                    return InVerified();
                case GardenPhase.Paused:
                    return InPaused();
                default:
                    throw new AssertionException("Unhandled phase in test setup: " + phase);
            }
        }

        /// <summary>
        /// A whole turn: one rejected completion while observing, a failed attempt, a retry paused and
        /// resumed, then a successful attempt. Used to prove the state machine is a pure function of
        /// the event sequence.
        /// </summary>
        private static GardenRunState ScriptedRun()
        {
            var state = new GardenRunState();
            state.ResolveAttempt(RouteFailure.None);
            state.BeginRoute(RouteRules.StandardTrail[0]);
            state.AppendRoutePoint(RouteRules.StandardTrail[1]);
            state.ResolveAttempt(RouteFailure.EndsBeforeReceiver);
            state.BeginRoute(RouteRules.StandardTrail[0]);
            state.Pause();
            state.Resume();
            for (var i = 1; i < RouteRules.StandardTrail.Length; i++)
            {
                state.AppendRoutePoint(RouteRules.StandardTrail[i]);
            }

            state.ResolveAttempt(RouteFailure.None);
            return state;
        }

        /// <summary>
        /// A full, order-sensitive rendering of every field, used to prove rejected actions are inert.
        /// Route points are formatted with an invariant culture so the string cannot vary by locale.
        /// </summary>
        private static string Snapshot(GardenRunState state)
        {
            var points = new StringBuilder();
            foreach (var point in state.routePoints)
            {
                points.Append(point.x.ToString("R", CultureInfo.InvariantCulture));
                points.Append(',');
                points.Append(point.y.ToString("R", CultureInfo.InvariantCulture));
                points.Append(';');
            }

            return string.Join("|", new[]
            {
                state.sourceId,
                state.receiverId,
                state.phase.ToString(),
                state.phaseBeforePause.ToString(),
                state.lastFailure.ToString(),
                state.attemptCount.ToString(CultureInfo.InvariantCulture),
                state.verificationCount.ToString(CultureInfo.InvariantCulture),
                state.resetCount.ToString(CultureInfo.InvariantCulture),
                state.wasReset ? "1" : "0",
                points.ToString()
            });
        }
    }
}
