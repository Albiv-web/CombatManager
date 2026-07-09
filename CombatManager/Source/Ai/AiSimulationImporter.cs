using System;
using System.Collections.Generic;
using BrilliantSkies.Ai.Interfaces;
using BrilliantSkies.Ai.Modules.Behaviour;
using BrilliantSkies.Ai.Modules.Behaviour.Examples;
using BrilliantSkies.Ai.Modules.Behaviour.Examples.Ftd;
using BrilliantSkies.Ai.Modules.Manoeuvre;
using BrilliantSkies.Ai.Modules.Manoeuvre.Examples;
using BrilliantSkies.Ai.Modules.Manoeuvre.Examples.Ftd;
using UnityEngine;

namespace CombatManager.Ai
{
    internal sealed class AiImportCandidate
    {
        internal int Index;
        internal string MainframeName;
        internal string BehaviourType;
        internal string ManoeuvreType;
        internal string MovementType;
        internal string FiringType;
        internal int Priority;
        internal bool Supported;
        internal string Summary;
    }

    internal static class AiSimulationImporter
    {
        internal static void RefreshCandidates(MainConstruct construct, AiSimulationState state)
        {
            state.ImportCandidates.Clear();
            state.SelectedImportIndex = -1;

            if (construct == null)
            {
                state.ImportStatus = "No focused craft to import from. Sandbox remains independent.";
                return;
            }

            List<AIMainframe> mainframes = construct.iBlockTypeStorage.MainframeStore.CopyToList();
            for (int i = 0; i < mainframes.Count; i++)
            {
                AiImportCandidate candidate = BuildCandidate(mainframes[i], i);
                if (candidate != null)
                    state.ImportCandidates.Add(candidate);
            }

            int supportedCount = 0;
            foreach (AiImportCandidate candidate in state.ImportCandidates)
            {
                if (!candidate.Supported)
                    continue;

                supportedCount++;
                if (supportedCount == 1)
                    state.SelectedImportIndex = candidate.Index;
            }

            if (state.ImportCandidates.Count == 0)
                state.ImportStatus = "No AI mainframes found on the focused craft.";
            else if (supportedCount == 0)
                state.ImportStatus = "Mainframes found, but no selected behaviour is supported yet.";
            else if (supportedCount == 1)
                state.ImportStatus = "One supported mainframe found. Press Import Selected to seed the sandbox.";
            else
            {
                state.SelectedImportIndex = -1;
                state.ImportStatus = "Multiple supported mainframes found. Select one, then Import Selected.";
            }
        }

        internal static bool TryImport(MainConstruct construct, AiSimulationState state, out string message)
        {
            return TryImport(construct, state, state.SelectedImportIndex, out message);
        }

        internal static bool TryImport(MainConstruct construct, AiSimulationState state, int selectedIndex, out string message)
        {
            if (construct == null)
            {
                message = "No focused craft to import from. Sandbox remains independent.";
                return false;
            }

            List<AIMainframe> mainframes = construct.iBlockTypeStorage.MainframeStore.CopyToList();
            if (selectedIndex < 0)
            {
                int supported = 0;
                int onlySupportedIndex = -1;
                for (int i = 0; i < mainframes.Count; i++)
                {
                    AiImportCandidate candidate = BuildCandidate(mainframes[i], i);
                    if (candidate == null || !candidate.Supported)
                        continue;

                    supported++;
                    onlySupportedIndex = i;
                }

                if (supported == 1)
                    selectedIndex = onlySupportedIndex;
                else if (supported == 0)
                {
                    RefreshCandidates(construct, state);
                    message = "No supported selected AI behaviour found on the focused craft.";
                    return false;
                }
                else if (supported > 1)
                {
                    RefreshCandidates(construct, state);
                    message = "Multiple supported mainframes found. Select one in Import, then press Import Selected.";
                    return false;
                }
            }

            if (selectedIndex < 0 || selectedIndex >= mainframes.Count)
            {
                RefreshCandidates(construct, state);
                message = "Select a mainframe in Import, then press Import Selected.";
                return false;
            }

            bool imported = TryImportMainframe(mainframes[selectedIndex], state, out message);
            if (!imported)
                RefreshCandidates(construct, state);
            state.SelectedImportIndex = selectedIndex;
            return imported;
        }

