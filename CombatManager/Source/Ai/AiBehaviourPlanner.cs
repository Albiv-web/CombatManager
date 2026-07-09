using UnityEngine;

namespace CombatManager.Ai
{
    internal struct AiPlanInput
    {
        internal AiSimulationPreset Preset;
        internal AiSimulationSide Side;
        internal AiSimulationNavalState NavalState;
        internal Vector3 CraftPosition;
        internal Vector3 CraftHeading;
        internal Vector3 CraftVelocity;
        internal Vector3 TargetPosition;
        internal Vector3 TargetVelocity;
        internal string TargetProfileName;
        internal float TargetSpeed;
        internal float Radius;
        internal float BroadsideOuterRadius;
        internal float BroadsideAngle;
        internal float CircleMinApproachAngle;
        internal float CraftSpeed;
        internal float PreferredAltitude;
        internal float SimulationTime;
        internal bool AttackRunActive;
        internal float AttackRunBeginDistance;
        internal float AttackRunAbortDistance;
        internal float AttackRunWaitTime;
        internal float AttackRunAttackAltitude;
        internal float AttackRunDisengageAltitude;
        internal float AttackRunBreakoffDistance;
        internal float AttackRunReengageDistance;
        internal float AttackRunReengageTime;
        internal float AttackRunPitchDistance;
        internal float AttackRunBreakoffAltitude;
        internal float AttackRunCombatAltitude;
        internal float AttackRunEngagementAltitude;
        internal float AttackRunPredictionPoint;
        internal float AttackRunFlyAwayYaw;
        internal bool AttackRunUsePrediction;
        internal bool AttackRunFlyover;
    }

    internal struct AiBehaviourPlan
    {
        internal Vector3 RawSteerPoint;
        internal Vector3 MotionPoint;
        internal Vector3 DesiredFacing;
        internal Vector3 DesiredTravel;
        internal Vector3 ToTarget;
        internal float Range;
        internal float GroundRange;
        internal float Azimuth;
        internal float BroadsideAngle;
        internal string Kind;
        internal string Summary;
        internal string AiState;
        internal string ApproximationNote;
        internal bool Approximate;
        internal bool HasRawSteerPoint;
        internal bool HasMotionPoint;
        internal bool HasDesiredFacing;
        internal bool ReversePreferred;
    }

    internal static class AiBehaviourPlanner
    {
        private const float NavalPredictionFactor = 5f;
        private const float AutoSideSwitchDeadbandDegrees = 12f;

        internal static AiBehaviourPlan Plan(AiPlanInput input)
        {
            PlannerTargetInfo target = BuildTargetInfo(input);
            switch (input.Preset)
            {
                case AiSimulationPreset.PointAt:
                    return PlanPointAt(input, target);
                case AiSimulationPreset.Broadside:
                    return PlanBroadside(input, target);
                case AiSimulationPreset.NavalBroadside:
                    return PlanNavalBroadside(input, target);
                case AiSimulationPreset.AttackRun1:
                    return PlanAttackRun1(input, target);
                case AiSimulationPreset.AttackRun2:
                    return PlanAttackRun2(input, target, aircraft30: false);
                case AiSimulationPreset.AttackRun3:
                    return PlanAttackRun2(input, target, aircraft30: true);
                default:
                    return PlanCircle(input, target);
            }
        }

