using System;
using System.Collections.Generic;
using BrilliantSkies.Ai.Interfaces;
using BrilliantSkies.Ai.Modules.Behaviour;
using BrilliantSkies.Ai.Modules.Behaviour.Examples;
using BrilliantSkies.Ai.Modules.Manoeuvre;
using BrilliantSkies.Ai.Modules.Manoeuvre.Examples;
using BrilliantSkies.Ai.Modules.Manoeuvre.Examples.Ftd;
using BrilliantSkies.Ai.Targetting;
using UnityEngine;

namespace CombatManager.Ai
{
    internal static class AiVanillaPredictor
    {
        private const float RequestDistanceScale = 120f;
        private const float RequestYawScale = 90f;
        private const float RequestAltitudeScale = 80f;

        internal static AiVanillaIntentPlan PredictIntent(
            object behaviour,
            Vector3 craftPosition,
            Quaternion craftRotation,
            Vector3 craftVelocity,
            TargetPositionInfo target,
            IPlatformInterface platform)
        {
            Vector3 craftHeading = PlanarMath.SafePlanarDirection(Vector3.zero, craftRotation * Vector3.forward, Vector3.forward);
            if (!target.Valid)
                return Unsupported("No target", "No target is available for behaviour prediction.");

            if (behaviour is FtdNaval naval)
                return PredictFtdNaval(naval, craftPosition, craftHeading, target, platform);

            if (behaviour is BehaviourBroadside broadside)
                return PredictBroadside(broadside, craftPosition, craftHeading, target);

            if (behaviour is BehaviourPointAndMaintainDistance pointAt)
                return PredictPointAt(pointAt, craftPosition, craftHeading, target);

            if (behaviour is BehaviourCircleAtDistance circle)
                return PredictCircle(circle, craftPosition, craftHeading, target);

            return Unsupported(
                behaviour?.GetType().Name ?? "No behaviour",
                "This behaviour is not mirrored by the vanilla-mapped predictor yet.");
        }

        internal static AiVanillaIntentPlan FromSimulationFrame(AiSimulationFrame frame)
        {
            return new AiVanillaIntentPlan
            {
                Supported = true,
                BehaviourClass = frame.Kind,
                Kind = frame.Kind,
                Summary = frame.Summary,
                State = frame.AiState,
                RawSteerPoint = frame.RawSteerPoint,
                MotionPoint = frame.MotionPoint,
                DesiredFacing = frame.DesiredFacing,
                DesiredTravel = frame.DesiredTravel,
                Range = frame.Range,
                GroundRange = frame.GroundRange,
                Azimuth = frame.Azimuth,
                MaintainDistanceLower = frame.Radius,
                MaintainDistanceUpper = frame.Radius,
                DesiredAngle = frame.BroadsideAngle,
                HasRawSteerPoint = frame.HasRawSteerPoint,
                HasMotionPoint = frame.HasMotionPoint,
                HasDesiredFacing = frame.HasDesiredFacing,
                ReversePreferred = frame.ReversePreferred,
                Approximate = frame.Approximate
            };
        }

        internal static List<AiControlRequestPrediction> PredictRequests(
            object manoeuvre,
            AiVanillaIntentPlan intent,
            Vector3 craftPosition,
            Quaternion craftRotation,
            Vector3 craftVelocity)
        {
            AiMovementRequestContext context = BuildLiveMovementContext(manoeuvre, craftPosition, craftRotation, craftVelocity);
            return PredictRequests(context, intent);
        }

