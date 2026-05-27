using System;
using System.Reflection;
using HarmonyLib;
using KKAPI.Studio;
using Studio;
using UnityEngine;

namespace MakerBlendShapeSync
{
    internal static class StudioKkspeBridge
    {
        internal static void Init(Harmony harmony)
        {
            var loadClothes = AccessTools.Method(typeof(OCIChar), "LoadClothesFile", new[] { typeof(string) });
            if (loadClothes != null)
            {
                harmony.Patch(loadClothes,
                    postfix: new HarmonyMethod(typeof(StudioKkspeBridge), nameof(OCIChar_LoadClothesFile_Postfix)));
            }
            else
            {
                MakerBlendShapeSyncPlugin.Log?.LogWarning("Could not patch OCIChar.LoadClothesFile; KKSPE bridge clothing refresh is disabled.");
            }

            var setCoordinate = AccessTools.Method(typeof(OCIChar), "SetCoordinateInfo",
                new[] { typeof(ChaFileDefine.CoordinateType), typeof(bool) });
            if (setCoordinate != null)
            {
                harmony.Patch(setCoordinate,
                    postfix: new HarmonyMethod(typeof(StudioKkspeBridge), nameof(OCIChar_SetCoordinateInfo_Postfix)));
            }
        }

        private static void OCIChar_LoadClothesFile_Postfix(OCIChar __instance)
        {
            ApplyAndRefreshKkspe(__instance);
        }

        private static void OCIChar_SetCoordinateInfo_Postfix(OCIChar __instance)
        {
            ApplyAndRefreshKkspe(__instance);
        }

        internal static void ApplyAndRefreshKkspe(OCIChar ociChar)
        {
            var ctrl = ociChar?.charInfo?.gameObject.GetComponent<BlendShapeSyncController>();
            if (ctrl == null) return;
            ctrl.ApplyCurrentCoordinate();
            TryRefreshKkspeBlendShapes(ociChar);
        }

        private static void TryRefreshKkspeBlendShapes(OCIChar ociChar)
        {
            if (!StudioAPI.InsideStudio || ociChar == null) return;

            try
            {
                var poseControllerType = Type.GetType("HSPE.PoseController, KKSPE");
                if (poseControllerType == null) return;

                foreach (var component in ociChar.charInfo.GetComponents<Component>())
                {
                    if (!poseControllerType.IsInstanceOfType(component)) continue;
                    var field = poseControllerType.GetField("_blendShapesEditor", BindingFlags.NonPublic | BindingFlags.Instance);
                    var editor = field?.GetValue(component);
                    if (editor == null) return;

                    var editorType = editor.GetType();
                    editorType.GetMethod("RefreshSkinnedMeshRendererList", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(editor, null);
                    editorType.GetMethod("ApplyBlendShapeWeights", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(editor, null);
                    return;
                }
            }
            catch (Exception ex)
            {
                MakerBlendShapeSyncPlugin.Log?.LogDebug($"KKSPE bridge refresh failed: {ex.Message}");
            }
        }
    }
}

