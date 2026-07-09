using System.Collections.Generic;
using UnityEngine;

namespace CombatManager.Ai
{
    internal enum AiBlueprintPreset
    {
        SlowShipBroadsider,
        FastPlanePointAt,
        HoverSniper,
        CircleShip,
        FastAircraftInterceptor,
        CloseRangeRammer,
        AttackRunOnePlane,
        AttackRunTwoBomber,
        AttackRunThreeInterceptor
    }

    internal enum AiBlueprintAltitudeReference
    {
        Ignore,
        Absolute,
        Above,
        OnWater,
        OnLand
    }

    internal sealed class AiMainframeBlueprint
    {
        internal string MainframeName { get; set; } = "CombatManager draft";
        internal int Priority { get; set; }
        internal string MovementMode { get; set; } = "Automatic";
        internal string FiringMode { get; set; } = "On";

        internal AiSimulationPreset Behaviour { get; set; } = AiSimulationPreset.Circle;
        internal AiSimulationSide Side { get; set; } = AiSimulationSide.Both;
        internal AiCraftMovementModel Manoeuvre { get; set; } = AiCraftMovementModel.ShipOrTank;
        internal AiCraftProfile CraftProfile { get; set; } = AiCraftProfile.SurfaceShip;
        internal bool PreviewOnly { get; set; }

        internal float Radius { get; set; } = 200f;
        internal float BroadsideOuterRadius { get; set; } = 300f;
        internal float BroadsideAngle { get; set; } = 75f;
        internal float CircleMinApproachAngle { get; set; } = 45f;
        internal float CraftSpeed { get; set; } = 45f;
        internal float CraftAcceleration { get; set; } = 18f;
        internal float CraftTurnRate { get; set; } = 90f;
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

        internal string AdjustmentVehicleType { get; set; } = "Surface ship";
        internal AiBlueprintAltitudeReference AltitudeReference { get; set; } = AiBlueprintAltitudeReference.OnWater;
        internal float MinimumAltitudeAboveLand { get; set; }
        internal float MinimumAltitudeAboveWater { get; set; }
        internal float MaximumAltitude { get; set; }
        internal float WaterDepthRequired { get; set; } = 10f;
        internal float LandHeightRequired { get; set; } = 5f;
        internal float TurningCircle { get; set; } = 100f;
        internal float TerrainPredictionTime { get; set; } = 10f;

        internal List<string> Warnings { get; } = new List<string>();

        internal bool ExportSupported => !PreviewOnly
            && BehaviourClassName() != null
            && ManoeuvreClassName() != null;

