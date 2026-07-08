using BrilliantSkies.Ftd.Avatar.Build;
using CombatManager.Ai;
using UnityEngine;

namespace CombatManager.Ui
{
    internal sealed class CombatManagerSession
    {
        private const float SimulationStepSeconds = 0.5f;

        private readonly AiSimulationState _state = new AiSimulationState();
        private bool _active;
        private Vector2 _blueScroll;
        private Vector2 _redScroll;

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
            DrawEditor(CombatManagerEditorLayout.For(Screen.width, Screen.height));
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

        private void DrawEditor(CombatManagerEditorLayout layout)
        {
            DrawFullscreenBackdrop(layout.Root);

            AiDuelFrame frame = _state.BuildDuelFrame();
            DrawToolbar(layout.Toolbar);
            DrawGlobalWarning(layout.Warning);
            DrawSidePanel(layout.BluePanel, _state.Blue, frame.Blue, "Blue / Player", "Blue", ref _blueScroll, includeImport: true);
            CombatManagerGridRenderer.Draw(layout.Grid, _state);
            DrawSidePanel(layout.RedPanel, _state.Red, frame.Red, "Red / Enemy", "Red", ref _redScroll, includeImport: false);
        }

        private void DrawToolbar(Rect rect)
        {
            GUILayout.BeginArea(rect, CombatManagerTheme.Panel);
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("CombatManager Duel Editor", CombatManagerTheme.Header, GUILayout.Width(176f));
            ScenarioButton("Ship Duel", AiScenarioPreset.ShipDuel, GUILayout.Width(82f));
            ScenarioButton("Broadside", AiScenarioPreset.BroadsideDuel, GUILayout.Width(82f));
            ScenarioButton("Hover", AiScenarioPreset.HoverDuel, GUILayout.Width(68f));
            ScenarioButton("Planes", AiScenarioPreset.PlaneIntercept, GUILayout.Width(68f));
            GUILayout.Space(8f);

            if (GUILayout.Button(_state.Playing ? "Pause" : "Play", _state.Playing ? CombatManagerTheme.ActiveButton : CombatManagerTheme.Button, GUILayout.Width(64f)))
                _state.Playing = !_state.Playing;

            if (GUILayout.Button("Step", CombatManagerTheme.Button, GUILayout.Width(54f)))
            {
                _state.Playing = false;
                _state.Step(SimulationStepSeconds);
            }

            if (GUILayout.Button("Reset", CombatManagerTheme.Button, GUILayout.Width(60f)))
                _state.ResetScenario();

            GUILayout.FlexibleSpace();
            GUILayout.Label("Ctrl+Shift+C toggles", CombatManagerTheme.Mini, GUILayout.Width(126f));
            if (GUILayout.Button("Close", CombatManagerTheme.Button, GUILayout.Width(62f)))
                Close();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("View", CombatManagerTheme.Mini, GUILayout.Width(36f));
            if (GUILayout.Button("Fit Duel", CombatManagerTheme.Button, GUILayout.Width(68f)))
                _state.GridZoom = 1f;
            _state.GridZoom = ToolbarSlider("Zoom", _state.GridZoom, 0.5f, 3f, "x", 142f);
            _state.PlaybackSpeed = ToolbarSlider("Speed", _state.PlaybackSpeed, 0.1f, 5f, "x", 142f);
            _state.ShowTrail = ToggleButton("Trails", _state.ShowTrail, GUILayout.Width(72f));
            _state.ShowDesiredTrail = ToggleButton("AI Trails", _state.ShowDesiredTrail, GUILayout.Width(76f));
            _state.ShowRawSteer = ToggleButton("Raw Steer", _state.ShowRawSteer, GUILayout.Width(82f));
            _state.ShowMotionPoint = ToggleButton("Motion", _state.ShowMotionPoint, GUILayout.Width(68f));
            _state.ShowLegend = ToggleButton("Legend", _state.ShowLegend, GUILayout.Width(68f));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private static float ToolbarSlider(string label, float value, float min, float max, string suffix, float sliderWidth)
        {
            GUILayout.Label(label, CombatManagerTheme.Mini, GUILayout.Width(40f));
            float adjusted = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(sliderWidth));
            GUILayout.Label($"{adjusted:0.#}{suffix}", CombatManagerTheme.Mini, GUILayout.Width(40f));
            return adjusted;
        }

