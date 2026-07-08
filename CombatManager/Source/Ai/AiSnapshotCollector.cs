using System;
using System.Collections.Generic;
using BrilliantSkies.Ai;
using BrilliantSkies.Ai.Interfaces;
using BrilliantSkies.Ai.Modules;
using BrilliantSkies.Ai.Modules.Behaviour;
using BrilliantSkies.Ai.Modules.Behaviour.Examples;
using BrilliantSkies.Ai.Modules.Manoeuvre;
using BrilliantSkies.Ai.Targetting;
using UnityEngine;

namespace CombatManager.Ai
{
    internal static class AiSnapshotCollector
    {
        internal static CombatAiSnapshot Capture(
            MainConstruct construct,
            int selectedIndex,
            bool useSandboxTarget,
            Vector3 sandboxTargetPosition)
        {
            var snapshot = new CombatAiSnapshot
            {
                Construct = construct,
                Status = construct == null ? "No focused construct." : null
            };

            if (construct == null)
                return snapshot;

            List<AIMainframe> mainframes = construct.iBlockTypeStorage.MainframeStore.CopyToList();
            if (mainframes.Count == 0)
            {
                snapshot.Status = "No AI mainframes on the focused construct.";
                snapshot.SelectedIndex = -1;
                return snapshot;
            }

            selectedIndex = Mathf.Clamp(selectedIndex, 0, mainframes.Count - 1);
            snapshot.SelectedIndex = selectedIndex;
            for (int i = 0; i < mainframes.Count; i++)
            {
                AIMainframe mainframe = mainframes[i];
                MainframeIntentSnapshot mainframeSnapshot =
                    CaptureMainframe(mainframe, i == selectedIndex, useSandboxTarget, sandboxTargetPosition);
                snapshot.Mainframes.Add(mainframeSnapshot);
            }

            return snapshot;
        }

        private static MainframeIntentSnapshot CaptureMainframe(
            AIMainframe mainframe,
            bool selected,
            bool useSandboxTarget,
            Vector3 sandboxTargetPosition)
        {
            var result = new MainframeIntentSnapshot
            {
                Name = mainframe?.Node?.Name ?? "AI mainframe",
                BehaviourName = "None",
                BehaviourType = "None",
                ManoeuvreName = "None",
                ManoeuvreType = "None",
                Prediction = new AiIntentPrediction
                {
                    Supported = false,
                    Kind = "Unavailable",
                    Summary = "AI mainframe is not linked to a node."
                }
            };

            if (mainframe?.Node?.Master == null)
            {
                result.Warnings.Add("Mainframe node is not available.");
                return result;
            }

            AiMaster master = mainframe.Node.Master;
            IPlatformInterface platform = master.PlatformInterface;
            result.Priority = master.Priority;
            result.MovementType = master.MovementType;
            result.FiringType = master.FiringType;
            result.CraftPosition = platform?.CentreOfMass ?? mainframe.Node.GetOurConstructablePosition();
            result.CraftRotation = platform?.SafeRotation ?? mainframe.Node.GetOurRotation();
            result.CraftVelocity = platform?.iVelocities?.VelocityVector ?? mainframe.Node.GetOurVelocity();

            IBehaviour behaviour = null;
            IManoeuvre manoeuvre = null;
            bool hasBehaviour = master.Pack.GetSelectedBehaviour(out behaviour);
            bool hasManoeuvre = master.Pack.GetSelectedManoeuvre(out manoeuvre);

            result.BehaviourName = RoutineName(behaviour);
            result.BehaviourType = behaviour?.GetType().Name ?? "None";
            result.ManoeuvreName = RoutineName(manoeuvre);
            result.ManoeuvreType = manoeuvre?.GetType().Name ?? "None";

            TargetPositionInfo target = mainframe.Node.GetTargetPositionInfoForEngagementTarget();
            result.HasLiveTarget = target.Valid;
            if (!target.Valid && useSandboxTarget)
            {
                target = new TargetPositionInfo(
                    sandboxTargetPosition,
                    result.CraftPosition,
                    result.CraftRotation * Vector3.forward,
                    result.CraftRotation * Vector3.right,
                    Vector3.zero);
                result.UsesSandboxTarget = true;
            }

            if (target.Valid)
            {
                result.TargetPosition = target.Position;
                result.TargetVelocity = target.Velocity;
                result.TargetRange = target.Range;
                result.TargetAzimuth = target.Azimuth;
            }
            else
            {
                result.Warnings.Add("No live target. Drag the sandbox target on the grid to preview intent.");
            }

            if (!hasBehaviour)
                result.Warnings.Add("No selected behaviour routine.");
            if (!hasManoeuvre)
                result.Warnings.Add("No selected manoeuvre routine.");
            if (master.MovementType != MovementType.Automatic)
                result.Warnings.Add("Movement mode is not Automatic.");

            AddParameters(result, behaviour, manoeuvre);
            AddControlRequests(result, platform);

            if (selected)
            {
                result.Prediction = AiIntentPredictor.Predict(
                    behaviour,
                    result.CraftPosition,
                    result.CraftRotation,
                    target,
                    platform);
            }

            return result;
        }

