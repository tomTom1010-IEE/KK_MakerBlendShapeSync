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
                var poseController = FindKkspePoseController(ociChar);
                if (poseController == null)
                {
                    MakerBlendShapeSyncPlugin.Log?.LogDebug("KKSPE PoseController was not found for OCIChar.");
                    return;
                }

                var poseControllerType = poseController.GetType();
                var field = poseControllerType.GetField("_blendShapesEditor", BindingFlags.NonPublic | BindingFlags.Instance);
                var editor = field?.GetValue(poseController);
                if (editor == null) return;

                var editorType = editor.GetType();
                editorType.GetMethod("RefreshSkinnedMeshRendererList", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(editor, null);
                editorType.GetMethod("ApplyBlendShapeWeights", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(editor, null);
                MakerBlendShapeSyncPlugin.Log?.LogDebug("Refreshed KKSPE BlendShapesEditor.");
            }
            catch (Exception ex)
            {
                MakerBlendShapeSyncPlugin.Log?.LogDebug($"KKSPE bridge refresh failed: {ex.Message}");
            }
        }

        private static object FindKkspePoseController(OCIChar ociChar)
        {
            var poseControllerType = Type.GetType("HSPE.PoseController, KKSPE");
            if (poseControllerType == null) return null;

            var controllersField = poseControllerType.GetField("_poseControllers", BindingFlags.NonPublic | BindingFlags.Static);
            var controllers = controllersField?.GetValue(null) as System.Collections.IEnumerable;
            if (controllers == null) return null;

            foreach (var controller in controllers)
            {
                var target = poseControllerType.GetProperty("target", BindingFlags.Public | BindingFlags.Instance)?.GetValue(controller, null);
                if (target == null) continue;
                var targetType = target.GetType();
                var targetOciChar = targetType.GetField("ociChar", BindingFlags.Public | BindingFlags.Instance)?.GetValue(target) as OCIChar;
                if (ReferenceEquals(targetOciChar, ociChar))
                    return controller;
            }
            return null;
        }
    }
}