        internal static List<AiControlRequestPrediction> PredictRequests(
            AiMovementRequestContext context,
            AiVanillaIntentPlan intent)
        {
            var result = new List<AiControlRequestPrediction>();
            if (intent == null || !intent.Supported)
            {
                Add(result, AiControlType.ThrustForward, 0f, context, AiPredictionConfidence.Unsupported, "No supported behaviour intent.");
                return result;
            }

            switch (context.Model)
            {
                case AiCraftMovementModel.Hover:
                    PredictHover(result, context, intent);
                    break;
                case AiCraftMovementModel.SixAxis:
                    PredictSixAxis(result, context, intent);
                    break;
                case AiCraftMovementModel.Airplane:
                    PredictAirplane(result, context, intent);
                    break;
                default:
                    PredictShipOrTank(result, context, intent);
                    break;
            }

            result.RemoveAll(request => request.Value <= 0.001f);
            return result;
        }

        internal static List<AiControlRequestDelta> CompareRequests(
            IList<AiControlRequestSnapshot> observed,
            IList<AiControlRequestPrediction> predicted)
        {
            var byType = new Dictionary<AiControlType, AiControlRequestDelta>();
            if (observed != null)
            {
                foreach (AiControlRequestSnapshot request in observed)
                {
                    AiControlRequestDelta delta = GetDelta(byType, request.Type);
                    delta.Observed = Mathf.Max(delta.Observed, Mathf.Abs(request.Value));
                }
            }

            if (predicted != null)
            {
                foreach (AiControlRequestPrediction request in predicted)
                {
                    AiControlRequestDelta delta = GetDelta(byType, request.Type);
                    delta.Predicted = Mathf.Max(delta.Predicted, Mathf.Abs(request.Value));
                    delta.Note = request.Note;
                }
            }

            var result = new List<AiControlRequestDelta>();
            foreach (AiControlRequestDelta delta in byType.Values)
            {
                delta.Delta = delta.Predicted - delta.Observed;
                delta.SignMatches = delta.Observed <= 0.001f || delta.Predicted <= 0.001f || Math.Sign(delta.Observed) == Math.Sign(delta.Predicted);
                result.Add(delta);
            }

            result.Sort((a, b) => Mathf.Abs(b.Delta).CompareTo(Mathf.Abs(a.Delta)));
            return result;
        }

        internal static float ShipTurnThrottle01(float azimuth)
        {
            float abs = Mathf.Abs(azimuth);
            if (abs <= 50f)
                return 1f;
            return Mathf.Lerp(1f, 0.2f, Mathf.Clamp01((abs - 50f) / 85f));
        }

        internal static AiIntentPrediction ToLegacyPrediction(AiVanillaIntentPlan plan)
        {
            return new AiIntentPrediction
            {
                Supported = plan.Supported,
                Kind = plan.Kind,
                Summary = plan.Summary,
                DesiredPoint = plan.RawSteerPoint,
                DesiredRotation = PlanarMath.SafeLookRotation(plan.DesiredFacing),
                MaintainDistanceLower = plan.MaintainDistanceLower,
                MaintainDistanceUpper = plan.MaintainDistanceUpper,
                DesiredAngle = plan.DesiredAngle,
                HasPoint = plan.HasRawSteerPoint,
                HasFacing = plan.HasDesiredFacing,
                Approximate = plan.Approximate
            };
        }

        private static AiVanillaIntentPlan PredictBroadside(
            BehaviourBroadside behaviour,
            Vector3 craftPosition,
            Vector3 craftHeading,
            TargetPositionInfo target)
        {
            Vector3 targetDirection = PlanarMath.SafePlanarDirection(craftPosition, target.Position, craftHeading);
            Vector3 broadsideForward = PlanarMath.RotateYaw(targetDirection, -behaviour.AngleToMaintain.Us);
            Vector3 point = craftPosition + broadsideForward * 100f;
            Vector3 fromTarget = PlanarMath.Flatten(point - target.Position);
            float distance = fromTarget.magnitude;
            if (distance < 0.001f)
                fromTarget = -targetDirection;

            if (distance < behaviour.DistanceToMaintain.Lower)
                point = target.Position + fromTarget.normalized * behaviour.DistanceToMaintain.Lower;
            else if (distance > behaviour.DistanceToMaintain.Upper)
                point = target.Position + fromTarget.normalized * behaviour.DistanceToMaintain.Upper;

            point.y = craftPosition.y;
            return Supported(
                behaviour.GetType().Name,
                "Broadside",
                $"Broadside at {behaviour.AngleToMaintain.Us:0.#} deg",
                behaviour.AngleToMaintain.Us >= 0f ? "left broadside" : "right broadside",
                point,
                point,
                broadsideForward,
                target,
                craftPosition,
                craftHeading,
                behaviour.DistanceToMaintain.Lower,
                behaviour.DistanceToMaintain.Upper,
                behaviour.AngleToMaintain.Us,
                approximate: false);
        }

