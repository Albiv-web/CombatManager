using System;
using CombatManager.Ai;
using UnityEngine;

namespace CombatManager.Verification
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                Fix180WrapsAngles();
                SafePlanarDirectionIgnoresY();
                RotateYawMatchesUnityTopDownConvention();
                GroundDistanceUsesOnlyXZ();
                CraftMovesDuringCircleSimulation();
                TargetProjectionStaysCentered();
                OrbitRingFitsResizeBounds();
                RectangularProjectionKeepsTargetCentered();
                ZoomChangesMetersPerPixelPredictably();
                SideModesProduceStableFrames();
                TargetCenteredCoordinateConversionSurvivesTargetMovement();
                TargetProfileSpeedTurnStepping();
                PointAtPlannerKeepsDesiredRangeAroundMovingTarget();
                BroadsidePlannerFlipsSidePredictably();
                CraftPursuitConvergesTowardStationaryDesiredPoint();
                Console.WriteLine("CombatManager verification passed.");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static void Fix180WrapsAngles()
        {
            AssertNear(-179f, PlanarMath.Fix180(181f), "181 wraps to -179");
            AssertNear(179f, PlanarMath.Fix180(-181f), "-181 wraps to 179");
        }

        private static void SafePlanarDirectionIgnoresY()
        {
            Vector3 direction = PlanarMath.SafePlanarDirection(
                new Vector3(0f, 20f, 0f),
                new Vector3(0f, -10f, 10f),
                Vector3.right);
            AssertNear(0f, direction.x, "planar direction x");
            AssertNear(0f, direction.y, "planar direction y");
            AssertNear(1f, direction.z, "planar direction z");
        }

        private static void RotateYawMatchesUnityTopDownConvention()
        {
            Vector3 rotated = PlanarMath.RotateYaw(Vector3.forward, 90f);
            AssertNear(1f, rotated.x, "yaw right x");
            AssertNear(0f, rotated.y, "yaw right y");
            AssertNear(0f, rotated.z, "yaw right z");
        }

        private static void GroundDistanceUsesOnlyXZ()
        {
            float distance = PlanarMath.GroundDistance(
                new Vector3(0f, 100f, 0f),
                new Vector3(3f, -100f, 4f));
            AssertNear(5f, distance, "3-4-5 ground distance");
        }

        private static void CraftMovesDuringCircleSimulation()
        {
            var state = new AiSimulationState
            {
                Preset = AiSimulationPreset.Circle,
                Side = AiSimulationSide.Left,
                Radius = 200f,
                CraftSpeed = 20f,
                PlaybackSpeed = 1f
            };

            state.Reset();
            Vector3 before = state.CraftPosition;
            state.Step(2f);

            if (PlanarMath.GroundDistance(before, state.CraftPosition) <= 0.1f)
                throw new InvalidOperationException("circle simulation did not move the craft");
        }

        private static void TargetProjectionStaysCentered()
        {
            var state = new AiSimulationState { Radius = 200f };
            Rect rect = new Rect(0f, 0f, 600f, 400f);
            AiSimulationGridProjection projection = AiSimulationGridProjection.For(rect, state);
            Vector2 center = projection.WorldToScreen(Vector3.zero);

            AssertNear(rect.center.x, center.x, "target center x");
            AssertNear(rect.center.y, center.y, "target center y");
        }

        private static void OrbitRingFitsResizeBounds()
        {
            var state = new AiSimulationState
            {
                Radius = 200f
            };
            Rect rect = new Rect(0f, 0f, 300f, 500f);
            AiSimulationGridProjection projection = AiSimulationGridProjection.For(rect, state);
            Vector2 center = projection.WorldToScreen(Vector3.zero);
            Vector2 craft = projection.WorldToScreen(state.BuildFrame().CraftPosition);
            float screenDistance = (craft - center).magnitude;

            if (screenDistance > Mathf.Min(rect.width, rect.height) * 0.5f)
                throw new InvalidOperationException("orbit ring exceeds the shortest grid dimension");
        }

        private static void RectangularProjectionKeepsTargetCentered()
        {
            var state = new AiSimulationState { Radius = 350f };
            Rect rect = new Rect(25f, 40f, 900f, 420f);
            AiSimulationGridProjection projection = AiSimulationGridProjection.For(rect, state);
            Vector2 center = projection.WorldToScreen(Vector3.zero);

            AssertNear(rect.center.x, center.x, "rectangular target center x");
            AssertNear(rect.center.y, center.y, "rectangular target center y");

            if (projection.VisibleHalfWidth <= projection.VisibleHalfHeight)
                throw new InvalidOperationException("rectangular projection did not expose a wider horizontal world span");
        }

        private static void ZoomChangesMetersPerPixelPredictably()
        {
            var state = new AiSimulationState { Radius = 200f, GridZoom = 1f };
            Rect rect = new Rect(0f, 0f, 800f, 500f);
            float baseMetersPerPixel = AiSimulationGridProjection.For(rect, state).MetersPerPixel;

            state.GridZoom = 2f;
            float zoomedMetersPerPixel = AiSimulationGridProjection.For(rect, state).MetersPerPixel;

            AssertNear(baseMetersPerPixel * 0.5f, zoomedMetersPerPixel, "zoom m/px");
        }

        private static void SideModesProduceStableFrames()
        {
            AssertStableSide(AiSimulationSide.Both, -1f);
            AssertStableSide(AiSimulationSide.Left, 1f);
            AssertStableSide(AiSimulationSide.Right, -1f);
        }

        private static void AssertStableSide(AiSimulationSide side, float expectedDirection)
        {
            var state = new AiSimulationState
            {
                Preset = AiSimulationPreset.Circle,
                Side = side,
                Radius = 200f
            };
            AiSimulationFrame frame = state.BuildFrame();

            AssertNear(expectedDirection, state.OrbitDirection(), $"{side} orbit direction");
            AssertNear(200f, PlanarMath.GroundDistance(Vector3.zero, frame.CraftPosition), $"{side} craft radius");

            if (float.IsNaN(frame.CraftHeading.x) || float.IsNaN(frame.CraftHeading.z))
                throw new InvalidOperationException($"{side} produced a NaN heading");
        }

        private static void TargetCenteredCoordinateConversionSurvivesTargetMovement()
        {
            var state = new AiSimulationState();
            state.SetTargetProfile(AiTargetProfile.FastMover);
            state.Step(5f);
            Rect rect = new Rect(0f, 0f, 900f, 500f);
            AiSimulationGridProjection projection = AiSimulationGridProjection.For(rect, state);
            Vector2 targetScreen = projection.WorldToScreen(state.TargetPosition);

            AssertNear(rect.center.x, targetScreen.x, "moving target center x");
            AssertNear(rect.center.y, targetScreen.y, "moving target center y");
        }

        private static void TargetProfileSpeedTurnStepping()
        {
            var state = new AiSimulationState();
            state.SetTargetProfile(AiTargetProfile.Ship);
            Vector3 heading = state.TargetHeading;
            state.Step(1f);

            AssertNear(18f, state.TargetVelocity.magnitude, "ship profile speed");
            if (PlanarMath.SignedPlanarAngle(heading, state.TargetHeading) <= 0.1f)
                throw new InvalidOperationException("ship profile did not turn on orbit path");
        }

        private static void PointAtPlannerKeepsDesiredRangeAroundMovingTarget()
        {
            var state = new AiSimulationState
            {
                Preset = AiSimulationPreset.PointAt,
                Radius = 275f
            };
            state.SetTargetProfile(AiTargetProfile.FastMover);
            state.Step(1f);
            AiSimulationFrame frame = state.BuildFrame();

            AssertNear(275f, PlanarMath.GroundDistance(frame.TargetPosition, frame.DesiredPoint), "point-at desired range");
        }

        private static void BroadsidePlannerFlipsSidePredictably()
        {
            var state = new AiSimulationState
            {
                Preset = AiSimulationPreset.Broadside,
                Radius = 200f,
                BroadsideAngle = 75f,
                Side = AiSimulationSide.Left
            };
            state.Reset();
            AssertNear(75f, state.BuildFrame().BroadsideAngle, "left broadside angle");

            state.Side = AiSimulationSide.Right;
            AssertNear(-75f, state.BuildFrame().BroadsideAngle, "right broadside angle");
        }

        private static void CraftPursuitConvergesTowardStationaryDesiredPoint()
        {
            var state = new AiSimulationState
            {
                Preset = AiSimulationPreset.PointAt,
                Radius = 400f,
                CraftSpeed = 50f,
                CraftAcceleration = 25f
            };
            state.SetTargetProfile(AiTargetProfile.Static);
            state.Reset();
            state.Radius = 520f;
            float initialError = PlanarMath.GroundDistance(state.CraftPosition, state.BuildFrame().DesiredPoint);

            for (int i = 0; i < 120; i++)
                state.Step(0.1f);

            AiSimulationFrame frame = state.BuildFrame();
            float finalError = PlanarMath.GroundDistance(state.CraftPosition, frame.DesiredPoint);
            if (finalError >= initialError * 0.25f)
                throw new InvalidOperationException($"craft pursuit did not converge enough: initial {initialError}, final {finalError}");
            if (Math.Abs(frame.GroundRange - state.Radius) > 35f)
                throw new InvalidOperationException($"craft pursuit overshot desired range: {frame.GroundRange}");
        }

        private static void AssertNear(float expected, float actual, string name)
        {
            if (Math.Abs(expected - actual) > 0.001f)
                throw new InvalidOperationException($"{name}: expected {expected}, got {actual}");
        }
    }
}
