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
        NavalBroadside,
        AttackRun1,
        AttackRun2,
        AttackRun3
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
        PlaneIntercept,
        AerialAttackRun
    }

    internal enum AiEntityRole
    {
        Blue,
        Red
    }

    internal enum AiGraphViewMode
    {
        RedCentered,
        BlueCentered,
        Freecam
    }

    internal enum AiGraphDetailMode
    {
        Clean,
        Tactical,
        Debug
    }

    internal enum AiGraphDimensionMode
    {
        Flat2D,
        Scene3D
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
        internal float VerticalSpeed { get; set; }
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

        internal bool AttackRunActive { get; set; } = true;
        internal float AttackRunLastFinishedAt { get; set; }
        internal float AttackRunAbortTimerStart { get; set; } = float.PositiveInfinity;
        internal float AttackRunFlyAwayYaw { get; set; }
        internal float AttackRunBeginDistance { get; set; } = 250f;
        internal float AttackRunAbortDistance { get; set; } = 50f;
        internal float AttackRunWaitTime { get; set; } = 15f;
        internal float AttackRunAttackAltitude { get; set; }
        internal float AttackRunDisengageAltitude { get; set; } = 75f;
        internal float AttackRunBreakoffDistance { get; set; } = 400f;
        internal float AttackRunReengageDistance { get; set; } = 1000f;
        internal float AttackRunReengageTime { get; set; } = 15f;
        internal float AttackRunPitchDistance { get; set; } = 800f;
        internal float AttackRunBreakoffAltitude { get; set; } = -1000f;
        internal float AttackRunAbortTime { get; set; } = 20f;
        internal float AttackRunAbortTimerStartDistance { get; set; }
        internal float AttackRunCombatAltitude { get; set; } = 200f;
        internal float AttackRunEngagementAltitude { get; set; } = 100f;
        internal float AttackRunPredictionPoint { get; set; } = 400f;
        internal bool AttackRunUsePrediction { get; set; }
        internal bool AttackRunFlyover { get; set; }
        internal bool AttackRunIgnoreAltitude { get; set; } = true;

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
            VerticalSpeed = 0f;
            NavalState = AiSimulationNavalState.Closing;
            AttackRunActive = true;
            AttackRunLastFinishedAt = 0f;
            AttackRunAbortTimerStart = float.PositiveInfinity;
            AttackRunFlyAwayYaw = PlanarMath.HeadingYaw(Heading);
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
        internal AiGraphViewMode GraphViewMode { get; private set; } = AiGraphViewMode.RedCentered;
        internal AiGraphDetailMode GraphDetailMode { get; private set; } = AiGraphDetailMode.Tactical;
        internal AiGraphDimensionMode GraphDimensionMode { get; private set; } = AiGraphDimensionMode.Flat2D;
        internal float Graph3DYaw { get; private set; } = 35f;
        internal float Graph3DPitch { get; private set; } = 56f;
        internal float GraphVerticalScale { get; private set; } = 0.35f;
        internal Vector3 FreecamOrigin { get; private set; }

        internal bool Playing { get; set; } = true;
        internal bool ShowTrail { get; set; } = true;
        internal bool ShowDesiredTrail { get; set; } = true;
        internal bool ShowRawSteer { get; set; }
        internal bool ShowMotionPoint { get; set; } = true;
        internal bool ShowLegend { get; set; }
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
            if (FreecamOrigin == Vector3.zero)
                FreecamOrigin = DuelMidpoint();

            AiDuelFrame frame = BuildDuelFrame();
            Blue.AddIntentPoint(FrameMotionPoint(frame.Blue));
            Red.AddIntentPoint(FrameMotionPoint(frame.Red));
        }

        internal void SetGraphViewMode(AiGraphViewMode mode)
        {
            if (GraphViewMode != AiGraphViewMode.Freecam && mode == AiGraphViewMode.Freecam)
                FreecamOrigin = GraphOriginWorld();
            GraphViewMode = mode;
        }

        internal void SetGraphDetailMode(AiGraphDetailMode mode)
        {
            GraphDetailMode = mode;
            switch (mode)
            {
                case AiGraphDetailMode.Clean:
                    ShowTrail = false;
                    ShowDesiredTrail = false;
                    ShowRawSteer = false;
                    ShowMotionPoint = false;
                    ShowLegend = false;
                    break;
                case AiGraphDetailMode.Debug:
                    ShowTrail = true;
                    ShowDesiredTrail = true;
                    ShowRawSteer = true;
                    ShowMotionPoint = true;
                    ShowLegend = true;
                    break;
                default:
                    ShowTrail = true;
                    ShowDesiredTrail = true;
                    ShowRawSteer = false;
                    ShowMotionPoint = true;
                    ShowLegend = false;
                    break;
            }
        }

        internal void SetGridZoom(float zoom)
        {
            GridZoom = Mathf.Clamp(zoom, 0.25f, 8f);
        }

        internal void AdjustGridZoom(float factor)
        {
            SetGridZoom(GridZoom * Mathf.Max(0.001f, factor));
        }

        internal void FitDuel()
        {
            SetGridZoom(1f);
            if (GraphViewMode == AiGraphViewMode.Freecam)
                FreecamOrigin = DuelMidpoint();
        }

        internal void SetGraphDimensionMode(AiGraphDimensionMode mode)
        {
            GraphDimensionMode = mode;
        }

        internal void ResetGraphView()
        {
            GraphDimensionMode = AiGraphDimensionMode.Flat2D;
            GraphViewMode = AiGraphViewMode.RedCentered;
            Graph3DYaw = 35f;
            Graph3DPitch = 56f;
            GraphVerticalScale = 0.35f;
            SetGridZoom(1f);
            FreecamOrigin = DuelMidpoint();
        }

        internal void Begin3DPan()
        {
            if (GraphDimensionMode != AiGraphDimensionMode.Scene3D)
                GraphDimensionMode = AiGraphDimensionMode.Scene3D;
            if (GraphViewMode != AiGraphViewMode.Freecam)
            {
                FreecamOrigin = GraphOriginWorld();
                GraphViewMode = AiGraphViewMode.Freecam;
            }
        }

        internal void RotateGraph3D(Vector2 screenDelta)
        {
            GraphDimensionMode = AiGraphDimensionMode.Scene3D;
            Graph3DYaw = PlanarMath.Fix180(Graph3DYaw + screenDelta.x * 0.25f);
            Graph3DPitch = Mathf.Clamp(Graph3DPitch - screenDelta.y * 0.18f, 18f, 82f);
        }

        internal void SetGraphVerticalScale(float scale)
        {
            GraphVerticalScale = Mathf.Clamp(scale, 0.05f, 2f);
        }

        internal void SetFreecamOrigin(Vector3 origin)
        {
            FreecamOrigin = new Vector3(origin.x, 0f, origin.z);
        }

        internal void PanFreecam(Vector2 screenDelta, float metersPerPixel)
        {
            if (GraphViewMode != AiGraphViewMode.Freecam)
                return;

            FreecamOrigin += new Vector3(
                -screenDelta.x * metersPerPixel,
                0f,
                screenDelta.y * metersPerPixel);
        }

        internal void PanFreecam3D(Vector2 screenDelta, float metersPerPixel)
        {
            if (GraphViewMode != AiGraphViewMode.Freecam)
                return;

            float yaw = Graph3DYaw * Mathf.Deg2Rad;
            Vector3 right = new Vector3(Mathf.Cos(yaw), 0f, -Mathf.Sin(yaw));
            Vector3 forward = new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw));
            float pitchGroundScale = Mathf.Max(0.2f, Mathf.Sin(Graph3DPitch * Mathf.Deg2Rad));
            FreecamOrigin +=
                right * (-screenDelta.x * metersPerPixel) +
                forward * (screenDelta.y * metersPerPixel / pitchGroundScale);
        }

        internal Vector3 GraphOriginWorld()
        {
            switch (GraphViewMode)
            {
                case AiGraphViewMode.BlueCentered:
                    return Blue.Position;
                case AiGraphViewMode.Freecam:
                    return FreecamOrigin;
                default:
                    return Red.Position;
            }
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
            AiBehaviourPlanner.AdvanceAerialState(Blue, blueInput);
            AiBehaviourPlanner.AdvanceAerialState(Red, redInput);

            blueInput.NavalState = Blue.NavalState;
            redInput.NavalState = Red.NavalState;
            blueInput.AttackRunActive = Blue.AttackRunActive;
            redInput.AttackRunActive = Red.AttackRunActive;
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
                case AiScenarioPreset.AerialAttackRun:
                    ConfigureAircraft(Blue, AiCraftProfile.FastAircraft, AiSimulationPreset.AttackRun3, 800f);
                    ConfigureAircraft(Red, AiCraftProfile.Airplane, AiSimulationPreset.AttackRun2, 700f);
                    Blue.AttackRunUsePrediction = true;
                    Red.AttackRunCombatAltitude = 220f;
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
                case AiSimulationPreset.AttackRun1:
                    return "Attack 1.0";
                case AiSimulationPreset.AttackRun2:
                    return "Attack 2.0";
                case AiSimulationPreset.AttackRun3:
                    return "Attack 3.0";
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
                case AiScenarioPreset.AerialAttackRun:
                    return "Aerial attack";
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

        private Vector3 DuelMidpoint()
        {
            Vector3 midpoint = (Blue.Position + Red.Position) * 0.5f;
            return new Vector3(midpoint.x, 0f, midpoint.z);
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

        private AiPlanInput BuildPlannerInput(AiSimEntity entity, AiSimEntity target)
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
                CraftSpeed = entity.CraftSpeed,
                PreferredAltitude = entity.Altitude,
                SimulationTime = SimulationTime,
                AttackRunActive = entity.AttackRunActive,
                AttackRunBeginDistance = entity.AttackRunBeginDistance,
                AttackRunAbortDistance = entity.AttackRunAbortDistance,
                AttackRunWaitTime = entity.AttackRunWaitTime,
                AttackRunAttackAltitude = entity.AttackRunAttackAltitude,
                AttackRunDisengageAltitude = entity.AttackRunDisengageAltitude,
                AttackRunBreakoffDistance = entity.AttackRunBreakoffDistance,
                AttackRunReengageDistance = entity.AttackRunReengageDistance,
                AttackRunReengageTime = entity.AttackRunReengageTime,
                AttackRunPitchDistance = entity.AttackRunPitchDistance,
                AttackRunBreakoffAltitude = entity.AttackRunBreakoffAltitude,
                AttackRunCombatAltitude = entity.AttackRunCombatAltitude,
                AttackRunEngagementAltitude = entity.AttackRunEngagementAltitude,
                AttackRunPredictionPoint = entity.AttackRunPredictionPoint,
                AttackRunFlyAwayYaw = entity.AttackRunFlyAwayYaw,
                AttackRunUsePrediction = entity.AttackRunUsePrediction,
                AttackRunFlyover = entity.AttackRunFlyover
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
            float zoom = Mathf.Clamp(state.GridZoom, 0.25f, 8f);
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
                OriginWorld = state.GraphOriginWorld(),
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

    internal struct AiSimulation3DProjection
    {
        internal Rect Rect;
        internal Vector3 OriginWorld;
        internal float MetersPerPixel;
        internal float VisibleRadius;
        internal float YawDegrees;
        internal float PitchDegrees;
        internal float VerticalScale;

        internal static AiSimulation3DProjection For(Rect rect, AiSimulationState state)
        {
            AiSimulationGridProjection grid = AiSimulationGridProjection.For(rect, state);
            return new AiSimulation3DProjection
            {
                Rect = rect,
                OriginWorld = state.GraphOriginWorld(),
                MetersPerPixel = grid.MetersPerPixel,
                VisibleRadius = grid.VisibleRadius,
                YawDegrees = state.Graph3DYaw,
                PitchDegrees = state.Graph3DPitch,
                VerticalScale = state.GraphVerticalScale
            };
        }

        internal Vector2 WorldToScreen(Vector3 world)
        {
            return RelativeToScreen(world - OriginWorld);
        }

        internal Vector2 RelativeToScreen(Vector3 relativeWorld)
        {
            Vector3 projected = ProjectWorld(relativeWorld);
            float pixelsPerMeter = 1f / Mathf.Max(0.001f, MetersPerPixel);
            return new Vector2(
                Rect.center.x + projected.x * pixelsPerMeter,
                Rect.center.y - projected.y * pixelsPerMeter);
        }

        internal Vector2 DirectionToScreen(Vector3 direction, float pixels)
        {
            Vector2 a = RelativeToScreen(Vector3.zero);
            Vector2 b = RelativeToScreen(direction);
            Vector2 delta = b - a;
            if (delta.sqrMagnitude < 0.001f)
                return Vector2.zero;
            return delta.normalized * pixels;
        }

        internal float Depth(Vector3 world)
        {
            return DepthRelative(world - OriginWorld);
        }

        private float DepthRelative(Vector3 relativeWorld)
        {
            Vector3 relative = RotateYaw(relativeWorld);
            float pitch = PitchDegrees * Mathf.Deg2Rad;
            return relative.z * Mathf.Cos(pitch) - relative.y * VerticalScale * Mathf.Sin(pitch);
        }

        internal Vector3 ProjectWorld(Vector3 relativeWorld)
        {
            Vector3 rotated = RotateYaw(relativeWorld);
            float pitch = PitchDegrees * Mathf.Deg2Rad;
            float y = rotated.z * Mathf.Sin(pitch) + rotated.y * VerticalScale * Mathf.Cos(pitch);
            return new Vector3(rotated.x, y, DepthRelative(relativeWorld));
        }

        private Vector3 RotateYaw(Vector3 relativeWorld)
        {
            float yaw = YawDegrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(yaw);
            float cos = Mathf.Cos(yaw);
            return new Vector3(
                relativeWorld.x * cos - relativeWorld.z * sin,
                relativeWorld.y,
                relativeWorld.x * sin + relativeWorld.z * cos);
        }
    }
}
