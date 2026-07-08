using BrilliantSkies.Ftd.Avatar.Build;
using CombatManager.Ai;
using UnityEngine;

namespace CombatManager.Ui
{
    internal sealed class CombatManagerSession
    {
        private const float MinWindowWidth = 720f;
        private const float MinWindowHeight = 480f;
        private const float SimulationStepSeconds = 0.5f;

        private readonly AiSimulationState _state = new AiSimulationState();
        private Rect _window;
        private bool _windowInitialised;
        private bool _active;
        private bool _resizing;
        private Vector2 _inspectorScroll;

        internal bool Active => _active;

        internal void Begin()
        {
            _active = true;
            _state.Reset();
        }

        internal void Close()
        {
            _active = false;
        }

        internal void Tick()
        {
            if (!_active || !_state.Playing)
                return;

            _state.Step(Time.unscaledDeltaTime);
        }

        internal void OnGUI()
        {
            if (!_active)
                return;

            CombatManagerTheme.Ensure();
            EnsureWindow();
            ClampWindow();
            _window = GUI.Window(483210, _window, DrawWindow, "CombatManager AI Sandbox", CombatManagerTheme.Window);
        }

        private void EnsureWindow()
        {
            if (_windowInitialised)
                return;

            float width = Mathf.Clamp(Screen.width * 0.8f, MinWindowWidth, Mathf.Max(MinWindowWidth, Screen.width - 40f));
            float height = Mathf.Clamp(Screen.height * 0.8f, MinWindowHeight, Mathf.Max(MinWindowHeight, Screen.height - 40f));
            _window = new Rect(
                (Screen.width - width) * 0.5f,
                (Screen.height - height) * 0.5f,
                width,
                height);
            _windowInitialised = true;
        }

        private void ClampWindow()
        {
            _window.width = Mathf.Clamp(_window.width, MinWindowWidth, Mathf.Max(MinWindowWidth, Screen.width - 40f));
            _window.height = Mathf.Clamp(_window.height, MinWindowHeight, Mathf.Max(MinWindowHeight, Screen.height - 40f));
            _window.x = Mathf.Clamp(_window.x, 0f, Mathf.Max(0f, Screen.width - _window.width));
            _window.y = Mathf.Clamp(_window.y, 0f, Mathf.Max(0f, Screen.height - _window.height));
        }

        private static MainConstruct GetFocusedConstruct()
        {
            try
            {
                return cBuild.GetSingleton()?.GetC()?.Main;
            }
            catch
            {
                return null;
            }
        }

        private void DrawWindow(int id)
        {
            Rect client = new Rect(10f, 26f, _window.width - 20f, _window.height - 36f);
            Rect toolbar = new Rect(client.x, client.y, client.width, 30f);
            Rect body = new Rect(client.x, toolbar.yMax + 8f, client.width, client.height - toolbar.height - 8f);

            DrawToolbar(toolbar);
            DrawBody(body);
            DrawResizeHandle();

            GUI.DragWindow(new Rect(0f, 0f, _window.width - 26f, 22f));
        }