        private static string RoutineName(object routine)
        {
            if (routine == null)
                return "None";

            if (routine is AiBaseAbstract aiBase)
            {
                string name = aiBase.OurId.Us.Name;
                if (!string.IsNullOrWhiteSpace(name))
                    return name;
            }

            return routine.GetType().Name;
        }

        private static void AddParameters(
            MainframeIntentSnapshot result,
            IBehaviour behaviour,
            IManoeuvre manoeuvre)
        {
            if (behaviour is FtdNaval naval)
            {
                result.Parameters.Add($"enter broadside <= {naval.BroadsideDistance.Lower:0.#}m");
                result.Parameters.Add($"leave broadside >= {naval.BroadsideDistance.Upper:0.#}m");
                result.Parameters.Add($"nominal angle {naval.NominalBroadsideAngle.Us:0.#} deg");
                result.Parameters.Add($"minimum distance {naval.MinimumBroadsideDistanceToMaintain.Us:0.#}m");
            }
            else if (behaviour is BehaviourBroadside broadside)
            {
                result.Parameters.Add($"angle {broadside.AngleToMaintain.Us:0.#} deg");
                result.Parameters.Add($"distance {broadside.DistanceToMaintain.Lower:0.#}-{broadside.DistanceToMaintain.Upper:0.#}m");
            }
            else if (behaviour is BehaviourPointAndMaintainDistance pointAt)
            {
                result.Parameters.Add($"distance {pointAt.DistanceToMaintain.Us:0.#}m");
                result.Parameters.Add($"altitude {pointAt.AltitudeType.Us} {pointAt.PreferredAltitude.Us:0.#}m");
                result.Parameters.Add($"reverse allowed before {pointAt.AzimuthBeforeReverse.Us:0.#} deg");
            }
            else if (behaviour is BehaviourCircleAtDistance circle)
            {
                result.Parameters.Add($"distance {circle.DistanceToMaintain.Us:0.#}m");
                result.Parameters.Add($"side {circle.PreferredSide.Us}");
                result.Parameters.Add($"minimum approach {circle.MinApproachAngle.Us:0.#} deg");
                result.Parameters.Add($"altitude {circle.AltitudeType.Us} {circle.PreferredAltitude.Us:0.#}m");
            }

            if (manoeuvre != null)
            {
                result.Parameters.Add($"manoeuvre {manoeuvre.GetType().Name}");
                result.Parameters.Add($"wander {manoeuvre.WanderDistance.Us:0.#}m");
            }
        }

        private static void AddControlRequests(MainframeIntentSnapshot result, IPlatformInterface platform)
        {
            if (platform == null)
                return;

            foreach (AiControlType type in Enum.GetValues(typeof(AiControlType)))
            {
                if (type == AiControlType.NumberOfEntries)
                    continue;

                float value = platform.GetRequest(type);
                if (Mathf.Abs(value) <= 0.001f)
                    continue;

                result.Requests.Add(new AiControlRequestSnapshot
                {
                    Type = type,
                    Value = value
                });
            }
        }
    }
}
