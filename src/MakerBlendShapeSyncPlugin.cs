using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Maker.UI.Sidebar;
using KKAPI.Studio;
using UniRx;
using UnityEngine;

namespace MakerBlendShapeSync
{
    [BepInPlugin(GUID, Name, Version)]
    [BepInDependency("marco.kkapi", "1.35")]
    [BepInDependency("com.bepis.bepinex.extendedsave")]
    [BepInDependency("com.joan6694.kkplugins.kkpe", BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class MakerBlendShapeSyncPlugin : BaseUnityPlugin
    {
        public const string GUID = "tomtom.kks.makerblendshapesync";
        public const string Name = "MakerBlendShapeSync";
        public const string Version = "0.1.0.0";

        internal const string ExtDataKey = "MakerBlendShapeSync";
        internal static ManualLogSource Log;
        internal static MakerBlendShapeWindow MakerWindow;
        internal static SidebarToggle MakerSidebarToggle;
        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
        }

        private void Start()
        {
            CharacterApi.RegisterExtraBehaviour<BlendShapeSyncController>(ExtDataKey);
            InitMakerUi();
            InitStudioBridge();
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }

        private void InitStudioBridge()
        {
            if (!StudioAPI.InsideStudio)
                return;

            _harmony = new Harmony(GUID);
            StudioPoseEditorBridge.Init(_harmony);
        }

        private void InitMakerUi()
        {
            MakerAPI.RegisterCustomSubCategories += (sender, args) =>
            {
                MakerWindow = gameObject.AddComponent<MakerBlendShapeWindow>();
                MakerSidebarToggle = args.AddSidebarControl(new SidebarToggle("BlendShapes", false, this));
                MakerSidebarToggle.ValueChanged.Subscribe(value =>
                {
                    if (MakerWindow != null)
                        MakerWindow.enabled = value;
                });
            };

            MakerAPI.MakerExiting += (sender, args) =>
            {
                if (MakerWindow != null)
                    Destroy(MakerWindow);
                MakerWindow = null;
                MakerSidebarToggle = null;
            };
        }
    }
}


