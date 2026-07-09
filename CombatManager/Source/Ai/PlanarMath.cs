using UnityEngine;

namespace CombatManager.Ai
{
    internal static class PlanarMath
    {
        internal static Vector3 Flatten(Vector3 value) => new Vector3(value.x, 0f, value.z);

        internal static float GroundDistance(Vector3 a, Vector3 b) => Flatten(a - b).magnitude;

        internal static Vector3 SafePlanarDirection(Vector3 from, Vector3 to, Vector3 fallback)
        {
            Vector3 direction = Flatten(to - from);
            if (direction.sqrMagnitude < 0.0001f)
                direction = Flatten(fallback);
            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector3.forward;
            return direction.normalized;
        }

        internal static Quaternion SafeLookRotation(Vector3 direction)
        {
            direction = Flatten(direction);
            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector3.forward;
            return Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        internal static float SignedPlanarAngle(Vector3 from, Vector3 to)
        {
            from = SafePlanarDirection(Vector3.zero, from, Vector3.forward);
            to = SafePlanarDirection(Vector3.zero, to, Vector3.forward);
            return Vector3.SignedAngle(from, to, Vector3.up);
        }

        internal static float Fix180(float angle)
        {
            while (angle > 180f)
                angle -= 360f;
            while (angle < -180f)
                angle += 360f;
            return angle;
        }

        internal static Vector3 RotateYaw(Vector3 direction, float degrees)
        {
            direction = SafePlanarDirection(Vector3.zero, direction, Vector3.forward);
            float radians = degrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            return new Vector3(
                direction.x * cos + direction.z * sin,
                0f,
                -direction.x * sin + direction.z * cos).normalized;
        }

        internal static float HeadingYaw(Vector3 direction)
        {
            direction = SafePlanarDirection(Vector3.zero, direction, Vector3.forward);
            return SignedPlanarAngle(Vector3.forward, direction);
        }

        internal static Vector2 Xz(Vector3 value) => new Vector2(value.x, value.z);

        internal static float ClampFinite(float value, float fallback = 0f)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? fallback : value;
        }

        internal static bool NearlyZero(Vector3 value) => Flatten(value).sqrMagnitude < 0.0001f;
    }
}
