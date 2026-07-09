using System;
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
        private CombatManagerPanelTab _blueTab = CombatManagerPanelTab.Ai;
        private CombatManagerPanelTab _redTab = CombatManagerPanelTab.Ai;
        private Vector2 _blueAiScroll;
        private Vector2 _blueMoveScroll;
        private Vector2 _blueStatusScroll;
        private Vector2 _blueImportScroll;
        private Vector2 _redAiScroll;
        private Vector2 _redMoveScroll;
        private Vector2 _redStatusScroll;
        private float _lastLiveParityRead = -100f;
        private bool _graphDragging;
        private bool _graphRotating;
        private Vector2 _lastGraphMouse;

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
            RefreshLiveParityIfNeeded(force: false);
            HandleGraphInput(layout.Grid);
            DrawFullscreenBackdrop(layout.Root);

            AiDuelFrame frame = _state.BuildDuelFrame();
            DrawToolbar(layout);
            DrawStatusChip(layout.Warning);
            DrawSidePanel(layout.BluePanel, layout.BlueTabContent, _state.Blue, frame.Blue, "Blue / Player", true);
            CombatManagerGridRenderer.Draw(layout.Grid, _state);
            DrawSidePanel(layout.RedPanel, layout.RedTabContent, _state.Red, frame.Red, "Red / Enemy", false);
        }

        private void DrawToolbar(CombatManagerEditorLayout layout)
        {
            GUI.Box(layout.Toolbar, GUIContent.none, CombatManagerTheme.Panel);
            DrawToolbarLeft(layout.ToolbarLeft);
            DrawToolbarMiddle(layout.ToolbarMiddle);
            DrawToolbarRight(layout.ToolbarRight);
        }

        private void DrawToolbarLeft(Rect rect)
        {
            GUILayout.BeginArea(rect);
            GUILayout.Label("CombatManager Duel Editor", CombatManagerTheme.Header);
            GUILayout.BeginHorizontal();
            ScenarioButton("Ship Duel", AiScenarioPreset.ShipDuel, GUILayout.Width(92f));
            ScenarioButton("Broadside", AiScenarioPreset.BroadsideDuel, GUILayout.Width(92f));
            ScenarioButton("Hover", AiScenarioPreset.HoverDuel, GUILayout.Width(72f));
            ScenarioButton("Planes", AiScenarioPreset.PlaneIntercept, GUILayout.Width(72f));
            ScenarioButton("Aerial", AiScenarioPreset.AerialAttackRun, GUILayout.Width(72f));
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawToolbarMiddle(Rect rect)
        {
            GUILayout.BeginArea(rect);
            GUILayout.Space(29f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_state.Playing ? "Pause" : "Play", _state.Playing ? CombatManagerTheme.ActiveButton : CombatManagerTheme.Button, GUILayout.Width(72f)))
                _state.Playing = !_state.Playing;

            if (GUILayout.Button("Step", CombatManagerTheme.Button, GUILayout.Width(62f)))
            {
                _state.Playing = false;
                _state.Step(SimulationStepSeconds);
            }

            if (GUILayout.Button("Reset", CombatManagerTheme.Button, GUILayout.Width(68f)))
                _state.ResetScenario();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawToolbarRight(Rect rect)
        {
            GUILayout.BeginArea(rect);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Fit Duel", CombatManagerTheme.Button, GUILayout.Width(78f)))
                _state.FitDuel();
            if (GUILayout.Button("Reset View", CombatManagerTheme.Button, GUILayout.Width(92f)))
                _state.ResetGraphView();
            if (GUILayout.Button("-", CombatManagerTheme.Button, GUILayout.Width(28f)))
                _state.AdjustGridZoom(1f / 1.2f);
            _state.SetGridZoom(ToolbarSlider("Zoom", _state.GridZoom, 0.25f, 8f, "x", 92f));
            if (GUILayout.Button("+", CombatManagerTheme.Button, GUILayout.Width(28f)))
                _state.AdjustGridZoom(1.2f);
            _state.PlaybackSpeed = ToolbarSlider("Speed", _state.PlaybackSpeed, 0.1f, 5f, "x", 96f);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", CombatManagerTheme.Button, GUILayout.Width(68f)))
                Close();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GraphDimensionButton("2D", AiGraphDimensionMode.Flat2D, GUILayout.Width(42f));
            GraphDimensionButton("3D", AiGraphDimensionMode.Scene3D, GUILayout.Width(42f));
            _state.SetGraphVerticalScale(ToolbarSlider("Alt", _state.GraphVerticalScale, 0.05f, 2f, "x", 82f));
            GUILayout.Space(8f);
            GraphViewButton("Red", AiGraphViewMode.RedCentered, GUILayout.Width(44f));
            GraphViewButton("Blue", AiGraphViewMode.BlueCentered, GUILayout.Width(48f));
            GraphViewButton("Free", AiGraphViewMode.Freecam, GUILayout.Width(48f));
            GUILayout.Space(8f);
            GraphDetailButton("Clean", AiGraphDetailMode.Clean, GUILayout.Width(60f));
            GraphDetailButton("Tactical", AiGraphDetailMode.Tactical, GUILayout.Width(74f));
            GraphDetailButton("Debug", AiGraphDetailMode.Debug, GUILayout.Width(62f));
            GUILayout.FlexibleSpace();
            GUILayout.Label("Ctrl+Shift+C", CombatManagerTheme.Mini, GUILayout.Width(96f));
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void GraphDimensionButton(string label, AiGraphDimensionMode mode, params GUILayoutOption[] options)
        {
            GUIStyle style = _state.GraphDimensionMode == mode ? CombatManagerTheme.ActiveButton : CombatManagerTheme.Button;
            if (GUILayout.Button(label, style, options))
                _state.SetGraphDimensionMode(mode);
        }

        private void GraphViewButton(string label, AiGraphViewMode mode, params GUILayoutOption[] options)
        {
            GUIStyle style = _state.GraphViewMode == mode ? CombatManagerTheme.ActiveButton : CombatManagerTheme.Button;
            if (GUILayout.Button(label, style, options))
                _state.SetGraphViewMode(mode);
        }

        private void GraphDetailButton(string label, AiGraphDetailMode mode, params GUILayoutOption[] options)
        {
            GUIStyle style = _state.GraphDetailMode == mode ? CombatManagerTheme.ActiveButton : CombatManagerTheme.Button;
            if (GUILayout.Button(label, style, options))
                _state.SetGraphDetailMode(mode);
        }

        private static float ToolbarSlider(string label, float value, float min, float max, string suffix, float sliderWidth)
        {
            GUILayout.Label(label, CombatManagerTheme.Mini, GUILayout.Width(38f));
            float adjusted = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(sliderWidth));
            GUILayout.Label($"{adjusted:0.#}{suffix}", CombatManagerTheme.Mini, GUILayout.Width(36f));
            return adjusted;
        }

        private void HandleGraphInput(Rect graphRect)
        {
            Event current = Event.current;
            if (current == null)
                return;

            bool insideGraph = graphRect.Contains(current.mousePosition);
            if (current.type == EventType.ScrollWheel && insideGraph)
            {
                _state.AdjustGridZoom(Mathf.Pow(1.12f, -current.delta.y));
                current.Use();
                return;
            }

            if (current.type == EventType.MouseDown && current.button == 0 && insideGraph)
            {
                _state.Begin3DPan();
                _graphDragging = true;
                _lastGraphMouse = current.mousePosition;
                current.Use();
                return;
            }

            if (current.type == EventType.MouseDrag && current.button == 0 && _graphDragging)
            {
                Vector2 delta = current.mousePosition - _lastGraphMouse;
                Rect innerGraph = new Rect(0f, 0f, Mathf.Max(1f, graphRect.width - 16f), Mathf.Max(1f, graphRect.height - 16f));
                if (_state.GraphDimensionMode == AiGraphDimensionMode.Scene3D)
                {
                    AiSimulation3DProjection projection = AiSimulation3DProjection.For(innerGraph, _state);
                    _state.PanFreecam3D(delta, projection.MetersPerPixel);
                }
                else
                {
                    AiSimulationGridProjection projection = AiSimulationGridProjection.For(innerGraph, _state);
                    _state.PanFreecam(delta, projection.MetersPerPixel);
                }
                _lastGraphMouse = current.mousePosition;
                current.Use();
                return;
            }

            if (current.type == EventType.MouseDown && current.button == 1 && insideGraph)
            {
                _state.SetGraphDimensionMode(AiGraphDimensionMode.Scene3D);
                _graphRotating = true;
                _lastGraphMouse = current.mousePosition;
                current.Use();
                return;
            }

            if (current.type == EventType.MouseDrag && current.button == 1 && _graphRotating)
            {
                Vector2 delta = current.mousePosition - _lastGraphMouse;
                _state.RotateGraph3D(delta);
                _lastGraphMouse = current.mousePosition;
                current.Use();
                return;
            }

            if (current.type == EventType.MouseUp && current.button == 0)
                _graphDragging = false;
            if (current.type == EventType.MouseUp && current.button == 1)
                _graphRotating = false;
        }

        private static void DrawStatusChip(Rect rect)
        {
            GUI.Box(rect, GUIContent.none, CombatManagerTheme.Panel);
            GUI.Label(
                new Rect(rect.x + 10f, rect.y + 4f, rect.width - 20f, 18f),
                "READ-ONLY AI INTENT  |  movement card output is approximate; full notes live in the Status tabs",
                CombatManagerTheme.Warning);
        }

        private void DrawSidePanel(Rect panel, Rect content, AiSimEntity entity, AiSimulationFrame frame, string title, bool blue)
        {
            GUI.Box(panel, GUIContent.none, CombatManagerTheme.Panel);

            Rect titleRect = new Rect(panel.x + 10f, panel.y + 10f, panel.width - 20f, 27f);
            GUI.Label(titleRect, title, CombatManagerTheme.Header);

            CombatManagerPanelTab selected = blue ? _blueTab : _redTab;
            Rect tabsRect = new Rect(panel.x + 10f, titleRect.yMax + 6f, panel.width - 20f, 30f);
            DrawPanelTabs(tabsRect, ref selected, blue);
            if (blue)
                _blueTab = selected;
            else
                _redTab = selected == CombatManagerPanelTab.Import ? CombatManagerPanelTab.Ai : selected;

            if (blue)
                DrawBlueTabContent(content, entity, frame);
            else
                DrawRedTabContent(content, entity, frame);
        }

        private static void DrawPanelTabs(Rect rect, ref CombatManagerPanelTab selected, bool includeImport)
        {
            GUILayout.BeginArea(rect);
            GUILayout.BeginHorizontal();
            PanelTabButton("AI", CombatManagerPanelTab.Ai, ref selected);
            PanelTabButton("Move", CombatManagerPanelTab.Move, ref selected);
            PanelTabButton("Status", CombatManagerPanelTab.Status, ref selected);
            if (includeImport)
                PanelTabButton("Import", CombatManagerPanelTab.Import, ref selected);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private static void PanelTabButton(string label, CombatManagerPanelTab tab, ref CombatManagerPanelTab selected)
        {
            GUIStyle style = selected == tab ? CombatManagerTheme.ActiveButton : CombatManagerTheme.Button;
            if (GUILayout.Button(label, style))
                selected = tab;
        }

        private void DrawBlueTabContent(Rect content, AiSimEntity entity, AiSimulationFrame frame)
        {
            switch (_blueTab)
            {
                case CombatManagerPanelTab.Move:
                    DrawScrollableContent(content, ref _blueMoveScroll, () => DrawEntityMovementControls(entity));
                    break;
                case CombatManagerPanelTab.Status:
                    DrawScrollableContent(content, ref _blueStatusScroll, () => DrawEntityStatus(frame, "Blue Status"));
                    break;
                case CombatManagerPanelTab.Import:
                    DrawScrollableContent(content, ref _blueImportScroll, DrawImportDrawer);
                    break;
                default:
                    DrawScrollableContent(content, ref _blueAiScroll, () => DrawEntityMainframeControls(entity));
                    break;
            }
        }

        private void DrawRedTabContent(Rect content, AiSimEntity entity, AiSimulationFrame frame)
        {
            switch (_redTab)
            {
                case CombatManagerPanelTab.Move:
                    DrawScrollableContent(content, ref _redMoveScroll, () => DrawEntityMovementControls(entity));
                    break;
                case CombatManagerPanelTab.Status:
                    DrawScrollableContent(content, ref _redStatusScroll, () => DrawEntityStatus(frame, "Red Status"));
                    break;
                default:
                    DrawScrollableContent(content, ref _redAiScroll, () => DrawEntityMainframeControls(entity));
                    break;
            }
        }

        private static void DrawScrollableContent(Rect rect, ref Vector2 scroll, Action drawContent)
        {
            scroll.x = 0f;
            GUILayout.BeginArea(rect);
            scroll = GUILayout.BeginScrollView(scroll, false, true, GUIStyle.none, GUI.skin.verticalScrollbar);
            drawContent();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            scroll.x = 0f;
        }

        private void DrawEntityMainframeControls(AiSimEntity entity)
        {
            AiMainframeBlueprint blueprint = _state.BlueprintFor(entity.Role);
            GUILayout.Label("Blueprint", CombatManagerTheme.Header);
            if (DrawBlueprintPresetButtons(entity.Role))
                return;

            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name", CombatManagerTheme.Body, GUILayout.Width(80f));
            blueprint.MainframeName = GUILayout.TextField(blueprint.MainframeName ?? string.Empty, GUILayout.MinWidth(120f));
            GUILayout.EndHorizontal();
            blueprint.Priority = Mathf.RoundToInt(SliderRow("Priority", blueprint.Priority, -500f, 500f, string.Empty));

            GUILayout.Label("Mainframe mode", CombatManagerTheme.Mini);
            GUILayout.BeginHorizontal();
            blueprint.MovementMode = StringOptionButton("Automatic", blueprint.MovementMode, "Automatic");
            blueprint.MovementMode = StringOptionButton("Off", blueprint.MovementMode, "Off");
            blueprint.MovementMode = StringOptionButton("Fleet", blueprint.MovementMode, "Fleet");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            blueprint.FiringMode = StringOptionButton("Firing On", blueprint.FiringMode, "On");
            blueprint.FiringMode = StringOptionButton("Firing Off", blueprint.FiringMode, "Off");
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            GUILayout.Label("AI Card", CombatManagerTheme.Header);
            GUILayout.Label($"{AiSimulationState.PresetName(entity.Preset)} | {AiSimulationState.CraftProfileName(entity.CraftProfile)} | {AiSimulationState.CraftMovementModelName(entity.CraftMovementModel)}", CombatManagerTheme.BodyWrap);

            GUILayout.Space(6f);
            GUILayout.Label("Behaviour", CombatManagerTheme.Mini);
            GUILayout.BeginHorizontal();
            EntityPresetButton(entity, "Circle", AiSimulationPreset.Circle);
            EntityPresetButton(entity, "Point At", AiSimulationPreset.PointAt);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            EntityPresetButton(entity, "Broadside", AiSimulationPreset.Broadside);
            EntityPresetButton(entity, "Naval 2.0", AiSimulationPreset.NavalBroadside);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            EntityPresetButton(entity, "Attack 1", AiSimulationPreset.AttackRun1);
            EntityPresetButton(entity, "Attack 2", AiSimulationPreset.AttackRun2);
            EntityPresetButton(entity, "Attack 3", AiSimulationPreset.AttackRun3);
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);
            GUILayout.Label("Preferred side", CombatManagerTheme.Mini);
            GUILayout.BeginHorizontal();
            EntitySideButton(entity, "Auto/Both", AiSimulationSide.Both);
            EntitySideButton(entity, "Left", AiSimulationSide.Left);
            EntitySideButton(entity, "Right", AiSimulationSide.Right);
            GUILayout.EndHorizontal();

            float radius = SliderRow("Range", entity.Radius, 25f, 1500f, "m");
            if (!Mathf.Approximately(radius, entity.Radius))
            {
                entity.Radius = radius;
                entity.BroadsideOuterRadius = Mathf.Max(entity.BroadsideOuterRadius, radius + 20f);
                _state.ResetScenario();
            }

            if (entity.Preset == AiSimulationPreset.Broadside || entity.Preset == AiSimulationPreset.NavalBroadside)
                entity.BroadsideAngle = SliderRow("Broadside", entity.BroadsideAngle, 10f, 170f, "deg");
            if (entity.Preset == AiSimulationPreset.NavalBroadside)
                entity.BroadsideOuterRadius = SliderRow("Leave range", entity.BroadsideOuterRadius, entity.Radius + 20f, 2500f, "m");
            if (entity.Preset == AiSimulationPreset.AttackRun1 || entity.Preset == AiSimulationPreset.AttackRun2 || entity.Preset == AiSimulationPreset.AttackRun3)
                DrawAttackRunControls(entity);

            _state.CaptureBlueprintFromEntity(entity);
        }

        private static void DrawAttackRunControls(AiSimEntity entity)
        {
            GUILayout.Space(8f);
            GUILayout.Label("Attack run", CombatManagerTheme.Header);
            if (entity.Preset == AiSimulationPreset.AttackRun1)
            {
                entity.AttackRunBeginDistance = SliderRow("Begin", entity.AttackRunBeginDistance, 50f, 1500f, "m");
                entity.AttackRunAbortDistance = SliderRow("Abort", entity.AttackRunAbortDistance, 10f, 800f, "m");
                entity.AttackRunWaitTime = SliderRow("Wait", entity.AttackRunWaitTime, 0f, 60f, "s");
                entity.AttackRunAttackAltitude = SliderRow("Attack alt", entity.AttackRunAttackAltitude, -100f, 1000f, "m");
                entity.AttackRunDisengageAltitude = SliderRow("Flee alt", entity.AttackRunDisengageAltitude, 0f, 1200f, "m");
                return;
            }

            entity.AttackRunBreakoffDistance = SliderRow("Breakoff", entity.AttackRunBreakoffDistance, 1f, 1000f, "m");
            entity.AttackRunPitchDistance = SliderRow("Pitch dist", entity.AttackRunPitchDistance, 0f, 2000f, "m");
            entity.AttackRunReengageDistance = SliderRow("Reengage", entity.AttackRunReengageDistance, 1f, 5000f, "m");
            entity.AttackRunReengageTime = SliderRow("Reengage t", entity.AttackRunReengageTime, 0f, 120f, "s");
            entity.AttackRunBreakoffAltitude = SliderRow("Break alt", entity.AttackRunBreakoffAltitude, -1000f, 1000f, "m");
            entity.AttackRunCombatAltitude = SliderRow("Flee alt", entity.AttackRunCombatAltitude, 0f, 2000f, "m");
            if (entity.Preset == AiSimulationPreset.AttackRun3)
            {
                entity.AttackRunEngagementAltitude = SliderRow("Engage alt", entity.AttackRunEngagementAltitude, -1000f, 1000f, "m");
                entity.AttackRunPredictionPoint = SliderRow("Direct pt", entity.AttackRunPredictionPoint, 0f, 1000f, "m");
                GUILayout.BeginHorizontal();
                entity.AttackRunUsePrediction = ToggleButton("Predict", entity.AttackRunUsePrediction);
                entity.AttackRunFlyover = ToggleButton("Flyover", entity.AttackRunFlyover);
                entity.AttackRunIgnoreAltitude = ToggleButton("Ignore alt", entity.AttackRunIgnoreAltitude);
                GUILayout.EndHorizontal();
            }
        }

        private void DrawEntityMovementControls(AiSimEntity entity)
        {
            AiMainframeBlueprint blueprint = _state.BlueprintFor(entity.Role);
            GUILayout.Label("Movement", CombatManagerTheme.Header);

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

            GUILayout.Space(6f);
            GUILayout.Label("Move card", CombatManagerTheme.Mini);
            GUILayout.BeginHorizontal();
            EntityMovementModelButton(entity, "Ship", AiCraftMovementModel.ShipOrTank);
            EntityMovementModelButton(entity, "Hover", AiCraftMovementModel.Hover);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            EntityMovementModelButton(entity, "Six-axis", AiCraftMovementModel.SixAxis);
            EntityMovementModelButton(entity, "Plane", AiCraftMovementModel.Airplane);
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);
            entity.CraftSpeed = SliderRow("Max speed", entity.CraftSpeed, 0f, 180f, "m/s");
            entity.CraftTurnRate = SliderRow("Turn rate", entity.CraftTurnRate, 5f, 240f, "deg/s");
            entity.CraftAcceleration = SliderRow("Accel", entity.CraftAcceleration, 1f, 100f, "m/s2");
            float altitude = SliderRow("Altitude", entity.Altitude, 0f, 900f, "m");
            if (!Mathf.Approximately(altitude, entity.Altitude))
            {
                entity.Altitude = altitude;
                entity.Position = new Vector3(entity.Position.x, entity.Altitude, entity.Position.z);
            }

            if (entity.CraftMovementModel == AiCraftMovementModel.ShipOrTank)
                entity.ShipTarryDistance = SliderRow("Tarry", entity.ShipTarryDistance, 0f, 200f, "m");
            if (entity.CraftMovementModel == AiCraftMovementModel.Hover)
            {
                entity.HoverYawLockDistance = SliderRow("Yaw lock", entity.HoverYawLockDistance, 0f, 1000f, "m");
                entity.HoverMoveWithinAzimuth = SliderRow("Move azi", entity.HoverMoveWithinAzimuth, 0f, 180f, "deg");
            }
            if (entity.CraftMovementModel == AiCraftMovementModel.SixAxis)
                entity.SixAxisLookAheadDistance = SliderRow("Look ahead", entity.SixAxisLookAheadDistance, 10f, 5000f, "m");
            if (entity.CraftMovementModel == AiCraftMovementModel.Airplane)
            {
                entity.AirplaneMinimumSpeed = SliderRow("Min speed", entity.AirplaneMinimumSpeed, 0f, 140f, "m/s");
                entity.AirplaneBankingTurnAbove = SliderRow("Bank above", entity.AirplaneBankingTurnAbove, 0f, 90f, "deg");
                entity.AirplaneBankingTurnRoll = SliderRow("Bank roll", entity.AirplaneBankingTurnRoll, 0f, 90f, "deg");
            }

            GUILayout.Space(8f);
            if (GUILayout.Button("Reset Scenario", CombatManagerTheme.Button))
                _state.ResetScenario();

            GUILayout.Space(8f);
            DrawAdjustmentControls(blueprint);
            _state.CaptureBlueprintFromEntity(entity);
        }

        private void DrawEntityStatus(AiSimulationFrame frame, string header)
        {
            AiMainframeBlueprint blueprint = _state.BlueprintFor(frame.Role);
            GUILayout.Label(header, CombatManagerTheme.Header);
            GUILayout.Label($"{frame.Kind} | {frame.AiState}", CombatManagerTheme.BodyWrap);
            GUILayout.Label($"Range {frame.GroundRange:0.#}m", CombatManagerTheme.BodyWrap);
            GUILayout.Label($"Azimuth {frame.Azimuth:0.#} deg", CombatManagerTheme.BodyWrap);
            GUILayout.Label($"{frame.CraftMovementModel} | speed {frame.CraftVelocity.magnitude:0.#}m/s", CombatManagerTheme.BodyWrap);

            GUILayout.Space(8f);
            GUILayout.Label("Blueprint mapping", CombatManagerTheme.Header);
            GUILayout.Label($"Mainframe: {blueprint.MainframeName}", CombatManagerTheme.BodyWrap);
            GUILayout.Label($"Behaviour: {blueprint.BehaviourClassName() ?? "unsupported / preview-only"}", CombatManagerTheme.BodyWrap);
            GUILayout.Label($"Manoeuvre: {blueprint.ManoeuvreClassName() ?? "unsupported"}", CombatManagerTheme.BodyWrap);
            GUILayout.Label($"Adjustments: {blueprint.AdjustmentVehicleType}, {blueprint.AltitudeReference}", CombatManagerTheme.BodyWrap);
            foreach (string warning in blueprint.Warnings)
                GUILayout.Label(warning, CombatManagerTheme.Warning);

            GUILayout.Space(8f);
            GUILayout.Label("Approximation", CombatManagerTheme.Header);
            GUILayout.Label("Read-only simulation. Behaviour intent is mirrored from researched AI cards; movement-card output approximates FTD physics, PID, propulsion, pathfinding, terrain, water, and firing-angle internals.", CombatManagerTheme.Warning);
            if (!string.IsNullOrWhiteSpace(frame.ApproximationNote))
                GUILayout.Label(frame.ApproximationNote, CombatManagerTheme.BodyWrap);
        }

        private bool DrawBlueprintPresetButtons(AiEntityRole role)
        {
            GUILayout.Label("Presets", CombatManagerTheme.Mini);
            GUILayout.BeginHorizontal();
            for (int i = 0; i < AiBlueprintPresetLibrary.All.Length; i++)
            {
                AiBlueprintPreset preset = AiBlueprintPresetLibrary.All[i];
                if (i > 0 && i % 2 == 0)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }

                if (GUILayout.Button(AiBlueprintPresetLibrary.Name(preset), CombatManagerTheme.Button))
                {
                    _state.ApplyBlueprintPreset(role, preset);
                    return true;
                }
            }

            GUILayout.EndHorizontal();
            return false;
        }

        private static void DrawAdjustmentControls(AiMainframeBlueprint blueprint)
        {
            GUILayout.Label("Adjustments draft", CombatManagerTheme.Header);
            GUILayout.BeginHorizontal();
            blueprint.AltitudeReference = AltitudeButton("On water", blueprint.AltitudeReference, AiBlueprintAltitudeReference.OnWater);
            blueprint.AltitudeReference = AltitudeButton("On land", blueprint.AltitudeReference, AiBlueprintAltitudeReference.OnLand);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            blueprint.AltitudeReference = AltitudeButton("Above", blueprint.AltitudeReference, AiBlueprintAltitudeReference.Above);
            blueprint.AltitudeReference = AltitudeButton("Ignore", blueprint.AltitudeReference, AiBlueprintAltitudeReference.Ignore);
            GUILayout.EndHorizontal();
            blueprint.MinimumAltitudeAboveLand = SliderRow("Min land", blueprint.MinimumAltitudeAboveLand, 0f, 1000f, "m");
            blueprint.MinimumAltitudeAboveWater = SliderRow("Min water", blueprint.MinimumAltitudeAboveWater, 0f, 1000f, "m");
            blueprint.MaximumAltitude = SliderRow("Max alt", blueprint.MaximumAltitude, 0f, 3000f, "m");
            blueprint.WaterDepthRequired = SliderRow("Water depth", blueprint.WaterDepthRequired, 0f, 300f, "m");
            blueprint.LandHeightRequired = SliderRow("Land height", blueprint.LandHeightRequired, 0f, 300f, "m");
            blueprint.TurningCircle = SliderRow("Turn circle", blueprint.TurningCircle, 0f, 2000f, "m");
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
            GUILayout.Label("Import seeds the Blue blueprint only. Red remains manually configured.", CombatManagerTheme.BodyWrap);
            LabelPair("Mainframe", _state.ImportedMainframe);
            LabelPair("Behaviour", _state.ImportedBehaviour);
            LabelPair("Move", _state.ImportedManoeuvre);

            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh", CombatManagerTheme.Button))
            {
                AiSimulationImporter.RefreshCandidates(GetFocusedConstruct(), _state);
                _state.ShowImportDetails = true;
            }
            if (GUILayout.Button("Import", CombatManagerTheme.Button))
            {
                AiSimulationImporter.TryImport(GetFocusedConstruct(), _state, _state.SelectedImportIndex, out string message);
                _state.ImportStatus = message;
                _state.ShowImportDetails = true;
            }
            GUILayout.EndHorizontal();

            string label = _state.ShowImportDetails ? "Hide Details" : "Show Details";
            if (GUILayout.Button(label, CombatManagerTheme.Button))
                _state.ShowImportDetails = !_state.ShowImportDetails;

            GUILayout.Label(_state.ImportStatus, CombatManagerTheme.Warning);

            GUILayout.Space(8f);
            GUILayout.Label("Export Preview", CombatManagerTheme.Header);
            GUILayout.Label("Dry-run only. This does not modify the focused craft.", CombatManagerTheme.BodyWrap);
            if (GUILayout.Button("Build Export Preview", CombatManagerTheme.Button))
                _state.BlueExportPlan = AiBlueprintExportPlanner.Build(GetFocusedConstruct(), _state.BlueBlueprint, _state.SelectedImportIndex);
            DrawExportPreview(_state.BlueExportPlan);

            GUILayout.Space(8f);
            DrawLiveParityDrawer();

            if (!_state.ShowImportDetails)
                return;

            GUILayout.Space(6f);
            GUILayout.Label("Mainframes", CombatManagerTheme.Header);
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
                    GUILayout.Label($"{candidate.Summary} | move {candidate.MovementType} | fire {candidate.FiringType}", candidate.Supported ? CombatManagerTheme.BodyWrap : CombatManagerTheme.Warning);
                }
            }

            GUILayout.Space(6f);
            GUILayout.Label("Parameters", CombatManagerTheme.Header);
            if (_state.ImportedParameters.Count == 0)
            {
                GUILayout.Label("No imported behaviour parameters.", CombatManagerTheme.BodyWrap);
            }
            else
            {
                foreach (string parameter in _state.ImportedParameters)
                    GUILayout.Label(parameter, CombatManagerTheme.BodyWrap);
            }

            GUILayout.Space(6f);
            GUILayout.Label("Requests", CombatManagerTheme.Header);
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

        private void DrawLiveParityDrawer()
        {
            GUILayout.Label("Live Parity", CombatManagerTheme.Header);
            GUILayout.Label("Read-only comparison against the focused craft's current AI requests.", CombatManagerTheme.BodyWrap);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_state.LiveParityEnabled ? "Disable" : "Enable", _state.LiveParityEnabled ? CombatManagerTheme.ActiveButton : CombatManagerTheme.Button))
            {
                _state.LiveParityEnabled = !_state.LiveParityEnabled;
                if (_state.LiveParityEnabled)
                    RefreshLiveParityIfNeeded(force: true);
                else
                    _state.LiveParityStatus = "Live Parity is off.";
            }

            if (GUILayout.Button("Capture Once", CombatManagerTheme.Button))
                RefreshLiveParityIfNeeded(force: true);
            GUILayout.EndHorizontal();

            GUILayout.Label(_state.LiveParityStatus, _state.LiveParityEnabled ? CombatManagerTheme.Warning : CombatManagerTheme.BodyWrap);
            AiLiveParitySnapshot parity = _state.LiveParity;
            if (parity == null)
            {
                GUILayout.Label("No parity snapshot captured yet.", CombatManagerTheme.Mini);
                return;
            }

            LabelPair("Mainframe", parity.MainframeName);
            LabelPair("Behaviour", parity.BehaviourType);
            LabelPair("Move", parity.ManoeuvreType);
            if (parity.PredictedIntent != null)
            {
                GUILayout.Label(parity.PredictedIntent.Summary, CombatManagerTheme.BodyWrap);
                GUILayout.Label($"range {parity.PredictedIntent.Range:0.#}m | azimuth {parity.PredictedIntent.Azimuth:0.#} deg | state {parity.PredictedIntent.State}", CombatManagerTheme.Mini);
            }

            GUILayout.Space(4f);
            GUILayout.Label("Observed vs predicted requests", CombatManagerTheme.Mini);
            if (parity.RequestDeltas.Count == 0)
            {
                GUILayout.Label("No non-zero observed or predicted requests.", CombatManagerTheme.BodyWrap);
            }
            else
            {
                int rows = Mathf.Min(8, parity.RequestDeltas.Count);
                for (int i = 0; i < rows; i++)
                {
                    AiControlRequestDelta delta = parity.RequestDeltas[i];
                    GUILayout.Label($"{delta.Type}: obs {delta.Observed:0.00} | pred {delta.Predicted:0.00} | delta {delta.Delta:+0.00;-0.00;0.00}", CombatManagerTheme.Body);
                }
            }

            if (parity.Warnings.Count > 0)
            {
                GUILayout.Space(4f);
                GUILayout.Label("Warnings", CombatManagerTheme.Mini);
                foreach (string warning in parity.Warnings)
                    GUILayout.Label(warning, CombatManagerTheme.Warning);
            }
        }

        private void RefreshLiveParityIfNeeded(bool force)
        {
            if (!_state.LiveParityEnabled && !force)
                return;

            float now = Time.unscaledTime;
            if (!force && now - _lastLiveParityRead < 0.25f)
                return;

            _lastLiveParityRead = now;
            _state.LiveParity = AiLiveParityCollector.Capture(GetFocusedConstruct(), _state.SelectedImportIndex);
            _state.LiveParityStatus = _state.LiveParity.Status;
        }

        private static void DrawExportPreview(AiBlueprintExportPlan plan)
        {
            if (plan == null)
            {
                GUILayout.Label("No export preview built yet.", CombatManagerTheme.Mini);
                return;
            }

            GUILayout.Label($"Target: {plan.TargetMainframeName}", CombatManagerTheme.BodyWrap);
            GUILayout.Label(plan.RoutineCapacity, CombatManagerTheme.BodyWrap);
            GUILayout.Label(plan.Supported ? "Supported mapping: yes" : "Supported mapping: no", plan.Supported ? CombatManagerTheme.BodyWrap : CombatManagerTheme.Warning);
            GUILayout.Space(4f);
            GUILayout.Label("Would write", CombatManagerTheme.Mini);
            foreach (string mutation in plan.Mutations)
                GUILayout.Label(mutation, CombatManagerTheme.BodyWrap);
            GUILayout.Space(4f);
            GUILayout.Label("Warnings", CombatManagerTheme.Mini);
            foreach (string warning in plan.Warnings)
                GUILayout.Label(warning, CombatManagerTheme.Warning);
        }

        private static float SliderRow(string label, float value, float min, float max, string suffix)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(30f));
            GUILayout.Label(label, CombatManagerTheme.Body, GUILayout.Width(112f));
            float adjusted = GUILayout.HorizontalSlider(value, min, max, GUILayout.MinWidth(90f));
            GUILayout.Label($"{adjusted:0.#}{suffix}", CombatManagerTheme.Mini, GUILayout.Width(68f));
            GUILayout.EndHorizontal();
            return adjusted;
        }

        private static bool ToggleButton(string label, bool value, params GUILayoutOption[] options)
        {
            GUIStyle style = value ? CombatManagerTheme.ActiveButton : CombatManagerTheme.Button;
            return GUILayout.Button(label, style, options) ? !value : value;
        }

        private static string StringOptionButton(string label, string current, string option)
        {
            GUIStyle style = current == option ? CombatManagerTheme.ActiveButton : CombatManagerTheme.Button;
            return GUILayout.Button(label, style) ? option : current;
        }

        private static AiBlueprintAltitudeReference AltitudeButton(string label, AiBlueprintAltitudeReference current, AiBlueprintAltitudeReference option)
        {
            GUIStyle style = current == option ? CombatManagerTheme.ActiveButton : CombatManagerTheme.Button;
            return GUILayout.Button(label, style) ? option : current;
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
            GUILayout.Label(label, CombatManagerTheme.Mini, GUILayout.Width(80f));
            GUILayout.Label(string.IsNullOrWhiteSpace(value) ? "--" : value, CombatManagerTheme.BodyWrap);
            GUILayout.EndHorizontal();
        }
    }
}