        private static AiVanillaIntentPlan PredictPointAt(
            BehaviourPointAndMaintainDistance behaviour,
            Vector3 craftPosition,
            Vector3 craftHeading,
            TargetPositionInfo target)
        {
            Vector3 dirNoY = PlanarMath.SafePlanarDirection(craftPosition, target.Position, craftHeading);
            float distanceNoY = PlanarMath.GroundDistance(craftPosition, target.Position);
            float distance = behaviour.DistanceToMaintain.Us;
            if (behaviour.UseCurrentDistanceIfLower.Us)
                distance = Mathf.Min(distance, distanceNoY);

            Vector3 point = target.Position - dirNoY * distance;
            point.y = ApplyAltitude(behaviour.AltitudeType.Us, behaviour.PreferredAltitude.Us, craftPosition, target);
            bool reversePreferred = distanceNoY < distance && Mathf.Abs(PlanarMath.SignedPlanarAngle(craftHeading, point - craftPosition)) > behaviour.AzimuthBeforeReverse.Us;
            AiVanillaIntentPlan plan = Supported(
                behaviour.GetType().Name,
                "Point at",
                $"Maintain {distance:0.#}m and face target",
                reversePreferred ? "hold range, reverse preferred" : "hold range",
                point,
                point,
                PlanarMath.SafePlanarDirection(craftPosition, target.Position, craftHeading),
                target,
                craftPosition,
                craftHeading,
                distance,
                distance,
                0f,
                approximate: false);
            plan.ReversePreferred = reversePreferred;
            return plan;
        }

        private static AiVanillaIntentPlan PredictCircle(
            BehaviourCircleAtDistance behaviour,
            Vector3 craftPosition,
            Vector3 craftHeading,
            TargetPositionInfo target)
        {
            Vector3 toTarget = PlanarMath.SafePlanarDirection(craftPosition, target.Position, craftHeading);
            float currentAzimuth = PlanarMath.SignedPlanarAngle(craftHeading, toTarget);
            float approachAngle = GetCircleApproachAngle(behaviour, target.GroundDistance, currentAzimuth);
            Vector3 desiredTravel = PlanarMath.RotateYaw(toTarget, approachAngle);
            Vector3 rawPoint = craftPosition + desiredTravel * 1000f;
            rawPoint.y = ApplyAltitude(behaviour.AltitudeType.Us, behaviour.PreferredAltitude.Us, craftPosition, target);
            return Supported(
                behaviour.GetType().Name,
                "Circle",
                $"Circle at {behaviour.DistanceToMaintain.Us:0.#}m",
                $"approach {approachAngle:0.#} deg",
                rawPoint,
                rawPoint,
                desiredTravel,
                target,
                craftPosition,
                craftHeading,
                behaviour.DistanceToMaintain.Us,
                behaviour.DistanceToMaintain.Us,
                approachAngle,
                approximate: false);
        }

