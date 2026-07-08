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
        Hover,
        SixAxis,
        Airplane
    }

    internal enum AiCraftProfile
    {
        SurfaceShip,
        Hovercraft,
        SixAxisDrone,
        Airplane,
        FastAircraft
    }

    internal enum AiScenarioPreset
    {
        ShipDuel,
        BroadsideDuel,
        HoverDuel,
        PlaneIntercept
    }

    internal enum AiEntityRole
    {
        Blue,
        Red
    }

    internal sealed class AiSimEntity
    {
        private const int MaxTrailPoints = 240;

        internal AiSimEntity(AiEntityRole role, string name)
        {
            Role = role;
            Name = name;
            ApplyCraftProfile(AiCraftProfile.SurfaceShip);
        }

        internal AiEntityRole Role { get; }
        internal string Name { get; set; }
        internal AiSimulationPreset Preset { get; set; } = AiSimulationPreset.Circle;
        internal AiSimulationSide Side { get; set; } = AiSimulationSide.Both;
        internal AiSimulationNavalState NavalState { get; set; } = AiSimulationNavalState.Closing;
        internal AiCraftMovementModel CraftMovementModel { get; set; } = AiCraftMovementModel.ShipOrTank;
        internal AiCraftProfile CraftProfile { get; private set; } = AiCraftProfile.SurfaceShip;

        internal float Radius { get; set; } = 200f;
        internal float BroadsideOuterRadius { get; set; } = 300f;
        internal float BroadsideAngle { get; set; } = 75f;
        internal float CircleMinApproachAngle { get; set; } = 45f;
        internal float CraftSpeed { get; set; } = 45f;
        internal float CraftAcceleration { get; set; } = 18f;
        internal float CraftTurnRate { get; set; } = 90f;
        internal float CraftCurrentSpeed { get; set; }
        internal float Altitude { get; set; }

        internal float ShipTarryDistance { get; set; } = 50f;
        internal bool ShipReverseAllowed { get; set; } = true;
        internal float HoverYawLockDistance { get; set; } = 150f;
        internal float HoverMoveWithinAzimuth { get; set; } = 30f;
        internal float HoverStrafeAuthority { get; set; } = 0.75f;
        internal float SixAxisLookAheadDistance { get; set; } = 50f;
        internal float AirplaneMinimumSpeed { get; set; } = 22f;
        internal float AirplaneMinimumTurnRadius { get; set; } = 80f;
        internal float AirplaneIdleThrust { get; set; } = 100f;
        internal float AirplaneIdleDistance { get; set; } = 300f;
        internal float AirplaneBankingTurnAbove { get; set; } = 20f;
        internal float AirplaneBankingTurnRoll { get; set; } = 45f;
        internal float AirplanePitchForAltitude { get; set; } = 15f;

        internal Vector3 Position { get; set; }
        internal Vector3 Heading { get; set; } = Vector3.forward;
        internal Vector3 Velocity { get; set; }

        internal List<Vector3> Trail { get; } = new List<Vector3>();
        internal List<Vector3> IntentTrail { get; } = new List<Vector3>();

        internal void ApplyCraftProfile(AiCraftProfile profile)
        {
            CraftProfile = profile;
            switch (profile)
            {
                case AiCraftProfile.Hovercraft:
                    CraftMovementModel = AiCraftMovementModel.Hover;
                    CraftSpeed = 35f;
                    CraftAcceleration = 28f;
                    CraftTurnRate = 135f;
                    HoverYawLockDistance = 150f;
                    HoverMoveWithinAzimuth = 30f;
                    HoverStrafeAuthority = 0.3f;
                    Altitude = 40f;
                    break;
                case AiCraftProfile.SixAxisDrone:
                    CraftMovementModel = AiCraftMovementModel.SixAxis;
                    CraftSpeed = 38f;
                    CraftAcceleration = 32f;
                    CraftTurnRate = 160f;
                    SixAxisLookAheadDistance = 50f;
                    Altitude = 80f;
                    break;
                case AiCraftProfile.Airplane:
                    CraftMovementModel = AiCraftMovementModel.Airplane;
                    CraftSpeed = 95f;
                    CraftAcceleration = 24f;
                    CraftTurnRate = 95f;
                    AirplaneMinimumSpeed = 32f;
                    AirplaneMinimumTurnRadius = 120f;
                    AirplaneIdleThrust = 100f;
                    AirplaneIdleDistance = 300f;
                    AirplaneBankingTurnAbove = 20f;
                    AirplaneBankingTurnRoll = 45f;
                    AirplanePitchForAltitude = 15f;
                    Altitude = 300f;
                    break;
                case AiCraftProfile.FastAircraft:
                    CraftMovementModel = AiCraftMovementModel.Airplane;
                    CraftSpeed = 160f;
                    CraftAcceleration = 36f;
                    CraftTurnRate = 130f;
                    AirplaneMinimumSpeed = 70f;
                    AirplaneMinimumTurnRadius = 180f;
                    AirplaneIdleThrust = 100f;
                    AirplaneIdleDistance = 450f;
                    AirplaneBankingTurnAbove = 16f;
                    AirplaneBankingTurnRoll = 55f;
                    AirplanePitchForAltitude = 18f;
                    Altitude = 450f;
                    break;
                case AiCraftProfile.SurfaceShip:
                default:
                    CraftMovementModel = AiCraftMovementModel.ShipOrTank;
                    CraftSpeed = 45f;
                    CraftAcceleration = 18f;
                    CraftTurnRate = 90f;
                    ShipTarryDistance = 50f;
                    ShipReverseAllowed = true;
                    Altitude = 0f;
                    break;
            }
        }

        internal void ResetMotion(Vector3 position, Vector3 heading)
        {
            Position = new Vector3(position.x, Altitude, position.z);
            Heading = PlanarMath.SafePlanarDirection(Vector3.zero, heading, Vector3.forward);
            Velocity = Vector3.zero;
            CraftCurrentSpeed = 0f;
            NavalState = AiSimulationNavalState.Closing;
            Trail.Clear();
            IntentTrail.Clear();
            AddTrailPoint(Position);
        }

        internal void AddTrailPoint(Vector3 point)
        {
            AddPoint(Trail, point, 2f);
        }

        internal void AddIntentPoint(Vector3 point)
        {
            AddPoint(IntentTrail, point, 3f);
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

    internal sealed class AiSimulationState
    {
        internal AiSimulationState()
        {
            Blue = new AiSimEntity(AiEntityRole.Blue, "Blue");
            Red = new AiSimEntity(AiEntityRole.Red, "Red");
            BlueBlueprint = AiBlueprintPresetLibrary.Create(AiBlueprintPreset.CircleShip, AiEntityRole.Blue);
            RedBlueprint = AiBlueprintPresetLibrary.Create(AiBlueprintPreset.SlowShipBroadsider, AiEntityRole.Red);
            ApplyScenarioPreset(AiScenarioPreset.ShipDuel);
        }

        internal AiSimEntity Blue { get; }
        internal AiSimEntity Red { get; }
        internal AiMainframeBlueprint BlueBlueprint { get; private set; }
        internal AiMainframeBlueprint RedBlueprint { get; private set; }
        internal AiBlueprintExportPlan BlueExportPlan { get; set; }
        internal AiScenarioPreset ScenarioPreset { get; private set; } = AiScenarioPreset.ShipDuel;

        internal float PlaybackSpeed { get; set; } = 1f;
        internal float GridZoom { get; set; } = 1f;
        internal float SimulationTime { get; private set; }

        internal bool Playing { get; set; } = true;
        internal bool ShowTrail { get; set; } = true;
        internal bool ShowDesiredTrail { get; set; } = true;
        internal bool ShowRawSteer { get; set; } = true;
        internal bool ShowMotionPoint { get; set; } = true;
        internal bool ShowLegend { get; set; } = true;
        internal bool ShowImportDetails { get; set; }
        internal bool LiveParityEnabled { get; set; }

        internal string ImportStatus { get; set; } = "Standalone duel sandbox. Blue import is optional.";
        internal string ImportedBehaviour { get; set; }
        internal string ImportedManoeuvre { get; set; }
        internal string ImportedMainframe { get; set; }
        internal string LiveParityStatus { get; set; } = "Live Parity is off.";
        internal AiLiveParitySnapshot LiveParity { get; set; }
        internal int SelectedImportIndex { get; set; } = -1;

        internal List<AiImportCandidate> ImportCandidates { get; } = new List<AiImportCandidate>();
        internal List<string> ImportedParameters { get; } = new List<string>();
        internal List<AiControlRequestSnapshot> ImportedRequests { get; } = new List<AiControlRequestSnapshot>();

        internal AiSimulationPreset Preset { get => Blue.Preset; set => Blue.Preset = value; }
        internal AiSimulationSide Side { get => Blue.Side; set => Blue.Side = value; }
        internal AiCraftProfile CraftProfile => Blue.CraftProfile;
        internal AiCraftMovementModel CraftMovementModel { get => Blue.CraftMovementModel; set => Blue.CraftMovementModel = value; }
        internal float Radius { get => Blue.Radius; set => Blue.Radius = value; }
        internal float BroadsideOuterRadius { get => Blue.BroadsideOuterRadius; set => Blue.BroadsideOuterRadius = value; }
        internal float BroadsideAngle { get => Blue.BroadsideAngle; set => Blue.BroadsideAngle = value; }
        internal float CircleMinApproachAngle { get => Blue.CircleMinApproachAngle; set => Blue.CircleMinApproachAngle = value; }
        internal float CraftSpeed { get => Blue.CraftSpeed; set => Blue.CraftSpeed = value; }
        internal float CraftAcceleration { get => Blue.CraftAcceleration; set => Blue.CraftAcceleration = value; }
        internal float CraftTurnRate { get => Blue.CraftTurnRate; set => Blue.CraftTurnRate = value; }
        internal float CraftCurrentSpeed => Blue.CraftCurrentSpeed;
        internal float ShipTarryDistance { get => Blue.ShipTarryDistance; set => Blue.ShipTarryDistance = value; }
        internal bool ShipReverseAllowed { get => Blue.ShipReverseAllowed; set => Blue.ShipReverseAllowed = value; }
        internal float HoverStrafeAuthority { get => Blue.HoverStrafeAuthority; set => Blue.HoverStrafeAuthority = value; }
        internal float HoverMoveWithinAzimuth { get => Blue.HoverMoveWithinAzimuth; set => Blue.HoverMoveWithinAzimuth = value; }
        internal float AirplaneMinimumSpeed { get => Blue.AirplaneMinimumSpeed; set => Blue.AirplaneMinimumSpeed = value; }
        internal Vector3 TargetPosition => Red.Position;
        internal Vector3 TargetVelocity => Red.Velocity;
        internal Vector3 TargetHeading => Red.Heading;
        internal Vector3 CraftPosition => Blue.Position;
        internal Vector3 CraftHeading => Blue.Heading;
        internal Vector3 CraftVelocity => Blue.Velocity;
        internal List<Vector3> Trail => Blue.Trail;
        internal List<Vector3> DesiredTrail => Blue.IntentTrail;
        internal List<Vector3> TargetTrail => Red.Trail;
        internal AiSimulationNavalState NavalState => Blue.NavalState;
        internal AiTargetProfile TargetProfile => AiTargetProfile.Ship;

        internal void Reset()
        {
            ResetScenario();
        }

        internal void ResetScenario()
        {
            SimulationTime = 0f;
            float spacing = Mathf.Max(Blue.Radius, Red.Radius) + 80f;
            Vector3 bluePosition = new Vector3(spacing, Blue.Altitude, 0f);
            Vector3 redPosition = new Vector3(0f, Red.Altitude, 0f);
            Blue.ResetMotion(bluePosition, InitialHeading(Blue, bluePosition, redPosition));
            Red.ResetMotion(redPosition, InitialHeading(Red, redPosition, bluePosition));

            AiDuelFrame frame = BuildDuelFrame();
            Blue.AddIntentPoint(FrameMotionPoint(frame.Blue));
            Red.AddIntentPoint(FrameMotionPoint(frame.Red));
        }

        internal void ResetCraft()
        {
            ResetScenario();
        }

        internal void ResetTargetPath()
        {
            ResetScenario();
        }

        internal void Step(float deltaSeconds)
        {
            float delta = Mathf.Max(0f, deltaSeconds) * Mathf.Max(0f, PlaybackSpeed);
            if (delta <= 0f)
                return;

            AiPlanInput blueInput = BuildPlannerInput(Blue, Red);
            AiPlanInput redInput = BuildPlannerInput(Red, Blue);
            Blue.NavalState = AiBehaviourPlanner.AdvanceNavalState(blueInput);
            Red.NavalState = AiBehaviourPlanner.AdvanceNavalState(redInput);

            blueInput.NavalState = Blue.NavalState;
            redInput.NavalState = Red.NavalState;
            AiSimulationFrame blueFrame = BuildEntityFrame(Blue, Red, blueInput);
            AiSimulationFrame redFrame = BuildEntityFrame(Red, Blue, redInput);

            AiManoeuvreSimulator.Advance(Blue, blueFrame, delta);
            AiManoeuvreSimulator.Advance(Red, redFrame, delta);

            SimulationTime += delta;
            Blue.AddTrailPoint(Blue.Position);
            Red.AddTrailPoint(Red.Position);

            AiDuelFrame updated = BuildDuelFrame();
            Blue.AddIntentPoint(FrameMotionPoint(updated.Blue));
            Red.AddIntentPoint(FrameMotionPoint(updated.Red));
        }

        internal void SetPreset(AiSimulationPreset preset)
        {
            if (Blue.Preset == preset)
                return;

            Blue.Preset = preset;
            ResetScenario();
        }

        internal void SetCraftProfile(AiCraftProfile profile)
        {
            Blue.ApplyCraftProfile(profile);
            ResetScenario();
        }

        internal void SetTargetProfile(AiTargetProfile profile)
        {
            switch (profile)
            {
                case AiTargetProfile.FastMover:
                    Red.ApplyCraftProfile(AiCraftProfile.FastAircraft);
                    break;
                case AiTargetProfile.Plane:
                    Red.ApplyCraftProfile(AiCraftProfile.Airplane);
                    break;
                case AiTargetProfile.SlowMover:
                    Red.ApplyCraftProfile(AiCraftProfile.SurfaceShip);
                    Red.CraftSpeed = 18f;
                    break;
                case AiTargetProfile.Static:
                    Red.ApplyCraftProfile(AiCraftProfile.SurfaceShip);
                    Red.CraftSpeed = 0f;
                    break;
                case AiTargetProfile.Ship:
                default:
                    Red.ApplyCraftProfile(AiCraftProfile.SurfaceShip);
                    break;
            }

            ResetScenario();
        }

        internal void SetTargetAltitude(float altitude)
        {
            Red.Altitude = Mathf.Max(0f, altitude);
            Red.Position = new Vector3(Red.Position.x, Red.Altitude, Red.Position.z);
        }

        internal void ApplyScenarioPreset(AiScenarioPreset preset)
        {
            ScenarioPreset = preset;
            switch (preset)
            {
                case AiScenarioPreset.BroadsideDuel:
                    ConfigureShip(Blue, AiSimulationPreset.NavalBroadside, 350f, 520f, AiSimulationSide.Both);
                    ConfigureShip(Red, AiSimulationPreset.NavalBroadside, 350f, 520f, AiSimulationSide.Both);
                    break;
                case AiScenarioPreset.HoverDuel:
                    ConfigureHover(Blue, AiSimulationPreset.PointAt, 260f);
                    ConfigureSixAxis(Red, AiSimulationPreset.PointAt, 240f);
                    break;
                case AiScenarioPreset.PlaneIntercept:
                    ConfigureAircraft(Blue, AiCraftProfile.Airplane, AiSimulationPreset.PointAt, 450f);
                    ConfigureAircraft(Red, AiCraftProfile.FastAircraft, AiSimulationPreset.Circle, 550f);
                    Red.Side = AiSimulationSide.Left;
                    break;
                case AiScenarioPreset.ShipDuel:
                default:
                    ConfigureShip(Blue, AiSimulationPreset.Circle, 220f, 320f, AiSimulationSide.Both);
                    ConfigureShip(Red, AiSimulationPreset.Broadside, 260f, 380f, AiSimulationSide.Left);
                    break;
            }

            BlueBlueprint = new AiMainframeBlueprint { MainframeName = $"Blue {ScenarioPresetName(preset)}" };
            RedBlueprint = new AiMainframeBlueprint { MainframeName = $"Red {ScenarioPresetName(preset)}" };
            CaptureBlueprintFromEntity(Blue);
            CaptureBlueprintFromEntity(Red);
            ResetScenario();
        }

        internal void ApplyBlueprintPreset(AiEntityRole role, AiBlueprintPreset preset)
        {
            ApplyBlueprint(role, AiBlueprintPresetLibrary.Create(preset, role), reset: true);
        }

        internal void ApplyBlueprint(AiEntityRole role, AiMainframeBlueprint blueprint, bool reset)
        {
            if (role == AiEntityRole.Blue)
            {
                BlueBlueprint = blueprint.Clone();
                BlueBlueprint.ApplyToEntity(Blue);
            }
            else
            {
                RedBlueprint = blueprint.Clone();
                RedBlueprint.ApplyToEntity(Red);
            }

            if (reset)
                ResetScenario();
        }

        internal AiMainframeBlueprint BlueprintFor(AiEntityRole role)
        {
            return role == AiEntityRole.Blue ? BlueBlueprint : RedBlueprint;
        }

        internal void CaptureBlueprintFromEntity(AiSimEntity entity)
        {
            BlueprintFor(entity.Role).CaptureEntityFields(entity);
        }

        internal AiDuelFrame BuildDuelFrame()
        {
            return new AiDuelFrame
            {
                Blue = BuildEntityFrame(Blue, Red, BuildPlannerInput(Blue, Red)),
                Red = BuildEntityFrame(Red, Blue, BuildPlannerInput(Red, Blue))
            };
        }

        internal AiSimulationFrame BuildFrame()
        {
            return BuildDuelFrame().Blue;
        }

        internal List<Vector3> BuildTargetFuturePath(int steps, float stepSeconds)
        {
            var path = new List<Vector3>();
            Vector3 position = Red.Position;
            Vector3 heading = Red.Heading;
            float speed = Mathf.Max(0f, Red.CraftSpeed);
            for (int i = 0; i < steps; i++)
            {
                position += heading * speed * stepSeconds;
                path.Add(position);
            }

            return path;
        }

        internal float OrbitDirection()
        {
            return Blue.Side == AiSimulationSide.Left ? 1f : -1f;
        }

        internal string TargetProfileName() => Red.Name;

        internal string CraftMovementModelName() => CraftMovementModelName(Blue.CraftMovementModel);

        internal string CraftProfileName() => CraftProfileName(Blue.CraftProfile);

        internal string ScenarioPresetName() => ScenarioPresetName(ScenarioPreset);

        internal static string PresetName(AiSimulationPreset preset)
        {
            switch (preset)
            {
                case AiSimulationPreset.PointAt:
                    return "Point At";
                case AiSimulationPreset.Broadside:
                    return "Broadside";
                case AiSimulationPreset.NavalBroadside:
                    return "Naval 2.0";
                default:
                    return "Circle";
            }
        }

        internal static string CraftMovementModelName(AiCraftMovementModel model)
        {
            switch (model)
            {
                case AiCraftMovementModel.Hover:
                    return "Hover";
                case AiCraftMovementModel.SixAxis:
                    return "Six-axis";
                case AiCraftMovementModel.Airplane:
                    return "Airplane";
                default:
                    return "Ship / Tank";
            }
        }

        internal static string CraftProfileName(AiCraftProfile profile)
        {
            switch (profile)
            {
                case AiCraftProfile.Hovercraft:
                    return "Hovercraft";
                case AiCraftProfile.SixAxisDrone:
                    return "Six-axis";
                case AiCraftProfile.Airplane:
                    return "Airplane";
                case AiCraftProfile.FastAircraft:
                    return "Fast aircraft";
                default:
                    return "Surface ship";
            }
        }

        internal static string ScenarioPresetName(AiScenarioPreset preset)
        {
            switch (preset)
            {
                case AiScenarioPreset.BroadsideDuel:
                    return "Broadside duel";
                case AiScenarioPreset.HoverDuel:
                    return "Hover duel";
                case AiScenarioPreset.PlaneIntercept:
                    return "Plane intercept";
                default:
                    return "Ship duel";
            }
        }

        internal static Vector3 FrameMotionPoint(AiSimulationFrame frame)
        {
            return frame.HasMotionPoint ? frame.MotionPoint : frame.DesiredPoint;
        }

        private static void ConfigureShip(
            AiSimEntity entity,
            AiSimulationPreset preset,
            float radius,
            float outerRadius,
            AiSimulationSide side)
        {
            entity.ApplyCraftProfile(AiCraftProfile.SurfaceShip);
            entity.Preset = preset;
            entity.Side = side;
            entity.Radius = radius;
            entity.BroadsideOuterRadius = outerRadius;
            entity.BroadsideAngle = 75f;
            entity.CircleMinApproachAngle = 45f;
        }

        private static void ConfigureHover(AiSimEntity entity, AiSimulationPreset preset, float radius)
        {
            entity.ApplyCraftProfile(AiCraftProfile.Hovercraft);
            entity.Preset = preset;
            entity.Side = AiSimulationSide.Both;
            entity.Radius = radius;
            entity.BroadsideOuterRadius = radius + 120f;
        }

        private static void ConfigureSixAxis(AiSimEntity entity, AiSimulationPreset preset, float radius)
        {
            entity.ApplyCraftProfile(AiCraftProfile.SixAxisDrone);
            entity.Preset = preset;
            entity.Side = AiSimulationSide.Both;
            entity.Radius = radius;
            entity.BroadsideOuterRadius = radius + 120f;
        }

        private static void ConfigureAircraft(
            AiSimEntity entity,
            AiCraftProfile profile,
            AiSimulationPreset preset,
            float radius)
        {
            entity.ApplyCraftProfile(profile);
            entity.Preset = preset;
            entity.Side = AiSimulationSide.Both;
            entity.Radius = radius;
            entity.BroadsideOuterRadius = radius + 180f;
        }

        private static Vector3 InitialHeading(AiSimEntity entity, Vector3 selfPosition, Vector3 targetPosition)
        {
            Vector3 toTarget = PlanarMath.SafePlanarDirection(selfPosition, targetPosition, entity.Role == AiEntityRole.Blue ? Vector3.left : Vector3.right);
            if (entity.Preset == AiSimulationPreset.Circle)
                return PlanarMath.RotateYaw(toTarget, entity.Side == AiSimulationSide.Left ? 90f : -90f);
            return toTarget;
        }

        private AiSimulationFrame BuildEntityFrame(AiSimEntity entity, AiSimEntity target, AiPlanInput input)
        {
            AiBehaviourPlan plan = AiBehaviourPlanner.Plan(input);
            AiSimulationFrame frame = new AiSimulationFrame
            {
                Role = entity.Role,
                EntityName = entity.Name,
                TargetEntityName = target.Name,
                Preset = entity.Preset,
                TargetPosition = target.Position,
                TargetVelocity = target.Velocity,
                TargetHeading = target.Heading,
                CraftPosition = entity.Position,
                CraftVelocity = entity.Velocity,
                CraftHeading = entity.Heading,
                DesiredPoint = plan.RawSteerPoint,
                RawSteerPoint = plan.RawSteerPoint,
                MotionPoint = plan.MotionPoint,
                DesiredFacing = plan.DesiredFacing,
                DesiredTravel = plan.DesiredTravel,
                ToTarget = plan.ToTarget,
                Radius = entity.Radius,
                BroadsideAngle = plan.BroadsideAngle,
                Range = plan.Range,
                GroundRange = plan.GroundRange,
                Azimuth = plan.Azimuth,
                Kind = plan.Kind,
                Summary = $"{entity.Name}: {plan.Summary}",
                AiState = plan.AiState,
                ApproximationNote = MovementApproximationNote(entity, plan),
                TargetProfile = target.Name,
                TargetSpeed = target.Velocity.magnitude,
                CraftMovementModel = CraftMovementModelName(entity.CraftMovementModel),
                CraftProfile = CraftProfileName(entity.CraftProfile),
                Approximate = true,
                HasDesiredPoint = plan.HasRawSteerPoint,
                HasRawSteerPoint = plan.HasRawSteerPoint,
                HasMotionPoint = plan.HasMotionPoint,
                HasDesiredFacing = plan.HasDesiredFacing,
                ReversePreferred = plan.ReversePreferred,
                PredictedRequests = new List<AiControlRequestPrediction>()
            };
            frame.PredictedRequests.AddRange(AiVanillaPredictor.PredictRequests(
                AiMovementRequestContext.FromEntity(entity),
                AiVanillaPredictor.FromSimulationFrame(frame)));
            return frame;
        }

        private static string MovementApproximationNote(AiSimEntity entity, AiBehaviourPlan plan)
        {
            string movement;
            switch (entity.CraftMovementModel)
            {
                case AiCraftMovementModel.Hover:
                    movement = "hover manoeuvre approximates yaw-lock/strafe PID";
                    break;
                case AiCraftMovementModel.SixAxis:
                    movement = "six-axis manoeuvre approximates independent PID axes";
                    break;
                case AiCraftMovementModel.Airplane:
                    movement = "airplane manoeuvre approximates thrust/banking/altitude PID";
                    break;
                default:
                    movement = "ship/tank manoeuvre approximates tarry/reverse/yaw PID";
                    break;
            }

            return string.IsNullOrWhiteSpace(plan.ApproximationNote)
                ? movement
                : $"{plan.ApproximationNote}; {movement}";
        }

        private static AiPlanInput BuildPlannerInput(AiSimEntity entity, AiSimEntity target)
        {
            return new AiPlanInput
            {
                Preset = entity.Preset,
                Side = entity.Side,
                NavalState = entity.NavalState,
                CraftPosition = entity.Position,
                CraftHeading = entity.Heading,
                CraftVelocity = entity.Velocity,
                TargetPosition = target.Position,
                TargetVelocity = target.Velocity,
                TargetProfileName = target.Name,
                TargetSpeed = target.Velocity.magnitude,
                Radius = entity.Radius,
                BroadsideOuterRadius = entity.BroadsideOuterRadius,
                BroadsideAngle = entity.BroadsideAngle,
                CircleMinApproachAngle = entity.CircleMinApproachAngle,
                CraftSpeed = entity.CraftSpeed
            };
        }
    }

    internal struct AiDuelFrame
    {
        internal AiSimulationFrame Blue;
        internal AiSimulationFrame Red;
    }

    internal struct AiSimulationFrame
    {
        internal AiEntityRole Role;
        internal string EntityName;
        internal string TargetEntityName;
        internal AiSimulationPreset Preset;
        internal Vector3 TargetPosition;
        internal Vector3 TargetVelocity;
        internal Vector3 TargetHeading;
        internal Vector3 CraftPosition;
        internal Vector3 CraftVelocity;
        internal Vector3 CraftHeading;
        internal Vector3 DesiredPoint;
        internal Vector3 RawSteerPoint;
        internal Vector3 MotionPoint;
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
        internal string ApproximationNote;
        internal string TargetProfile;
        internal float TargetSpeed;
        internal string CraftMovementModel;
        internal string CraftProfile;
        internal bool Approximate;
        internal bool HasDesiredPoint;
        internal bool HasRawSteerPoint;
        internal bool HasMotionPoint;
        internal bool HasDesiredFacing;
        internal bool ReversePreferred;
        internal List<AiControlRequestPrediction> PredictedRequests;
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
            float duelRange = PlanarMath.GroundDistance(state.Blue.Position, state.Red.Position);
            float desiredSpan = Mathf.Max(
                Mathf.Max(state.Blue.Radius, state.Red.Radius) * 1.45f,
                duelRange * 1.15f);
            float radius = Mathf.Max(160f, desiredSpan) / zoom;
            float shortestSide = Mathf.Max(1f, Mathf.Min(rect.width, rect.height));
            float metersPerPixel = radius * 2f / shortestSide;
            return new AiSimulationGridProjection
            {
                Rect = rect,
                OriginWorld = state.Red.Position,
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
}
