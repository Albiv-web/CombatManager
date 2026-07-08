using System;
using System.IO;
using System.Reflection;
using BrilliantSkies.Core.Logger;
using BrilliantSkies.Modding;
using CombatManager.Runtime;

namespace CombatManager
{
    public sealed class Plugin : GamePlugin_PostLoad
    {
        public string name => "CombatManager";
        public Version version => new Version(0, 1, 9, 0);

        public void OnLoad()
        {
            try
            {
                CombatManagerRuntimeRegistration.Register();
            }
            catch (Exception exception)
            {
                TryLogException("[CombatManager] Failed to register the AI intent visualizer", exception);
            }

            try
            {
                RegisterActiveStatus();
            }
            catch (Exception exception)
            {
                TryLogException(
                    "[CombatManager] Loaded, but the active-mod status could not be registered",
                    exception);
            }

            TryLogInfo($"[CombatManager] v{version.ToString(3)} loaded.");
        }

        public bool AfterAllPluginsLoaded() => true;

        public void OnSave()
        {
        }

        private void RegisterActiveStatus()
        {
            string modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrWhiteSpace(modPath))
                throw new InvalidOperationException("The installed mod folder could not be resolved.");

            ModProblems.AllModProblems.Remove(modPath);
            ModProblems.AddModProblem(
                $"{name}  v{version.ToString(3)}  Active!",
                modPath,
                string.Empty,
                false);
        }

        private static void TryLogInfo(string message)
        {
            try
            {
                AdvLogger.LogInfo(message);
            }
            catch
            {
            }
        }

        private static void TryLogException(string message, Exception exception)
        {
            try
            {
                AdvLogger.LogException(message, exception, LogOptions._AlertDevInGame);
            }
            catch
            {
            }
        }
    }
}