        internal AiMainframeBlueprint Clone()
        {
            var copy = new AiMainframeBlueprint
            {
                MainframeName = MainframeName,
                Priority = Priority,
                MovementMode = MovementMode,
                FiringMode = FiringMode,
                Behaviour = Behaviour,
                Side = Side,
                Manoeuvre = Manoeuvre,
                CraftProfile = CraftProfile,
                PreviewOnly = PreviewOnly,
                Radius = Radius,
                BroadsideOuterRadius = BroadsideOuterRadius,
                BroadsideAngle = BroadsideAngle,
                CircleMinApproachAngle = CircleMinApproachAngle,
                CraftSpeed = CraftSpeed,
                CraftAcceleration = CraftAcceleration,
                CraftTurnRate = CraftTurnRate,
                Altitude = Altitude,
                ShipTarryDistance = ShipTarryDistance,
                ShipReverseAllowed = ShipReverseAllowed,
                HoverYawLockDistance = HoverYawLockDistance,
                HoverMoveWithinAzimuth = HoverMoveWithinAzimuth,
                HoverStrafeAuthority = HoverStrafeAuthority,
                SixAxisLookAheadDistance = SixAxisLookAheadDistance,
                AirplaneMinimumSpeed = AirplaneMinimumSpeed,
                AirplaneMinimumTurnRadius = AirplaneMinimumTurnRadius,
                AirplaneIdleThrust = AirplaneIdleThrust,
                AirplaneIdleDistance = AirplaneIdleDistance,
                AirplaneBankingTurnAbove = AirplaneBankingTurnAbove,
                AirplaneBankingTurnRoll = AirplaneBankingTurnRoll,
                AirplanePitchForAltitude = AirplanePitchForAltitude,
                AttackRunBeginDistance = AttackRunBeginDistance,
                AttackRunAbortDistance = AttackRunAbortDistance,
                AttackRunWaitTime = AttackRunWaitTime,
                AttackRunAttackAltitude = AttackRunAttackAltitude,
                AttackRunDisengageAltitude = AttackRunDisengageAltitude,
                AttackRunBreakoffDistance = AttackRunBreakoffDistance,
                AttackRunReengageDistance = AttackRunReengageDistance,
                AttackRunReengageTime = AttackRunReengageTime,
                AttackRunPitchDistance = AttackRunPitchDistance,
                AttackRunBreakoffAltitude = AttackRunBreakoffAltitude,
                AttackRunAbortTime = AttackRunAbortTime,
                AttackRunAbortTimerStartDistance = AttackRunAbortTimerStartDistance,
                AttackRunCombatAltitude = AttackRunCombatAltitude,
                AttackRunEngagementAltitude = AttackRunEngagementAltitude,
                AttackRunPredictionPoint = AttackRunPredictionPoint,
                AttackRunUsePrediction = AttackRunUsePrediction,
                AttackRunFlyover = AttackRunFlyover,
                AttackRunIgnoreAltitude = AttackRunIgnoreAltitude,
                AdjustmentVehicleType = AdjustmentVehicleType,
                AltitudeReference = AltitudeReference,
                MinimumAltitudeAboveLand = MinimumAltitudeAboveLand,
                MinimumAltitudeAboveWater = MinimumAltitudeAboveWater,
                MaximumAltitude = MaximumAltitude,
                WaterDepthRequired = WaterDepthRequired,
                LandHeightRequired = LandHeightRequired,
                TurningCircle = TurningCircle,
                TerrainPredictionTime = TerrainPredictionTime
            };
            copy.Warnings.AddRange(Warnings);
            return copy;
        }