        private static AiVanillaIntentPlan PredictFtdNaval(
            FtdNaval behaviour,
            Vector3 craftPosition,
            Vector3 craftHeading,
            TargetPositionInfo target,
            IPlatformInterface platform)
        {
            float range = target.Range;
            float enterBelow = behaviour.BroadsideDistance.Lower;
            float leaveAbove = behaviour.BroadsideDistance.Upper;
            string state = PredictNavalState(behaviour, target, craftPosition, craftHeading, platform, range, enterBelow, leaveAbove);
            bool closing = state == "closing";
            Vector3 point;
            Vector3 desiredFacing;
            float desiredAngle = 0f;

            if (closing)
            {
                point = target.Position;
                point.y = craftPosition.y;
                desiredFacing = PlanarMath.SafePlanarDirection(craftPosition, target.Position, craftHeading);
            }
            else
            {
                float sideSign = state == "broadsideRight" ? -1f : 1f;
                desiredAngle = behaviour.NominalBroadsideAngle.Us * sideSign;
                float desiredDistance = Mathf.Max(behaviour.MinimumBroadsideDistanceToMaintain.Us, enterBelow);
                Vector3 fromTarget = -PlanarMath.SafePlanarDirection(craftPosition, target.Position, craftHeading);
                point = target.Position + PlanarMath.RotateYaw(fromTarget, desiredAngle) * desiredDistance;
                point.y = craftPosition.y;
                desiredFacing = PlanarMath.SafePlanarDirection(point, target.Position, craftHeading);
            }

            AiVanillaIntentPlan plan = Supported(
                behaviour.GetType().Name,
                "Broadside 2.0",
                closing ? $"Closing until {enterBelow:0.#}m" : $"{state.Replace("broadside", "broadside ")} at {behaviour.NominalBroadsideAngle.Us:0.#} deg",
                closing ? "closing" : state,
                point,
                point,
                desiredFacing,
                target,
                craftPosition,
                craftHeading,
                enterBelow,
                leaveAbove,
                desiredAngle,
                approximate: !closing);
            if (!closing)
                plan.Warnings.Add("FiringAngleCalculator and sea-surface relocation are summarized but not fully solved in this request prediction.");
            return plan;
        }

        private static void PredictShipOrTank(
            List<AiControlRequestPrediction> result,
            AiMovementRequestContext context,
            AiVanillaIntentPlan intent)
        {
            LocalSteer(context, intent.RequestPoint, out _, out float localZ, out float goalAzimuth, out float distance);
            bool reversing = context.ReverseAllowed && intent.ReversePreferred;
            bool tarrying = distance < Mathf.Max(0f, context.TarryDistance);
            if (tarrying)
            {
                float forwardVelocity = Vector3.Dot(PlanarMath.Flatten(context.CraftVelocity), context.CraftHeading);
                AddSigned(result, AiControlType.ThrustForward, AiControlType.ThrustBackward, -forwardVelocity, Mathf.Max(1f, RequestDistanceScale), context, AiPredictionConfidence.Approximate, "Ship/tank tarry distance: bring forward velocity to zero.");
                float facingAzimuth = intent.HasDesiredFacing ? PlanarMath.SignedPlanarAngle(context.CraftHeading, intent.DesiredFacing) : goalAzimuth;
                AddYaw(result, facingAzimuth, context, AiPredictionConfidence.Approximate, "Ship/tank idling yaw to desired end rotation.");
                return;
            }

            if (reversing)
            {
                Add(result, AiControlType.ThrustBackward, 1f, context, AiPredictionConfidence.Approximate, "Ship/tank reverse preferred by behaviour.");
                float reverseAzimuth = PlanarMath.Fix180(goalAzimuth > 0f ? goalAzimuth - 180f : goalAzimuth + 180f);
                AddYaw(result, reverseAzimuth, context, AiPredictionConfidence.Approximate, "Ship/tank reverse yaw to 180 degrees from steer point.");
                return;
            }

            float throttle = Mathf.Clamp01(localZ / RequestDistanceScale) * ShipTurnThrottle01(goalAzimuth);
            Add(result, AiControlType.ThrustForward, Mathf.Max(0.2f, throttle), context, AiPredictionConfidence.Approximate, "Ship/tank thrust slowed by steer azimuth.");
            AddYaw(result, goalAzimuth, context, AiPredictionConfidence.Approximate, "Ship/tank yaw-to-zero steer azimuth.");
        }

