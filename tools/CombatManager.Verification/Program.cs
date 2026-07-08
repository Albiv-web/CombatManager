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

        private static void AssertNear(float expected, float actual, string name)
        {
            if (Math.Abs(expected - actual) > 0.001f)
                throw new InvalidOperationException($"{name}: expected {expected}, got {actual}");
        }
    }
}