        private static void DrawGlobalWarning(Rect rect)
        {
            GUI.Box(rect, GUIContent.none, CombatManagerTheme.Panel);
            GUI.Label(
                new Rect(rect.x + 8f, rect.y + 4f, rect.width - 16f, 18f),
                "Read-only duel simulation: behaviour intent is mirrored from researched AI cards; movement-card output is still an approximation of FTD physics/PID/pathfinding.",
                CombatManagerTheme.Warning);
        }

        private void DrawSidePanel(Rect rect, AiSimEntity entity, AiSimulationFrame frame, string title, string sideName, ref Vector2 scroll, bool includeImport)
        {
            GUILayout.BeginArea(rect, CombatManagerTheme.Panel);
            scroll = GUILayout.BeginScrollView(scroll);

            GUILayout.Label(title, CombatManagerTheme.Header);
            DrawEntityMainframeControls(entity, $"{sideName} Mainframe");
            GUILayout.Space(8f);
            DrawEntityMovementControls(entity, $"{sideName} Movement");
            GUILayout.Space(8f);
            DrawEntityStatus(frame, $"{sideName} Status");
            if (includeImport)
            {
                GUILayout.Space(8f);
                DrawImportDrawer();
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawEntityMainframeControls(AiSimEntity entity, string header)
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

            float radius = SliderRow("Distance/radius", entity.Radius, 25f, 1500f, "m");
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
        }

        private void DrawEntityMovementControls(AiSimEntity entity, string header)
        {
            GUILayout.Label(header, CombatManagerTheme.Header);

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

            if (GUILayout.Button("Reset Scenario", CombatManagerTheme.Button))
                _state.ResetScenario();
        }

        private static void DrawEntityStatus(AiSimulationFrame frame, string header)
        {
            GUILayout.Label(header, CombatManagerTheme.Header);
            GUILayout.Label($"{frame.Kind} | {frame.AiState}", CombatManagerTheme.BodyWrap);
            GUILayout.Label($"range {frame.GroundRange:0.#}m | azimuth {frame.Azimuth:0.#} deg", CombatManagerTheme.BodyWrap);
            GUILayout.Label($"{frame.CraftMovementModel} | speed {frame.CraftVelocity.magnitude:0.#}m/s", CombatManagerTheme.BodyWrap);
            if (!string.IsNullOrWhiteSpace(frame.ApproximationNote))
                GUILayout.Label(frame.ApproximationNote, CombatManagerTheme.Mini);
        }

        private void ScenarioButton(string label, AiScenarioPreset preset, params GUILayoutOption[] options)
        {
            GUIStyle style = _state.ScenarioPreset == preset ? CombatManagerTheme.ActiveButton : CombatManagerTheme.Button;
            if (GUILayout.Button(label, style, options))
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
            GUILayout.Label("Import Blue AI", CombatManagerTheme.Header);
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
                    string prefix = candidate.Supported ? string.Empty : "[unsupported] ";
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
            GUILayout.Label(label, CombatManagerTheme.Body, GUILayout.Width(102f));
            float adjusted = GUILayout.HorizontalSlider(value, min, max, GUILayout.MinWidth(60f));
            GUILayout.Label($"{adjusted:0.#}{suffix}", CombatManagerTheme.Mini, GUILayout.Width(54f));
            GUILayout.EndHorizontal();
            return adjusted;
        }

        private static bool ToggleButton(string label, bool value, params GUILayoutOption[] options)
        {
            GUIStyle style = value ? CombatManagerTheme.ActiveButton : CombatManagerTheme.Button;
            return GUILayout.Button(label, style, options) ? !value : value;
        }

        private static void DrawFullscreenBackdrop(Rect rect)
        {
            Color old = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(rect, CombatManagerTheme.WindowTexture);
            GUI.color = old;
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