        private static void PredictHover(
            List<AiControlRequestPrediction> result,
            AiMovementRequestContext context,
            AiVanillaIntentPlan intent)
        {
            LocalSteer(context, intent.RequestPoint, out float localX, out float localZ, out float goalAzimuth, out _);
            float facingAzimuth = intent.HasDesiredFacing
                ? PlanarMath.SignedPlanarAngle(context.CraftHeading, intent.DesiredFacing)
                : goalAzimuth;
            bool aziTooHigh = Mathf.Abs(facingAzimuth) > context.HoverMoveWithinAzimuth;
            float driveScale = aziTooHigh ? 0.3f : 1f;
            AddSigned(result, AiControlType.ThrustForward, AiControlType.ThrustBackward, localZ * driveScale, RequestDistanceScale, context, AiPredictionConfidence.Approximate, aziTooHigh ? "Hover forward reduced above MoveWithinAzi." : "Hover forward/back PID to waypoint.");
            if (!aziTooHigh)
                AddSigned(result, AiControlType.StrafeRight, AiControlType.StrafeLeft, localX, RequestDistanceScale, context, AiPredictionConfidence.Approximate, "Hover strafe PID to waypoint lateral offset.");
            AddYaw(result, facingAzimuth, context, AiPredictionConfidence.Approximate, "Hover yaw to desired rotation or steer point.");
            AddAltitude(result, context, intent.RequestPoint.y, "Hover altitude PID to requested point.");
        }

        private static void PredictSixAxis(
            List<AiControlRequestPrediction> result,
            AiMovementRequestContext context,
            AiVanillaIntentPlan intent)
        {
            LocalSteer(context, intent.RequestPoint, out float localX, out float localZ, out float goalAzimuth, out _);
            Vector3 desiredFacing = intent.HasDesiredFacing ? intent.DesiredFacing : PlanarMath.SafePlanarDirection(context.CraftPosition, intent.RequestPoint, context.CraftHeading);
            float facingAzimuth = PlanarMath.SignedPlanarAngle(context.CraftHeading, desiredFacing);
            AddSigned(result, AiControlType.ThrustForward, AiControlType.ThrustBackward, localZ, RequestDistanceScale, context, AiPredictionConfidence.Approximate, "Six-axis forward/back PID to waypoint.");
            AddSigned(result, AiControlType.StrafeRight, AiControlType.StrafeLeft, localX, RequestDistanceScale, context, AiPredictionConfidence.Approximate, "Six-axis independent strafe PID to waypoint.");
            AddYaw(result, Mathf.Abs(facingAzimuth) > 0.5f ? facingAzimuth : goalAzimuth, context, AiPredictionConfidence.Approximate, "Six-axis yaw toward look-ahead desired facing.");
            AddAltitude(result, context, intent.RequestPoint.y, "Six-axis hover PID to waypoint altitude.");
        }

        private static void PredictAirplane(
            List<AiControlRequestPrediction> result,
            AiMovementRequestContext context,
            AiVanillaIntentPlan intent)
        {
            Vector3 point = intent.RequestPoint;
            LocalSteer(context, point, out _, out _, out float goalAzimuth, out float distance);
            float thrust = distance < Mathf.Max(10f, context.AirplaneIdleDistance)
                ? Mathf.Clamp01(context.AirplaneIdleThrust / 100f)
                : 1f;
            Add(result, AiControlType.ThrustForward, Mathf.Max(0.05f, thrust), context, AiPredictionConfidence.Approximate, "Airplane always requests forward thrust; idle thrust applies near placeholder/idle distance.");

            if (Mathf.Abs(goalAzimuth) >= context.AirplaneBankingTurnAbove && context.AirplaneBankingTurnRoll > 0f)
            {
                AddSigned(result, AiControlType.RollRight, AiControlType.RollLeft, goalAzimuth, RequestYawScale, context, AiPredictionConfidence.Approximate, "Airplane banking turn roll follows steer azimuth.");
            }

            AddYaw(result, goalAzimuth, context, AiPredictionConfidence.Approximate, "Airplane yaw to waypoint outside full roll/pitch PID simulation.");
            AddPitchForAltitude(result, context, point.y);
            AddAltitude(result, context, point.y, "Airplane hover-axis altitude output used by pitch/yaw logic.");
        }

