using System;
using System.Collections.Generic;
using BrilliantSkies.Ai.Interfaces;
using UnityEngine;

namespace CombatManager.Ai
{
    internal enum AiSimulationPreset
    {
        Circle,
        PointAt,
        Broadside
    }

    internal enum AiSimulationSide
    {
        Both,
        Left,
        Right
    }

    internal sealed class AiSimulationState
    {
        private const int MaxTrailPoints = 180;

        internal AiSimulationPreset Preset { get; set; } = AiSimulationPreset.Circle;
        internal AiSimulationSide Side { get; set; } = AiSimulationSide.Right;
        internal float Radius { get; set; } = 200f;
        internal float CraftSpeed { get; set; } = 30f;
        internal float PlaybackSpeed { get; set; } = 1f;
        internal float BroadsideAngle { get; set; } = 75f;
        internal float OrbitAngleDegrees { get; private set; }
        internal bool Playing { get; set; } = true;
        internal bool ShowInspector { get; set; } = true;
        internal string ImportStatus { get; set; } = "Standalone sandbox. Import is optional.";
        internal string ImportedBehaviour { get; set; }
        internal string ImportedMainframe { get; set; }
        internal List<string> ImportedParameters { get; } = new List<string>();
        internal List<AiControlRequestSnapshot> ImportedRequests { get; } = new List<AiControlRequestSnapshot>();
        internal List<Vector3> Trail { get; } = new List<Vector3>();

        internal void Reset()
        {
            OrbitAngleDegrees = 0f;
            Trail.Clear();
            Trail.Add(BuildFrame().CraftPosition);
        }

        internal void Step(float deltaSeconds)
        {
            deltaSeconds = Mathf.Max(0f, deltaSeconds);
            if (Preset == AiSimulationPreset.Circle)
            {
                float direction = OrbitDirection();
                float radians = CraftSpeed / Mathf.Max(1f, Radius) * deltaSeconds * Mathf.Max(0f, PlaybackSpeed);
                OrbitAngleDegrees = PlanarMath.Fix180(OrbitAngleDegrees + radians * Mathf.Rad2Deg * direction);
                AddTrailPoint(BuildFrame().CraftPosition);
            }
        }

        internal AiSimulationFrame BuildFrame()
        {
            switch (Preset)
            {
                case AiSimulationPreset.PointAt:
                    return BuildPointAtFrame();
                case AiSimulationPreset.Broadside:
                    return BuildBroadsideFrame();
                default:
                    return BuildCircleFrame();
            }
        }

        internal float OrbitDirection()
        {
            return Side == AiSimulationSide.Left ? 1f : -1f;
        }

        internal void SetPreset(AiSimulationPreset preset)
        {
            if (Preset == preset)
                return;

            Preset = preset;
            Reset();
        }

        private AiSimulationFrame BuildCircleFrame()
        {
            Vector3 radial = PlanarMath.RotateYaw(Vector3.right, OrbitAngleDegrees);
            Vector3 craft = radial * Radius;
            Vector3 tangent = new Vector3(-radial.z, 0f, radial.x).normalized * OrbitDirection();
            return new AiSimulationFrame
            {
                Preset = Preset,
                TargetPosition = Vector3.zero,
                CraftPosition = craft,
                Heading = tangent,
                DesiredTravel = tangent,
                ToTarget = -radial,
                Radius = Radius,
                Summary = $"Circle {Radius:0.#}m, {CraftSpeed:0.#}m/s"
            };
        }

        private AiSimulationFrame BuildPointAtFrame()
        {
            Vector3 radial = PlanarMath.RotateYaw(Vector3.right, OrbitAngleDegrees);
            Vector3 craft = radial * Radius;
            return new AiSimulationFrame
            {
                Preset = Preset,
                TargetPosition = Vector3.zero,
                CraftPosition = craft,
                Heading = -radial,
                DesiredTravel = Vector3.zero,
                ToTarget = -radial,
                Radius = Radius,
                Summary = $"Point at target from {Radius:0.#}m"
            };
        }

        private AiSimulationFrame BuildBroadsideFrame()
        {
            Vector3 radial = PlanarMath.RotateYaw(Vector3.right, OrbitAngleDegrees);
            Vector3 craft = radial * Radius;
            float side = Side == AiSimulationSide.Left ? 1f : -1f;
            Vector3 heading = PlanarMath.RotateYaw(-radial, BroadsideAngle * side);
            return new AiSimulationFrame
            {
                Preset = Preset,
                TargetPosition = Vector3.zero,
                CraftPosition = craft,
                Heading = heading,
                DesiredTravel = Vector3.zero,
                ToTarget = -radial,
                Radius = Radius,
                BroadsideAngle = BroadsideAngle * side,
                Summary = $"Broadside {Mathf.Abs(BroadsideAngle):0.#} deg at {Radius:0.#}m"
            };
        }

        private void AddTrailPoint(Vector3 point)
        {
            if (Trail.Count > 0 && PlanarMath.GroundDistance(Trail[Trail.Count - 1], point) < 2f)
                return;

            Trail.Add(point);
            while (Trail.Count > MaxTrailPoints)
                Trail.RemoveAt(0);
        }
    }

    internal struct AiSimulationFrame
    {
        internal AiSimulationPreset Preset;
        internal Vector3 TargetPosition;
        internal Vector3 CraftPosition;
        internal Vector3 Heading;
        internal Vector3 DesiredTravel;
        internal Vector3 ToTarget;
        internal float Radius;
        internal float BroadsideAngle;
        internal string Summary;
    }

    internal struct AiSimulationGridProjection
    {
        internal Rect Rect;
        internal float VisibleRadius;
        internal float MetersPerPixel;

        internal static AiSimulationGridProjection For(Rect rect, AiSimulationState state)
        {
            float radius = Mathf.Max(120f, state.Radius * 1.45f);
            float shortestSide = Mathf.Max(1f, Mathf.Min(rect.width, rect.height));
            return new AiSimulationGridProjection
            {
                Rect = rect,
                VisibleRadius = radius,
                MetersPerPixel = radius * 2f / shortestSide
            };
        }

        internal Vector2 WorldToScreen(Vector3 relativeWorld)
        {
            float pixelsPerMeter = 1f / Mathf.Max(0.001f, MetersPerPixel);
            return new Vector2(
                Rect.center.x + relativeWorld.x * pixelsPerMeter,
                Rect.center.y - relativeWorld.z * pixelsPerMeter);
        }

        internal Vector2 DirectionToScreen(Vector3 direction, float pixels)
        {
            Vector3 flat = PlanarMath.Flatten(direction);
            if (flat.sqrMagnitude < 0.0001f)
                return Vector2.zero;

            return new Vector2(flat.x, -flat.z).normalized * pixels;
        }
    }
}
