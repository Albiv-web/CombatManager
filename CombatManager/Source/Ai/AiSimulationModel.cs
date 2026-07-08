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
        Broadside,
        NavalBroadside
    }

    internal enum AiSimulationSide
    {
        Both,
        Left,
        Right
    }

    internal enum AiTargetProfile
    {
        Static,
        SlowMover,
        Ship,
        FastMover,
        Plane
    }

    internal enum AiTargetPathMode
    {
        Straight,
        Orbit,
        SCurve
    }

    internal enum AiSimulationNavalState
    {
        Closing,
        BroadsideLeft,
        BroadsideRight
    }

    internal enum AiCraftMovementModel
    {
        ShipOrTank,
        HoverSixAxis,
        Airplane
    }

    internal sealed class AiSimulationState
    {
        private const int MaxTrailPoints = 220;
        private const float NavalPredictionFactor = 5f;

        internal AiSimulationState()
        {
            ApplyTargetProfile(AiTargetProfile.Ship);
            ResetScenario();
        }

        internal AiSimulationPreset Preset { get; set; } = AiSimulationPreset.Circle;
        internal AiSimulationSide Side { get; set; } = AiSimulationSide.Both;
        internal AiTargetProfile TargetProfile { get; private set; } = AiTargetProfile.Ship;
        internal AiTargetPathMode TargetPathMode { get; set; } = AiTargetPathMode.Orbit;
        internal AiSimulationNavalState NavalState { get; private set; } = AiSimulationNavalState.Closing;
        internal AiCraftMovementModel CraftMovementModel { get; set; } = AiCraftMovementModel.ShipOrTank;

        internal float Radius { get; set; } = 200f;
        internal float BroadsideOuterRadius { get; set; } = 300f;
        internal float CraftSpeed { get; set; } = 45f;
        internal float CraftAcceleration { get; set; } = 18f;
        internal float CraftTurnRate { get; set; } = 90f;
        internal float CraftCurrentSpeed { get; private set; }
        internal float PlaybackSpeed { get; set; } = 1f;
        internal float BroadsideAngle { get; set; } = 75f;
        internal float CircleMinApproachAngle { get; set; } = 45f;
        internal float TargetSpeed { get; set; }
        internal float TargetTurnRate { get; set; }
        internal float TargetAltitude { get; set; }
        internal float GridZoom { get; set; } = 1f;
        internal float OrbitAngleDegrees { get; private set; }
        internal float SimulationTime { get; private set; }

        internal Vector3 TargetPosition { get; private set; }
        internal Vector3 TargetHeading { get; private set; } = Vector3.forward;
        internal Vector3 TargetVelocity { get; private set; }
        internal Vector3 CraftPosition { get; private set; }
        internal Vector3 CraftHeading { get; private set; } = Vector3.back;
        internal Vector3 CraftVelocity { get; private set; }

        internal bool Playing { get; set; } = true;
        internal bool ShowInspector { get; set; } = true;
        internal bool ShowTrail { get; set; } = true;
        internal bool ShowDesiredTrail { get; set; } = true;
        internal bool ShowTargetPath { get; set; } = true;
        internal bool ShowLegend { get; set; } = true;
        internal bool ShowImportDetails { get; set; }
        internal string ImportStatus { get; set; } = "Standalone sandbox. Import is optional.";
        internal string ImportedBehaviour { get; set; }
        internal string ImportedManoeuvre { get; set; }
        internal string ImportedMainframe { get; set; }

        internal List<string> ImportedParameters { get; } = new List<string>();
        internal List<AiControlRequestSnapshot> ImportedRequests { get; } = new List<AiControlRequestSnapshot>();
        internal List<Vector3> Trail { get; } = new List<Vector3>();
        internal List<Vector3> DesiredTrail { get; } = new List<Vector3>();
        internal List<Vector3> TargetTrail { get; } = new List<Vector3>();

        internal void Reset()
        {
            ResetScenario();
        }

        internal void ResetScenario()
        {
            ResetTargetPath();
            ResetCraft();
            DesiredTrail.Clear();
            AddDesiredTrailPoint(BuildFrame().DesiredPoint);
        }

        internal void ResetTargetPath()
        {
            SimulationTime = 0f;
            TargetPosition = new Vector3(0f, TargetAltitude, 0f);
            TargetHeading = Vector3.forward;
            TargetVelocity = TargetHeading * TargetSpeed;
            TargetTrail.Clear();
            TargetTrail.Add(TargetPosition);
        }

        internal void ResetCraft()
        {
            CraftPosition = TargetPosition + Vector3.right * Mathf.Max(10f, Radius);
            CraftPosition = new Vector3(CraftPosition.x, 0f, CraftPosition.z);
            CraftHeading = InitialCraftHeading();
            CraftVelocity = Vector3.zero;
            CraftCurrentSpeed = 0f;
            NavalState = AiSimulationNavalState.Closing;
            UpdateOrbitAngle();
            Trail.Clear();
            Trail.Add(CraftPosition);
        }

        internal void Step(float deltaSeconds)
        {
            float delta = Mathf.Max(0f, deltaSeconds) * Mathf.Max(0f, PlaybackSpeed);
            if (delta <= 0f)
                return;

            AdvanceTarget(delta);
            AdvanceAiState(BuildTargetInfo());
            AiSimulationFrame frame = BuildFrame();
            AdvanceCraft(frame, delta);
            AiSimulationFrame updatedFrame = BuildFrame();

            AddTrailPoint(CraftPosition);
            AddTargetTrailPoint(TargetPosition);
            AddDesiredTrailPoint(updatedFrame.DesiredPoint);
            UpdateOrbitAngle();
        }

        internal void SetPreset(AiSimulationPreset preset)
        {
            if (Preset == preset)
                return;

            Preset = preset;
            ResetCraft();
            DesiredTrail.Clear();
            AddDesiredTrailPoint(BuildFrame().DesiredPoint);
        }

        internal void SetTargetProfile(AiTargetProfile profile)
        {
            if (TargetProfile == profile)
                return;

            ApplyTargetProfile(profile);
            ResetScenario();
        }

        internal string TargetProfileName() => TargetProfileName(TargetProfile);

        internal string CraftMovementModelName() => CraftMovementModelName(CraftMovementModel);

        internal void SetTargetAltitude(float altitude)
        {
            TargetAltitude = Mathf.Max(0f, altitude);
            TargetPosition = new Vector3(TargetPosition.x, TargetAltitude, TargetPosition.z);
        }

        internal AiSimulationFrame BuildFrame()
        {
            SyntheticTargetInfo target = BuildTargetInfo();
            switch (Preset)
            {
                case AiSimulationPreset.PointAt:
                    return BuildPointAtFrame(target);
                case AiSimulationPreset.Broadside:
                    return BuildBroadsideFrame(target);
                case AiSimulationPreset.NavalBroadside:
                    return BuildNavalBroadsideFrame(target);
                default:
                    return BuildCircleFrame(target);
            }
        }

        internal List<Vector3> BuildTargetFuturePath(int steps, float stepSeconds)
        {
            var path = new List<Vector3>();
            Vector3 position = TargetPosition;
            Vector3 heading = TargetHeading;
            float time = SimulationTime;
            float speed = Mathf.Max(0f, TargetSpeed);

            for (int i = 0; i < steps; i++)
            {
                float yaw = TargetYawDelta(stepSeconds, time);
                heading = PlanarMath.RotateYaw(heading, yaw);
                position += heading * speed * stepSeconds;
                position.y = TargetAltitude;
                time += stepSeconds;
                path.Add(position);
            }

            return path;
        }

        internal float OrbitDirection()
        {
            return Side == AiSimulationSide.Left ? 1f : -1f;
        }

        private void ApplyTargetProfile(AiTargetProfile profile)
        {
            TargetProfile = profile;
            switch (profile)
            {
                case AiTargetProfile.Static:
                    TargetSpeed = 0f;
                    TargetTurnRate = 0f;
                    TargetAltitude = 0f;
                    TargetPathMode = AiTargetPathMode.Straight;
                    break;
                case AiTargetProfile.SlowMover:
                    TargetSpeed = 8f;
                    TargetTurnRate = 2f;
                    TargetAltitude = 0f;
                    TargetPathMode = AiTargetPathMode.SCurve;
                    break;
                case AiTargetProfile.FastMover:
                    TargetSpeed = 45f;
                    TargetTurnRate = 8f;
                    TargetAltitude = 0f;
                    TargetPathMode = AiTargetPathMode.SCurve;
                    break;
                case AiTargetProfile.Plane:
                    TargetSpeed = 90f;
                    TargetTurnRate = 3f;
                    TargetAltitude = 300f;
                    TargetPathMode = AiTargetPathMode.Orbit;
                    break;
                case AiTargetProfile.Ship:
                default:
                    TargetSpeed = 18f;
                    TargetTurnRate = 1.2f;
                    TargetAltitude = 0f;
                    TargetPathMode = AiTargetPathMode.Orbit;
                    break;
            }
        }

        private static string TargetProfileName(AiTargetProfile profile)
        {
            switch (profile)
            {
                case AiTargetProfile.Static:
                    return "Static";
                case AiTargetProfile.SlowMover:
                    return "Slow mover";
                case AiTargetProfile.FastMover:
                    return "Fast mover";
                case AiTargetProfile.Plane:
                    return "Plane";
                default:
                    return "Ship";
            }
        }

        private static string CraftMovementModelName(AiCraftMovementModel model)
        {
            switch (model)
            {
                case AiCraftMovementModel.HoverSixAxis:
                    return "Hover / Six-axis";
                case AiCraftMovementModel.Airplane:
                    return "Airplane";
                default:
                    return "Ship / Tank";
            }
        }

        private Vector3 InitialCraftHeading()
        {
            if (Preset == AiSimulationPreset.Circle)
                return new Vector3(0f, 0f, 1f) * OrbitDirection();
            return PlanarMath.SafePlanarDirection(CraftPosition, TargetPosition, Vector3.back);
        }

        private void AdvanceTarget(float delta)
        {
            float yaw = TargetYawDelta(delta, SimulationTime);
            TargetHeading = PlanarMath.RotateYaw(TargetHeading, yaw);
            TargetVelocity = TargetHeading * Mathf.Max(0f, TargetSpeed);
            TargetPosition += TargetVelocity * delta;
            TargetPosition = new Vector3(TargetPosition.x, TargetAltitude, TargetPosition.z);
            SimulationTime += delta;
        }

        private float TargetYawDelta(float delta, float time)
        {
            switch (TargetPathMode)
            {
                case AiTargetPathMode.Orbit:
                    return TargetTurnRate * delta;
                case AiTargetPathMode.SCurve:
                    return Mathf.Sin(time * 0.45f) * TargetTurnRate * delta;
                default:
                    return 0f;
            }
        }

        private SyntheticTargetInfo BuildTargetInfo()
        {
            Vector3 direction = TargetPosition - CraftPosition;
            Vector3 flatDirection = PlanarMath.Flatten(direction);
            float range = direction.magnitude;
            float groundDistance = flatDirection.magnitude;
            Vector3 directionFlat = PlanarMath.SafePlanarDirection(CraftPosition, TargetPosition, CraftHeading);
            float azimuth = PlanarMath.SignedPlanarAngle(CraftHeading, directionFlat);
            return new SyntheticTargetInfo
            {
                Valid = true,
                Position = TargetPosition,
                Velocity = TargetVelocity,
                Direction = direction,
                DirectionFlat = directionFlat,
                Range = range,
                GroundDistance = groundDistance,
                Azimuth = azimuth
            };
        }

        private AiSimulationFrame BuildCircleFrame(SyntheticTargetInfo target)
        {
            float approachAngle = GetCircleApproachAngle(target);
            Vector3 desiredTravel = PlanarMath.RotateYaw(target.DirectionFlat, approachAngle);
            Vector3 desiredPoint = CraftPosition + desiredTravel * 1000f;
            return BuildCommonFrame(
                target,
                desiredPoint,
                desiredTravel,
                desiredTravel,
                "Circle",
                $"Circle {Radius:0.#}m vs {TargetProfileName()} {TargetSpeed:0.#}m/s",
                approximate: false,
                state: $"approach {approachAngle:0.#} deg");
        }

        private AiSimulationFrame BuildPointAtFrame(SyntheticTargetInfo target)
        {
            float distance = Mathf.Max(10f, Radius);
            Vector3 desiredPoint = TargetPosition - target.DirectionFlat * distance;
            desiredPoint.y = CraftPosition.y;
            return BuildCommonFrame(
                target,
                desiredPoint,
                target.DirectionFlat,
                Vector3.zero,
                "Point at",
                $"Point at target from {distance:0.#}m",
                approximate: false,
                state: "hold range");
        }

        private AiSimulationFrame BuildBroadsideFrame(SyntheticTargetInfo target)
        {
            float signedAngle = BroadsideAngle * BroadsideSideSign(target);
            Vector3 broadsideForward = PlanarMath.RotateYaw(target.DirectionFlat, -signedAngle);
            Vector3 point = CraftPosition + broadsideForward * 100f;
            Vector3 fromTarget = PlanarMath.Flatten(point - TargetPosition);
            if (fromTarget.sqrMagnitude < 0.0001f)
                fromTarget = -target.DirectionFlat;

            Vector3 desiredPoint = TargetPosition + fromTarget.normalized * Mathf.Max(10f, Radius);
            desiredPoint.y = CraftPosition.y;
            return BuildCommonFrame(
                target,
                desiredPoint,
                broadsideForward,
                Vector3.zero,
                "Broadside",
                $"Broadside {signedAngle:0.#} deg at {Radius:0.#}m",
                approximate: false,
                state: signedAngle >= 0f ? "left broadside" : "right broadside",
                broadsideAngle: signedAngle);
        }

        private AiSimulationFrame BuildNavalBroadsideFrame(SyntheticTargetInfo target)
        {
            float enterBelow = Mathf.Max(10f, Radius);
            if (NavalState == AiSimulationNavalState.Closing)
            {
                return BuildCommonFrame(
                    target,
                    TargetPosition,
                    target.DirectionFlat,
                    target.DirectionFlat,
                    "Broadside 2.0",
                    $"Closing until {enterBelow:0.#}m",
                    approximate: true,
                    state: "closing");
            }

            AiSimulationNavalState resolvedState = ResolveNavalSide(target);
            float desiredAngle = BroadsideAngle * (resolvedState == AiSimulationNavalState.BroadsideRight ? -1f : 1f);
            Vector3 fromTarget = -target.DirectionFlat;
            Vector3 desiredPoint = TargetPosition + PlanarMath.RotateYaw(fromTarget, desiredAngle) * enterBelow;
            desiredPoint.y = CraftPosition.y;
            Vector3 desiredFacing = PlanarMath.SafePlanarDirection(desiredPoint, TargetPosition, CraftHeading);
            return BuildCommonFrame(
                target,
                desiredPoint,
                desiredFacing,
                Vector3.zero,
                "Broadside 2.0",
                $"{NavalStateLabel(resolvedState)} at {desiredAngle:0.#} deg",
                approximate: true,
                state: NavalStateLabel(resolvedState),
                broadsideAngle: desiredAngle);
        }

        private AiSimulationFrame BuildCommonFrame(
            SyntheticTargetInfo target,
            Vector3 desiredPoint,
            Vector3 desiredFacing,
            Vector3 desiredTravel,
            string kind,
            string summary,
            bool approximate,
            string state,
            float broadsideAngle = 0f)
        {
            return new AiSimulationFrame
            {
                Preset = Preset,
                TargetPosition = TargetPosition,
                TargetVelocity = TargetVelocity,
                TargetHeading = TargetHeading,
                CraftPosition = CraftPosition,
                CraftVelocity = CraftVelocity,
                CraftHeading = CraftHeading,
                DesiredPoint = desiredPoint,
                DesiredFacing = PlanarMath.SafePlanarDirection(Vector3.zero, desiredFacing, CraftHeading),
                DesiredTravel = PlanarMath.Flatten(desiredTravel),
                ToTarget = target.DirectionFlat,
                Radius = Radius,
                BroadsideAngle = broadsideAngle,
                Range = target.Range,
                GroundRange = target.GroundDistance,
                Azimuth = target.Azimuth,
                Kind = kind,
                Summary = summary,
                AiState = state,
                TargetProfile = TargetProfileName(),
                TargetSpeed = TargetSpeed,
                CraftMovementModel = CraftMovementModelName(),
                Approximate = approximate,
                HasDesiredPoint = true,
                HasDesiredFacing = true
            };
        }

        private float GetCircleApproachAngle(SyntheticTargetInfo target)
        {
            float range = 90f - CircleMinApproachAngle;
            float rangeCorrection = range * Mathf.Min(Mathf.Max((Radius - target.GroundDistance) / 200f, -1f), 1f);
            float result = 90f + rangeCorrection;
            if (Side == AiSimulationSide.Right || (Side == AiSimulationSide.Both && target.Azimuth > 0f))
                result = -result;
            return result;
        }

        private float BroadsideSideSign(SyntheticTargetInfo target)
        {
            if (Side == AiSimulationSide.Left)
                return 1f;
            if (Side == AiSimulationSide.Right)
                return -1f;
            return target.Azimuth > 0f ? -1f : 1f;
        }

        private void AdvanceAiState(SyntheticTargetInfo target)
        {
            if (Preset != AiSimulationPreset.NavalBroadside)
                return;

            float enterBelow = Mathf.Max(10f, Radius);
            float leaveAbove = Mathf.Max(enterBelow + 20f, BroadsideOuterRadius);
            if (NavalState == AiSimulationNavalState.Closing && target.Range <= enterBelow)
                NavalState = ResolveNavalSide(target);
            else if (NavalState != AiSimulationNavalState.Closing && target.Range >= leaveAbove)
                NavalState = AiSimulationNavalState.Closing;
            else if (NavalState != AiSimulationNavalState.Closing)
                NavalState = ResolveNavalSide(target);
        }

        private AiSimulationNavalState ResolveNavalSide(SyntheticTargetInfo target)
        {
            if (Side == AiSimulationSide.Left)
                return AiSimulationNavalState.BroadsideLeft;
            if (Side == AiSimulationSide.Right)
                return AiSimulationNavalState.BroadsideRight;

            Vector3 predicted = target.Position + target.Velocity * NavalPredictionFactor;
            Vector3 vector = PlanarMath.SafePlanarDirection(CraftPosition, predicted, target.DirectionFlat);
            return PlanarMath.SignedPlanarAngle(vector, CraftHeading) < 0f
                ? AiSimulationNavalState.BroadsideRight
                : AiSimulationNavalState.BroadsideLeft;
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

        private void AdvanceCraft(AiSimulationFrame frame, float delta)
        {
            Vector3 toGoal = PlanarMath.Flatten(frame.DesiredPoint - CraftPosition);
            float distance = toGoal.magnitude;
            Vector3 desiredMoveDirection = distance > 0.1f ? toGoal.normalized : Vector3.zero;
            Vector3 desiredHeading = DesiredCraftHeading(frame, desiredMoveDirection);
            CraftHeading = RotateTowardsFlat(CraftHeading, desiredHeading, CraftTurnRate * delta);

            Vector3 moveDirection = CraftMoveDirection(desiredMoveDirection);
            float desiredSpeed = CraftSpeed * CraftThrottle(distance, desiredMoveDirection);
            CraftCurrentSpeed = Mathf.MoveTowards(CraftCurrentSpeed, desiredSpeed, CraftAcceleration * delta);

            float travel = CraftCurrentSpeed * delta;
            if (CraftMovementModel == AiCraftMovementModel.HoverSixAxis)
                travel = Mathf.Min(travel, distance);

            if (distance <= 1f)
            {
                CraftCurrentSpeed = 0f;
                CraftVelocity = Vector3.zero;
            }
            else
            {
                CraftPosition += moveDirection * travel;
                CraftVelocity = moveDirection * (travel / Mathf.Max(0.001f, delta));
            }

            CraftPosition = new Vector3(CraftPosition.x, 0f, CraftPosition.z);
        }

        private Vector3 DesiredCraftHeading(AiSimulationFrame frame, Vector3 desiredMoveDirection)
        {
            if (CraftMovementModel == AiCraftMovementModel.HoverSixAxis && frame.HasDesiredFacing)
                return frame.DesiredFacing;
            if (desiredMoveDirection.sqrMagnitude > 0.0001f)
                return desiredMoveDirection;
            return frame.HasDesiredFacing ? frame.DesiredFacing : CraftHeading;
        }

        private Vector3 CraftMoveDirection(Vector3 desiredMoveDirection)
        {
            if (CraftMovementModel == AiCraftMovementModel.HoverSixAxis)
                return desiredMoveDirection;
            return CraftHeading;
        }

        private float CraftThrottle(float distance, Vector3 desiredMoveDirection)
        {
            if (distance <= 1f || desiredMoveDirection.sqrMagnitude < 0.0001f)
                return 0f;

            float distanceScale = Mathf.Clamp01(distance / 120f);
            if (CraftMovementModel == AiCraftMovementModel.HoverSixAxis)
                return CraftSpeed * distanceScale;
            if (CraftMovementModel == AiCraftMovementModel.Airplane)
                return CraftSpeed * Mathf.Lerp(0.55f, 1f, distanceScale);

            float absAngle = Mathf.Abs(PlanarMath.SignedPlanarAngle(CraftHeading, desiredMoveDirection));
            float turnScale = absAngle <= 50f
                ? 1f
                : Mathf.Lerp(1f, 0.2f, Mathf.Clamp01((absAngle - 50f) / 85f));
            return CraftSpeed * distanceScale * turnScale;
        }

        private static Vector3 RotateTowardsFlat(Vector3 current, Vector3 desired, float maxDegrees)
        {
            current = PlanarMath.SafePlanarDirection(Vector3.zero, current, Vector3.forward);
            desired = PlanarMath.SafePlanarDirection(Vector3.zero, desired, current);
            float angle = PlanarMath.SignedPlanarAngle(current, desired);
            float step = Mathf.Clamp(angle, -Mathf.Abs(maxDegrees), Mathf.Abs(maxDegrees));
            return PlanarMath.RotateYaw(current, step);
        }

        private void UpdateOrbitAngle()
        {
            Vector3 relative = PlanarMath.Flatten(CraftPosition - TargetPosition);
            if (relative.sqrMagnitude < 0.001f)
                return;

            OrbitAngleDegrees = Mathf.Atan2(relative.z, relative.x) * Mathf.Rad2Deg;
        }

        private void AddTrailPoint(Vector3 point)
        {
            AddPoint(Trail, point, 2f);
        }

        private void AddTargetTrailPoint(Vector3 point)
        {
            AddPoint(TargetTrail, point, 2f);
        }

        private void AddDesiredTrailPoint(Vector3 point)
        {
            AddPoint(DesiredTrail, point, 3f);
        }

        private static void AddPoint(List<Vector3> points, Vector3 point, float minimumDistance)
        {
            if (points.Count > 0 && PlanarMath.GroundDistance(points[points.Count - 1], point) < minimumDistance)
                return;

            points.Add(point);
            while (points.Count > MaxTrailPoints)
                points.RemoveAt(0);
        }
    }

    internal struct AiSimulationFrame
    {
        internal AiSimulationPreset Preset;
        internal Vector3 TargetPosition;
        internal Vector3 TargetVelocity;
        internal Vector3 TargetHeading;
        internal Vector3 CraftPosition;
        internal Vector3 CraftVelocity;
        internal Vector3 CraftHeading;
        internal Vector3 DesiredPoint;
        internal Vector3 DesiredFacing;
        internal Vector3 DesiredTravel;
        internal Vector3 ToTarget;
        internal float Radius;
        internal float BroadsideAngle;
        internal float Range;
        internal float GroundRange;
        internal float Azimuth;
        internal string Kind;
        internal string Summary;
        internal string AiState;
        internal string TargetProfile;
        internal float TargetSpeed;
        internal string CraftMovementModel;
        internal bool Approximate;
        internal bool HasDesiredPoint;
        internal bool HasDesiredFacing;
    }

    internal struct AiSimulationGridProjection
    {
        internal Rect Rect;
        internal Vector3 OriginWorld;
        internal float VisibleRadius;
        internal float VisibleHalfWidth;
        internal float VisibleHalfHeight;
        internal float MetersPerPixel;

        internal static AiSimulationGridProjection For(Rect rect, AiSimulationState state)
        {
            float zoom = Mathf.Clamp(state.GridZoom, 0.5f, 3f);
            float radius = Mathf.Max(120f, state.Radius * 1.25f) / zoom;
            float shortestSide = Mathf.Max(1f, Mathf.Min(rect.width, rect.height));
            float metersPerPixel = radius * 2f / shortestSide;
            return new AiSimulationGridProjection
            {
                Rect = rect,
                OriginWorld = state.TargetPosition,
                VisibleRadius = radius,
                VisibleHalfWidth = rect.width * metersPerPixel * 0.5f,
                VisibleHalfHeight = rect.height * metersPerPixel * 0.5f,
                MetersPerPixel = metersPerPixel
            };
        }

        internal Vector2 WorldToScreen(Vector3 world)
        {
            return RelativeToScreen(world - OriginWorld);
        }

        internal Vector2 RelativeToScreen(Vector3 relativeWorld)
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

    internal struct SyntheticTargetInfo
    {
        internal bool Valid;
        internal Vector3 Position;
        internal Vector3 Velocity;
        internal Vector3 Direction;
        internal Vector3 DirectionFlat;
        internal float Range;
        internal float GroundDistance;
        internal float Azimuth;
    }
}
