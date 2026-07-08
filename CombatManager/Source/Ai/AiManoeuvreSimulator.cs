using UnityEngine;

namespace CombatManager.Ai
{
    internal static class AiManoeuvreSimulator
    {
        internal static void Advance(AiSimEntity entity, AiSimulationFrame frame, float delta)
        {
            Vector3 goal = AiSimulationState.FrameMotionPoint(frame);
            Vector3 toGoal = PlanarMath.Flatten(goal - entity.Position);
            float distance = toGoal.magnitude;
            Vector3 desiredMoveDirection = distance > 0.1f ? toGoal.normalized : Vector3.zero;

            switch (entity.CraftMovementModel)
            {
                case AiCraftMovementModel.Hover:
                    AdvanceHover(entity, frame, desiredMoveDirection, distance, delta);
                    break;
                case AiCraftMovementModel.SixAxis:
                    AdvanceSixAxis(entity, frame, desiredMoveDirection, distance, delta);
                    break;
                case AiCraftMovementModel.Airplane:
                    AdvanceAirplane(entity, frame, desiredMoveDirection, distance, delta);
                    break;
                default:
                    AdvanceShipOrTank(entity, frame, desiredMoveDirection, distance, delta);
                    break;
            }

            entity.Position = new Vector3(entity.Position.x, entity.Altitude, entity.Position.z);
        }

        private static void AdvanceShipOrTank(AiSimEntity entity, AiSimulationFrame frame, Vector3 desiredMoveDirection, float distance, float delta)
        {
            if (distance <= 1f || desiredMoveDirection.sqrMagnitude < 0.0001f)
            {
                entity.CraftCurrentSpeed = 0f;
                entity.Velocity = Vector3.zero;
                return;
            }

            bool reversing = entity.ShipReverseAllowed && frame.ReversePreferred;
            Vector3 desiredHeading = reversing && frame.HasDesiredFacing ? frame.DesiredFacing : desiredMoveDirection;
            entity.Heading = RotateTowardsFlat(entity.Heading, desiredHeading, entity.CraftTurnRate * delta);

            Vector3 moveDirection = reversing ? -entity.Heading : entity.Heading;
            float desiredSpeed = entity.CraftSpeed * ShipThrottle01(entity, distance, desiredMoveDirection, reversing);
            entity.CraftCurrentSpeed = Mathf.MoveTowards(entity.CraftCurrentSpeed, desiredSpeed, entity.CraftAcceleration * delta);

            float travel = entity.CraftCurrentSpeed * delta;
            if (distance < Mathf.Max(4f, entity.ShipTarryDistance))
                travel = Mathf.Min(travel, distance);

            entity.Position += moveDirection * travel;
            entity.Velocity = moveDirection * (travel / Mathf.Max(0.001f, delta));
        }

        private static void AdvanceHover(AiSimEntity entity, AiSimulationFrame frame, Vector3 desiredMoveDirection, float distance, float delta)
        {
            if (distance <= 1f || desiredMoveDirection.sqrMagnitude < 0.0001f)
            {
                entity.CraftCurrentSpeed = 0f;
                entity.Velocity = Vector3.zero;
                if (frame.HasDesiredFacing)
                    entity.Heading = RotateTowardsFlat(entity.Heading, frame.DesiredFacing, entity.CraftTurnRate * delta);
                return;
            }

            Vector3 desiredFacing = frame.HasDesiredFacing ? frame.DesiredFacing : desiredMoveDirection;
            entity.Heading = RotateTowardsFlat(entity.Heading, desiredFacing, entity.CraftTurnRate * delta);

            float facingError = Mathf.Abs(PlanarMath.SignedPlanarAngle(entity.Heading, desiredMoveDirection));
            bool strafeDisabled = facingError > entity.HoverMoveWithinAzimuth;
            float driveScale = strafeDisabled ? 0.3f : 1f;
            Vector3 moveDirection = strafeDisabled
                ? PlanarMath.SafePlanarDirection(Vector3.zero, entity.Heading * Mathf.Sign(Vector3.Dot(entity.Heading, desiredMoveDirection)), desiredMoveDirection)
                : desiredMoveDirection;
            float desiredSpeed = entity.CraftSpeed * Mathf.Clamp01(distance / 120f) * driveScale;
            entity.CraftCurrentSpeed = Mathf.MoveTowards(entity.CraftCurrentSpeed, desiredSpeed, entity.CraftAcceleration * delta);

            float travel = Mathf.Min(entity.CraftCurrentSpeed * delta, distance);
            entity.Position += moveDirection * travel;
            entity.Velocity = moveDirection * (travel / Mathf.Max(0.001f, delta));
        }

