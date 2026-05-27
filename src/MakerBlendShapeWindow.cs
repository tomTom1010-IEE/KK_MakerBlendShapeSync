using System.Collections.Generic;
using System.Linq;
using KKAPI.Maker;
using UnityEngine;

namespace MakerBlendShapeSync
{
    internal sealed class MakerBlendShapeWindow : MonoBehaviour
    {
        private ChaControl _chaCtrl;
        private BlendShapeSyncController _controller;
        private readonly List<SkinnedMeshRenderer> _renderers = new List<SkinnedMeshRenderer>();
        private SkinnedMeshRenderer _selectedRenderer;
        private Rect _windowRect = new Rect(120f, 80f, 760f, 620f);
        private int _windowId;
        private Vector2 _rendererScroll;
        private Vector2 _shapeScroll;
        private string _rendererSearch = "";
        private string _shapeSearch = "";
        private bool _saveGlobal;
        private GUIStyle _selectedStyle;
        private GUIStyle _savedStyle;

        private int TargetCoordinate => _saveGlobal ? -1 : (_chaCtrl?.fileStatus?.coordinateType ?? 0);

        private void Awake()
        {
            enabled = false;
            DontDestroyOnLoad(this);
            _windowId = GUIUtility.GetControlID(FocusType.Passive);
        }

        private void OnEnable()
        {
            RefreshCharacter();
            RefreshRendererList();
        }

        private void OnGUI()
        {
            if (!MakerAPI.InsideMaker) { enabled = false; return; }
            if (_chaCtrl == null || _controller == null)
                RefreshCharacter();
            if (_chaCtrl == null || _controller == null) { enabled = false; return; }

            if (_selectedStyle == null)
            {
                _selectedStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };
                _selectedStyle.normal.textColor = Color.cyan;
                _selectedStyle.hover.textColor = Color.cyan;
                _savedStyle = new GUIStyle(GUI.skin.button);
                _savedStyle.normal.textColor = Color.magenta;
                _savedStyle.hover.textColor = Color.magenta;
            }

            _windowRect = GUILayout.Window(_windowId, _windowRect, DrawWindow, "Maker Blend Shapes");
        }

        private void RefreshCharacter()
        {
            _chaCtrl = MakerAPI.GetCharacterControl();
            _controller = _chaCtrl == null ? null : _chaCtrl.GetComponent<BlendShapeSyncController>();
        }

        private void RefreshRendererList()
        {
            _renderers.Clear();
            if (_chaCtrl == null) return;
            _renderers.AddRange(BlendShapeUtilities.EnumerateRenderers(_chaCtrl.transform)
                .Where(x => x != null && x.sharedMesh != null && x.sharedMesh.blendShapeCount > 0)
                .OrderBy(x => BlendShapeUtilities.GetRelativePath(_chaCtrl.transform, x.transform)));

            if (_selectedRenderer == null || !_renderers.Contains(_selectedRenderer))
                _selectedRenderer = _renderers.FirstOrDefault();
        }

        private void DrawWindow(int id)
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh", GUILayout.Width(70f))) RefreshRendererList();
            _saveGlobal = GUILayout.Toggle(_saveGlobal, " Body/global", GUILayout.Width(105f));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Apply", GUILayout.Width(60f))) _controller.ApplyCurrentCoordinate();
            if (GUILayout.Button("X", GUILayout.Width(26f)))
            {
                enabled = false;
                MakerBlendShapeSyncPlugin.MakerSidebarToggle?.SetValue(false, false);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            DrawRendererPanel();
            DrawShapePanel();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0f, 0f, _windowRect.width, 22f));
        }

        private void DrawRendererPanel()
        {
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(260f), GUILayout.ExpandHeight(true));
            GUILayout.Label("Renderers");
            _rendererSearch = GUILayout.TextField(_rendererSearch);
            _rendererScroll = GUILayout.BeginScrollView(_rendererScroll);

            foreach (var renderer in _renderers)
            {
                string path = BlendShapeUtilities.GetRelativePath(_chaCtrl.transform, renderer.transform);
                if (!Matches(path, _rendererSearch) && !Matches(renderer.name, _rendererSearch))
                    continue;

                string label = string.IsNullOrEmpty(path) ? renderer.name : path;
                label += $" ({renderer.sharedMesh.blendShapeCount})";
                var style = renderer == _selectedRenderer ? _selectedStyle : GUI.skin.button;
                if (GUILayout.Button(label, style, GUILayout.ExpandWidth(true)))
                    _selectedRenderer = renderer;
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawShapePanel()
        {
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandHeight(true));
            GUILayout.Label(_selectedRenderer == null ? "Blend Shapes" : _selectedRenderer.name);
            _shapeSearch = GUILayout.TextField(_shapeSearch);

            if (_selectedRenderer == null || _selectedRenderer.sharedMesh == null)
            {
                GUILayout.Box("", GUILayout.ExpandHeight(true));
                GUILayout.EndVertical();
                return;
            }

            string rendererPath = BlendShapeUtilities.GetRelativePath(_chaCtrl.transform, _selectedRenderer.transform);
            _shapeScroll = GUILayout.BeginScrollView(_shapeScroll);

            for (int i = 0; i < _selectedRenderer.sharedMesh.blendShapeCount; i++)
            {
                string shapeName = _selectedRenderer.sharedMesh.GetBlendShapeName(i);
                if (!Matches(shapeName, _shapeSearch))
                    continue;
                DrawShapeRow(rendererPath, i, shapeName);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawShapeRow(string rendererPath, int index, string shapeName)
        {
            int coordinate = TargetCoordinate;
            var record = _controller.GetRecord(coordinate, rendererPath, shapeName);
            bool saved = record != null;
            float current = _selectedRenderer.GetBlendShapeWeight(index);

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label(shapeName, saved ? _savedStyle : GUI.skin.label, GUILayout.Width(220f));
            GUILayout.Label(current.ToString("F1"), GUILayout.Width(48f));
            if (GUILayout.Button("Save", GUILayout.Width(48f)))
                SaveRecord(rendererPath, shapeName, current);
            if (GUILayout.Button("Del", GUILayout.Width(40f)))
            {
                _controller.RemoveRecord(coordinate, rendererPath, shapeName);
                _controller.ApplyCurrentCoordinate();
            }
            if (GUILayout.Button("0", GUILayout.Width(28f)))
            {
                _selectedRenderer.SetBlendShapeWeight(index, 0f);
                SaveRecord(rendererPath, shapeName, 0f);
            }
            GUILayout.EndHorizontal();

            float next = GUILayout.HorizontalSlider(current, -100f, 200f);
            if (Mathf.Abs(next - current) > 0.001f)
            {
                _selectedRenderer.SetBlendShapeWeight(index, next);
                SaveRecord(rendererPath, shapeName, next);
            }
            GUILayout.EndVertical();
        }

        private void SaveRecord(string rendererPath, string shapeName, float weight)
        {
            if (_selectedRenderer == null || _selectedRenderer.sharedMesh == null) return;
            _controller.UpsertRecord(new BlendShapeRecord
            {
                Coordinate = TargetCoordinate,
                RendererPath = rendererPath,
                RendererName = _selectedRenderer.name,
                MeshName = _selectedRenderer.sharedMesh.name,
                ShapeName = shapeName,
                Weight = weight
            });
        }

        private static bool Matches(string value, string search)
        {
            return string.IsNullOrEmpty(search) ||
                   (!string.IsNullOrEmpty(value) && value.ToLowerInvariant().Contains(search.ToLowerInvariant()));
        }
    }
}