        private static AiMovementRequestContext BuildLiveMovementContext(
            object manoeuvre,
            Vector3 craftPosition,
            Quaternion craftRotation,
            Vector3 craftVelocity)
        {
            var context = new AiMovementRequestContext
            {
                Model = AiCraftMovementModel.ShipOrTank,
                SourceManoeuvre = manoeuvre?.GetType().Name ?? "No manoeuvre",
                CraftPosition = craftPosition,
                CraftHeading = PlanarMath.SafePlanarDirection(Vector3.zero, craftRotation * Vector3.forward, Vector3.forward),
                CraftVelocity = craftVelocity,
                CraftAltitude = craftPosition.y,
                TarryDistance = 50f,
                ReverseAllowed = true,
                HoverYawLockDistance = 150f,
                HoverMoveWithinAzimuth = 30f,
                SixAxisLookAheadDistance = 50f,
                AirplaneIdleThrust = 100f,
                AirplaneIdleDistance = 300f,
                AirplaneBankingTurnAbove = 30f,
                AirplaneBankingTurnRoll = 0f,
                AirplanePitchForAltitude = 15f
            };

            if (manoeuvre is ManoeuvreHover hover)
            {
                context.Model = AiCraftMovementModel.Hover;
                context.HoverYawLockDistance = hover.YawLockDistance.Us;
                context.HoverMoveWithinAzimuth = hover.MoveWithinAzi.Us;
            }
            else if (manoeuvre is ManoeuvreSixAxis sixAxis)
            {
                context.Model = AiCraftMovementModel.SixAxis;
                context.SixAxisLookAheadDistance = sixAxis.LookAheadDistance.Us;
            }
            else if (manoeuvre is ManoeuvreAirplane airplane)
            {
                context.Model = AiCraftMovementModel.Airplane;
                context.AirplaneIdleThrust = airplane.IdleThrust.Us;
                context.AirplaneIdleDistance = airplane.WanderDistance.Us;
                context.AirplaneBankingTurnAbove = airplane.BankingTurnAbove.Us;
                context.AirplaneBankingTurnRoll = airplane.BankingTurnRoll.Us;
                context.AirplanePitchForAltitude = airplane.PitchForAltitude.Us;
            }
            else if (manoeuvre is FtdAerialMovement aerial)
            {
                context.Model = AiCraftMovementModel.Airplane;
                context.AirplaneBankingTurnAbove = aerial.BankingTurnAbove.Us;
                context.AirplaneBankingTurnRoll = aerial.BankingTurnRoll.Us;
                context.AirplanePitchForAltitude = aerial.PitchForAltitude.Us;
            }
            else if (manoeuvre is FtdNavalAndLandManoeuvre ship)
            {
                context.Model = AiCraftMovementModel.ShipOrTank;
                context.TarryDistance = ship.TarryDistance.Us;
            }

            return context;
        }