        internal void ApplyToEntity(AiSimEntity entity)
        {
            entity.Name = entity.Role == AiEntityRole.Blue ? "Blue" : "Red";
            entity.ApplyCraftProfile(CraftProfile);
            entity.Preset = Behaviour;
            entity.Side = Side;
            entity.CraftMovementModel = Manoeuvre;
            entity.Radius = Mathf.Max(10f, Radius);
            entity.BroadsideOuterRadius = Mathf.Max(entity.Radius + 20f, BroadsideOuterRadius);
            entity.BroadsideAngle = BroadsideAngle;
            entity.CircleMinApproachAngle = CircleMinApproachAngle;
            entity.CraftSpeed = Mathf.Max(0f, CraftSpeed);
            entity.CraftAcceleration = Mathf.Max(0f, CraftAcceleration);
            entity.CraftTurnRate = Mathf.Max(1f, CraftTurnRate);
            entity.Altitude = Mathf.Max(0f, Altitude);
            entity.ShipTarryDistance = Mathf.Max(0f, ShipTarryDistance);
            entity.ShipReverseAllowed = ShipReverseAllowed;
            entity.HoverYawLockDistance = Mathf.Max(0f, HoverYawLockDistance);
            entity.HoverMoveWithinAzimuth = Mathf.Clamp(HoverMoveWithinAzimuth, 0f, 180f);
            entity.HoverStrafeAuthority = Mathf.Clamp01(HoverStrafeAuthority);
            entity.SixAxisLookAheadDistance = Mathf.Max(0f, SixAxisLookAheadDistance);
            entity.AirplaneMinimumSpeed = Mathf.Max(0f, AirplaneMinimumSpeed);
            entity.AirplaneMinimumTurnRadius = Mathf.Max(20f, AirplaneMinimumTurnRadius);
            entity.AirplaneIdleThrust = Mathf.Max(0f, AirplaneIdleThrust);
            entity.AirplaneIdleDistance = Mathf.Max(0f, AirplaneIdleDistance);
            entity.AirplaneBankingTurnAbove = Mathf.Clamp(AirplaneBankingTurnAbove, 0f, 180f);
            entity.AirplaneBankingTurnRoll = Mathf.Clamp(AirplaneBankingTurnRoll, 0f, 90f);
            entity.AirplanePitchForAltitude = Mathf.Clamp(AirplanePitchForAltitude, -45f, 45f);
            entity.AttackRunBeginDistance = Mathf.Max(1f, AttackRunBeginDistance);
            entity.AttackRunAbortDistance = Mathf.Max(1f, AttackRunAbortDistance);
            entity.AttackRunWaitTime = Mathf.Max(0f, AttackRunWaitTime);
            entity.AttackRunAttackAltitude = AttackRunAttackAltitude;
            entity.AttackRunDisengageAltitude = Mathf.Max(0f, AttackRunDisengageAltitude);
            entity.AttackRunBreakoffDistance = Mathf.Max(1f, AttackRunBreakoffDistance);
            entity.AttackRunReengageDistance = Mathf.Max(1f, AttackRunReengageDistance);
            entity.AttackRunReengageTime = Mathf.Max(0f, AttackRunReengageTime);
            entity.AttackRunPitchDistance = Mathf.Max(0f, AttackRunPitchDistance);
            entity.AttackRunBreakoffAltitude = AttackRunBreakoffAltitude;
            entity.AttackRunAbortTime = Mathf.Max(0f, AttackRunAbortTime);
            entity.AttackRunAbortTimerStartDistance = Mathf.Max(0f, AttackRunAbortTimerStartDistance);
            entity.AttackRunCombatAltitude = Mathf.Max(0f, AttackRunCombatAltitude);
            entity.AttackRunEngagementAltitude = AttackRunEngagementAltitude;
            entity.AttackRunPredictionPoint = Mathf.Max(0f, AttackRunPredictionPoint);
            entity.AttackRunUsePrediction = AttackRunUsePrediction;
            entity.AttackRunFlyover = AttackRunFlyover;
            entity.AttackRunIgnoreAltitude = AttackRunIgnoreAltitude;
        }

        internal void CaptureEntityFields(AiSimEntity entity)
        {
            Behaviour = entity.Preset;
            Side = entity.Side;
            Manoeuvre = entity.CraftMovementModel;
            CraftProfile = entity.CraftProfile;
            Radius = entity.Radius;
            BroadsideOuterRadius = entity.BroadsideOuterRadius;
            BroadsideAngle = entity.BroadsideAngle;
            CircleMinApproachAngle = entity.CircleMinApproachAngle;
            CraftSpeed = entity.CraftSpeed;
            CraftAcceleration = entity.CraftAcceleration;
            CraftTurnRate = entity.CraftTurnRate;
            Altitude = entity.Altitude;
            ShipTarryDistance = entity.ShipTarryDistance;
            ShipReverseAllowed = entity.ShipReverseAllowed;
            HoverYawLockDistance = entity.HoverYawLockDistance;
            HoverMoveWithinAzimuth = entity.HoverMoveWithinAzimuth;
            HoverStrafeAuthority = entity.HoverStrafeAuthority;
            SixAxisLookAheadDistance = entity.SixAxisLookAheadDistance;
            AirplaneMinimumSpeed = entity.AirplaneMinimumSpeed;
            AirplaneMinimumTurnRadius = entity.AirplaneMinimumTurnRadius;
            AirplaneIdleThrust = entity.AirplaneIdleThrust;
            AirplaneIdleDistance = entity.AirplaneIdleDistance;
            AirplaneBankingTurnAbove = entity.AirplaneBankingTurnAbove;
            AirplaneBankingTurnRoll = entity.AirplaneBankingTurnRoll;
            AirplanePitchForAltitude = entity.AirplanePitchForAltitude;
            AttackRunBeginDistance = entity.AttackRunBeginDistance;
            AttackRunAbortDistance = entity.AttackRunAbortDistance;
            AttackRunWaitTime = entity.AttackRunWaitTime;
            AttackRunAttackAltitude = entity.AttackRunAttackAltitude;
            AttackRunDisengageAltitude = entity.AttackRunDisengageAltitude;
            AttackRunBreakoffDistance = entity.AttackRunBreakoffDistance;
            AttackRunReengageDistance = entity.AttackRunReengageDistance;
            AttackRunReengageTime = entity.AttackRunReengageTime;
            AttackRunPitchDistance = entity.AttackRunPitchDistance;
            AttackRunBreakoffAltitude = entity.AttackRunBreakoffAltitude;
            AttackRunAbortTime = entity.AttackRunAbortTime;
            AttackRunAbortTimerStartDistance = entity.AttackRunAbortTimerStartDistance;
            AttackRunCombatAltitude = entity.AttackRunCombatAltitude;
            AttackRunEngagementAltitude = entity.AttackRunEngagementAltitude;
            AttackRunPredictionPoint = entity.AttackRunPredictionPoint;
            AttackRunUsePrediction = entity.AttackRunUsePrediction;
            AttackRunFlyover = entity.AttackRunFlyover;
            AttackRunIgnoreAltitude = entity.AttackRunIgnoreAltitude;
        }

