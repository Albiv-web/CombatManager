using System;
using BrilliantSkies.Ai.Interfaces;
using BrilliantSkies.Ai.Modules.Behaviour;
using BrilliantSkies.Ai.Modules.Behaviour.Examples;
using BrilliantSkies.Ai.Targetting;
using UnityEngine;

namespace CombatManager.Ai
{
    internal static class AiIntentPredictor
    {
        internal static AiIntentPrediction Predict(
            object behaviour,
            Vector3 craftPosition,
            Quaternion craftRotation,
            TargetPositionInfo target,
            IPlatformInterface platform)
        {
            if (!target.Valid)
            {
                return Unsupported("No target", "No target is available for behaviour prediction.");
            }

            if (behaviour is FtdNaval naval)
                return PredictFtdNaval(naval, craftPosition, craftRotation, target, platform);

            if (behaviour is BehaviourBroadside broadside)
                return PredictBroadside(broadside, craftPosition, target);

            if (behaviour is BehaviourPointAndMaintainDistance pointAt)
                return PredictPointAt(pointAt, craftPosition, target);

            if (behaviour is BehaviourCircleAtDistance circle)
                return PredictCircle(circle, craftPosition, craftRotation, target);

            return Unsupported(
                behaviour?.GetType().Name ?? "No behaviour",
                "This behaviour is not mirrored by the V1 passive predictor.");
        }

        private static AiIntentPrediction PredictBroadside(
            BehaviourBroadside behaviour,
            Vector3 craftPosition,
            TargetPositionInfo target)
        {
            Vector3 targetDirection = PlanarMath.SafePlanarDirection(craftPosition, target.Position, Vector3.forward);
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
            return new AiIntentPrediction
            {
                Supported = true,
                Kind = "Broadside",
                Summary = $"Broadside at {behaviour.AngleToMaintain.Us:0.#} deg",
                DesiredPoint = point,
                DesiredRotation = PlanarMath.SafeLookRotation(broadsideForward),
                HasPoint = true,
                HasFacing = true,
                MaintainDistanceLower = behaviour.DistanceToMaintain.Lower,
                MaintainDistanceUpper = behaviour.DistanceToMaintain.Upper,
                DesiredAngle = behaviour.AngleToMaintain.Us
            };
        }

        private static AiIntentPrediction PredictPointAt(
            BehaviourPointAndMaintainDistance behaviour,
            Vector3 craftPosition,
            TargetPositionInfo target)
        {
            Vector3 dirNoY = PlanarMath.SafePlanarDirection(craftPosition, target.Position, Vector3.forward);
            float distanceNoY = PlanarMath.GroundDistance(craftPosition, target.Position);
            float distance = behaviour.DistanceToMaintain.Us;
            if (behaviour.UseCurrentDistanceIfLower.Us)
                distance = Mathf.Min(distance, distanceNoY);

            Vector3 point = target.Position - dirNoY * distance;
            point.y = ApplyAltitude(behaviour.AltitudeType.Us, behaviour.PreferredAltitude.Us, craftPosition, target);
            Quaternion rotation = PlanarMath.SafeLookRotation(target.Position - craftPosition);

            return new AiIntentPrediction
            {
                Supported = true,
                Kind = "Point at",
                Summary = $"Maintain {distance:0.#}m and face target",
                DesiredPoint = point,
                DesiredRotation = rotation,
                HasPoint = true,
                HasFacing = true,
                MaintainDistanceLower = distance,
                MaintainDistanceUpper = distance,
                DesiredAngle = 0f
            };
        }

