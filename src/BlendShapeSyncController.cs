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

        protected override void OnReload(GameMode currentGameMode, bool maintainState)
        {
            if (!maintainState)
                LoadFromExtendedData();
            base.OnReload(currentGameMode, maintainState);
        }

        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {
            SaveToExtendedData();
        }

        protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate)
        {
            ApplyCurrentCoordinate();
            base.OnCoordinateBeingLoaded(coordinate);
        }

        internal void UpsertRecord(BlendShapeRecord record)
        {
            Records.RemoveAll(x => x.Coordinate == record.Coordinate &&
                                   x.RendererPath == record.RendererPath &&
                                   x.ShapeName == record.ShapeName);
            Records.Add(record);
        }

        internal void ApplyCurrentCoordinate()
        {
            foreach (var record in Records.Where(x => x.Coordinate == -1 || x.Coordinate == CurrentCoordinateIndex))
                TryApply(record);
        }

        private void TryApply(BlendShapeRecord record)
        {
            var renderer = BlendShapeUtilities.FindRenderer(ChaControl.transform, record);
            if (renderer == null || renderer.sharedMesh == null) return;
            int index = BlendShapeUtilities.FindBlendShapeIndex(renderer, record.ShapeName);
            if (index < 0) return;
            renderer.SetBlendShapeWeight(index, record.Weight);
        }

        private void LoadFromExtendedData()
        {
            Records.Clear();
            var data = ExtendedSave.GetExtendedDataById(ChaControl.chaFile, MakerBlendShapeSyncPlugin.ExtDataKey);
            var bytes = data?.data != null && data.data.TryGetValue("BlendShapeSyncData", out var value) ? value as byte[] : null;
            if (bytes == null) return;
            var unpacked = MessagePackSerializer.Deserialize<BlendShapeSyncData>(bytes);
            if (unpacked?.Records != null)
                Records.AddRange(unpacked.Records);
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
