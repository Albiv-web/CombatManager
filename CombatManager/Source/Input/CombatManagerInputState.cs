using System;
using System.Reflection;
using Assets.Scripts;
using BrilliantSkies.Core.Constants;
using BrilliantSkies.Ftd.Avatar.Build;
using UnityEngine;

namespace CombatManager.Input
{
    internal static class CombatManagerInputState
    {
        private static readonly FieldInfo ChatGuiInstanceField =
            typeof(ChatGUI).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);

        internal static bool ToggleDown()
        {
            return CanUseHotkeys() &&
                   IsControlHeld() &&
                   IsShiftHeld() &&
                   UnityEngine.Input.GetKeyDown(KeyCode.C);
        }

        internal static bool CanUseHotkeys()
        {
            return AllGameControlsEnabled() &&
                   !IsTextInputActive();
        }

        internal static bool IsInBuildModeWithConstruct()
        {
            try
            {
                cBuild build = cBuild.GetSingleton();
                return build != null &&
                       (build.buildMode == enumBuildMode.active ||
                        build.buildMode == enumBuildMode.activeInventory) &&
                       build.GetC() != null &&
                       build.GetCC() != null;
            }
            catch
            {
                return false;
            }
        }

        internal static bool IsTextInputActive()
        {
            try
            {
                if (ChatGuiInstanceField?.GetValue(null) is ChatGUI chat)
                    return chat.IsTyping();
            }
            catch
            {
            }

            return false;
        }

        private static bool IsControlHeld()
        {
            return UnityEngine.Input.GetKey(KeyCode.LeftControl) ||
                   UnityEngine.Input.GetKey(KeyCode.RightControl);
        }

        private static bool IsShiftHeld()
        {
            return UnityEngine.Input.GetKey(KeyCode.LeftShift) ||
                   UnityEngine.Input.GetKey(KeyCode.RightShift);
        }

        private static bool AllGameControlsEnabled()
        {
            try
            {
                return Get.UserInput.AllGameControlsEnabled;
            }
            catch
            {
                return false;
            }
        }
    }
}