        internal string BehaviourClassName()
        {
            if (PreviewOnly)
                return null;

            switch (Behaviour)
            {
                case AiSimulationPreset.PointAt:
                    return "BehaviourPointAndMaintainDistance";
                case AiSimulationPreset.Broadside:
                    return "BehaviourBroadside";
                case AiSimulationPreset.NavalBroadside:
                    return "FtdNaval";
                case AiSimulationPreset.AttackRun1:
                    return "FtdAerial";
                case AiSimulationPreset.AttackRun2:
                    return "BehaviourBombingRun";
                case AiSimulationPreset.AttackRun3:
                    return "BehaviourAircraft";
                default:
                    return "BehaviourCircleAtDistance";
            }
        }

        internal string ManoeuvreClassName()
        {
            switch (Manoeuvre)
            {
                case AiCraftMovementModel.Hover:
                    return "ManoeuvreHover";
                case AiCraftMovementModel.SixAxis:
                    return "ManoeuvreSixAxis";
                case AiCraftMovementModel.Airplane:
                    return "ManoeuvreAirplane";
                default:
                    return "FtdNavalAndLandManoeuvre";
            }
        }
    }

    internal static class AiBlueprintPresetLibrary
    {
        internal static readonly AiBlueprintPreset[] All =
        {
            AiBlueprintPreset.SlowShipBroadsider,
            AiBlueprintPreset.FastPlanePointAt,
            AiBlueprintPreset.HoverSniper,
            AiBlueprintPreset.CircleShip,
            AiBlueprintPreset.FastAircraftInterceptor,
            AiBlueprintPreset.CloseRangeRammer,
            AiBlueprintPreset.AttackRunOnePlane,
            AiBlueprintPreset.AttackRunTwoBomber,
            AiBlueprintPreset.AttackRunThreeInterceptor
        };

        internal static string Name(AiBlueprintPreset preset)
        {
            switch (preset)
            {
                case AiBlueprintPreset.FastPlanePointAt:
                    return "Fast plane point-at";
                case AiBlueprintPreset.HoverSniper:
                    return "Hover sniper";
                case AiBlueprintPreset.CircleShip:
                    return "Circle ship";
                case AiBlueprintPreset.FastAircraftInterceptor:
                    return "Fast aircraft interceptor";
                case AiBlueprintPreset.CloseRangeRammer:
                    return "Close-range rammer";
                case AiBlueprintPreset.AttackRunOnePlane:
                    return "Attack run 1.0 plane";
                case AiBlueprintPreset.AttackRunTwoBomber:
                    return "Attack run 2.0 bomber";
                case AiBlueprintPreset.AttackRunThreeInterceptor:
                    return "Attack run 3.0 interceptor";
                default:
                    return "Slow ship broadsider";
            }
        }

