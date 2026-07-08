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
    internal static class AiSimulationImporter
    {
        internal static bool TryImport(MainConstruct construct, AiSimulationState state, out string message)
        {
            if (construct == null)
            {
                message = "No focused craft to import from. Sandbox remains independent.";
                return false;
            }

            List<AIMainframe> mainframes = construct.iBlockTypeStorage.MainframeStore.CopyToList();
            foreach (AIMainframe mainframe in mainframes)
            {
                if (TryImportMainframe(mainframe, state, out message))
                    return true;
            }

            message = "No supported selected AI behaviour found on the focused craft.";
            return false;
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
                state.Side = AiSimulationSide.Left;
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
                state.CraftMovementModel = AiCraftMovementModel.HoverSixAxis;
                state.ImportedParameters.Add($"movement {manoeuvre.GetType().Name} -> hover/six-axis pursuit");
            }
            else if (manoeuvre is ManoeuvreAirplane || manoeuvre is FtdAerialMovement)
            {
                state.CraftMovementModel = AiCraftMovementModel.Airplane;
                state.ImportedParameters.Add($"movement {manoeuvre.GetType().Name} -> airplane pursuit");
            }
            else if (manoeuvre is FtdNavalAndLandManoeuvre)
            {
                state.CraftMovementModel = AiCraftMovementModel.ShipOrTank;
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