        private static AiImportCandidate BuildCandidate(AIMainframe mainframe, int index)
        {
            if (mainframe?.Node?.Master == null)
                return null;

            mainframe.Node.Master.Pack.GetSelectedBehaviour(out BrilliantSkies.Ai.Modules.Behaviour.IBehaviour selectedBehaviour);
            mainframe.Node.Master.Pack.GetSelectedManoeuvre(out IManoeuvre selectedManoeuvre);
            string behaviour = selectedBehaviour?.GetType().Name ?? "--";
            string manoeuvre = selectedManoeuvre?.GetType().Name ?? "--";
            bool supported = IsSupportedBehaviour(selectedBehaviour);
            return new AiImportCandidate
            {
                Index = index,
                MainframeName = mainframe.Node.Name,
                BehaviourType = behaviour,
                ManoeuvreType = manoeuvre,
                MovementType = mainframe.Node.Master.MovementType.ToString(),
                FiringType = mainframe.Node.Master.FiringType.ToString(),
                Priority = mainframe.Node.Master.Priority,
                Supported = supported,
                Summary = supported ? $"{behaviour} / {manoeuvre}" : $"Unsupported: {behaviour}"
            };
        }

        private static bool IsSupportedBehaviour(object behaviour)
        {
            return behaviour is BehaviourCircleAtDistance
                || behaviour is BehaviourPointAndMaintainDistance
                || behaviour is FtdNaval
                || behaviour is BehaviourBroadside
                || behaviour is FtdAerial
                || behaviour is BehaviourBombingRun
                || behaviour is BehaviourAircraft;
        }