        internal static AiMainframeBlueprint Create(AiBlueprintPreset preset, AiEntityRole role)
        {
            string side = role == AiEntityRole.Blue ? "Blue" : "Red";
            var blueprint = new AiMainframeBlueprint
            {
                MainframeName = $"{side} {Name(preset)}",
                Priority = 0,
                MovementMode = "Automatic",
                FiringMode = "On"
            };

            switch (preset)
            {
                case AiBlueprintPreset.FastPlanePointAt:
                    ConfigureAircraft(blueprint, AiSimulationPreset.PointAt, AiCraftProfile.FastAircraft, 900f, 160f, 36f, 130f, 450f);
                    blueprint.Warnings.Add("Airplane pitch/roll/PID details are approximated in the sandbox.");
                    break;
                case AiBlueprintPreset.HoverSniper:
                    ConfigureHover(blueprint, AiCraftMovementModel.SixAxis, 3000f, 300f);
                    blueprint.CraftSpeed = 32f;
                    blueprint.Warnings.Add("Represents a hover/six-axis sniper that holds long range and faces the target.");
                    break;
                case AiBlueprintPreset.CircleShip:
                    ConfigureSurface(blueprint, AiSimulationPreset.Circle, 320f, 460f, 35f);
                    blueprint.Warnings.Add("Vanilla circling ship cards use hover-style movement; this preset previews surface handling.");
                    break;
                case AiBlueprintPreset.FastAircraftInterceptor:
                    ConfigureAircraft(blueprint, AiSimulationPreset.Circle, AiCraftProfile.FastAircraft, 650f, 170f, 40f, 140f, 500f);
                    blueprint.Side = AiSimulationSide.Left;
                    break;
                case AiBlueprintPreset.CloseRangeRammer:
                    ConfigureSurface(blueprint, AiSimulationPreset.PointAt, 80f, 160f, 65f);
                    blueprint.MainframeName = $"{side} rammer preview";
                    blueprint.PreviewOnly = true;
                    blueprint.Warnings.Add("Ram is preview-only until BehaviourRam is researched and mapped.");
                    break;
                case AiBlueprintPreset.AttackRunOnePlane:
                    ConfigureAircraft(blueprint, AiSimulationPreset.AttackRun1, AiCraftProfile.Airplane, 250f, 95f, 24f, 95f, 75f);
                    blueprint.MainframeName = $"{side} attack run 1.0 plane";
                    blueprint.AttackRunBeginDistance = 250f;
                    blueprint.AttackRunAbortDistance = 50f;
                    blueprint.AttackRunAttackAltitude = 0f;
                    blueprint.AttackRunDisengageAltitude = 75f;
                    blueprint.Warnings.Add("Mirrors FtdAerial attack/fly-away states; pitch PID and adjuster output are approximate.");
                    break;
                case AiBlueprintPreset.AttackRunTwoBomber:
                    ConfigureAircraft(blueprint, AiSimulationPreset.AttackRun2, AiCraftProfile.Airplane, 800f, 105f, 24f, 95f, 200f);
                    blueprint.MainframeName = $"{side} attack run 2.0 bomber";
                    blueprint.AttackRunCombatAltitude = 200f;
                    blueprint.Warnings.Add("Mirrors BehaviourBombingRun attack/flee states; run-away adjustment and pitch PID are approximate.");
                    break;
                case AiBlueprintPreset.AttackRunThreeInterceptor:
                    ConfigureAircraft(blueprint, AiSimulationPreset.AttackRun3, AiCraftProfile.FastAircraft, 1000f, 165f, 38f, 135f, 220f);
                    blueprint.MainframeName = $"{side} attack run 3.0 interceptor";
                    blueprint.AttackRunEngagementAltitude = 100f;
                    blueprint.AttackRunCombatAltitude = 200f;
                    blueprint.AttackRunUsePrediction = true;
                    blueprint.Warnings.Add("Mirrors BehaviourAircraft run phases; interception, adjuster, and PID details are approximate.");
                    break;
                case AiBlueprintPreset.SlowShipBroadsider:
                default:
                    ConfigureSurface(blueprint, AiSimulationPreset.NavalBroadside, 2500f, 3000f, 18f);
                    blueprint.MainframeName = $"{side} 2500m broadsider";
                    blueprint.BroadsideAngle = 75f;
                    blueprint.CraftAcceleration = 6f;
                    blueprint.CraftTurnRate = 35f;
                    blueprint.TurningCircle = 250f;
                    break;
            }

            return blueprint;
        }