        private static void AdvanceSixAxis(AiSimEntity entity, AiSimulationFrame frame, Vector3 desiredMoveDirection, float distance, float delta)
        {
            if (distance <= 1f || desiredMoveDirection.sqrMagnitude < 0.0001f)
            {
                entity.CraftCurrentSpeed = 0f;
                entity.Velocity = Vector3.zero;
                if (frame.HasDesiredFacing)
                    entity.Heading = RotateTowardsFlat(entity.Heading, frame.DesiredFacing, entity.CraftTurnRate * delta);
                return;
            }

            Vector3 desiredFacing = frame.HasDesiredFacing ? frame.DesiredFacing : desiredMoveDirection;
            float lookWeight = Mathf.Clamp01(entity.SixAxisLookAheadDistance / Mathf.Max(entity.SixAxisLookAheadDistance + distance, 1f));
            Vector3 blendedFacing = Vector3.Lerp(desiredMoveDirection, desiredFacing, lookWeight);
            entity.Heading = RotateTowardsFlat(entity.Heading, blendedFacing, entity.CraftTurnRate * delta);

            float desiredSpeed = entity.CraftSpeed * Mathf.Clamp01(distance / 100f);
            entity.CraftCurrentSpeed = Mathf.MoveTowards(entity.CraftCurrentSpeed, desiredSpeed, entity.CraftAcceleration * delta);

            float travel = Mathf.Min(entity.CraftCurrentSpeed * delta, distance);
            entity.Position += desiredMoveDirection * travel;
            entity.Velocity = desiredMoveDirection * (travel / Mathf.Max(0.001f, delta));
        }

        private static void AdvanceAirplane(AiSimEntity entity, AiSimulationFrame frame, Vector3 desiredMoveDirection, float distance, float delta)
        {
            Vector3 desiredHeading = desiredMoveDirection.sqrMagnitude > 0.0001f
                ? desiredMoveDirection
                : (frame.HasDesiredFacing ? frame.DesiredFacing : entity.Heading);
            float headingError = Mathf.Abs(PlanarMath.SignedPlanarAngle(entity.Heading, desiredHeading));

            float speedForTurn = Mathf.Max(entity.AirplaneMinimumSpeed, entity.CraftCurrentSpeed);
            float radiusLimitedTurnRate = speedForTurn / Mathf.Max(20f, entity.AirplaneMinimumTurnRadius) * Mathf.Rad2Deg;
            float bankScale = headingError >= entity.AirplaneBankingTurnAbove && entity.AirplaneBankingTurnRoll > 0f ? 1.15f : 0.75f;
            float turnLimit = Mathf.Min(entity.CraftTurnRate * bankScale, Mathf.Max(8f, radiusLimitedTurnRate));
            entity.Heading = RotateTowardsFlat(entity.Heading, desiredHeading, turnLimit * delta);

            float distanceScale = Mathf.Clamp01(distance / Mathf.Max(120f, entity.AirplaneIdleDistance));
            float idleScale = Mathf.Clamp01(entity.AirplaneIdleThrust / 100f);
            float targetScale = Mathf.Max(idleScale, Mathf.Lerp(0.65f, 1f, distanceScale));
            float desiredSpeed = Mathf.Max(entity.AirplaneMinimumSpeed, entity.CraftSpeed * targetScale);
            entity.CraftCurrentSpeed = Mathf.MoveTowards(entity.CraftCurrentSpeed, desiredSpeed, entity.CraftAcceleration * delta);

            float travel = entity.CraftCurrentSpeed * delta;
            entity.Position += entity.Heading * travel;
            entity.Velocity = entity.Heading * (travel / Mathf.Max(0.001f, delta));
        }

        private static float ShipThrottle01(AiSimEntity entity, float distance, Vector3 desiredMoveDirection, bool reversing)
        {
            float distanceScale = Mathf.Clamp01(distance / 120f);
            if (distance < entity.ShipTarryDistance)
                distanceScale *= Mathf.Clamp01(distance / Mathf.Max(1f, entity.ShipTarryDistance));

            float facing = reversing
                ? Mathf.Abs(PlanarMath.SignedPlanarAngle(-entity.Heading, desiredMoveDirection))
                : Mathf.Abs(PlanarMath.SignedPlanarAngle(entity.Heading, desiredMoveDirection));
            float turnScale = AiVanillaPredictor.ShipTurnThrottle01(facing);

            if (reversing)
                turnScale *= 0.65f;
            return distanceScale * turnScale;
        }

        private static Vector3 RotateTowardsFlat(Vector3 current, Vector3 desired, float maxDegrees)
        {
            current = PlanarMath.SafePlanarDirection(Vector3.zero, current, Vector3.forward);
            desired = PlanarMath.SafePlanarDirection(Vector3.zero, desired, current);
            float angle = PlanarMath.SignedPlanarAngle(current, desired);
            float step = Mathf.Clamp(angle, -Mathf.Abs(maxDegrees), Mathf.Abs(maxDegrees));
            return PlanarMath.RotateYaw(current, step);
        }
    }
}