        private static AiIntentPrediction PredictCircle(
            BehaviourCircleAtDistance behaviour,
            Vector3 craftPosition,
            Quaternion craftRotation,
            TargetPositionInfo target)
        {
            Vector3 toTarget = PlanarMath.SafePlanarDirection(craftPosition, target.Position, craftRotation * Vector3.forward);
            float currentAzimuth = PlanarMath.SignedPlanarAngle(craftRotation * Vector3.forward, toTarget);
            float approachAngle = GetCircleApproachAngle(behaviour, target.GroundDistance, currentAzimuth);
            Quaternion rotation = PlanarMath.SafeLookRotation(PlanarMath.RotateYaw(toTarget, approachAngle));
            Vector3 point = craftPosition + rotation * Vector3.forward * 1000f;
            point.y = ApplyAltitude(behaviour.AltitudeType.Us, behaviour.PreferredAltitude.Us, craftPosition, target);

            return new AiIntentPrediction
            {
                Supported = true,
                Kind = "Circle",
                Summary = $"Circle at {behaviour.DistanceToMaintain.Us:0.#}m",
                DesiredPoint = point,
                DesiredRotation = rotation,
                HasPoint = true,
                HasFacing = true,
                MaintainDistanceLower = behaviour.DistanceToMaintain.Us,
                MaintainDistanceUpper = behaviour.DistanceToMaintain.Us,
                DesiredAngle = approachAngle
            };
        }

        private static AiIntentPrediction PredictFtdNaval(
            FtdNaval behaviour,
            Vector3 craftPosition,
            Quaternion craftRotation,
            TargetPositionInfo target,
            IPlatformInterface platform)
        {
            float range = target.Range;
            float enterBelow = behaviour.BroadsideDistance.Lower;
            float leaveAbove = behaviour.BroadsideDistance.Upper;
            string state = PredictNavalState(behaviour, target, craftPosition, craftRotation, platform, range, enterBelow, leaveAbove);
            bool closing = state == "closing";
            Vector3 point;
            Quaternion desiredRotation;

            if (closing)
            {
                point = target.Position;
                point.y = craftPosition.y;
                desiredRotation = PlanarMath.SafeLookRotation(target.Position - craftPosition);
            }
            else
            {
                float sideSign = state == "broadsideRight" ? -1f : 1f;
                float desiredAngle = behaviour.NominalBroadsideAngle.Us * sideSign;
                float desiredDistance = Mathf.Max(behaviour.MinimumBroadsideDistanceToMaintain.Us, enterBelow);
                Vector3 fromTarget = -PlanarMath.SafePlanarDirection(craftPosition, target.Position, craftRotation * Vector3.forward);
                point = target.Position + PlanarMath.RotateYaw(fromTarget, desiredAngle) * desiredDistance;
                point.y = craftPosition.y;
                desiredRotation = PlanarMath.SafeLookRotation(target.Position - point);
            }

            return new AiIntentPrediction
            {
                Supported = true,
                Kind = "Broadside 2.0",
                Summary = closing
                    ? $"Closing until {enterBelow:0.#}m"
                    : $"{state.Replace("broadside", "broadside ")} at {behaviour.NominalBroadsideAngle.Us:0.#} deg",
                DesiredPoint = point,
                DesiredRotation = desiredRotation,
                HasPoint = true,
                HasFacing = true,
                Approximate = !closing,
                MaintainDistanceLower = enterBelow,
                MaintainDistanceUpper = leaveAbove,
                DesiredAngle = closing ? 0f : behaviour.NominalBroadsideAngle.Us
            };
        }

        private static string PredictNavalState(
            FtdNaval behaviour,
            TargetPositionInfo target,
            Vector3 craftPosition,
            Quaternion craftRotation,
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
            Vector3 vector = PlanarMath.SafePlanarDirection(craftPosition, predictedTarget, craftRotation * Vector3.forward);
            Vector3 forward = PlanarMath.SafePlanarDirection(Vector3.zero, craftRotation * Vector3.forward, Vector3.forward);
            return Vector3.SignedAngle(vector, forward, Vector3.up) < 0f
                ? "broadsideRight"
                : "broadsideLeft";
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
            float num = 90f - behaviour.MinApproachAngle.Us;
            float num2 = num * Mathf.Min(Mathf.Max((behaviour.DistanceToMaintain.Us - groundDistance) / 200f, -1f), 1f);
            float result = 90f + num2;
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

        private static AiIntentPrediction Unsupported(string kind, string summary)
        {
            return new AiIntentPrediction
            {
                Supported = false,
                Kind = kind,
                Summary = summary
            };
        }
    }
}
