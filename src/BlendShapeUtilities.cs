using System.Collections.Generic;
using UnityEngine;

namespace MakerBlendShapeSync
{
    internal static class BlendShapeUtilities
    {
        internal static IEnumerable<SkinnedMeshRenderer> EnumerateRenderers(Transform root)
        {
            return root == null ? new SkinnedMeshRenderer[0] : root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        }

        internal static string GetRelativePath(Transform root, Transform target)
        {
            if (root == null || target == null) return "";
            var parts = new Stack<string>();
            var cur = target;
            while (cur != null && cur != root)
            {
                parts.Push(cur.name);
                cur = cur.parent;
            }
            return string.Join("/", parts.ToArray());
        }

        internal static SkinnedMeshRenderer FindRenderer(Transform root, BlendShapeRecord record)
        {
            foreach (var renderer in EnumerateRenderers(root))
            {
                if (!string.IsNullOrEmpty(record.RendererPath) && GetRelativePath(root, renderer.transform) == record.RendererPath)
                    return renderer;
                if (renderer.name == record.RendererName && renderer.sharedMesh != null && renderer.sharedMesh.name == record.MeshName)
                    return renderer;
            }
            return null;
        }

        internal static int FindBlendShapeIndex(SkinnedMeshRenderer renderer, string shapeName)
        {
            if (renderer?.sharedMesh == null) return -1;
            for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
            {
                if (renderer.sharedMesh.GetBlendShapeName(i) == shapeName)
                    return i;
            }
            return -1;
        }
    }
}
