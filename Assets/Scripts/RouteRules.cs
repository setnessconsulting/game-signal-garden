using System.Collections.Generic;
using UnityEngine;

namespace SignalGarden
{
    public static class RouteRules
    {
        public const float CorridorRadius = 0.36f;
        public const float EndpointTolerance = 0.50f;
        public const float MaximumSampleStep = 0.12f;
        public const float DirectionSlack = 0.20f;

        public static readonly Vector2[] StandardTrail =
        {
            new Vector2(-4.30f, 2.20f),
            new Vector2(-2.90f, 2.20f),
            new Vector2(-2.90f, 1.00f),
            new Vector2(-1.40f, 1.00f),
            new Vector2(-1.40f, -0.30f),
            new Vector2(0.00f, -0.30f),
            new Vector2(0.00f, -1.70f),
            new Vector2(1.80f, -1.70f),
            new Vector2(1.80f, -2.60f),
            new Vector2(4.30f, -2.60f)
        };

        public static readonly Vector2[] DeadEndSpur =
        {
            new Vector2(-2.90f, 1.00f),
            new Vector2(-2.05f, 2.05f),
            new Vector2(-0.65f, 2.05f)
        };

        public static RouteFailure Validate(
            IList<Vector2> route,
            IList<Vector2> trail,
            float corridorRadius = CorridorRadius,
            float endpointTolerance = EndpointTolerance,
            float maximumSampleStep = MaximumSampleStep)
        {
            if (route == null || trail == null || route.Count < 2 || trail.Count < 2)
            {
                return RouteFailure.TooFewPoints;
            }

            if (Vector2.Distance(route[0], trail[0]) > endpointTolerance)
            {
                return RouteFailure.StartsAwayFromSource;
            }

            var trailLengths = new float[trail.Count - 1];
            var trailLength = 0f;
            for (var i = 0; i < trail.Count - 1; i++)
            {
                var length = Vector2.Distance(trail[i], trail[i + 1]);
                if (length <= Mathf.Epsilon)
                {
                    return RouteFailure.TooFewPoints;
                }

                trailLengths[i] = length;
                trailLength += length;
            }

            var lastProgress = -DirectionSlack;
            var reversed = false;
            for (var segmentIndex = 0; segmentIndex < route.Count - 1; segmentIndex++)
            {
                var start = route[segmentIndex];
                var end = route[segmentIndex + 1];
                var segmentLength = Vector2.Distance(start, end);
                var sampleCount = Mathf.Max(1, Mathf.CeilToInt(segmentLength / Mathf.Max(0.02f, maximumSampleStep)));

                for (var sampleIndex = 0; sampleIndex <= sampleCount; sampleIndex++)
                {
                    var t = sampleIndex / (float)sampleCount;
                    var sample = Vector2.Lerp(start, end, t);
                    if (!TryProjectToTrail(sample, trail, trailLengths, out var distance, out var progress))
                    {
                        return RouteFailure.TooFewPoints;
                    }

                    if (distance > corridorRadius)
                    {
                        return RouteFailure.LeavesTrail;
                    }

                    if (progress + DirectionSlack < lastProgress)
                    {
                        reversed = true;
                    }

                    lastProgress = Mathf.Max(lastProgress, progress);
                }
            }

            if (reversed)
            {
                return RouteFailure.ReversesDirection;
            }

            if (lastProgress < trailLength - endpointTolerance)
            {
                return RouteFailure.EndsBeforeReceiver;
            }

            if (Vector2.Distance(route[route.Count - 1], trail[trail.Count - 1]) > endpointTolerance)
            {
                return RouteFailure.EndsBeforeReceiver;
            }

            return RouteFailure.None;
        }

        public static float DistanceToTrail(Vector2 point, IList<Vector2> trail)
        {
            if (trail == null || trail.Count < 2)
            {
                return float.PositiveInfinity;
            }

            var lengths = new float[trail.Count - 1];
            for (var i = 0; i < trail.Count - 1; i++)
            {
                lengths[i] = Vector2.Distance(trail[i], trail[i + 1]);
            }

            return TryProjectToTrail(point, trail, lengths, out var distance, out _) ? distance : float.PositiveInfinity;
        }

        private static bool TryProjectToTrail(
            Vector2 point,
            IList<Vector2> trail,
            IReadOnlyList<float> trailLengths,
            out float distance,
            out float progress)
        {
            distance = float.PositiveInfinity;
            progress = 0f;
            var accumulated = 0f;
            var found = false;

            for (var i = 0; i < trail.Count - 1; i++)
            {
                var start = trail[i];
                var delta = trail[i + 1] - start;
                var length = trailLengths[i];
                if (length <= Mathf.Epsilon)
                {
                    continue;
                }

                var t = Mathf.Clamp01(Vector2.Dot(point - start, delta) / delta.sqrMagnitude);
                var nearest = start + delta * t;
                var candidateDistance = Vector2.Distance(point, nearest);
                var candidateProgress = accumulated + length * t;
                if (!found ||
                    candidateDistance < distance - 0.0001f ||
                    (Mathf.Abs(candidateDistance - distance) <= 0.0001f && candidateProgress > progress))
                {
                    found = true;
                    distance = candidateDistance;
                    progress = candidateProgress;
                }

                accumulated += length;
            }

            return found;
        }
    }
}
