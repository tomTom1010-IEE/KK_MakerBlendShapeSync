using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExtensibleSaveFormat;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using MessagePack;
using UnityEngine;

namespace MakerBlendShapeSync
{
    public sealed class BlendShapeSyncController : CharaCustomFunctionController
    {
        internal readonly List<BlendShapeRecord> Records = new List<BlendShapeRecord>();
        public int CurrentCoordinateIndex => ChaControl.fileStatus.coordinateType;
        private ChaFile LoadedChaFile =>
            MakerAPI.InsideMaker && MakerAPI.LastLoadedChaFile != null
                ? MakerAPI.LastLoadedChaFile
                : ChaControl.chaFile;

        protected override void OnReload(GameMode currentGameMode, bool maintainState)
        {
            if (!maintainState)
            {
                LoadFromExtendedData();
                StartCoroutine(DelayedApply());
            }
            base.OnReload(currentGameMode, maintainState);
        }

        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {
            SaveToExtendedData();
        }

        protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate)
        {
            StartCoroutine(DelayedApply());
            base.OnCoordinateBeingLoaded(coordinate);
        }

        private IEnumerator DelayedApply()
        {
            yield return null;
            yield return null;
            ApplyCurrentCoordinate();
        }

        internal void UpsertRecord(BlendShapeRecord record)
        {
            Records.RemoveAll(x => x.Coordinate == record.Coordinate &&
                                   x.RendererPath == record.RendererPath &&
                                   x.ShapeName == record.ShapeName);
            Records.Add(record);
        }

        internal void RemoveRecord(int coordinate, string rendererPath, string shapeName)
        {
            Records.RemoveAll(x => x.Coordinate == coordinate &&
                                   x.RendererPath == rendererPath &&
                                   x.ShapeName == shapeName);
        }

        internal BlendShapeRecord GetRecord(int coordinate, string rendererPath, string shapeName)
        {
            return Records.FirstOrDefault(x => x.Coordinate == coordinate &&
                                               x.RendererPath == rendererPath &&
                                               x.ShapeName == shapeName);
        }

        internal void ApplyCurrentCoordinate()
        {
            MakerBlendShapeSyncPlugin.Log?.LogDebug($"Applying {Records.Count} blendshape record(s) for {ChaControl?.name}");
            foreach (var record in Records.Where(x => x.Coordinate == -1 || x.Coordinate == CurrentCoordinateIndex))
                TryApply(record);
        }

        private void TryApply(BlendShapeRecord record)
        {
            var renderer = BlendShapeUtilities.FindRenderer(ChaControl.transform, record);
            if (renderer == null || renderer.sharedMesh == null)
            {
                MakerBlendShapeSyncPlugin.Log?.LogDebug($"Renderer not found for blendshape {record.RendererPath} / {record.RendererName}");
                return;
            }

            int index = BlendShapeUtilities.FindBlendShapeIndex(renderer, record.ShapeName);
            if (index < 0)
            {
                MakerBlendShapeSyncPlugin.Log?.LogDebug($"Blendshape not found: {record.ShapeName} on {renderer.name}");
                return;
            }

            renderer.SetBlendShapeWeight(index, record.Weight);
        }

        private void LoadFromExtendedData()
        {
            Records.Clear();
            var data = ExtendedSave.GetExtendedDataById(LoadedChaFile, MakerBlendShapeSyncPlugin.ExtDataKey);
            var bytes = data?.data != null && data.data.TryGetValue("BlendShapeSyncData", out var value) ? value as byte[] : null;
            if (bytes == null)
            {
                MakerBlendShapeSyncPlugin.Log?.LogDebug("No MakerBlendShapeSync ExtendedData found.");
                return;
            }

            var unpacked = MessagePackSerializer.Deserialize<BlendShapeSyncData>(bytes);
            if (unpacked?.Records != null)
                Records.AddRange(unpacked.Records);
            MakerBlendShapeSyncPlugin.Log?.LogDebug($"Loaded {Records.Count} blendshape record(s) from ExtendedData.");
        }

        private void SaveToExtendedData()
        {
            if (Records.Count == 0)
            {
                SetExtendedData(null);
                return;
            }

            var data = new PluginData { version = 1 };
            data.data["BlendShapeSyncData"] = MessagePackSerializer.Serialize(new BlendShapeSyncData { Records = Records });
            SetExtendedData(data);
        }
    }
}