        private void DrawToolbar(Rect rect)
        {
            GUILayout.BeginArea(rect);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Standalone sandbox", CombatManagerTheme.Header, GUILayout.Width(190f));

            if (GUILayout.Button(_state.Playing ? "Pause" : "Play", _state.Playing ? CombatManagerTheme.ActiveButton : CombatManagerTheme.Button, GUILayout.Width(74f)))
                _state.Playing = !_state.Playing;

            if (GUILayout.Button("Step", CombatManagerTheme.Button, GUILayout.Width(62f)))
            {
                _state.Playing = false;
                _state.Step(SimulationStepSeconds);
            }

            if (GUILayout.Button("Reset", CombatManagerTheme.Button, GUILayout.Width(62f)))
                _state.Reset();

            if (GUILayout.Button("Import Current AI", CombatManagerTheme.Button, GUILayout.Width(128f)))
            {
                AiSimulationImporter.TryImport(GetFocusedConstruct(), _state, out string message);
                _state.ImportStatus = message;
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button(_state.ShowInspector ? "Hide Inspector" : "Show Inspector", CombatManagerTheme.Button, GUILayout.Width(112f)))
                _state.ShowInspector = !_state.ShowInspector;
            GUILayout.Label("Ctrl+Shift+C toggles", CombatManagerTheme.Mini, GUILayout.Width(132f));
            if (GUILayout.Button("Close", CombatManagerTheme.Button, GUILayout.Width(64f)))
                Close();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawBody(Rect rect)
        {
            float inspectorWidth = _state.ShowInspector ? Mathf.Min(330f, rect.width * 0.42f) : 0f;
            Rect inspector = new Rect(rect.x, rect.y, inspectorWidth, rect.height);
            Rect gridArea = _state.ShowInspector
                ? new Rect(inspector.xMax + 8f, rect.y, rect.width - inspectorWidth - 8f, rect.height)
                : rect;

            if (_state.ShowInspector)
                DrawInspector(inspector);

            Rect grid = SquareInside(gridArea);
            CombatManagerGridRenderer.Draw(grid, _state);

            if (gridArea.height - grid.height > 24f)
            {
                Rect status = new Rect(gridArea.x, grid.yMax + 6f, gridArea.width, 20f);
                GUI.Label(status, _state.ImportStatus, CombatManagerTheme.Mini);
            }
        }

        private void DrawInspector(Rect rect)
        {
            GUILayout.BeginArea(rect, CombatManagerTheme.Panel);
            _inspectorScroll = GUILayout.BeginScrollView(_inspectorScroll);

            GUILayout.Label("Sandbox controls", CombatManagerTheme.Header);
            GUILayout.Space(4f);

            GUILayout.Label("Behaviour preset", CombatManagerTheme.Mini);
            GUILayout.BeginHorizontal();
            PresetButton("Circle", AiSimulationPreset.Circle);
            PresetButton("Point At", AiSimulationPreset.PointAt);
            PresetButton("Broadside", AiSimulationPreset.Broadside);
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            GUILayout.Label("Preferred side", CombatManagerTheme.Mini);
            GUILayout.BeginHorizontal();
            SideButton("Both", AiSimulationSide.Both);
            SideButton("Left", AiSimulationSide.Left);
            SideButton("Right", AiSimulationSide.Right);
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            float radius = SliderRow("Distance / radius", _state.Radius, 25f, 1500f, "m");
            if (!Mathf.Approximately(radius, _state.Radius))
            {
                _state.Radius = radius;
                _state.Reset();
            }

            _state.CraftSpeed = SliderRow("Craft speed", _state.CraftSpeed, 1f, 120f, "m/s");
            _state.PlaybackSpeed = SliderRow("Playback speed", _state.PlaybackSpeed, 0.1f, 5f, "x");

            if (_state.Preset == AiSimulationPreset.Broadside)
                _state.BroadsideAngle = SliderRow("Broadside angle", _state.BroadsideAngle, 10f, 170f, "deg");

            GUILayout.Space(10f);
            GUILayout.Label("Simulation status", CombatManagerTheme.Header);
            GUILayout.Label(_state.BuildFrame().Summary, CombatManagerTheme.BodyWrap);
            GUILayout.Label(_state.ImportStatus, CombatManagerTheme.Warning);

            GUILayout.Space(8f);
            GUILayout.Label("Imported AI snapshot", CombatManagerTheme.Header);
            LabelPair("Mainframe", _state.ImportedMainframe);
            LabelPair("Behaviour", _state.ImportedBehaviour);

            if (_state.ImportedParameters.Count == 0)
            {
                GUILayout.Label("No imported behaviour parameters.", CombatManagerTheme.BodyWrap);
            }
            else
            {
                foreach (string parameter in _state.ImportedParameters)
                    GUILayout.Label(parameter, CombatManagerTheme.Body);
            }

            GUILayout.Space(6f);
            GUILayout.Label("Imported requests", CombatManagerTheme.Header);
            if (_state.ImportedRequests.Count == 0)
            {
                GUILayout.Label("No imported control requests.", CombatManagerTheme.BodyWrap);
            }
            else
            {
                foreach (AiControlRequestSnapshot request in _state.ImportedRequests)
                    GUILayout.Label($"{request.Type}: {request.Value:0.00}", CombatManagerTheme.Body);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void PresetButton(string label, AiSimulationPreset preset)
        {
            GUIStyle style = _state.Preset == preset ? CombatManagerTheme.ActiveButton : CombatManagerTheme.Button;
            if (GUILayout.Button(label, style))
                _state.SetPreset(preset);
        }

        private void SideButton(string label, AiSimulationSide side)
        {
            GUIStyle style = _state.Side == side ? CombatManagerTheme.ActiveButton : CombatManagerTheme.Button;
            if (GUILayout.Button(label, style))
            {
                _state.Side = side;
                _state.Reset();
            }
        }

        private static float SliderRow(string label, float value, float min, float max, string suffix)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, CombatManagerTheme.Body, GUILayout.Width(118f));
            float adjusted = GUILayout.HorizontalSlider(value, min, max, GUILayout.MinWidth(80f));
            GUILayout.Label($"{adjusted:0.#}{suffix}", CombatManagerTheme.Mini, GUILayout.Width(62f));
            GUILayout.EndHorizontal();
            return adjusted;
        }

        private static Rect SquareInside(Rect rect)
        {
            float size = Mathf.Max(180f, Mathf.Min(rect.width, rect.height));
            return new Rect(
                rect.x + (rect.width - size) * 0.5f,
                rect.y + (rect.height - size) * 0.5f,
                size,
                size);
        }

        private void DrawResizeHandle()
        {
            Rect handle = new Rect(_window.width - 22f, _window.height - 22f, 18f, 18f);
            GUI.Label(handle, "///", CombatManagerTheme.Mini);

            Event current = Event.current;
            if (current.type == EventType.MouseDown && current.button == 0 && handle.Contains(current.mousePosition))
            {
                _resizing = true;
                current.Use();
            }

            if (_resizing && current.type == EventType.MouseDrag)
            {
                _window.width = Mathf.Clamp(current.mousePosition.x + 8f, MinWindowWidth, Mathf.Max(MinWindowWidth, Screen.width - 40f));
                _window.height = Mathf.Clamp(current.mousePosition.y + 8f, MinWindowHeight, Mathf.Max(MinWindowHeight, Screen.height - 40f));
                current.Use();
            }

            if (current.rawType == EventType.MouseUp)
                _resizing = false;
        }

        private static void LabelPair(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, CombatManagerTheme.Mini, GUILayout.Width(70f));
            GUILayout.Label(string.IsNullOrWhiteSpace(value) ? "--" : value, CombatManagerTheme.BodyWrap);
            GUILayout.EndHorizontal();
        }
    }
}
