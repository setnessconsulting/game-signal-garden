using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SignalGarden.Tests
{
    /// <summary>
    /// Boundary, malformed-input and determinism coverage for the route rule engine.
    ///
    /// RouteRules.Validate is the pure function that decides whether a dragged route verifies, and it
    /// is the only place a turn can succeed. RouteRulesTests already covers the shape of each failure
    /// on the authored trail; this suite pins the edges those tests step around:
    ///
    /// - malformed inputs (null, too short, degenerate trail segments) fail safely as TooFewPoints;
    /// - each inclusive tolerance accepts the boundary value and rejects just beyond it;
    /// - the tolerance parameters are actually consumed rather than ignored;
    /// - validation is a pure, deterministic function of its arguments and never mutates them;
    /// - the authored trail is a connected, solvable puzzle whose dead end is genuinely off-trail.
    /// </summary>
    public sealed class RouteRulesBoundaryTests
    {
        // A straight reference trail keeps every expected distance exact and easy to reason about.
        private static readonly Vector2 Origin = new Vector2(0f, 0f);
        private static readonly Vector2 StraightEnd = new Vector2(10f, 0f);

        private static List<Vector2> StraightTrail()
        {
            return new List<Vector2> { Origin, StraightEnd };
        }

        private static List<Vector2> StraightRoute(params float[] xs)
        {
            var route = new List<Vector2>();
            foreach (var x in xs)
            {
                route.Add(new Vector2(x, 0f));
            }

            return route;
        }

        // ---------------------------------------------------------------------------------------
        // Malformed input
        // ---------------------------------------------------------------------------------------

        [Test]
        public void ValidateRejectsNullRouteOrTrailWithoutThrowing()
        {
            Assert.That(RouteRules.Validate(null, StraightTrail()), Is.EqualTo(RouteFailure.TooFewPoints));
            Assert.That(RouteRules.Validate(StraightRoute(0f, 10f), null), Is.EqualTo(RouteFailure.TooFewPoints));
            Assert.That(RouteRules.Validate(null, null), Is.EqualTo(RouteFailure.TooFewPoints));
        }

        [Test]
        public void ValidateRejectsRoutesAndTrailsWithFewerThanTwoPoints()
        {
            var singlePointRoute = new List<Vector2> { Origin };
            var emptyRoute = new List<Vector2>();
            var singlePointTrail = new List<Vector2> { Origin };
            var emptyTrail = new List<Vector2>();

            Assert.That(RouteRules.Validate(singlePointRoute, StraightTrail()), Is.EqualTo(RouteFailure.TooFewPoints));
            Assert.That(RouteRules.Validate(emptyRoute, StraightTrail()), Is.EqualTo(RouteFailure.TooFewPoints));
            Assert.That(RouteRules.Validate(StraightRoute(0f, 10f), singlePointTrail), Is.EqualTo(RouteFailure.TooFewPoints));
            Assert.That(RouteRules.Validate(StraightRoute(0f, 10f), emptyTrail), Is.EqualTo(RouteFailure.TooFewPoints));
        }

        [Test]
        public void ValidateRejectsADegenerateTrailSegment()
        {
            var degenerateTrail = new List<Vector2> { Origin, Origin, StraightEnd };

            Assert.That(RouteRules.Validate(StraightRoute(0f, 10f), degenerateTrail), Is.EqualTo(RouteFailure.TooFewPoints));
        }

        // ---------------------------------------------------------------------------------------
        // Inclusive tolerance boundaries
        // ---------------------------------------------------------------------------------------

        [Test]
        public void ValidateAcceptsARouteThatStartsExactlyOnTheSourceTolerance()
        {
            var route = new List<Vector2>(RouteRules.StandardTrail);
            route[0] += Vector2.right * RouteRules.EndpointTolerance;

            Assert.That(RouteRules.Validate(route, RouteRules.StandardTrail), Is.EqualTo(RouteFailure.None));

            var beyond = new List<Vector2>(RouteRules.StandardTrail);
            beyond[0] += Vector2.right * (RouteRules.EndpointTolerance + 0.01f);

            Assert.That(RouteRules.Validate(beyond, RouteRules.StandardTrail), Is.EqualTo(RouteFailure.StartsAwayFromSource));
        }

        [Test]
        public void ValidateAcceptsAReleaseExactlyOnTheReceiverToleranceAndRejectsJustPast()
        {
            // The route stops EndpointTolerance short of the receiver: still a completed route.
            var atBoundary = StraightRoute(0f, 10f - RouteRules.EndpointTolerance);
            Assert.That(RouteRules.Validate(atBoundary, StraightTrail()), Is.EqualTo(RouteFailure.None));

            var pastBoundary = StraightRoute(0f, 10f - RouteRules.EndpointTolerance - 0.1f);
            Assert.That(RouteRules.Validate(pastBoundary, StraightTrail()), Is.EqualTo(RouteFailure.EndsBeforeReceiver));
        }

        [Test]
        public void ValidateRejectsARouteThatLeavesTheCorridorBeyondItsRadius()
        {
            var inside = new List<Vector2> { Origin, new Vector2(5f, RouteRules.CorridorRadius - 0.01f), StraightEnd };
            Assert.That(RouteRules.Validate(inside, StraightTrail()), Is.EqualTo(RouteFailure.None));

            var outside = new List<Vector2> { Origin, new Vector2(5f, RouteRules.CorridorRadius + 0.04f), StraightEnd };
            Assert.That(RouteRules.Validate(outside, StraightTrail()), Is.EqualTo(RouteFailure.LeavesTrail));
        }

        [Test]
        public void ValidateToleratesBacktrackingUpToTheDirectionSlackAndRejectsBeyondIt()
        {
            var withinSlack = StraightRoute(0f, 4f, 3.9f, 5f, 10f);
            Assert.That(RouteRules.Validate(withinSlack, StraightTrail()), Is.EqualTo(RouteFailure.None));

            var beyondSlack = StraightRoute(0f, 4f, 3.7f, 5f, 10f);
            Assert.That(RouteRules.Validate(beyondSlack, StraightTrail()), Is.EqualTo(RouteFailure.ReversesDirection));
        }

        [Test]
        public void ValidateHonoursTheSuppliedCorridorRadius()
        {
            // Off-corridor under the default radius...
            var route = new List<Vector2> { Origin, new Vector2(5f, 1f), StraightEnd };
            Assert.That(RouteRules.Validate(route, StraightTrail()), Is.EqualTo(RouteFailure.LeavesTrail));

            // ...but inside a deliberately widened corridor, so the parameter is consumed, not ignored.
            Assert.That(
                RouteRules.Validate(route, StraightTrail(), corridorRadius: 1.5f),
                Is.EqualTo(RouteFailure.None));
        }

        // ---------------------------------------------------------------------------------------
        // Purity and determinism
        // ---------------------------------------------------------------------------------------

        [Test]
        public void ValidateDoesNotMutateItsInputs()
        {
            var route = new List<Vector2>(RouteRules.StandardTrail);
            route[4] += new Vector2(0f, 0.75f);
            var trail = new List<Vector2>(RouteRules.StandardTrail);

            var routeCopy = new List<Vector2>(route);
            var trailCopy = new List<Vector2>(trail);

            RouteRules.Validate(route, trail);

            Assert.That(route, Is.EqualTo(routeCopy), "the route list must not be reordered or rewritten");
            Assert.That(trail, Is.EqualTo(trailCopy), "the trail list must not be mutated");
        }

        [Test]
        public void ValidateIsDeterministicAcrossRepeatedCallsAndFreshLists()
        {
            var first = RouteRules.Validate(new List<Vector2>(RouteRules.StandardTrail), new List<Vector2>(RouteRules.StandardTrail));

            for (var i = 0; i < 5; i++)
            {
                Assert.That(
                    RouteRules.Validate(new List<Vector2>(RouteRules.StandardTrail), new List<Vector2>(RouteRules.StandardTrail)),
                    Is.EqualTo(first));
            }

            var failing = new List<Vector2> { RouteRules.StandardTrail[0], RouteRules.StandardTrail[1] };
            for (var i = 0; i < 5; i++)
            {
                Assert.That(
                    RouteRules.Validate(failing, RouteRules.StandardTrail),
                    Is.EqualTo(RouteFailure.EndsBeforeReceiver));
            }
        }

        [Test]
        public void ValidateAcceptsADenselySampledTrailAsASolvableRoute()
        {
            var route = new List<Vector2>();
            for (var segment = 0; segment < RouteRules.StandardTrail.Length - 1; segment++)
            {
                var start = RouteRules.StandardTrail[segment];
                var end = RouteRules.StandardTrail[segment + 1];
                for (var step = 0; step < 24; step++)
                {
                    route.Add(Vector2.Lerp(start, end, step / 24f));
                }
            }

            route.Add(RouteRules.StandardTrail[RouteRules.StandardTrail.Length - 1]);

            Assert.That(RouteRules.Validate(route, RouteRules.StandardTrail), Is.EqualTo(RouteFailure.None));
        }

        [Test]
        public void DistanceToTrailReturnsInfinityForMissingOrShortTrails()
        {
            Assert.That(RouteRules.DistanceToTrail(Origin, null), Is.EqualTo(float.PositiveInfinity));
            Assert.That(RouteRules.DistanceToTrail(Origin, new List<Vector2> { Origin }), Is.EqualTo(float.PositiveInfinity));
        }

        [Test]
        public void DistanceToTrailMeasuresPerpendicularDistanceAndProjectsAtTheEnds()
        {
            var trail = StraightTrail();

            Assert.That(RouteRules.DistanceToTrail(new Vector2(5f, 0f), trail), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(RouteRules.DistanceToTrail(new Vector2(5f, 3f), trail), Is.EqualTo(3f).Within(1e-5f));
            Assert.That(RouteRules.DistanceToTrail(new Vector2(-4f, 0f), trail), Is.EqualTo(4f).Within(1e-5f));
            Assert.That(RouteRules.DistanceToTrail(new Vector2(20f, 0f), trail), Is.EqualTo(10f).Within(1e-5f));
        }

        // ---------------------------------------------------------------------------------------
        // The authored puzzle itself
        // ---------------------------------------------------------------------------------------

        [Test]
        public void StandardTrailIsAConnectedPuzzleWithDistinctEndpointsAndNoDegenerateSegments()
        {
            var trail = RouteRules.StandardTrail;

            Assert.That(trail.Length, Is.GreaterThanOrEqualTo(2));
            Assert.That(trail[0], Is.Not.EqualTo(trail[trail.Length - 1]), "source and receiver must differ");

            for (var i = 0; i < trail.Length - 1; i++)
            {
                Assert.That(
                    Vector2.Distance(trail[i], trail[i + 1]),
                    Is.GreaterThan(0f),
                    "trail segment " + i + " must have a real length");
            }
        }

        [Test]
        public void DeadEndSpurBranchesOffTheTrailButItsTipIsGenuinelyOffTrail()
        {
            var junction = RouteRules.DeadEndSpur[0];

            Assert.That(junction, Is.EqualTo(RouteRules.StandardTrail[2]), "the spur must start on the trail");

            var tip = RouteRules.DeadEndSpur[RouteRules.DeadEndSpur.Length - 1];
            Assert.That(
                RouteRules.DistanceToTrail(tip, RouteRules.StandardTrail),
                Is.GreaterThan(RouteRules.CorridorRadius),
                "the spur tip must sit outside the verified corridor to be a real dead end");
        }
    }
}