        private static bool TryImportMainframe(AIMainframe mainframe, AiSimulationState state, out string message)
        {
            message = null;
            if (mainframe?.Node?.Master == null)
                return false;

            object behaviour = null;
            object manoeuvre = null;
            mainframe.Node.Master.Pack.GetSelectedBehaviour(out BrilliantSkies.Ai.Modules.Behaviour.IBehaviour selectedBehaviour);
            mainframe.Node.Master.Pack.GetSelectedManoeuvre(out IManoeuvre selectedManoeuvre);
            behaviour = selectedBehaviour;
            manoeuvre = selectedManoeuvre;
            if (behaviour == null)
                return false;

            state.ImportedParameters.Clear();
            state.ImportedRequests.Clear();
            state.ImportedMainframe = mainframe.Node.Name;
            state.ImportedBehaviour = behaviour.GetType().Name;
            state.ImportedManoeuvre = manoeuvre?.GetType().Name;
            state.ImportedParameters.Add($"mainframe priority {mainframe.Node.Master.Priority}");
            state.ImportedParameters.Add($"movement mode {mainframe.Node.Master.MovementType}");
            state.ImportedParameters.Add($"firing mode {mainframe.Node.Master.FiringType}");
            AiMainframeBlueprint blueprint = state.BlueBlueprint.Clone();
            blueprint.MainframeName = mainframe.Node.Name;
            blueprint.Priority = mainframe.Node.Master.Priority;
            blueprint.MovementMode = mainframe.Node.Master.MovementType.ToString();
            blueprint.FiringMode = mainframe.Node.Master.FiringType.ToString();
            blueprint.PreviewOnly = false;
            blueprint.Warnings.Clear();
            ApplyManoeuvre(manoeuvre, blueprint, state.ImportedParameters);

            if (behaviour is BehaviourCircleAtDistance circle)
            {
                blueprint.Behaviour = AiSimulationPreset.Circle;
                blueprint.Radius = Mathf.Max(10f, circle.DistanceToMaintain.Us);
                blueprint.CircleMinApproachAngle = circle.MinApproachAngle.Us;
                blueprint.Side = MapSide(circle.PreferredSide.Us);
                state.ImportedParameters.Add($"distance {circle.DistanceToMaintain.Us:0.#}m");
                state.ImportedParameters.Add($"side {circle.PreferredSide.Us}");
                state.ImportedParameters.Add($"minimum approach {circle.MinApproachAngle.Us:0.#} deg");
            }
            else if (behaviour is BehaviourPointAndMaintainDistance pointAt)
            {
                blueprint.Behaviour = AiSimulationPreset.PointAt;
                blueprint.Radius = Mathf.Max(10f, pointAt.DistanceToMaintain.Us);
                state.ImportedParameters.Add($"distance {pointAt.DistanceToMaintain.Us:0.#}m");
                state.ImportedParameters.Add($"altitude {pointAt.AltitudeType.Us} {pointAt.PreferredAltitude.Us:0.#}m");
            }
            else if (behaviour is FtdNaval naval)
            {
                blueprint.Behaviour = AiSimulationPreset.NavalBroadside;
                blueprint.Radius = Mathf.Max(
                    10f,
                    Mathf.Max(naval.MinimumBroadsideDistanceToMaintain.Us, naval.BroadsideDistance.Lower));
                blueprint.BroadsideOuterRadius = Mathf.Max(blueprint.Radius + 20f, naval.BroadsideDistance.Upper);
                blueprint.BroadsideAngle = Mathf.Abs(naval.NominalBroadsideAngle.Us);
                blueprint.Side = AiSimulationSide.Both;
                state.ImportedParameters.Add($"enter broadside <= {naval.BroadsideDistance.Lower:0.#}m");
                state.ImportedParameters.Add($"leave broadside >= {naval.BroadsideDistance.Upper:0.#}m");
                state.ImportedParameters.Add($"nominal angle {naval.NominalBroadsideAngle.Us:0.#} deg");
            }
            else if (behaviour is BehaviourBroadside broadside)
            {
                blueprint.Behaviour = AiSimulationPreset.Broadside;
                blueprint.Radius = Mathf.Max(10f, (broadside.DistanceToMaintain.Lower + broadside.DistanceToMaintain.Upper) * 0.5f);
                blueprint.BroadsideAngle = Mathf.Abs(broadside.AngleToMaintain.Us);
                blueprint.Side = broadside.AngleToMaintain.Us < 0f ? AiSimulationSide.Right : AiSimulationSide.Left;
                state.ImportedParameters.Add($"angle {broadside.AngleToMaintain.Us:0.#} deg");
                state.ImportedParameters.Add($"distance {broadside.DistanceToMaintain.Lower:0.#}-{broadside.DistanceToMaintain.Upper:0.#}m");
            }
            else if (behaviour is FtdAerial aerial)
            {
                blueprint.Behaviour = AiSimulationPreset.AttackRun1;
                blueprint.Radius = aerial.BombingRunRangeBracket.Upper;
                blueprint.AttackRunAbortDistance = aerial.BombingRunRangeBracket.Lower;
                blueprint.AttackRunBeginDistance = aerial.BombingRunRangeBracket.Upper;
                blueprint.AttackRunWaitTime = aerial.EngageOverrideTime.Us;
                blueprint.AttackRunAttackAltitude = aerial.FlyoverHeight.Us;
                blueprint.AttackRunDisengageAltitude = aerial.MinimumAndCruiseAltitude.Upper;
                state.ImportedParameters.Add($"attack run 1 bracket {aerial.BombingRunRangeBracket.Lower:0.#}-{aerial.BombingRunRangeBracket.Upper:0.#}m");
                state.ImportedParameters.Add($"attack altitude {aerial.FlyoverHeight.Us:0.#}m, flee altitude {aerial.MinimumAndCruiseAltitude.Upper:0.#}m");
            }
            else if (behaviour is BehaviourAircraft aircraft)
            {
                blueprint.Behaviour = AiSimulationPreset.AttackRun3;
                ApplyAircraftFields(aircraft, blueprint, state.ImportedParameters);
                blueprint.AttackRunEngagementAltitude = aircraft.EngagementAltitude.Us;
                blueprint.AttackRunUsePrediction = aircraft.UsePrediction.Us;
                blueprint.AttackRunFlyover = aircraft.Flyover.Us;
                blueprint.AttackRunIgnoreAltitude = aircraft.IgnoreAltitude.Us;
                blueprint.AttackRunPredictionPoint = aircraft.PointDirect.Us;
                state.ImportedParameters.Add($"engagement altitude {aircraft.EngagementAltitude.Us:0.#}m, prediction {aircraft.UsePrediction.Us}");
            }
            else if (behaviour is BehaviourBombingRun bombing)
            {
                blueprint.Behaviour = AiSimulationPreset.AttackRun2;
                ApplyBombingRunFields(bombing, blueprint, state.ImportedParameters);
                blueprint.AttackRunCombatAltitude = bombing.PreferredAltitude.Us;
                state.ImportedParameters.Add($"combat altitude {bombing.PreferredAltitude.Us:0.#}m");
            }
            else
            {
                message = $"Unsupported behaviour: {behaviour.GetType().Name}.";
                return false;
            }

            state.ApplyBlueprint(AiEntityRole.Blue, blueprint, reset: false);
            ImportRequests(mainframe, state);
            state.ImportStatus = $"Imported once from {state.ImportedMainframe}: {state.ImportedBehaviour}.";
            state.Reset();
            message = state.ImportStatus;
            return true;
        }

