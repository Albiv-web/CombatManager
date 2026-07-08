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
            _window = GUI.Window(483210, _window, DrawWindow, "CombatManager AI Duel Sandbox", CombatManagerTheme.Window);
        }

        private void EnsureWindow()
        {
            if (_windowInitialised)
                return;

            float width = Mathf.Clamp(Screen.width * 0.8f, MinWindowWidth, Mathf.Max(MinWindowWidth, Screen.width - 40f));
            float height = Mathf.Clamp(Screen.height * 0.8f, MinWindowHeight, Mathf.Max(MinWindowHeight, Screen.height - 40f));
            _window = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
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
            GUI.Label(new Rect(0f, 2f, _window.width, 20f), "CombatManager AI Duel Sandbox", CombatManagerTheme.Title);

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
            GUILayout.Label("Symmetric duel", CombatManagerTheme.Header, GUILayout.Width(190f));

            if (GUILayout.Button(_state.Playing ? "Pause" : "Play", _state.Playing ? CombatManagerTheme.ActiveButton : CombatManagerTheme.Button, GUILayout.Width(74f)))
                _state.Playing = !_state.Playing;

            if (GUILayout.Button("Step", CombatManagerTheme.Button, GUILayout.Width(62f)))
            {
                _state.Playing = false;
                _state.Step(SimulationStepSeconds);
            }

            if (GUILayout.Button("Reset", CombatManagerTheme.Button, GUILayout.Width(62f)))
                _state.ResetScenario();

            if (GUILayout.Button("Import Blue AI", CombatManagerTheme.Button, GUILayout.Width(128f)))
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
            float inspectorWidth = _state.ShowInspector ? Mathf.Min(360f, rect.width * 0.45f) : 0f;
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

            DrawScenarioControls();
            GUILayout.Space(8f);
            DrawEntityControls(_state.Blue, "Blue Mainframe");
            GUILayout.Space(8f);
            DrawEntityControls(_state.Red, "Red Mainframe");
            GUILayout.Space(8f);
            DrawVisualControls();
            GUILayout.Space(8f);
            DrawStatusAndWarnings();
            GUILayout.Space(8f);
            DrawImportDrawer();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawScenarioControls()
        {
            GUILayout.Label("Scenario", CombatManagerTheme.Header);
            GUILayout.BeginHorizontal();
            ScenarioButton("Ship Duel", AiScenarioPreset.ShipDuel);
            ScenarioButton("Broadside", AiScenarioPreset.BroadsideDuel);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            ScenarioButton("Hover Duel", AiScenarioPreset.HoverDuel);
            ScenarioButton("Plane Intercept", AiScenarioPreset.PlaneIntercept);
            GUILayout.EndHorizontal();
            _state.PlaybackSpeed = SliderRow("Playback speed", _state.PlaybackSpeed, 0.1f, 5f, "x");
        }

        private void DrawEntityControls(AiSimEntity entity, string header)
        {
            GUILayout.Label(header, CombatManagerTheme.Header);
            GUILayout.Label($"{AiSimulationState.PresetName(entity.Preset)} | {AiSimulationState.CraftProfileName(entity.CraftProfile)} | {AiSimulationState.CraftMovementModelName(entity.CraftMovementModel)}", CombatManagerTheme.Mini);

            GUILayout.Label("Behaviour card", CombatManagerTheme.Mini);
            GUILayout.BeginHorizontal();
            EntityPresetButton(entity, "Circle", AiSimulationPreset.Circle);
            EntityPresetButton(entity, "Point At", AiSimulationPreset.PointAt);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            EntityPresetButton(entity, "Broadside", AiSimulationPreset.Broadside);
            EntityPresetButton(entity, "Naval 2.0", AiSimulationPreset.NavalBroadside);
            GUILayout.EndHorizontal();

            GUILayout.Label("Preferred side", CombatManagerTheme.Mini);
            GUILayout.BeginHorizontal();
            EntitySideButton(entity, "Auto/Both", AiSimulationSide.Both);
            EntitySideButton(entity, "Left", AiSimulationSide.Left);
            EntitySideButton(entity, "Right", AiSimulationSide.Right);
            GUILayout.EndHorizontal();

            float radius = SliderRow("Distance / radius", entity.Radius, 25f, 1500f, "m");
            if (!Mathf.Approximately(radius, entity.Radius))
            {
                entity.Radius = radius;
                entity.BroadsideOuterRadius = Mathf.Max(entity.BroadsideOuterRadius, radius + 20f);
                _state.ResetScenario();
            }

            if (entity.Preset == AiSimulationPreset.Broadside || entity.Preset == AiSimulationPreset.NavalBroadside)
                entity.BroadsideAngle = SliderRow("Broadside angle", entity.BroadsideAngle, 10f, 170f, "deg");
            if (entity.Preset == AiSimulationPreset.NavalBroadside)
                entity.BroadsideOuterRadius = SliderRow("Leave range", entity.BroadsideOuterRadius, entity.Radius + 20f, 2500f, "m");

            GUILayout.Label("Craft profile", CombatManagerTheme.Mini);
            GUILayout.BeginHorizontal();
            EntityCraftProfileButton(entity, "Ship", AiCraftProfile.SurfaceShip);
            EntityCraftProfileButton(entity, "Hover", AiCraftProfile.Hovercraft);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            EntityCraftProfileButton(entity, "Six-axis", AiCraftProfile.SixAxisDrone);
            EntityCraftProfileButton(entity, "Plane", AiCraftProfile.Airplane);
            EntityCraftProfileButton(entity, "Fast", AiCraftProfile.FastAircraft);
            GUILayout.EndHorizontal();

            GUILayout.Label("Movement card model", CombatManagerTheme.Mini);
            GUILayout.BeginHorizontal();
            EntityMovementModelButton(entity, "Ship", AiCraftMovementModel.ShipOrTank);
            EntityMovementModelButton(entity, "Hover", AiCraftMovementModel.Hover);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            EntityMovementModelButton(entity, "Six-axis", AiCraftMovementModel.SixAxis);
            EntityMovementModelButton(entity, "Plane", AiCraftMovementModel.Airplane);
            GUILayout.EndHorizontal();

            entity.CraftSpeed = SliderRow("Max speed", entity.CraftSpeed, 0f, 180f, "m/s");
            entity.CraftTurnRate = SliderRow("Turn rate", entity.CraftTurnRate, 5f, 240f, "deg/s");
            entity.CraftAcceleration = SliderRow("Acceleration", entity.CraftAcceleration, 1f, 100f, "m/s2");
            float altitude = SliderRow("Altitude", entity.Altitude, 0f, 900f, "m");
            if (!Mathf.Approximately(altitude, entity.Altitude))
            {
                entity.Altitude = altitude;
                entity.Position = new Vector3(entity.Position.x, entity.Altitude, entity.Position.z);
            }

            if (entity.CraftMovementModel == AiCraftMovementModel.ShipOrTank)
                entity.ShipTarryDistance = SliderRow("Tarry distance", entity.ShipTarryDistance, 0f, 200f, "m");
            if (entity.CraftMovementModel == AiCraftMovementModel.Hover)
            {
                entity.HoverYawLockDistance = SliderRow("Yaw lock", entity.HoverYawLockDistance, 0f, 1000f, "m");
                entity.HoverMoveWithinAzimuth = SliderRow("Move within azi", entity.HoverMoveWithinAzimuth, 0f, 180f, "deg");
            }
            if (entity.CraftMovementModel == AiCraftMovementModel.SixAxis)
                entity.SixAxisLookAheadDistance = SliderRow("Look ahead", entity.SixAxisLookAheadDistance, 10f, 5000f, "m");
            if (entity.CraftMovementModel == AiCraftMovementModel.Airplane)
            {
                entity.AirplaneMinimumSpeed = SliderRow("Minimum speed", entity.AirplaneMinimumSpeed, 0f, 140f, "m/s");
                entity.AirplaneBankingTurnAbove = SliderRow("Bank above", entity.AirplaneBankingTurnAbove, 0f, 90f, "deg");
                entity.AirplaneBankingTurnRoll = SliderRow("Bank roll", entity.AirplaneBankingTurnRoll, 0f, 90f, "deg");
            }
        }

        private void DrawVisualControls()
        {
            GUILayout.Label("Visuals", CombatManagerTheme.Header);
            _state.GridZoom = SliderRow("Zoom", _state.GridZoom, 0.5f, 3f, "x");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Fit Duel", CombatManagerTheme.Button))
                _state.GridZoom = 1f;
            _state.ShowTrail = ToggleButton("Trails", _state.ShowTrail);
            _state.ShowDesiredTrail = ToggleButton("AI Trails", _state.ShowDesiredTrail);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            _state.ShowRawSteer = ToggleButton("Raw Steer", _state.ShowRawSteer);
            _state.ShowMotionPoint = ToggleButton("Motion Point", _state.ShowMotionPoint);
            _state.ShowLegend = ToggleButton("Legend", _state.ShowLegend);
            GUILayout.EndHorizontal();
        }

        private void DrawStatusAndWarnings()
        {
            GUILayout.Label("Warnings", CombatManagerTheme.Header);
            AiDuelFrame frame = _state.BuildDuelFrame();
            DrawFrameStatus(frame.Blue);
            DrawFrameStatus(frame.Red);
            GUILayout.Label("Simulation is read-only and approximates movement-card PID, propulsion, pathfinding, terrain, water, and firing-angle internals.", CombatManagerTheme.Warning);
        }

        private static void DrawFrameStatus(AiSimulationFrame frame)
        {
            GUILayout.Label($"{frame.EntityName}: {frame.Kind} | {frame.AiState}", CombatManagerTheme.BodyWrap);
            GUILayout.Label($"range {frame.GroundRange:0.#}m | azimuth {frame.Azimuth:0.#} deg | {frame.CraftMovementModel}", CombatManagerTheme.Mini);
            if (!string.IsNullOrWhiteSpace(frame.ApproximationNote))
                GUILayout.Label(frame.ApproximationNote, CombatManagerTheme.Mini);
        }

        private void ScenarioButton(string label, AiScenarioPreset preset)
        {
            GUIStyle style = _state.ScenarioPreset == preset ? CombatManagerTheme.ActiveButton : CombatManagerTheme.Button;
            if (GUILayout.Button(label, style))
                _state.ApplyScenarioPreset(preset);
        }

        private void EntityPresetButton(AiSimEntity entity, string label, AiSimulationPreset preset)
        {
            GUIStyle style = entity.Preset == preset ? CombatManagerTheme.ActiveButton : CombatManagerTheme.Button;
            if (GUILayout.Button(label, style))
            {
                entity.Preset = preset;
                _state.ResetScenario();
            }
        }

        private void EntitySideButton(AiSimEntity entity, string label, AiSimulationSide side)
        {
            GUIStyle style = entity.Side == side ? CombatManagerTheme.ActiveButton : CombatManagerTheme.Button;
            if (GUILayout.Button(label, style))
            {
                entity.Side = side;
                _state.ResetScenario();
            }
        }

        private void EntityCraftProfileButton(AiSimEntity entity, string label, AiCraftProfile profile)
        {
            GUIStyle style = entity.CraftProfile == profile ? CombatManagerTheme.ActiveButton : CombatManagerTheme.Button;
            if (GUILayout.Button(label, style))
            {
                entity.ApplyCraftProfile(profile);
                _state.ResetScenario();
            }
        }

        private void EntityMovementModelButton(AiSimEntity entity, string label, AiCraftMovementModel model)
        {
            GUIStyle style = entity.CraftMovementModel == model ? CombatManagerTheme.ActiveButton : CombatManagerTheme.Button;
            if (GUILayout.Button(label, style))
            {
                entity.CraftMovementModel = model;
                _state.ResetScenario();
            }
        }

        private void DrawImportDrawer()
        {
            GUILayout.Label("Import", CombatManagerTheme.Header);
            GUILayout.Label("Import seeds Blue only. Red remains manually configured.", CombatManagerTheme.Mini);
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

            GUILayout.Label(_state.ImportStatus, CombatManagerTheme.Warning);
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