        internal static void AdvanceAerialState(AiSimEntity entity, AiPlanInput input)
        {
            PlannerTargetInfo target = BuildTargetInfo(input);
            if (input.Preset == AiSimulationPreset.AttackRun1)
            {
                if (entity.AttackRunActive)
                {
                    if (target.GroundDistance < Mathf.Max(1f, entity.AttackRunAbortDistance))
                    {
                        entity.AttackRunActive = false;
                        entity.AttackRunLastFinishedAt = input.SimulationTime;
                        entity.AttackRunFlyAwayYaw = PlanarMath.HeadingYaw(input.CraftHeading);
                    }
                }
                else if (target.GroundDistance > Mathf.Max(entity.AttackRunAbortDistance + 1f, entity.AttackRunBeginDistance) ||
                    input.SimulationTime - entity.AttackRunLastFinishedAt > Mathf.Max(0f, entity.AttackRunWaitTime))
                {
                    entity.AttackRunActive = true;
                }

                return;
            }

            if (input.Preset != AiSimulationPreset.AttackRun2 && input.Preset != AiSimulationPreset.AttackRun3)
                return;

            float range = input.Preset == AiSimulationPreset.AttackRun3 && !entity.AttackRunIgnoreAltitude
                ? target.Range
                : target.GroundDistance;
            if (entity.AttackRunActive)
            {
                if (range < Mathf.Max(1f, entity.AttackRunBreakoffDistance) || input.CraftPosition.y < entity.AttackRunBreakoffAltitude)
                {
                    entity.AttackRunActive = false;
                    entity.AttackRunLastFinishedAt = input.SimulationTime;
                    entity.AttackRunFlyAwayYaw = PlanarMath.HeadingYaw(input.CraftHeading);
                }
                else if (!float.IsPositiveInfinity(entity.AttackRunAbortTimerStart) &&
                    input.SimulationTime - entity.AttackRunAbortTimerStart > Mathf.Max(0f, entity.AttackRunAbortTime))
                {
                    entity.AttackRunActive = false;
                    entity.AttackRunLastFinishedAt = input.SimulationTime;
                    entity.AttackRunFlyAwayYaw = PlanarMath.HeadingYaw(input.CraftHeading);
                }

                if (float.IsPositiveInfinity(entity.AttackRunAbortTimerStart) &&
                    range < Mathf.Max(0f, entity.AttackRunAbortTimerStartDistance))
                {
                    entity.AttackRunAbortTimerStart = input.SimulationTime;
                }
            }
            else if (range > Mathf.Max(1f, entity.AttackRunReengageDistance) ||
                input.SimulationTime - entity.AttackRunLastFinishedAt > Mathf.Max(0f, entity.AttackRunReengageTime))
            {
                entity.AttackRunActive = true;
                entity.AttackRunAbortTimerStart = float.PositiveInfinity;
            }
        }

        internal static AiSimulationNavalState AdvanceNavalState(AiPlanInput input)
        {
            if (input.Preset == AiSimulationPreset.Broadside)
                return ResolveBroadsideSide(input, BuildTargetInfo(input));

            if (input.Preset != AiSimulationPreset.NavalBroadside)
                return input.NavalState;

            PlannerTargetInfo target = BuildTargetInfo(input);
            float enterBelow = Mathf.Max(10f, input.Radius);
            float leaveAbove = Mathf.Max(enterBelow + 20f, input.BroadsideOuterRadius);
            if (input.NavalState == AiSimulationNavalState.Closing && target.Range <= enterBelow)
                return ResolveBroadsideSide(input, target);
            if (input.NavalState != AiSimulationNavalState.Closing && target.Range >= leaveAbove)
                return AiSimulationNavalState.Closing;
            if (input.NavalState != AiSimulationNavalState.Closing)
                return ResolveBroadsideSide(input, target);
            return input.NavalState;
        }

        private static AiBehaviourPlan PlanCircle(AiPlanInput input, PlannerTargetInfo target)
        {
            float approachAngle = GetCircleApproachAngle(input, target);
            Vector3 desiredTravel = PlanarMath.RotateYaw(target.DirectionFlat, approachAngle);
            Vector3 rawSteer = input.CraftPosition + desiredTravel * 1000f;
            rawSteer.y = input.PreferredAltitude;
            Vector3 motionPoint = BuildCircleMotionPoint(input, target, desiredTravel);
            return BuildCommonPlan(
                input,
                target,
                rawSteer,
                motionPoint,
                desiredTravel,
                desiredTravel,
                "Circle",
                $"Circle {input.Radius:0.#}m vs {input.TargetProfileName} {input.TargetSpeed:0.#}m/s",
                "mirrors circle steer bearing; finite motion point is sandbox-only",
                approximate: false,
                state: $"approach {approachAngle:0.#} deg",
                broadsideAngle: 0f,
                reversePreferred: false);
        }

