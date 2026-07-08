using System;
using BrilliantSkies.Core.Logger;
using BrilliantSkies.Ui.Special.InfoStore;
using CombatManager.Input;
using UnityEngine;

namespace CombatManager.Ui
{
    internal sealed class CombatManagerOverlayBehaviour : MonoBehaviour
    {
        private CombatManagerSession _session;

        internal bool Active => _session != null && _session.Active;

        internal void ForceClose()
        {
            _session?.Close();
            _session = null;
        }

        private void Update()
        {
            try
            {
                if (CombatManagerInputState.ToggleDown())
                {
                    Toggle();
                    return;
                }

                if (_session == null)
                    return;

                if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                {
                    ForceClose();
                    return;
                }

                _session.Tick();
            }
            catch (Exception exception)
            {
                AdvLogger.LogException(
                    "[CombatManager] AI sandbox update failed",
                    exception,
                    LogOptions._AlertDevAndCustomerInGame);
                ForceClose();
            }
        }

        private void OnGUI()
        {
            try
            {
                _session?.OnGUI();
            }
            catch (Exception exception)
            {
                AdvLogger.LogException(
                    "[CombatManager] AI sandbox GUI failed",
                    exception,
                    LogOptions._AlertDevAndCustomerInGame);
                ForceClose();
            }
        }

        private void Toggle()
        {
            if (Active)
            {
                ForceClose();
                InfoStore.Add("CombatManager AI sandbox closed.");
                return;
            }

            _session = new CombatManagerSession();
            _session.Begin();
            InfoStore.Add("CombatManager AI sandbox opened.");
        }
    }
}
