using System.Collections.Generic;
using BrilliantSkies.Ai;
using BrilliantSkies.Ai.Interfaces;
using UnityEngine;

namespace CombatManager.Ai
{
    internal sealed class CombatAiSnapshot
    {
        internal MainConstruct Construct { get; set; }
        internal List<MainframeIntentSnapshot> Mainframes { get; } = new List<MainframeIntentSnapshot>();
        internal int SelectedIndex { get; set; }
        internal string Status { get; set; }

        internal MainframeIntentSnapshot SelectedMainframe =>
            SelectedIndex >= 0 && SelectedIndex < Mainframes.Count ? Mainframes[SelectedIndex] : null;
    }

    internal sealed class MainframeIntentSnapshot
    {
        internal string Name { get; set; }
        internal int Priority { get; set; }
        internal MovementType MovementType { get; set; }
        internal FiringType FiringType { get; set; }
        internal string BehaviourName { get; set; }
        internal string BehaviourType { get; set; }
        internal string ManoeuvreName { get; set; }
        internal string ManoeuvreType { get; set; }
        internal Vector3 CraftPosition { get; set; }
        internal Quaternion CraftRotation { get; set; }
        internal Vector3 CraftVelocity { get; set; }
        internal bool HasLiveTarget { get; set; }
        internal bool UsesSandboxTarget { get; set; }
        internal Vector3 TargetPosition { get; set; }
        internal Vector3 TargetVelocity { get; set; }
        internal float TargetRange { get; set; }
        internal float TargetAzimuth { get; set; }
        internal AiIntentPrediction Prediction { get; set; }
        internal List<AiControlRequestSnapshot> Requests { get; } = new List<AiControlRequestSnapshot>();
        internal List<string> Parameters { get; } = new List<string>();
        internal List<string> Warnings { get; } = new List<string>();
    }

    internal sealed class AiIntentPrediction
    {
        internal bool Supported { get; set; }
        internal string Kind { get; set; }
        internal string Summary { get; set; }
        internal Vector3 DesiredPoint { get; set; }
        internal Quaternion DesiredRotation { get; set; }
        internal float MaintainDistanceLower { get; set; }
        internal float MaintainDistanceUpper { get; set; }
        internal float DesiredAngle { get; set; }
        internal bool HasPoint { get; set; }
        internal bool HasFacing { get; set; }
        internal bool Approximate { get; set; }
    }

    internal sealed class AiControlRequestSnapshot
    {
        internal AiControlType Type { get; set; }
        internal float Value { get; set; }
    }
}
