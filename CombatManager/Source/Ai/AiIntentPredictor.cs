using BrilliantSkies.Ai.Interfaces;
using BrilliantSkies.Ai.Targetting;
using UnityEngine;

namespace CombatManager.Ai
{
    internal static class AiIntentPredictor
    {
        internal static AiIntentPrediction Predict(
            object behaviour,
            Vector3 craftPosition,
            Quaternion craftRotation,
            TargetPositionInfo target,
            IPlatformInterface platform)
        {
            AiVanillaIntentPlan plan = AiVanillaPredictor.PredictIntent(
                behaviour,
                craftPosition,
                craftRotation,
                Vector3.zero,
                target,
                platform);
            return AiVanillaPredictor.ToLegacyPrediction(plan);
        }
    }
}
