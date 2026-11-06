using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace ValheimRadioMod
{
    public static class MouseScrollPatch
    {
        public static float LastScrollDelta { get; set; }

        public static void ApplyPatch(Harmony harmony)
        {
            try
            {
                Type zInputType = AccessTools.TypeByName("ZInput");
                if (zInputType == null) return;

                MethodInfo original = AccessTools.Method(zInputType, "GetMouseScrollWheel");
                if (original == null) return;

                MethodInfo prefix = AccessTools.Method(typeof(MouseScrollPatch), nameof(Prefix));
                harmony.Patch(original, prefix: new HarmonyMethod(prefix));

                Jotunn.Logger.LogInfo("Mouse scroll patch applied");
            }
            catch (Exception e)
            {
                Jotunn.Logger.LogError($"Failed to patch mouse scroll: {e.Message}");
            }
        }

        public static bool Prefix(ref float __result)
        {
            LastScrollDelta = Input.GetAxis("Mouse ScrollWheel");

            // Consume scroll if any radio is currently being hovered
            // (we rely on the fact that GetHoverText was called recently)
            if (LastScrollDelta != 0f)
            {
                // Simple approach: always consume if we have a non-zero scroll and a radio exists
                // Better: track hovered radios (see note below)
                __result = 0f;
                return false;
            }
            return true;
        }
    }
}