        private static AiBehaviourPlan PlanPointAt(AiPlanInput input, PlannerTargetInfo target)
        {
            float distance = Mathf.Max(10f, input.Radius);
            Vector3 rawSteer = input.TargetPosition - target.DirectionFlat * distance;
            rawSteer.y = input.PreferredAltitude;
            Vector3 moveDirection = PlanarMath.SafePlanarDirection(input.CraftPosition, rawSteer, input.CraftHeading);
            float moveAngle = Mathf.Abs(PlanarMath.SignedPlanarAngle(input.CraftHeading, moveDirection));
            bool reversePreferred = target.GroundDistance < distance && moveAngle > 120f;

            return BuildCommonPlan(
                input,
                target,
                rawSteer,
                rawSteer,
                target.DirectionFlat,
                Vector3.zero,
                "Point at",
                $"Point at target from {distance:0.#}m",
                "range hold mirrored; propulsion/PID behaviour is movement-model approximation",
                approximate: false,
                state: reversePreferred ? "hold range, reverse preferred" : "hold range",
                broadsideAngle: 0f,
                reversePreferred: reversePreferred);
        }

        private static AiBehaviourPlan PlanBroadside(AiPlanInput input, PlannerTargetInfo target)
        {
            AiSimulationNavalState resolvedState = ResolveBroadsideSide(input, target);
            float signedAngle = input.BroadsideAngle * BroadsideSideSign(resolvedState);
            Vector3 broadsideForward = PlanarMath.RotateYaw(target.DirectionFlat, -signedAngle);
            Vector3 projected = input.CraftPosition + broadsideForward * 100f;
            Vector3 fromTarget = PlanarMath.Flatten(projected - input.TargetPosition);
            if (fromTarget.sqrMagnitude < 0.0001f)
                fromTarget = -target.DirectionFlat;

            Vector3 rawSteer = input.TargetPosition + fromTarget.normalized * Mathf.Max(10f, input.Radius);
            rawSteer.y = input.CraftPosition.y;
            return BuildCommonPlan(
                input,
                target,
                rawSteer,
                rawSteer,
                broadsideForward,
                Vector3.zero,
                "Broadside",
                $"Broadside {signedAngle:0.#} deg at {input.Radius:0.#}m",
                "mirrors broadside angle/range; firing-angle and sea-surface adjustments are not simulated",
                approximate: false,
                state: signedAngle >= 0f ? "left broadside" : "right broadside",
                broadsideAngle: signedAngle,
                reversePreferred: false);
        }

        private static AiBehaviourPlan PlanNavalBroadside(AiPlanInput input, PlannerTargetInfo target)
        {
            float enterBelow = Mathf.Max(10f, input.Radius);
            if (input.NavalState == AiSimulationNavalState.Closing)
            {
                return BuildCommonPlan(
                    input,
                    target,
                    input.TargetPosition,
                    input.TargetPosition,
                    target.DirectionFlat,
                    target.DirectionFlat,
                    "Broadside 2.0",
                    $"Closing until {enterBelow:0.#}m",
                    "approximates Naval 2.0 without sea-surface pathfinding or firing-angle solver",
                    approximate: true,
                    state: "closing",
                    broadsideAngle: 0f,
                    reversePreferred: false);
            }

            AiSimulationNavalState resolvedState = ResolveBroadsideSide(input, target);
            float desiredAngle = input.BroadsideAngle * (resolvedState == AiSimulationNavalState.BroadsideRight ? -1f : 1f);
            Vector3 fromTarget = -target.DirectionFlat;
            Vector3 rawSteer = input.TargetPosition + PlanarMath.RotateYaw(fromTarget, desiredAngle) * enterBelow;
            rawSteer.y = input.CraftPosition.y;
            Vector3 desiredFacing = PlanarMath.SafePlanarDirection(rawSteer, input.TargetPosition, input.CraftHeading);
            return BuildCommonPlan(
                input,
                target,
                rawSteer,
                rawSteer,
                desiredFacing,
                Vector3.zero,
                "Broadside 2.0",
                $"{NavalStateLabel(resolvedState)} at {desiredAngle:0.#} deg",
                "approximates Naval 2.0 without sea-surface pathfinding or firing-angle solver",
                approximate: true,
                state: NavalStateLabel(resolvedState),
                broadsideAngle: desiredAngle,
                reversePreferred: false);
        }

