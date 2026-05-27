using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        internal static string GetDisplayName(Transform root, SkinnedMeshRenderer renderer)
        {
            if (renderer == null) return "";
            string path = GetRelativePath(root, renderer.transform);
            string leaf = renderer.transform != null ? renderer.transform.name : renderer.name;
            string scope = GetDisplayScope(path, renderer);
            return string.IsNullOrEmpty(scope) ? leaf : scope + "/" + leaf;
        }

        internal static void EatInputInRect(Rect rect)
        {
            TryCallKkapiEatInputInRect(rect);

            var evt = Event.current;
            if (evt == null || !rect.Contains(evt.mousePosition))
                return;

            switch (evt.type)
            {
                case EventType.MouseDown:
                case EventType.MouseDrag:
                case EventType.MouseUp:
                case EventType.ScrollWheel:
                    Input.ResetInputAxes();
                    evt.Use();
                    break;
            }
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

        private static string GetDisplayScope(string path, SkinnedMeshRenderer renderer)
        {
            string lowerPath = (path ?? "").ToLowerInvariant();
            string lowerRenderer = renderer == null ? "" : (renderer.name ?? "").ToLowerInvariant();
            string lowerMesh = renderer?.sharedMesh == null ? "" : (renderer.sharedMesh.name ?? "").ToLowerInvariant();
            string combined = lowerPath + "/" + lowerRenderer + "/" + lowerMesh;

            string accSlot = TryGetAccessorySlotScope(path);
            if (!string.IsNullOrEmpty(accSlot))
                return accSlot;
            if (combined.Contains("accessory") || combined.Contains("/acs") || combined.Contains("_acs"))
                return "acc";

            if (combined.Contains("face") || combined.Contains("head") || combined.Contains("cf_o_face") ||
                combined.Contains("cf_o_nose") || combined.Contains("cf_o_mouth") || combined.Contains("cf_o_ey") ||
                combined.Contains("cf_o_cha") || combined.Contains("cf_o_tooth") || combined.Contains("cf_o_tang"))
                return "face";

            string clothes = TryGetClothesScope(combined);
            if (!string.IsNullOrEmpty(clothes))
                return clothes;

            if (combined.Contains("body") || combined.Contains("cf_o_body") || combined.Contains("p_cf_body"))
                return "body";

            if (!string.IsNullOrEmpty(path))
                return path.Split('/').FirstOrDefault();
            return "";
        }

        private static string TryGetClothesScope(string combined)
        {
            var markers = new[]
            {
                new { Key = "ct_top", Label = "Top" },
                new { Key = "top", Label = "Top" },
                new { Key = "ct_bot", Label = "Bottom" },
                new { Key = "bottom", Label = "Bottom" },
                new { Key = "bot", Label = "Bottom" },
                new { Key = "bra", Label = "Bra" },
                new { Key = "shorts", Label = "Shorts" },
                new { Key = "short", Label = "Shorts" },
                new { Key = "gloves", Label = "Gloves" },
                new { Key = "glove", Label = "Gloves" },
                new { Key = "panst", Label = "Pantyhose" },
                new { Key = "pantyhose", Label = "Pantyhose" },
                new { Key = "socks", Label = "Socks" },
                new { Key = "sock", Label = "Socks" },
                new { Key = "shoes", Label = "Shoes" },
                new { Key = "shoe", Label = "Shoes" }
            };

            foreach (var marker in markers)
            {
                if (combined.Contains(marker.Key))
                    return marker.Label;
            }
            return "";
        }

        private static string TryGetAccessorySlotScope(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            string lower = path.ToLowerInvariant();
            int marker = lower.IndexOf("slot");
            if (marker < 0)
                marker = lower.IndexOf("accessory");
            if (marker < 0)
                marker = lower.IndexOf("acs");
            if (marker < 0)
                return "";

            for (int i = marker; i < lower.Length; i++)
            {
                if (!char.IsDigit(lower[i])) continue;
                int start = i;
                while (i < lower.Length && char.IsDigit(lower[i]))
                    i++;
                string digits = lower.Substring(start, i - start);
                if (int.TryParse(digits, out int slot))
                    return "accslot" + (slot + 1);
                return "accslot" + digits;
            }
            return "acc";
        }

        private static void TryCallKkapiEatInputInRect(Rect rect)
        {
            var type = typeof(KKAPI.KoikatuAPI).Assembly.GetType("KKAPI.Utilities.IMGUIUtils");
            var method = type?.GetMethod("EatInputInRect", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null) return;
            method.Invoke(null, new object[] { rect });
        }
    }
}
