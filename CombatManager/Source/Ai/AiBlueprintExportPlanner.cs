using System.Collections.Generic;

namespace CombatManager.Ai
{
    internal sealed class AiBlueprintExportPlan
    {
        internal string TargetMainframeName;
        internal string RoutineCapacity;
        internal bool HasFocusedCraft;
        internal bool HasSelectedMainframe;
        internal bool Supported;
        internal bool HasEnoughRoutineCapacity;
        internal List<string> Mutations { get; } = new List<string>();
        internal List<string> Warnings { get; } = new List<string>();
    }

    internal static class AiBlueprintExportPlanner
    {
        internal static AiBlueprintExportPlan Build(MainConstruct construct, AiMainframeBlueprint blueprint, int selectedMainframeIndex)
        {
            var plan = new AiBlueprintExportPlan
            {
                HasFocusedCraft = construct != null,
                Supported = blueprint.ExportSupported,
                HasEnoughRoutineCapacity = true
            };

            plan.Warnings.Add("Preview only: CombatManager does not write to real mainframes, cards, or craft AI state yet.");
            foreach (string warning in blueprint.Warnings)
                plan.Warnings.Add(warning);

            AIMainframe target = null;
            if (construct == null)
            {
                plan.TargetMainframeName = "No focused craft";
                plan.HasSelectedMainframe = false;
                plan.Warnings.Add("Focus a craft and refresh mainframes before a future guarded apply.");
            }
            else
            {
                List<AIMainframe> mainframes = construct.iBlockTypeStorage.MainframeStore.CopyToList();
                if (mainframes.Count == 0)
                {
                    plan.TargetMainframeName = "Focused craft has no AI mainframe";
                    plan.HasSelectedMainframe = false;
                    plan.HasEnoughRoutineCapacity = false;
                    plan.Warnings.Add("A future writer will require an existing mainframe or a separate create-mainframe feature.");
                }
                else
                {
                    int index = selectedMainframeIndex >= 0 && selectedMainframeIndex < mainframes.Count ? selectedMainframeIndex : 0;
                    target = mainframes[index];
                    plan.TargetMainframeName = target.Node?.Name ?? $"Mainframe {index}";
                    plan.HasSelectedMainframe = true;
                    AddCapacity(target, plan);
                }
            }

            if (!blueprint.ExportSupported)
            {
                plan.Supported = false;
                plan.Warnings.Add("This blueprint contains preview-only or unsupported routine mappings.");
            }

            plan.Mutations.Add($"Target mainframe: {plan.TargetMainframeName}");
            plan.Mutations.Add($"Set mainframe name to \"{blueprint.MainframeName}\"");
            plan.Mutations.Add($"Set priority {blueprint.Priority}, movement {blueprint.MovementMode}, firing {blueprint.FiringMode}");
            plan.Mutations.Add($"Create/select behaviour: {blueprint.BehaviourClassName() ?? "unsupported"}");
            plan.Mutations.Add($"Create/select manoeuvre: {blueprint.ManoeuvreClassName() ?? "unsupported"}");
            plan.Mutations.Add($"Set behaviour range {blueprint.Radius:0.#}m, leave range {blueprint.BroadsideOuterRadius:0.#}m, side {blueprint.Side}");
            plan.Mutations.Add($"Set broadside angle {blueprint.BroadsideAngle:0.#} deg and circle minimum approach {blueprint.CircleMinApproachAngle:0.#} deg");
            plan.Mutations.Add($"Set movement speed {blueprint.CraftSpeed:0.#}m/s, acceleration {blueprint.CraftAcceleration:0.#}m/s2, turn {blueprint.CraftTurnRate:0.#}deg/s");
            plan.Mutations.Add($"Set altitude {blueprint.Altitude:0.#}m and adjustment reference {blueprint.AltitudeReference}");
            if (blueprint.Behaviour == AiSimulationPreset.AttackRun1)
                plan.Mutations.Add($"Set Attack Run 1.0 begin {blueprint.AttackRunBeginDistance:0.#}m, abort {blueprint.AttackRunAbortDistance:0.#}m, attack altitude {blueprint.AttackRunAttackAltitude:0.#}m, flee altitude {blueprint.AttackRunDisengageAltitude:0.#}m");
            if (blueprint.Behaviour == AiSimulationPreset.AttackRun2 || blueprint.Behaviour == AiSimulationPreset.AttackRun3)
                plan.Mutations.Add($"Set attack-run breakoff {blueprint.AttackRunBreakoffDistance:0.#}m, pitch distance {blueprint.AttackRunPitchDistance:0.#}m, reengage {blueprint.AttackRunReengageDistance:0.#}m/{blueprint.AttackRunReengageTime:0.#}s, flee altitude {blueprint.AttackRunCombatAltitude:0.#}m");
            if (blueprint.Behaviour == AiSimulationPreset.AttackRun3)
                plan.Mutations.Add($"Set Attack Run 3.0 engagement altitude {blueprint.AttackRunEngagementAltitude:0.#}m, prediction {blueprint.AttackRunUsePrediction}, flyover {blueprint.AttackRunFlyover}, ignore altitude {blueprint.AttackRunIgnoreAltitude}");
            plan.Mutations.Add($"Set adjustment clearance land {blueprint.MinimumAltitudeAboveLand:0.#}m, water {blueprint.MinimumAltitudeAboveWater:0.#}m, max {blueprint.MaximumAltitude:0.#}m");
            plan.Mutations.Add($"Set path defaults water depth {blueprint.WaterDepthRequired:0.#}m, land height {blueprint.LandHeightRequired:0.#}m, turning circle {blueprint.TurningCircle:0.#}m");

            if (target == null)
                plan.RoutineCapacity = "Unknown until a focused mainframe is selected.";

            return plan;
        }

        private static void AddCapacity(AIMainframe mainframe, AiBlueprintExportPlan plan)
        {
            try
            {
                uint used = mainframe.Node.Master.Pack.RoutineAvailability.Used;
                uint available = mainframe.Node.Master.Pack.RoutineAvailability.Available;
                plan.HasEnoughRoutineCapacity = used < available;
                plan.RoutineCapacity = $"Routine capacity {used}/{available} used; future writer needs one behaviour slot.";
                if (!plan.HasEnoughRoutineCapacity)
                    plan.Warnings.Add("Routine availability is full. A future writer must select an existing routine or ask before replacing one.");
            }
            catch
            {
                plan.RoutineCapacity = "Routine capacity could not be inspected.";
                plan.Warnings.Add("Could not inspect routine/card capacity on the selected mainframe.");
            }
        }
    }
}
