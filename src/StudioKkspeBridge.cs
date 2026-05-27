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
        [HarmonyPostfix]
        [HarmonyPatch(typeof(OCIChar), "OnLoadClothesFile")]
        private static void OCIChar_OnLoadClothesFile_Postfix(OCIChar __instance)
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