        private static AiBehaviourPlan PlanAttackRun1(AiPlanInput input, PlannerTargetInfo target)
        {
            if (input.AttackRunActive)
            {
                Vector3 aimPoint = input.TargetPosition;
                aimPoint.y = input.TargetPosition.y + input.AttackRunAttackAltitude;
                Vector3 facing = PlanarMath.SafePlanarDirection(input.CraftPosition, input.TargetPosition, input.CraftHeading);
                return BuildCommonPlan(
                    input,
                    target,
                    aimPoint,
                    aimPoint,
                    facing,
                    facing,
                    "Attack run 1.0",
                    $"Attack flyover below {input.AttackRunAbortDistance:0.#}m",
                    "mirrors Attack Run 1.0 towards/fly-away state; adjuster and pitch PID are approximated",
                    approximate: true,
                    state: $"attack altitude {aimPoint.y:0.#}m",
                    broadsideAngle: 0f,
                    reversePreferred: false);
            }

            Vector3 fleeDirection = PlanarMath.RotateYaw(Vector3.forward, input.AttackRunFlyAwayYaw);
            Vector3 fleePoint = input.CraftPosition + fleeDirection * 250f;
            fleePoint.y = input.AttackRunDisengageAltitude;
            return BuildCommonPlan(
                input,
                target,
                fleePoint,
                fleePoint,
                fleeDirection,
                fleeDirection,
                "Attack run 1.0",
                $"Fly away until {input.AttackRunBeginDistance:0.#}m or {input.AttackRunWaitTime:0.#}s",
                "fly-away bearing is sandbox-held from current heading; adjuster and pitch PID are approximated",
                approximate: true,
                state: $"flee altitude {fleePoint.y:0.#}m",
                broadsideAngle: 0f,
                reversePreferred: false);
        }