        private static void ApplyBombingRunFields(BehaviourBombingRun bombing, AiMainframeBlueprint blueprint, List<string> importedParameters)
        {
            blueprint.Radius = Mathf.Max(10f, bombing.PitchDistance.Us);
            blueprint.AttackRunBreakoffDistance = bombing.BreakoffDistance.Us;
            blueprint.AttackRunReengageDistance = bombing.ReengageDistance.Us;
            blueprint.AttackRunReengageTime = bombing.ReengageTime.Us;
            blueprint.AttackRunPitchDistance = bombing.PitchDistance.Us;
            blueprint.AttackRunBreakoffAltitude = bombing.BreakoffAltitude.Us;
            blueprint.AttackRunAbortTime = bombing.AbortTime.Us;
            blueprint.AttackRunAbortTimerStartDistance = bombing.AbortTimeStartDistance.Us;
            importedParameters.Add($"breakoff {bombing.BreakoffDistance.Us:0.#}m, pitch distance {bombing.PitchDistance.Us:0.#}m");
            importedParameters.Add($"reengage {bombing.ReengageDistance.Us:0.#}m / {bombing.ReengageTime.Us:0.#}s");
        }

        private static void ApplyAircraftFields(BehaviourAircraft aircraft, AiMainframeBlueprint blueprint, List<string> importedParameters)
        {
            blueprint.Radius = Mathf.Max(10f, aircraft.PitchDistance.Us);
            blueprint.AttackRunBreakoffDistance = aircraft.BreakoffDistance.Us;
            blueprint.AttackRunReengageDistance = aircraft.ReengageDistance.Us;
            blueprint.AttackRunReengageTime = aircraft.ReengageTime.Us;
            blueprint.AttackRunPitchDistance = aircraft.PitchDistance.Us;
            blueprint.AttackRunBreakoffAltitude = aircraft.BreakoffAltitude.Us;
            blueprint.AttackRunAbortTime = aircraft.AbortTime.Us;
            blueprint.AttackRunAbortTimerStartDistance = aircraft.AbortTimeStartDistance.Us;
            importedParameters.Add($"breakoff {aircraft.BreakoffDistance.Us:0.#}m, pitch distance {aircraft.PitchDistance.Us:0.#}m");
            importedParameters.Add($"reengage {aircraft.ReengageDistance.Us:0.#}m / {aircraft.ReengageTime.Us:0.#}s");
        }

        private static AiSimulationSide MapSide(SideOptions side)
        {
            switch (side)
            {
                case SideOptions.Left:
                    return AiSimulationSide.Left;
                case SideOptions.Right:
                    return AiSimulationSide.Right;
                default:
                    return AiSimulationSide.Both;
            }
        }

        private static void ApplyManoeuvre(object manoeuvre, AiMainframeBlueprint blueprint, List<string> importedParameters)
        {
            if (manoeuvre == null)
                return;

            if (manoeuvre is ManoeuvreSixAxis || manoeuvre is ManoeuvreHover)
            {
                if (manoeuvre is ManoeuvreSixAxis)
                {
                    blueprint.CraftProfile = AiCraftProfile.SixAxisDrone;
                    blueprint.Manoeuvre = AiCraftMovementModel.SixAxis;
                    importedParameters.Add($"movement {manoeuvre.GetType().Name} -> six-axis pursuit");
                }
                else
                {
                    blueprint.CraftProfile = AiCraftProfile.Hovercraft;
                    blueprint.Manoeuvre = AiCraftMovementModel.Hover;
                    importedParameters.Add($"movement {manoeuvre.GetType().Name} -> hover pursuit");
                }
            }
            else if (manoeuvre is ManoeuvreAirplane || manoeuvre is FtdAerialMovement)
            {
                blueprint.CraftProfile = AiCraftProfile.Airplane;
                blueprint.Manoeuvre = AiCraftMovementModel.Airplane;
                importedParameters.Add($"movement {manoeuvre.GetType().Name} -> airplane pursuit");
            }
            else if (manoeuvre is FtdNavalAndLandManoeuvre)
            {
                blueprint.CraftProfile = AiCraftProfile.SurfaceShip;
                blueprint.Manoeuvre = AiCraftMovementModel.ShipOrTank;
                importedParameters.Add($"movement {manoeuvre.GetType().Name} -> ship/tank pursuit");
            }
            else
            {
                importedParameters.Add($"movement {manoeuvre.GetType().Name} not mirrored; keeping {AiSimulationState.CraftMovementModelName(blueprint.Manoeuvre)}");
            }
        }

        private static void ImportRequests(AIMainframe mainframe, AiSimulationState state)
        {
            IPlatformInterface platform = mainframe.Node.Master.PlatformInterface;
            if (platform == null)
                return;

            foreach (AiControlType type in Enum.GetValues(typeof(AiControlType)))
            {
                if (type == AiControlType.NumberOfEntries)
                    continue;

                float value = platform.GetRequest(type);
                if (Mathf.Abs(value) <= 0.001f)
                    continue;

                state.ImportedRequests.Add(new AiControlRequestSnapshot
                {
                    Type = type,
                    Value = value
                });
            }
        }
    }
}
