using System;
using System.Collections.Generic;
using MessagePack;

namespace MakerBlendShapeSync
{
    [Serializable]
    [MessagePackObject(true)]
    public sealed class BlendShapeRecord
    {
        public int Coordinate { get; set; } = -1;
        public string RendererPath { get; set; } = "";
        public string RendererName { get; set; } = "";
        public string MeshName { get; set; } = "";
        public string ShapeName { get; set; } = "";
        public float Weight { get; set; }
    }

    [Serializable]
    [MessagePackObject(true)]
    public sealed class BlendShapeSyncData
    {
        public List<BlendShapeRecord> Records { get; set; } = new List<BlendShapeRecord>();
    }
}
