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
                CircleStepAdvancesBySpeedOverRadius();
                TargetProjectionStaysCentered();
                OrbitRingFitsResizeBounds();
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

        private static void CircleStepAdvancesBySpeedOverRadius()
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
            state.Step(10f);
            AssertNear(57.29578f, state.OrbitAngleDegrees, "circle angular step");
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

        private static void AssertNear(float expected, float actual, string name)
        {
            if (Math.Abs(expected - actual) > 0.001f)
                throw new InvalidOperationException($"{name}: expected {expected}, got {actual}");
        }
    }
}
