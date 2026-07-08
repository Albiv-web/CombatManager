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
            DrawWindowBackdrop();
            GUI.Label(new Rect(0f, 2f, _window.width, 20f), "CombatManager AI Sandbox", CombatManagerTheme.Title);

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
                _state.ResetScenario();

            if (GUILayout.Button("Import Current AI", CombatManagerTheme.Button, GUILayout.Width(128f)))
            {
                AiSimulationImporter.TryImport(GetFocusedConstruct(), _state, out string message);
                _state.ImportStatus = message;
                _state.ShowImportDetails = true;
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

            CombatManagerGridRenderer.Draw(gridArea, _state);
        }

        private void DrawInspector(Rect rect)
        {
            GUILayout.BeginArea(rect, CombatManagerTheme.Panel);
            _inspectorScroll = GUILayout.BeginScrollView(_inspectorScroll);

            GUILayout.Label("Scenario", CombatManagerTheme.Header);
            GUILayout.BeginHorizontal();
            ScenarioButton("Ship circle", AiScenarioPreset.ShipCircle);
            ScenarioButton("Hover point", AiScenarioPreset.HoverPointAt);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            ScenarioButton("Naval 2.0", AiScenarioPreset.NavalBroadside);
            ScenarioButton("Plane intercept", AiScenarioPreset.PlaneIntercept);
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            GUILayout.Label("Behaviour", CombatManagerTheme.Header);
            GUILayout.Space(4f);

            GUILayout.BeginHorizontal();
            PresetButton("Circle", AiSimulationPreset.Circle);
            PresetButton("Point At", AiSimulationPreset.PointAt);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            PresetButton("Broadside", AiSimulationPreset.Broadside);
            PresetButton("Naval 2.0", AiSimulationPreset.NavalBroadside);
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            DrawTargetControls();

            GUILayout.Space(8f);
            GUILayout.Label("Manoeuvre", CombatManagerTheme.Header);
            GUILayout.Label("Preferred side", CombatManagerTheme.Mini);
            GUILayout.BeginHorizontal();
            SideButton("Auto/Both", AiSimulationSide.Both);
            SideButton("Left", AiSimulationSide.Left);
            SideButton("Right", AiSimulationSide.Right);
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            float radius = SliderRow("Distance / radius", _state.Radius, 25f, 1500f, "m");
            if (!Mathf.Approximately(radius, _state.Radius))
            {
                _state.Radius = radius;
                _state.BroadsideOuterRadius = Mathf.Max(_state.BroadsideOuterRadius, radius + 20f);
                _state.ResetScenario();
            }

            _state.PlaybackSpeed = SliderRow("Playback speed", _state.PlaybackSpeed, 0.1f, 5f, "x");

            if (_state.Preset == AiSimulationPreset.Broadside || _state.Preset == AiSimulationPreset.NavalBroadside)
                _state.BroadsideAngle = SliderRow("Broadside angle", _state.BroadsideAngle, 10f, 170f, "deg");
            if (_state.Preset == AiSimulationPreset.NavalBroadside)
                _state.BroadsideOuterRadius = SliderRow("Leave range", _state.BroadsideOuterRadius, _state.Radius + 20f, 2500f, "m");

            GUILayout.Space(8f);
            GUILayout.Label("Craft", CombatManagerTheme.Header);
            GUILayout.Label("Craft profile", CombatManagerTheme.Mini);
            GUILayout.BeginHorizontal();
            CraftProfileButton("Ship", AiCraftProfile.SurfaceShip);
            CraftProfileButton("Hover", AiCraftProfile.Hovercraft);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            CraftProfileButton("Plane", AiCraftProfile.Airplane);
            CraftProfileButton("Fast plane", AiCraftProfile.FastAircraft);
            GUILayout.EndHorizontal();
            GUILayout.Space(4f);
            GUILayout.Label("Movement card model", CombatManagerTheme.Mini);
            GUILayout.BeginHorizontal();
            MovementModelButton("Ship", AiCraftMovementModel.ShipOrTank);
            MovementModelButton("Hover", AiCraftMovementModel.HoverSixAxis);
            MovementModelButton("Plane", AiCraftMovementModel.Airplane);
            GUILayout.EndHorizontal();
            _state.CraftSpeed = SliderRow("Max speed", _state.CraftSpeed, 1f, 160f, "m/s");
            _state.CraftTurnRate = SliderRow("Turn rate", _state.CraftTurnRate, 5f, 240f, "deg/s");
            _state.CraftAcceleration = SliderRow("Acceleration", _state.CraftAcceleration, 1f, 80f, "m/s2");
            if (_state.CraftMovementModel == AiCraftMovementModel.ShipOrTank)
                _state.ShipTarryDistance = SliderRow("Tarry distance", _state.ShipTarryDistance, 0f, 120f, "m");
            if (_state.CraftMovementModel == AiCraftMovementModel.HoverSixAxis)
                _state.HoverStrafeAuthority = SliderRow("Strafe authority", _state.HoverStrafeAuthority, 0.1f, 1f, "x");
            if (_state.CraftMovementModel == AiCraftMovementModel.Airplane)
                _state.AirplaneMinimumSpeed = SliderRow("Minimum speed", _state.AirplaneMinimumSpeed, 0f, 120f, "m/s");
            if (GUILayout.Button("Reset Craft", CombatManagerTheme.Button))
                _state.ResetCraft();

            GUILayout.Space(10f);
            GUILayout.Label("Visuals", CombatManagerTheme.Header);
            _state.GridZoom = SliderRow("Zoom", _state.GridZoom, 0.5f, 3f, "x");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Fit Orbit", CombatManagerTheme.Button))
                _state.GridZoom = 1f;
            _state.ShowTrail = ToggleButton("Show Trail", _state.ShowTrail);
            _state.ShowDesiredTrail = ToggleButton("AI Trail", _state.ShowDesiredTrail);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            _state.ShowRawSteer = ToggleButton("Raw Steer", _state.ShowRawSteer);
            _state.ShowMotionPoint = ToggleButton("Motion Point", _state.ShowMotionPoint);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            _state.ShowTargetPath = ToggleButton("Target Path", _state.ShowTargetPath);
            _state.ShowLegend = ToggleButton("Legend", _state.ShowLegend);
            GUILayout.EndHorizontal();

            GUILayout.Space(10f);
            GUILayout.Label("Status", CombatManagerTheme.Header);
            AiSimulationFrame frame = _state.BuildFrame();
            GUILayout.Label(frame.Summary, CombatManagerTheme.BodyWrap);
            GUILayout.Label($"Range {frame.GroundRange:0.#}m | Azimuth {frame.Azimuth:0.#} deg", CombatManagerTheme.BodyWrap);
            GUILayout.Label($"Craft: {frame.CraftProfile} | Movement card: {frame.CraftMovementModel}", CombatManagerTheme.BodyWrap);
            if (frame.HasRawSteerPoint && frame.HasMotionPoint)
                GUILayout.Label($"Raw steer {PlanarMath.GroundDistance(frame.CraftPosition, frame.RawSteerPoint):0.#}m | motion point {PlanarMath.GroundDistance(frame.CraftPosition, frame.MotionPoint):0.#}m", CombatManagerTheme.BodyWrap);
            if (frame.ReversePreferred)
                GUILayout.Label("Movement note: reverse preferred for this intent.", CombatManagerTheme.Warning);
            GUILayout.Label($"{frame.Kind}: {frame.AiState}{(frame.Approximate ? " (approximated)" : string.Empty)}", frame.Approximate ? CombatManagerTheme.Warning : CombatManagerTheme.BodyWrap);
            if (!string.IsNullOrWhiteSpace(frame.ApproximationNote))
                GUILayout.Label(frame.ApproximationNote, frame.Approximate ? CombatManagerTheme.Warning : CombatManagerTheme.Mini);
            GUILayout.Label(_state.ImportStatus, CombatManagerTheme.Warning);

            GUILayout.Space(8f);
            DrawImportDrawer();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void PresetButton(string label, AiSimulationPreset preset)
        {
            GUIStyle style = _state.Preset == preset ? CombatManagerTheme.ActiveButton : CombatManagerTheme.Button;
            if (GUILayout.Button(label, style))
                _state.SetPreset(preset);
        }

        private void ScenarioButton(string label, AiScenarioPreset preset)
        {
            GUIStyle style = _state.ScenarioPreset == preset ? CombatManagerTheme.ActiveButton : CombatManagerTheme.Button;
            if (GUILayout.Button(label, style))
                _state.ApplyScenarioPreset(preset);
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

        private void DrawTargetControls()
        {
            GUILayout.Label("Target", CombatManagerTheme.Header);
            GUILayout.BeginHorizontal();
            TargetProfileButton("Static", AiTargetProfile.Static);
            TargetProfileButton("Slow", AiTargetProfile.SlowMover);
            TargetProfileButton("Ship", AiTargetProfile.Ship);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            TargetProfileButton("Fast", AiTargetProfile.FastMover);
            TargetProfileButton("Plane", AiTargetProfile.Plane);
            GUILayout.EndHorizontal();

            GUILayout.Label("Path mode", CombatManagerTheme.Mini);
            GUILayout.BeginHorizontal();
            PathModeButton("Straight", AiTargetPathMode.Straight);
            PathModeButton("Orbit", AiTargetPathMode.Orbit);
            PathModeButton("S-Curve", AiTargetPathMode.SCurve);
            GUILayout.EndHorizontal();

            _state.TargetSpeed = SliderRow("Target speed", _state.TargetSpeed, 0f, 140f, "m/s");
            _state.TargetTurnRate = SliderRow("Turn rate", _state.TargetTurnRate, 0f, 20f, "deg/s");
            float targetAltitude = SliderRow("Altitude", _state.TargetAltitude, 0f, 800f, "m");
            if (!Mathf.Approximately(targetAltitude, _state.TargetAltitude))
                _state.SetTargetAltitude(targetAltitude);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset Target Path", CombatManagerTheme.Button))
                _state.ResetTargetPath();
            if (GUILayout.Button("Reset Scenario", CombatManagerTheme.Button))
                _state.ResetScenario();
            GUILayout.EndHorizontal();
        }

        private void TargetProfileButton(string label, AiTargetProfile profile)
        {
            GUIStyle style = _state.TargetProfile == profile ? CombatManagerTheme.ActiveButton : CombatManagerTheme.Button;
            if (GUILayout.Button(label, style))
                _state.SetTargetProfile(profile);
        }

        private void PathModeButton(string label, AiTargetPathMode mode)
        {
            GUIStyle style = _state.TargetPathMode == mode ? CombatManagerTheme.ActiveButton : CombatManagerTheme.Button;
            if (GUILayout.Button(label, style))
            {
                _state.TargetPathMode = mode;
                _state.ResetTargetPath();
            }
        }

        private void MovementModelButton(string label, AiCraftMovementModel model)
        {
            GUIStyle style = _state.CraftMovementModel == model ? CombatManagerTheme.ActiveButton : CombatManagerTheme.Button;
            if (GUILayout.Button(label, style))
            {
                _state.CraftMovementModel = model;
                _state.ResetCraft();
            }
        }

        private void CraftProfileButton(string label, AiCraftProfile profile)
        {
            GUIStyle style = _state.CraftProfile == profile ? CombatManagerTheme.ActiveButton : CombatManagerTheme.Button;
            if (GUILayout.Button(label, style))
                _state.SetCraftProfile(profile);
        }

        private void DrawImportDrawer()
        {
            GUILayout.Label("Import", CombatManagerTheme.Header);
            LabelPair("Mainframe", _state.ImportedMainframe);
            LabelPair("Behaviour", _state.ImportedBehaviour);
            LabelPair("Manoeuvre", _state.ImportedManoeuvre);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh Mainframes", CombatManagerTheme.Button))
            {
                AiSimulationImporter.RefreshCandidates(GetFocusedConstruct(), _state);
                _state.ShowImportDetails = true;
            }
            if (GUILayout.Button("Import Selected", CombatManagerTheme.Button))
            {
                AiSimulationImporter.TryImport(GetFocusedConstruct(), _state, _state.SelectedImportIndex, out string message);
                _state.ImportStatus = message;
                _state.ShowImportDetails = true;
            }
            GUILayout.EndHorizontal();

            string label = _state.ShowImportDetails ? "Hide Import Details" : "Show Import Details";
            if (GUILayout.Button(label, CombatManagerTheme.Button))
                _state.ShowImportDetails = !_state.ShowImportDetails;

            if (!_state.ShowImportDetails)
                return;

            GUILayout.Space(4f);
            GUILayout.Label("Mainframe selector", CombatManagerTheme.Mini);
            if (_state.ImportCandidates.Count == 0)
            {
                GUILayout.Label("Refresh while focused on a craft to list mainframes.", CombatManagerTheme.BodyWrap);
            }
            else
            {
                foreach (AiImportCandidate candidate in _state.ImportCandidates)
                {
                    GUIStyle style = _state.SelectedImportIndex == candidate.Index ? CombatManagerTheme.SelectedRow : CombatManagerTheme.Row;
                    string prefix = candidate.Supported ? "" : "[unsupported] ";
                    if (GUILayout.Button($"{prefix}{candidate.Index}: {candidate.MainframeName}  P{candidate.Priority}", style))
                        _state.SelectedImportIndex = candidate.Index;
                    GUILayout.Label($"{candidate.Summary} | move {candidate.MovementType} | fire {candidate.FiringType}", candidate.Supported ? CombatManagerTheme.Mini : CombatManagerTheme.Warning);
                }
            }

            GUILayout.Space(6f);
            GUILayout.Label("Parameters", CombatManagerTheme.Mini);
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
            GUILayout.Label("Requests", CombatManagerTheme.Mini);
            if (_state.ImportedRequests.Count == 0)
            {
                GUILayout.Label("No imported control requests.", CombatManagerTheme.BodyWrap);
            }
            else
            {
                foreach (AiControlRequestSnapshot request in _state.ImportedRequests)
                    GUILayout.Label($"{request.Type}: {request.Value:0.00}", CombatManagerTheme.Body);
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

        private static bool ToggleButton(string label, bool value)
        {
            GUIStyle style = value ? CombatManagerTheme.ActiveButton : CombatManagerTheme.Button;
            return GUILayout.Button(label, style) ? !value : value;
        }

        private void DrawWindowBackdrop()
        {
            Color old = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(0f, 0f, _window.width, _window.height), CombatManagerTheme.WindowTexture);
            GUI.color = old;
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
