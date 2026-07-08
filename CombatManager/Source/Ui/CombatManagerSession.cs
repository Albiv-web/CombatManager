using System;
using System.Collections.Generic;
using BrilliantSkies.Ftd.Avatar.Build;
using CombatManager.Ai;
using UnityEngine;

namespace CombatManager.Ui
{
    internal sealed class CombatManagerSession
    {
        private const float RefreshInterval = 0.15f;
        private Rect _window = new Rect(70f, 70f, 1040f, 650f);
        private CombatAiSnapshot _snapshot;
        private int _selectedIndex;
        private bool _playing = true;
        private bool _stepRequested;
        private bool _active;
        private float _nextRefresh;
        private Vector3 _sandboxTarget;
        private bool _sandboxInitialised;
        private bool _draggingSandbox;
        private Vector2 _scrollParams;
        private Vector2 _scrollWarnings;

        internal bool Active => _active;

        internal void Begin()
        {
            _active = true;
            Refresh(force: true);
        }

        internal void Close()
        {
            _active = false;
        }

        internal void Tick()
        {
            if (!_active)
                return;

            if (_playing || _stepRequested)
                Refresh(force: _stepRequested);

            _stepRequested = false;
        }

        internal void OnGUI()
        {
            if (!_active)
                return;

            CombatManagerTheme.Ensure();
            _window.width = Mathf.Clamp(_window.width, 760f, Mathf.Max(760f, Screen.width - 40f));
            _window.height = Mathf.Clamp(_window.height, 480f, Mathf.Max(480f, Screen.height - 40f));
            _window.x = Mathf.Clamp(_window.x, 0f, Mathf.Max(0f, Screen.width - _window.width));
            _window.y = Mathf.Clamp(_window.y, 0f, Mathf.Max(0f, Screen.height - _window.height));
            _window = GUI.Window(483210, _window, DrawWindow, "CombatManager AI Intent");
        }

