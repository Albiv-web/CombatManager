using System.Collections.Generic;
using BrilliantSkies.Ai.Interfaces;
using UnityEngine;

namespace CombatManager.Ai
{
    internal enum AiPredictionConfidence
    {
        Exact,
        Approximate,
        Unsupported
    }

    internal sealed class AiVanillaIntentPlan
    {
        internal string BehaviourClass { get; set; }
        internal string Kind { get; set; }
        internal string Summary { get; set; }
        internal string State { get; set; }
        internal Vector3 RawSteerPoint { get; set; }
        internal Vector3 MotionPoint { get; set; }
        internal Vector3 DesiredFacing { get; set; }
        internal Vector3 DesiredTravel { get; set; }
        internal float Range { get; set; }
        internal float GroundRange { get; set; }
        internal float Azimuth { get; set; }
        internal float MaintainDistanceLower { get; set; }
        internal float MaintainDistanceUpper { get; set; }
        internal float DesiredAngle { get; set; }
        internal bool Supported { get; set; }
        internal bool HasRawSteerPoint { get; set; }
        internal bool HasMotionPoint { get; set; }
        internal bool HasDesiredFacing { get; set; }
        internal bool ReversePreferred { get; set; }
        internal bool Approximate { get; set; }
        internal List<string> Warnings { get; } = new List<string>();

        internal Vector3 RequestPoint => HasMotionPoint ? MotionPoint : RawSteerPoint;
    }

    internal sealed class AiControlRequestPrediction
    {
        internal AiControlType Type { get; set; }
        internal float Value { get; set; }
        internal string SourceManoeuvre { get; set; }
        internal AiPredictionConfidence Confidence { get; set; }
        internal string Note { get; set; }
    }

    internal sealed class AiControlRequestDelta
    {
        internal AiControlType Type { get; set; }
        internal float Observed { get; set; }
        internal float Predicted { get; set; }
        internal float Delta { get; set; }
        internal bool SignMatches { get; set; }
        internal string Note { get; set; }
    }

    internal sealed class AiLiveParitySnapshot
    {
        internal string Status { get; set; }
        internal string MainframeName { get; set; }
        internal string BehaviourType { get; set; }
        internal string ManoeuvreType { get; set; }
        internal bool HasFocusedConstruct { get; set; }
        internal bool HasMainframe { get; set; }
        internal bool HasTarget { get; set; }
        internal AiVanillaIntentPlan PredictedIntent { get; set; }
        internal List<AiControlRequestSnapshot> ObservedRequests { get; } = new List<AiControlRequestSnapshot>();
        internal List<AiControlRequestPrediction> PredictedRequests { get; } = new List<AiControlRequestPrediction>();
        internal List<AiControlRequestDelta> RequestDeltas { get; } = new List<AiControlRequestDelta>();
        internal List<string> Warnings { get; } = new List<string>();
    }

    internal struct AiMovementRequestContext
    {
        internal AiCraftMovementModel Model;
        internal string SourceManoeuvre;
        internal Vector3 CraftPosition;
        internal Vector3 CraftHeading;
        internal Vector3 CraftVelocity;
        internal float CraftAltitude;
        internal float TarryDistance;
        internal bool ReverseAllowed;
        internal float HoverYawLockDistance;
        internal float HoverMoveWithinAzimuth;
        internal float SixAxisLookAheadDistance;
        internal float AirplaneIdleThrust;
        internal float AirplaneIdleDistance;
        internal float AirplaneBankingTurnAbove;
        internal float AirplaneBankingTurnRoll;
        internal float AirplanePitchForAltitude;

        internal static AiMovementRequestContext FromEntity(AiSimEntity entity)
        {
            return new AiMovementRequestContext
            {
                Model = entity.CraftMovementModel,
                SourceManoeuvre = AiSimulationState.CraftMovementModelName(entity.CraftMovementModel),
                CraftPosition = entity.Position,
                CraftHeading = entity.Heading,
                CraftVelocity = entity.Velocity,
                CraftAltitude = entity.Position.y,
                TarryDistance = entity.ShipTarryDistance,
                ReverseAllowed = entity.ShipReverseAllowed,
                HoverYawLockDistance = entity.HoverYawLockDistance,
                HoverMoveWithinAzimuth = entity.HoverMoveWithinAzimuth,
                SixAxisLookAheadDistance = entity.SixAxisLookAheadDistance,
                AirplaneIdleThrust = entity.AirplaneIdleThrust,
                AirplaneIdleDistance = entity.AirplaneIdleDistance,
                AirplaneBankingTurnAbove = entity.AirplaneBankingTurnAbove,
                AirplaneBankingTurnRoll = entity.AirplaneBankingTurnRoll,
                AirplanePitchForAltitude = entity.AirplanePitchForAltitude
            };
        }
    }
}
