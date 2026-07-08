using System;
using System.Collections.Generic;
using BrilliantSkies.Ai.Interfaces;
using BrilliantSkies.Ai.Modules.Behaviour;
using BrilliantSkies.Ai.Modules.Behaviour.Examples;
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
                || behaviour is BehaviourBroadside;
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
            ApplyManoeuvre(manoeuvre, state);

            if (behaviour is BehaviourCircleAtDistance circle)
            {
                state.SetPreset(AiSimulationPreset.Circle);
                state.Radius = Mathf.Max(10f, circle.DistanceToMaintain.Us);
                state.CircleMinApproachAngle = circle.MinApproachAngle.Us;
                state.Side = MapSide(circle.PreferredSide.Us);
                state.ImportedParameters.Add($"distance {circle.DistanceToMaintain.Us:0.#}m");
                state.ImportedParameters.Add($"side {circle.PreferredSide.Us}");
                state.ImportedParameters.Add($"minimum approach {circle.MinApproachAngle.Us:0.#} deg");
            }
            else if (behaviour is BehaviourPointAndMaintainDistance pointAt)
            {
                state.SetPreset(AiSimulationPreset.PointAt);
                state.Radius = Mathf.Max(10f, pointAt.DistanceToMaintain.Us);
                state.ImportedParameters.Add($"distance {pointAt.DistanceToMaintain.Us:0.#}m");
                state.ImportedParameters.Add($"altitude {pointAt.AltitudeType.Us} {pointAt.PreferredAltitude.Us:0.#}m");
            }
            else if (behaviour is FtdNaval naval)
            {
                state.SetPreset(AiSimulationPreset.NavalBroadside);
                state.Radius = Mathf.Max(
                    10f,
                    Mathf.Max(naval.MinimumBroadsideDistanceToMaintain.Us, naval.BroadsideDistance.Lower));
                state.BroadsideOuterRadius = Mathf.Max(state.Radius + 20f, naval.BroadsideDistance.Upper);
                state.BroadsideAngle = Mathf.Abs(naval.NominalBroadsideAngle.Us);
                state.Side = AiSimulationSide.Both;
                state.ImportedParameters.Add($"enter broadside <= {naval.BroadsideDistance.Lower:0.#}m");
                state.ImportedParameters.Add($"leave broadside >= {naval.BroadsideDistance.Upper:0.#}m");
                state.ImportedParameters.Add($"nominal angle {naval.NominalBroadsideAngle.Us:0.#} deg");
            }
            else if (behaviour is BehaviourBroadside broadside)
            {
                state.SetPreset(AiSimulationPreset.Broadside);
                state.Radius = Mathf.Max(10f, (broadside.DistanceToMaintain.Lower + broadside.DistanceToMaintain.Upper) * 0.5f);
                state.BroadsideAngle = Mathf.Abs(broadside.AngleToMaintain.Us);
                state.Side = broadside.AngleToMaintain.Us < 0f ? AiSimulationSide.Right : AiSimulationSide.Left;
                state.ImportedParameters.Add($"angle {broadside.AngleToMaintain.Us:0.#} deg");
                state.ImportedParameters.Add($"distance {broadside.DistanceToMaintain.Lower:0.#}-{broadside.DistanceToMaintain.Upper:0.#}m");
            }
            else
            {
                message = $"Unsupported behaviour: {behaviour.GetType().Name}.";
                return false;
            }

            ImportRequests(mainframe, state);
            state.ImportStatus = $"Imported once from {state.ImportedMainframe}: {state.ImportedBehaviour}.";
            state.Reset();
            message = state.ImportStatus;
            return true;
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

        private static void ApplyManoeuvre(object manoeuvre, AiSimulationState state)
        {
            if (manoeuvre == null)
                return;

            if (manoeuvre is ManoeuvreSixAxis || manoeuvre is ManoeuvreHover)
            {
                state.SetCraftProfile(AiCraftProfile.Hovercraft);
                state.ImportedParameters.Add($"movement {manoeuvre.GetType().Name} -> hover/six-axis pursuit");
            }
            else if (manoeuvre is ManoeuvreAirplane || manoeuvre is FtdAerialMovement)
            {
                state.SetCraftProfile(AiCraftProfile.Airplane);
                state.ImportedParameters.Add($"movement {manoeuvre.GetType().Name} -> airplane pursuit");
            }
            else if (manoeuvre is FtdNavalAndLandManoeuvre)
            {
                state.SetCraftProfile(AiCraftProfile.SurfaceShip);
                state.ImportedParameters.Add($"movement {manoeuvre.GetType().Name} -> ship/tank pursuit");
            }
            else
            {
                state.ImportedParameters.Add($"movement {manoeuvre.GetType().Name} not mirrored; keeping {state.CraftMovementModelName()}");
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
