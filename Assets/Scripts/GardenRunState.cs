using System;
using System.Collections.Generic;
using UnityEngine;

namespace SignalGarden
{
    public enum GardenPhase
    {
        Observe = 0,
        Routing = 1,
        Recovery = 2,
        Verified = 3,
        Paused = 4
    }

    public enum RouteFailure
    {
        None = 0,
        TooFewPoints = 1,
        StartsAwayFromSource = 2,
        EndsBeforeReceiver = 3,
        LeavesTrail = 4,
        ReversesDirection = 5,
        Cancelled = 6,
        FocusInterrupted = 7
    }

    [Serializable]
    public sealed class GardenRunState
    {
        public string sourceId = "source-coral";
        public string receiverId = "receiver-glass";
        public List<Vector2> routePoints = new List<Vector2>();
        public GardenPhase phase = GardenPhase.Observe;
        public GardenPhase phaseBeforePause = GardenPhase.Observe;
        public RouteFailure lastFailure = RouteFailure.None;
        public int attemptCount;
        public int verificationCount;
        public int resetCount;
        public bool wasReset;

        public bool BeginRoute(Vector2 sourcePoint)
        {
            if (phase != GardenPhase.Observe && phase != GardenPhase.Recovery)
            {
                return false;
            }

            routePoints.Clear();
            routePoints.Add(sourcePoint);
            phase = GardenPhase.Routing;
            lastFailure = RouteFailure.None;
            attemptCount++;
            return true;
        }

        public bool AppendRoutePoint(Vector2 point, float minimumSpacing = 0.045f)
        {
            if (phase != GardenPhase.Routing)
            {
                return false;
            }

            if (routePoints.Count > 0 &&
                Vector2.Distance(routePoints[routePoints.Count - 1], point) < minimumSpacing)
            {
                return false;
            }

            routePoints.Add(point);
            return true;
        }

        public bool ResolveAttempt(RouteFailure failure)
        {
            if (phase != GardenPhase.Routing)
            {
                return false;
            }

            lastFailure = failure;
            if (failure == RouteFailure.None)
            {
                phase = GardenPhase.Verified;
                verificationCount++;
            }
            else
            {
                phase = GardenPhase.Recovery;
            }

            return true;
        }

        public bool CancelRoute(RouteFailure reason = RouteFailure.Cancelled)
        {
            if (phase != GardenPhase.Routing)
            {
                return false;
            }

            routePoints.Clear();
            lastFailure = reason;
            phase = GardenPhase.Recovery;
            return true;
        }

        public bool Pause()
        {
            if (phase == GardenPhase.Paused)
            {
                return false;
            }

            phaseBeforePause = phase;
            phase = GardenPhase.Paused;
            return true;
        }

        public bool Resume()
        {
            if (phase != GardenPhase.Paused)
            {
                return false;
            }

            phase = phaseBeforePause;
            return true;
        }

        public void Reset()
        {
            routePoints.Clear();
            phase = GardenPhase.Observe;
            phaseBeforePause = GardenPhase.Observe;
            lastFailure = RouteFailure.None;
            attemptCount = 0;
            verificationCount = 0;
            resetCount++;
            wasReset = true;
        }
    }
}
