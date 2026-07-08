using System;
using System.Collections.Generic;
using BrilliantSkies.Ai;
using BrilliantSkies.Ai.Interfaces;
using BrilliantSkies.Ai.Modules;
using BrilliantSkies.Ai.Modules.Behaviour;
using BrilliantSkies.Ai.Modules.Manoeuvre;
using BrilliantSkies.Ai.Targetting;
using UnityEngine;

namespace CombatManager.Ai
{
    internal static class AiLiveParityCollector
    {
        internal static AiLiveParitySnapshot Capture(MainConstruct construct, int selectedMainframeIndex)
        {
            var snapshot = new AiLiveParitySnapshot
            {
                HasFocusedConstruct = construct != null,
                Status = construct == null ? "Live Parity is on, but no focused construct is available." : "Live Parity captured."
            };

            if (construct == null)
            {
                snapshot.Warnings.Add("Focus a craft with an AI mainframe to compare observed vs predicted requests.");
                return snapshot;
            }

            List<AIMainframe> mainframes = construct.iBlockTypeStorage.MainframeStore.CopyToList();
            if (mainframes.Count == 0)
            {
                snapshot.Status = "Focused construct has no AI mainframes.";
                snapshot.Warnings.Add("No mainframe found.");
                return snapshot;
            }

            int index = selectedMainframeIndex >= 0 && selectedMainframeIndex < mainframes.Count ? selectedMainframeIndex : 0;
            AIMainframe mainframe = mainframes[index];
            snapshot.HasMainframe = true;
            snapshot.MainframeName = mainframe?.Node?.Name ?? $"Mainframe {index}";
            if (mainframe?.Node?.Master == null)
            {
                snapshot.Status = "Selected mainframe is not linked to an AI master.";
                snapshot.Warnings.Add("Mainframe node/master unavailable.");
                return snapshot;
            }

            AiMaster master = mainframe.Node.Master;
            IPlatformInterface platform = master.PlatformInterface;
            master.Pack.GetSelectedBehaviour(out IBehaviour behaviour);
            master.Pack.GetSelectedManoeuvre(out IManoeuvre manoeuvre);
            snapshot.BehaviourType = behaviour?.GetType().Name ?? "None";
            snapshot.ManoeuvreType = manoeuvre?.GetType().Name ?? "None";

            Vector3 craftPosition = platform?.CentreOfMass ?? mainframe.Node.GetOurConstructablePosition();
            Quaternion craftRotation = platform?.SafeRotation ?? mainframe.Node.GetOurRotation();
            Vector3 craftVelocity = platform?.iVelocities?.VelocityVector ?? mainframe.Node.GetOurVelocity();
            TargetPositionInfo target = mainframe.Node.GetTargetPositionInfoForEngagementTarget();
            snapshot.HasTarget = target.Valid;

            if (!target.Valid)
            {
                snapshot.Status = $"Live Parity: {snapshot.MainframeName} has no live target.";
                snapshot.Warnings.Add("No engagement target. Prediction is skipped.");
                AddObservedRequests(snapshot, platform);
                return snapshot;
            }

            AddObservedRequests(snapshot, platform);
            snapshot.PredictedIntent = AiVanillaPredictor.PredictIntent(
                behaviour,
                craftPosition,
                craftRotation,
                craftVelocity,
                target,
                platform);

            foreach (string warning in snapshot.PredictedIntent.Warnings)
                snapshot.Warnings.Add(warning);

            snapshot.PredictedRequests.AddRange(AiVanillaPredictor.PredictRequests(
                manoeuvre,
                snapshot.PredictedIntent,
                craftPosition,
                craftRotation,
                craftVelocity));
            snapshot.RequestDeltas.AddRange(AiVanillaPredictor.CompareRequests(
                snapshot.ObservedRequests,
                snapshot.PredictedRequests));

            if (snapshot.PredictedIntent.Approximate)
                snapshot.Warnings.Add("Intent includes approximation flags; pathfinding/firing-angle/terrain/PID may differ from vanilla.");
            if (!snapshot.PredictedIntent.Supported)
                snapshot.Warnings.Add("Unsupported behaviour: requests are observed only.");

            snapshot.Status = $"Live Parity: {snapshot.MainframeName} | {snapshot.BehaviourType} / {snapshot.ManoeuvreType}";
            return snapshot;
        }

        private static void AddObservedRequests(AiLiveParitySnapshot snapshot, IPlatformInterface platform)
        {
            if (platform == null)
            {
                snapshot.Warnings.Add("Platform interface unavailable, so observed requests cannot be read.");
                return;
            }

            foreach (AiControlType type in Enum.GetValues(typeof(AiControlType)))
            {
                if (type == AiControlType.NumberOfEntries)
                    continue;

                float value = platform.GetRequest(type);
                if (Mathf.Abs(value) <= 0.001f)
                    continue;

                snapshot.ObservedRequests.Add(new AiControlRequestSnapshot
                {
                    Type = type,
                    Value = value
                });
            }
        }
    }
}