        private static AiBehaviourPlan PlanAttackRun2(AiPlanInput input, PlannerTargetInfo target, bool aircraft30)
        {
            Vector3 facingToTarget = PlanarMath.SafePlanarDirection(input.CraftPosition, input.TargetPosition, input.CraftHeading);
            Vector3 awayFromTarget = -facingToTarget;
            float chosenAltitude;
            Vector3 motionPoint;
            Vector3 facing;
            string state;

            if (input.AttackRunActive)
            {
                facing = facingToTarget;
                Vector3 aimTarget = input.TargetPosition;
                if (aircraft30 && input.AttackRunUsePrediction && target.GroundDistance > input.AttackRunPredictionPoint)
                    aimTarget += input.TargetVelocity * Mathf.Clamp(target.GroundDistance / Mathf.Max(1f, input.CraftSpeed), 0f, 8f);

                motionPoint = input.CraftPosition + facing * 100f;
                if (aircraft30 && target.GroundDistance > input.AttackRunPitchDistance)
                    chosenAltitude = input.TargetPosition.y + input.AttackRunEngagementAltitude;
                else if (!aircraft30 && target.GroundDistance > input.AttackRunPitchDistance)
                    chosenAltitude = input.AttackRunCombatAltitude;
                else
                    chosenAltitude = input.CraftPosition.y;
                motionPoint.y = chosenAltitude;
                aimTarget.y = chosenAltitude;
                state = target.GroundDistance <= input.AttackRunPitchDistance
                    ? "attack, pitch locked near target"
                    : $"attack altitude {chosenAltitude:0.#}m";

                return BuildCommonPlan(
                    input,
                    target,
                    aimTarget,
                    motionPoint,
                    facing,
                    facing,
                    aircraft30 ? "Attack run 3.0" : "Attack run 2.0",
                    $"Attack until {input.AttackRunBreakoffDistance:0.#}m",
                    aircraft30
                        ? "mirrors Aircraft 3.0 attack/flee states; interception, adjustment, and PID are approximated"
                        : "mirrors BombingRun 2.0 attack/flee states; adjustment and pitch PID are approximated",
                    approximate: true,
                    state: state,
                    broadsideAngle: 0f,
                    reversePreferred: false);
            }

            facing = aircraft30 && input.AttackRunFlyover
                ? PlanarMath.RotateYaw(Vector3.forward, input.AttackRunFlyAwayYaw)
                : awayFromTarget;
            motionPoint = input.CraftPosition + facing * 100f;
            chosenAltitude = input.AttackRunCombatAltitude;
            motionPoint.y = chosenAltitude;
            state = $"flee until {input.AttackRunReengageDistance:0.#}m or {input.AttackRunReengageTime:0.#}s";

            return BuildCommonPlan(
                input,
                target,
                motionPoint,
                motionPoint,
                facing,
                facing,
                aircraft30 ? "Attack run 3.0" : "Attack run 2.0",
                state,
                aircraft30
                    ? "mirrors Aircraft 3.0 flee phase; run-away adjuster and prediction are approximated"
                    : "mirrors BombingRun 2.0 flee phase; run-away adjuster is approximated",
                approximate: true,
                state: $"flee altitude {chosenAltitude:0.#}m",
                broadsideAngle: 0f,
                reversePreferred: false);
        }

        private static AiBehaviourPlan BuildCommonPlan(
            AiPlanInput input,
            PlannerTargetInfo target,
            Vector3 rawSteer,
            Vector3 motionPoint,
            Vector3 desiredFacing,
            Vector3 desiredTravel,
            string kind,
            string summary,
            string approximationNote,
            bool approximate,
            string state,
            float broadsideAngle,
            bool reversePreferred)
        {
            return new AiBehaviourPlan
            {
                RawSteerPoint = rawSteer,
                MotionPoint = motionPoint,
                DesiredFacing = PlanarMath.SafePlanarDirection(Vector3.zero, desiredFacing, input.CraftHeading),
                DesiredTravel = PlanarMath.Flatten(desiredTravel),
                ToTarget = target.DirectionFlat,
                Range = target.Range,
                GroundRange = target.GroundDistance,
                Azimuth = target.Azimuth,
                BroadsideAngle = broadsideAngle,
                Kind = kind,
                Summary = summary,
                AiState = state,
                ApproximationNote = approximationNote,
                Approximate = approximate,
                HasRawSteerPoint = true,
                HasMotionPoint = true,
                HasDesiredFacing = true,
                ReversePreferred = reversePreferred
            };
        }

        private static PlannerTargetInfo BuildTargetInfo(AiPlanInput input)
        {
            Vector3 direction = input.TargetPosition - input.CraftPosition;
            Vector3 flatDirection = PlanarMath.Flatten(direction);
            Vector3 directionFlat = PlanarMath.SafePlanarDirection(input.CraftPosition, input.TargetPosition, input.CraftHeading);
            return new PlannerTargetInfo
            {
                Position = input.TargetPosition,
                Velocity = input.TargetVelocity,
                DirectionFlat = directionFlat,
                Range = direction.magnitude,
                GroundDistance = flatDirection.magnitude,
                Azimuth = PlanarMath.SignedPlanarAngle(input.CraftHeading, directionFlat)
            };
        }

