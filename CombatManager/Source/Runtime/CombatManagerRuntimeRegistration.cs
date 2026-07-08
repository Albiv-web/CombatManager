using System;
using BrilliantSkies.Core.Logger;
using CombatManager.Ui;
using UnityEngine;

namespace CombatManager.Runtime
{
    internal static class CombatManagerRuntimeRegistration
    {
        private const string HostName = "CombatManager.AiIntentVisualizer";
        private static GameObject _host;
        private static CombatManagerOverlayBehaviour _behaviour;
        private static bool _registered;

        internal static void Register()
        {
            if (_registered)
                return;

            _host = new GameObject(HostName);
            UnityEngine.Object.DontDestroyOnLoad(_host);
            _behaviour = _host.AddComponent<CombatManagerOverlayBehaviour>();
            _registered = true;
        }

        internal static void ForceClose()
        {
            try
            {
                _behaviour?.ForceClose();
            }
            catch (Exception exception)
            {
                AdvLogger.LogException(
                    "[CombatManager] Failed to close the AI intent visualizer",
                    exception,
                    LogOptions._AlertDevInGame);
            }
        }
    }
}