        private static void ConfigureSurface(AiMainframeBlueprint blueprint, AiSimulationPreset behaviour, float radius, float outerRadius, float speed)
        {
            blueprint.Behaviour = behaviour;
            blueprint.Side = AiSimulationSide.Both;
            blueprint.CraftProfile = AiCraftProfile.SurfaceShip;
            blueprint.Manoeuvre = AiCraftMovementModel.ShipOrTank;
            blueprint.Radius = radius;
            blueprint.BroadsideOuterRadius = outerRadius;
            blueprint.BroadsideAngle = 75f;
            blueprint.CircleMinApproachAngle = 45f;
            blueprint.CraftSpeed = speed;
            blueprint.CraftAcceleration = Mathf.Max(6f, speed * 0.4f);
            blueprint.CraftTurnRate = 60f;
            blueprint.Altitude = 0f;
            blueprint.AdjustmentVehicleType = "Surface ship";
            blueprint.AltitudeReference = AiBlueprintAltitudeReference.OnWater;
            blueprint.MinimumAltitudeAboveLand = 0f;
            blueprint.MinimumAltitudeAboveWater = 0f;
            blueprint.WaterDepthRequired = 10f;
            blueprint.MaximumAltitude = 0f;
            blueprint.TerrainPredictionTime = 10f;
        }

        private static void ConfigureHover(AiMainframeBlueprint blueprint, AiCraftMovementModel movement, float radius, float altitude)
        {
            blueprint.Behaviour = AiSimulationPreset.PointAt;
            blueprint.Side = AiSimulationSide.Both;
            blueprint.CraftProfile = movement == AiCraftMovementModel.SixAxis ? AiCraftProfile.SixAxisDrone : AiCraftProfile.Hovercraft;
            blueprint.Manoeuvre = movement;
            blueprint.Radius = radius;
            blueprint.BroadsideOuterRadius = radius + 300f;
            blueprint.CraftSpeed = 38f;
            blueprint.CraftAcceleration = 32f;
            blueprint.CraftTurnRate = 160f;
            blueprint.Altitude = altitude;
            blueprint.AdjustmentVehicleType = "Hover / six-axis";
            blueprint.AltitudeReference = AiBlueprintAltitudeReference.Above;
            blueprint.MinimumAltitudeAboveLand = 80f;
            blueprint.MinimumAltitudeAboveWater = 80f;
            blueprint.MaximumAltitude = 900f;
        }

        private static void ConfigureAircraft(AiMainframeBlueprint blueprint, AiSimulationPreset behaviour, AiCraftProfile profile, float radius, float speed, float acceleration, float turnRate, float altitude)
        {
            blueprint.Behaviour = behaviour;
            blueprint.Side = AiSimulationSide.Both;
            blueprint.CraftProfile = profile;
            blueprint.Manoeuvre = AiCraftMovementModel.Airplane;
            blueprint.Radius = radius;
            blueprint.BroadsideOuterRadius = radius + 350f;
            blueprint.CraftSpeed = speed;
            blueprint.CraftAcceleration = acceleration;
            blueprint.CraftTurnRate = turnRate;
            blueprint.Altitude = altitude;
            blueprint.AirplaneMinimumSpeed = Mathf.Max(32f, speed * 0.45f);
            blueprint.AirplaneMinimumTurnRadius = Mathf.Max(120f, speed * 1.2f);
            blueprint.AirplaneIdleDistance = Mathf.Max(300f, radius * 0.5f);
            blueprint.AdjustmentVehicleType = "Aircraft";
            blueprint.AltitudeReference = AiBlueprintAltitudeReference.Above;
            blueprint.MinimumAltitudeAboveLand = 80f;
            blueprint.MinimumAltitudeAboveWater = 80f;
            blueprint.MaximumAltitude = 1200f;
        }
    }
}
