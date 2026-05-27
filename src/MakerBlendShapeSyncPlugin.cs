using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI.Chara;
using UnityEngine;

namespace MakerBlendShapeSync
{
    [BepInPlugin(GUID, Name, Version)]
    [BepInDependency("marco.kkapi", "1.35")]
    [BepInDependency("com.bepis.bepinex.extendedsave", "21.1.2")]
    [BepInDependency("com.joan6694.kkplugins.kkpe", BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class MakerBlendShapeSyncPlugin : BaseUnityPlugin
    {
        public const string GUID = "madevil.kks.MakerBlendShapeSync";
        public const string Name = "MakerBlendShapeSync";
        public const string Version = "0.1.0.0";

        internal const string ExtDataKey = "MakerBlendShapeSync";
        internal static ManualLogSource Log;
        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
        }

        private void Start()
        {
            _harmony = Harmony.CreateAndPatchAll(typeof(StudioKkspeBridge), GUID);
            CharacterApi.RegisterExtraBehaviour<BlendShapeSyncController>(ExtDataKey);
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