        private void Refresh(bool force)
        {
            if (!force && Time.unscaledTime < _nextRefresh)
                return;

            MainConstruct construct = GetFocusedConstruct();
            EnsureSandboxTarget(construct);
            _snapshot = AiSnapshotCollector.Capture(
                construct,
                _selectedIndex,
                useSandboxTarget: true,
                _sandboxTarget);
            _selectedIndex = _snapshot.SelectedIndex;
            _nextRefresh = Time.unscaledTime + RefreshInterval;
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

        private void EnsureSandboxTarget(MainConstruct construct)
        {
            if (_sandboxInitialised || construct == null)
                return;

            _sandboxTarget = construct.CentreOfMass + construct.SafeForward * 600f;
            _sandboxTarget.y = construct.CentreOfMass.y;
            _sandboxInitialised = true;
        }

        private void DrawWindow(int id)
        {
            DrawToolbar();

            if (_snapshot == null || _snapshot.Status != null)
            {
                GUILayout.Label(_snapshot?.Status ?? "No AI snapshot available.", CombatManagerTheme.Warning);
                GUI.DragWindow(new Rect(0f, 0f, _window.width, 22f));
                return;
            }

            GUILayout.BeginHorizontal();
            DrawMainframePanel();
            DrawGridAndDetails();
            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0f, 0f, _window.width, 22f));
        }

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Passive visualizer", CombatManagerTheme.Header, GUILayout.Width(180f));
            if (GUILayout.Button(_playing ? "Pause" : "Play", _playing ? CombatManagerTheme.ActiveButton : CombatManagerTheme.Button, GUILayout.Width(78f)))
                _playing = !_playing;
            if (GUILayout.Button("Step", CombatManagerTheme.Button, GUILayout.Width(68f)))
            {
                _playing = false;
                _stepRequested = true;
            }
            if (GUILayout.Button("Refresh", CombatManagerTheme.Button, GUILayout.Width(78f)))
                Refresh(force: true);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Ctrl+Shift+C toggles", CombatManagerTheme.Mini, GUILayout.Width(140f));
            if (GUILayout.Button("Close", CombatManagerTheme.Button, GUILayout.Width(70f)))
                Close();
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);
        }

        private void DrawMainframePanel()
        {
            GUILayout.BeginVertical(CombatManagerTheme.Panel, GUILayout.Width(270f), GUILayout.ExpandHeight(true));
            GUILayout.Label("Mainframes", CombatManagerTheme.Header);

            for (int i = 0; i < _snapshot.Mainframes.Count; i++)
            {
                MainframeIntentSnapshot item = _snapshot.Mainframes[i];
                GUIStyle style = i == _selectedIndex ? CombatManagerTheme.SelectedRow : CombatManagerTheme.Row;
                if (GUILayout.Button($"{item.Name}  P{item.Priority}", style))
                {
                    _selectedIndex = i;
                    Refresh(force: true);
                }
            }

            GUILayout.Space(8f);
            MainframeIntentSnapshot selected = _snapshot.SelectedMainframe;
            if (selected != null)
            {
                GUILayout.Label("Selected AI", CombatManagerTheme.Header);
                LabelPair("Movement", selected.MovementType.ToString());
                LabelPair("Firing", selected.FiringType.ToString());
                LabelPair("Behaviour", selected.BehaviourName);
                LabelPair("Type", selected.BehaviourType);
                LabelPair("Manoeuvre", selected.ManoeuvreName);
                LabelPair("Type", selected.ManoeuvreType);
                if (selected.Prediction != null)
                {
                    LabelPair("Intent", selected.Prediction.Kind);
                    GUILayout.Label(selected.Prediction.Summary, CombatManagerTheme.BodyWrap);
                    if (selected.Prediction.Approximate)
                        GUILayout.Label("Broadside pathfinding/terrain adjustment is approximated.", CombatManagerTheme.Warning);
                }
            }

            GUILayout.EndVertical();
        }

        private void DrawGridAndDetails()
        {
            MainframeIntentSnapshot selected = _snapshot.SelectedMainframe;
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            Rect gridRect = GUILayoutUtility.GetRect(420f, 420f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            CombatManagerGridRenderer.Draw(gridRect, selected);
            HandleSandboxTargetDrag(gridRect, selected);

            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            DrawParameters(selected);
            DrawWarningsAndRequests(selected);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawParameters(MainframeIntentSnapshot selected)
        {
            GUILayout.BeginVertical(CombatManagerTheme.Panel, GUILayout.MinWidth(320f), GUILayout.Height(150f));
            GUILayout.Label("Read-only parameters", CombatManagerTheme.Header);
            _scrollParams = GUILayout.BeginScrollView(_scrollParams);
            if (selected == null || selected.Parameters.Count == 0)
            {
                GUILayout.Label("No mirrored parameter details for this routine.", CombatManagerTheme.Body);
            }
            else
            {
                foreach (string parameter in selected.Parameters)
                    GUILayout.Label(parameter, CombatManagerTheme.Body);
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawWarningsAndRequests(MainframeIntentSnapshot selected)
        {
            GUILayout.BeginVertical(CombatManagerTheme.Panel, GUILayout.MinWidth(330f), GUILayout.Height(150f));
            GUILayout.Label("Status and requests", CombatManagerTheme.Header);
            _scrollWarnings = GUILayout.BeginScrollView(_scrollWarnings);

            if (selected == null)
            {
                GUILayout.Label("No selected mainframe.", CombatManagerTheme.Warning);
            }
            else
            {
                foreach (string warning in selected.Warnings)
                    GUILayout.Label(warning, CombatManagerTheme.Warning);

                if (selected.Requests.Count == 0)
                {
                    GUILayout.Label("No current movement requests.", CombatManagerTheme.Body);
                }
                else
                {
                    foreach (AiControlRequestSnapshot request in selected.Requests)
                        GUILayout.Label($"{request.Type}: {request.Value:0.00}", CombatManagerTheme.Body);
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void HandleSandboxTargetDrag(Rect gridRect, MainframeIntentSnapshot selected)
        {
            if (selected == null || selected.HasLiveTarget || !selected.UsesSandboxTarget)
                return;

            Event current = Event.current;
            Vector2 mouse = current.mousePosition;
            Vector2 targetScreen = CombatManagerGridRenderer.TargetScreenPosition(gridRect, selected);
            if (current.type == EventType.MouseDown &&
                current.button == 0 &&
                gridRect.Contains(mouse) &&
                Vector2.Distance(mouse, targetScreen) <= 18f)
            {
                _draggingSandbox = true;
                current.Use();
            }

            if (_draggingSandbox && current.type == EventType.MouseDrag)
            {
                _sandboxTarget = CombatManagerGridRenderer.ScreenToWorld(gridRect, selected, mouse);
                Refresh(force: true);
                current.Use();
            }

            if (current.type == EventType.MouseUp)
                _draggingSandbox = false;
        }

        private static void LabelPair(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, CombatManagerTheme.Mini, GUILayout.Width(76f));
            GUILayout.Label(string.IsNullOrWhiteSpace(value) ? "--" : value, CombatManagerTheme.Body);
            GUILayout.EndHorizontal();
        }
    }
}