        private static AiVanillaIntentPlan Supported(
            string behaviourClass,
            string kind,
            string summary,
            string state,
            Vector3 rawSteer,
            Vector3 motionPoint,
            Vector3 desiredFacing,
            TargetPositionInfo target,
            Vector3 craftPosition,
            Vector3 craftHeading,
            float maintainLower,
            float maintainUpper,
            float desiredAngle,
            bool approximate)
        {
            Vector3 targetDirection = PlanarMath.SafePlanarDirection(craftPosition, target.Position, craftHeading);
            return new AiVanillaIntentPlan
            {
                Supported = true,
                BehaviourClass = behaviourClass,
                Kind = kind,
                Summary = summary,
                State = state,
                RawSteerPoint = rawSteer,
                MotionPoint = motionPoint,
                DesiredFacing = PlanarMath.SafePlanarDirection(Vector3.zero, desiredFacing, craftHeading),
                DesiredTravel = PlanarMath.SafePlanarDirection(craftPosition, rawSteer, craftHeading),
                Range = target.Range,
                GroundRange = target.GroundDistance,
                Azimuth = PlanarMath.SignedPlanarAngle(craftHeading, targetDirection),
                MaintainDistanceLower = maintainLower,
                MaintainDistanceUpper = maintainUpper,
                DesiredAngle = desiredAngle,
                HasRawSteerPoint = true,
                HasMotionPoint = true,
                HasDesiredFacing = true,
                Approximate = approximate
            };
        }

        private static AiVanillaIntentPlan Unsupported(string kind, string summary)
        {
            var plan = new AiVanillaIntentPlan
            {
                Supported = false,
                Kind = kind,
                BehaviourClass = kind,
                Summary = summary,
                State = "unsupported"
            };
            plan.Warnings.Add(summary);
            return plan;
        }

        private static string PredictNavalState(
            FtdNaval behaviour,
            TargetPositionInfo target,
            Vector3 craftPosition,
            Vector3 craftHeading,
            IPlatformInterface platform,
            float range,
            float enterBelow,
            float leaveAbove)
        {
            string state = behaviour.state.ToString();
            if (state == "closing" && range <= enterBelow)
                state = "broadside";
            else if (state != "closing" && range >= leaveAbove)
                state = "closing";

            if (state != "broadside" && state != "broadsideLeft" && state != "broadsideRight")
                return state;

            if (TryPreferredFirepowerSide(platform, behaviour.MinimumFirepowerFractionBeforeSwitchingSide.Us, out string firepowerSide))
                return firepowerSide;

            Vector3 predictedTarget = target.Position + target.Velocity * behaviour.PositionpredictionFactor;
            Vector3 vector = PlanarMath.SafePlanarDirection(craftPosition, predictedTarget, craftHeading);
            return PlanarMath.SignedPlanarAngle(vector, craftHeading) < 0f ? "broadsideRight" : "broadsideLeft";
        }

        private static bool TryPreferredFirepowerSide(
            IPlatformInterface platform,
            float minimumFractionBeforeSwitch,
            out string side)
        {
            side = null;
            if (platform == null || minimumFractionBeforeSwitch <= 0f)
                return false;

            float left = platform.GetLeftFirepower();
            float right = platform.GetRightFirepower();
            if (left <= 0f && right <= 0f)
                return false;

            if (left * minimumFractionBeforeSwitch > right)
            {
                side = "broadsideLeft";
                return true;
            }

            if (right * minimumFractionBeforeSwitch > left)
            {
                side = "broadsideRight";
                return true;
            }

            return false;
        }

        private static float GetCircleApproachAngle(
            BehaviourCircleAtDistance behaviour,
            float groundDistance,
            float azimuth)
        {
            float range = 90f - behaviour.MinApproachAngle.Us;
            float rangeCorrection = range * Mathf.Min(Mathf.Max((behaviour.DistanceToMaintain.Us - groundDistance) / 200f, -1f), 1f);
            float result = 90f + rangeCorrection;
            if (behaviour.PreferredSide.Us == SideOptions.Right ||
                (behaviour.PreferredSide.Us == SideOptions.Both && azimuth > 0f))
            {
                result = -result;
            }

            return result;
        }