        private static Vector3 BuildCircleMotionPoint(AiPlanInput input, PlannerTargetInfo target, Vector3 desiredTravel)
        {
            Vector3 fromTarget = PlanarMath.SafePlanarDirection(input.TargetPosition, input.CraftPosition, -target.DirectionFlat);
            Vector3 motionDirection = PlanarMath.SafePlanarDirection(Vector3.zero, desiredTravel, input.CraftHeading);
            float radius = Mathf.Max(10f, input.Radius);
            float lookAhead = Mathf.Clamp(Mathf.Max(90f, input.CraftSpeed * 3f), 90f, Mathf.Max(120f, radius * 0.8f));
            Vector3 point = input.TargetPosition + fromTarget * radius + motionDirection * lookAhead;
            point.y = input.PreferredAltitude;
            return point;
        }

        private static float GetCircleApproachAngle(AiPlanInput input, PlannerTargetInfo target)
        {
            float range = 90f - input.CircleMinApproachAngle;
            float rangeCorrection = range * Mathf.Min(Mathf.Max((input.Radius - target.GroundDistance) / 200f, -1f), 1f);
            float result = 90f + rangeCorrection;
            if (input.Side == AiSimulationSide.Right || (input.Side == AiSimulationSide.Both && target.Azimuth > 0f))
                result = -result;
            return result;
        }

        private static float BroadsideSideSign(AiSimulationNavalState state)
        {
            return state == AiSimulationNavalState.BroadsideRight ? -1f : 1f;
        }

        private static AiSimulationNavalState ResolveBroadsideSide(AiPlanInput input, PlannerTargetInfo target)
        {
            if (input.Side == AiSimulationSide.Left)
                return AiSimulationNavalState.BroadsideLeft;
            if (input.Side == AiSimulationSide.Right)
                return AiSimulationNavalState.BroadsideRight;

            float sideScore = AutoBroadsideSideScore(input, target);
            AiSimulationNavalState candidate = sideScore < 0f
                ? AiSimulationNavalState.BroadsideRight
                : AiSimulationNavalState.BroadsideLeft;

            if (IsBroadsideSide(input.NavalState)
                && candidate != input.NavalState
                && IsAutoSideTie(sideScore))
            {
                return input.NavalState;
            }

            return candidate;
        }

        private static float AutoBroadsideSideScore(AiPlanInput input, PlannerTargetInfo target)
        {
            Vector3 predicted = target.Position + target.Velocity * NavalPredictionFactor;
            Vector3 vector = PlanarMath.SafePlanarDirection(input.CraftPosition, predicted, target.DirectionFlat);
            return PlanarMath.SignedPlanarAngle(vector, input.CraftHeading);
        }

        private static bool IsAutoSideTie(float sideScore)
        {
            float absolute = Mathf.Abs(PlanarMath.Fix180(sideScore));
            float distanceToWrap = Mathf.Abs(180f - absolute);
            return absolute < AutoSideSwitchDeadbandDegrees || distanceToWrap < AutoSideSwitchDeadbandDegrees;
        }

        private static bool IsBroadsideSide(AiSimulationNavalState state)
        {
            return state == AiSimulationNavalState.BroadsideLeft
                || state == AiSimulationNavalState.BroadsideRight;
        }

        private static string NavalStateLabel(AiSimulationNavalState state)
        {
            switch (state)
            {
                case AiSimulationNavalState.BroadsideRight:
                    return "right broadside";
                case AiSimulationNavalState.BroadsideLeft:
                    return "left broadside";
                default:
                    return "closing";
            }
        }

        private struct PlannerTargetInfo
        {
            internal Vector3 Position;
            internal Vector3 Velocity;
            internal Vector3 DirectionFlat;
            internal float Range;
            internal float GroundDistance;
            internal float Azimuth;
        }
    }
}