        private static float ApplyAltitude(
            AltitudeOptions type,
            float preferredAltitude,
            Vector3 craftPosition,
            TargetPositionInfo target)
        {
            switch (type)
            {
                case AltitudeOptions.Absolute:
                    return preferredAltitude;
                case AltitudeOptions.Relative:
                    return preferredAltitude + target.Position.y;
                case AltitudeOptions.Ignore:
                default:
                    return craftPosition.y;
            }
        }

        private static void LocalSteer(
            AiMovementRequestContext context,
            Vector3 point,
            out float localX,
            out float localZ,
            out float azimuth,
            out float distance)
        {
            Vector3 delta = PlanarMath.Flatten(point - context.CraftPosition);
            distance = delta.magnitude;
            Vector3 forward = PlanarMath.SafePlanarDirection(Vector3.zero, context.CraftHeading, Vector3.forward);
            Vector3 right = PlanarMath.RotateYaw(forward, 90f);
            localX = Vector3.Dot(delta, right);
            localZ = Vector3.Dot(delta, forward);
            azimuth = distance <= 0.001f ? 0f : PlanarMath.SignedPlanarAngle(forward, delta);
        }

        private static void AddYaw(
            List<AiControlRequestPrediction> result,
            float azimuth,
            AiMovementRequestContext context,
            AiPredictionConfidence confidence,
            string note)
        {
            AddSigned(result, AiControlType.YawRight, AiControlType.YawLeft, azimuth, RequestYawScale, context, confidence, note);
        }

        private static void AddAltitude(
            List<AiControlRequestPrediction> result,
            AiMovementRequestContext context,
            float targetAltitude,
            string note)
        {
            float delta = targetAltitude - context.CraftAltitude;
            AddSigned(result, AiControlType.HoverUp, AiControlType.HoverDown, delta, RequestAltitudeScale, context, AiPredictionConfidence.Approximate, note);
        }

        private static void AddPitchForAltitude(
            List<AiControlRequestPrediction> result,
            AiMovementRequestContext context,
            float targetAltitude)
        {
            float delta = targetAltitude - context.CraftAltitude;
            if (Mathf.Abs(delta) <= 1f || context.AirplanePitchForAltitude <= 0f)
                return;

            AddSigned(
                result,
                AiControlType.PitchUp,
                AiControlType.PitchDown,
                delta,
                RequestAltitudeScale,
                context,
                AiPredictionConfidence.Approximate,
                "Airplane pitch sign follows altitude error approximation.");
        }

        private static void AddSigned(
            List<AiControlRequestPrediction> result,
            AiControlType positive,
            AiControlType negative,
            float signed,
            float scale,
            AiMovementRequestContext context,
            AiPredictionConfidence confidence,
            string note)
        {
            float value = Mathf.Clamp01(Mathf.Abs(signed) / Mathf.Max(0.001f, scale));
            Add(result, signed >= 0f ? positive : negative, value, context, confidence, note);
        }

        private static void Add(
            List<AiControlRequestPrediction> result,
            AiControlType type,
            float value,
            AiMovementRequestContext context,
            AiPredictionConfidence confidence,
            string note)
        {
            value = Mathf.Clamp01(value);
            if (value <= 0.001f)
                return;

            foreach (AiControlRequestPrediction request in result)
            {
                if (request.Type != type)
                    continue;

                if (value > request.Value)
                {
                    request.Value = value;
                    request.Note = note;
                    request.Confidence = confidence;
                }
                return;
            }

            result.Add(new AiControlRequestPrediction
            {
                Type = type,
                Value = value,
                SourceManoeuvre = context.SourceManoeuvre,
                Confidence = confidence,
                Note = note
            });
        }

        private static AiControlRequestDelta GetDelta(
            Dictionary<AiControlType, AiControlRequestDelta> byType,
            AiControlType type)
        {
            if (!byType.TryGetValue(type, out AiControlRequestDelta delta))
            {
                delta = new AiControlRequestDelta { Type = type, SignMatches = true };
                byType.Add(type, delta);
            }

            return delta;
        }
    }
}
