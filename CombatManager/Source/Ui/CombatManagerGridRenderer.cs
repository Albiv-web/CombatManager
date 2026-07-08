using System.Collections.Generic;
using CombatManager.Ai;
using UnityEngine;

namespace CombatManager.Ui
{
    internal static class CombatManagerGridRenderer
    {
        internal static void Draw(Rect rect, MainframeIntentSnapshot snapshot)
        {
            CombatManagerTheme.Ensure();
            GUI.Box(rect, GUIContent.none, CombatManagerTheme.Panel);

            if (snapshot == null)
            {
                GUI.Label(Inner(rect), "No mainframe selected.", CombatManagerTheme.Body);
                return;
            }

            GridTransform transform = GridTransform.For(rect, snapshot);
            DrawGrid(rect, transform);
            DrawPredictionRings(transform, snapshot);

            Vector2 craft = transform.WorldToScreen(snapshot.CraftPosition);
            DrawDisc(craft, 6f, CombatManagerTheme.Craft);
            DrawArrow(craft, transform.DirectionToScreen(snapshot.CraftRotation * Vector3.forward, 34f), CombatManagerTheme.Craft, 3f);
            DrawLabel(craft + new Vector2(8f, -22f), $"craft Y {snapshot.CraftPosition.y:0.#}");

            if (snapshot.HasLiveTarget || snapshot.UsesSandboxTarget)
            {
                Vector2 target = transform.WorldToScreen(snapshot.TargetPosition);
                DrawDiamond(target, 7f, snapshot.UsesSandboxTarget ? CombatManagerTheme.Amber : CombatManagerTheme.Target);
                DrawArrow(target, transform.DirectionToScreen(snapshot.TargetVelocity, 38f), CombatManagerTheme.Target, 2f);
                DrawLabel(target + new Vector2(8f, 8f), snapshot.UsesSandboxTarget ? "sandbox target" : $"target {snapshot.TargetRange:0.#}m");
            }

            AiIntentPrediction prediction = snapshot.Prediction;
            if (prediction != null && prediction.HasPoint)
            {
                Vector2 goal = transform.WorldToScreen(prediction.DesiredPoint);
                DrawSquare(goal, 7f, prediction.Supported ? CombatManagerTheme.Intent : CombatManagerTheme.Amber);
                DrawLine(craft, goal, CombatManagerTheme.Intent, 2f);
                if (prediction.HasFacing)
                    DrawArrow(goal, transform.DirectionToScreen(prediction.DesiredRotation * Vector3.forward, 34f), CombatManagerTheme.Intent, 2f);
                DrawLabel(goal + new Vector2(8f, -20f), prediction.Kind);
            }

            DrawControlRequests(transform, snapshot);
            GUI.Label(new Rect(rect.x + 8f, rect.y + rect.height - 22f, rect.width - 16f, 18f),
                $"{transform.MetersPerPixel:0.#} m/px  |  X/Z top-down", CombatManagerTheme.Mini);
        }

        internal static Vector3 ScreenToWorld(Rect rect, MainframeIntentSnapshot snapshot, Vector2 mousePosition)
        {
            GridTransform transform = GridTransform.For(rect, snapshot);
            return transform.ScreenToWorld(mousePosition, snapshot.CraftPosition.y);
        }

        internal static Vector2 TargetScreenPosition(Rect rect, MainframeIntentSnapshot snapshot)
        {
            return GridTransform.For(rect, snapshot).WorldToScreen(snapshot.TargetPosition);
        }

        private static Rect Inner(Rect rect) =>
            new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, rect.height - 16f);

        private static void DrawGrid(Rect rect, GridTransform transform)
        {
            const int lines = 8;
            for (int i = -lines; i <= lines; i++)
            {
                float offset = i * 100f;
                Vector3 horizontalA = transform.CenterWorld + new Vector3(-lines * 100f, 0f, offset);
                Vector3 horizontalB = transform.CenterWorld + new Vector3(lines * 100f, 0f, offset);
                Vector3 verticalA = transform.CenterWorld + new Vector3(offset, 0f, -lines * 100f);
                Vector3 verticalB = transform.CenterWorld + new Vector3(offset, 0f, lines * 100f);
                Color color = i == 0
                    ? new Color(0.32f, 0.55f, 0.6f, 0.75f)
                    : new Color(0.18f, 0.34f, 0.38f, 0.45f);
                DrawLine(transform.WorldToScreen(horizontalA), transform.WorldToScreen(horizontalB), color, i == 0 ? 2f : 1f);
                DrawLine(transform.WorldToScreen(verticalA), transform.WorldToScreen(verticalB), color, i == 0 ? 2f : 1f);
            }
        }

        private static void DrawPredictionRings(GridTransform transform, MainframeIntentSnapshot snapshot)
        {
            AiIntentPrediction prediction = snapshot.Prediction;
            if (prediction == null || (!snapshot.HasLiveTarget && !snapshot.UsesSandboxTarget))
                return;

            if (prediction.MaintainDistanceLower > 0f)
                DrawCircle(transform, snapshot.TargetPosition, prediction.MaintainDistanceLower, new Color(0.9f, 0.9f, 0.2f, 0.55f));
            if (prediction.MaintainDistanceUpper > prediction.MaintainDistanceLower + 1f)
                DrawCircle(transform, snapshot.TargetPosition, prediction.MaintainDistanceUpper, new Color(0.9f, 0.55f, 0.2f, 0.5f));
        }

        private static void DrawControlRequests(GridTransform transform, MainframeIntentSnapshot snapshot)
        {
            Vector2 craft = transform.WorldToScreen(snapshot.CraftPosition);
            foreach (AiControlRequestSnapshot request in snapshot.Requests)
            {
                Vector2 direction = RequestDirection(request.Type);
                if (direction == Vector2.zero)
                    continue;

                Vector2 scaled = direction.normalized * Mathf.Lerp(18f, 48f, Mathf.Clamp01(request.Value));
                DrawArrow(craft + direction.normalized * 46f, scaled, CombatManagerTheme.Amber, 2f);
            }
        }

        private static Vector2 RequestDirection(BrilliantSkies.Ai.Interfaces.AiControlType type)
        {
            switch (type)
            {
                case BrilliantSkies.Ai.Interfaces.AiControlType.ThrustForward:
                    return new Vector2(0f, -1f);
                case BrilliantSkies.Ai.Interfaces.AiControlType.ThrustBackward:
                    return new Vector2(0f, 1f);
                case BrilliantSkies.Ai.Interfaces.AiControlType.StrafeRight:
                    return new Vector2(1f, 0f);
                case BrilliantSkies.Ai.Interfaces.AiControlType.StrafeLeft:
                    return new Vector2(-1f, 0f);
                case BrilliantSkies.Ai.Interfaces.AiControlType.YawLeft:
                    return new Vector2(-0.7f, -0.7f);
                case BrilliantSkies.Ai.Interfaces.AiControlType.YawRight:
                    return new Vector2(0.7f, -0.7f);
                default:
                    return Vector2.zero;
            }
        }

        private static void DrawCircle(GridTransform transform, Vector3 center, float radius, Color color)
        {
            const int segments = 72;
            Vector2 previous = Vector2.zero;
            for (int i = 0; i <= segments; i++)
            {
                float angle = (Mathf.PI * 2f * i) / segments;
                Vector3 point = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Vector2 current = transform.WorldToScreen(point);
                if (i > 0)
                    DrawLine(previous, current, color, 1f);
                previous = current;
            }
        }

        private static void DrawLabel(Vector2 position, string label)
        {
            GUI.Label(new Rect(position.x, position.y, 220f, 18f), label, CombatManagerTheme.Mini);
        }

        private static void DrawDisc(Vector2 center, float radius, Color color)
        {
            DrawSquare(center, radius, color);
        }

        private static void DrawDiamond(Vector2 center, float radius, Color color)
        {
            Vector2 top = center + new Vector2(0f, -radius);
            Vector2 right = center + new Vector2(radius, 0f);
            Vector2 bottom = center + new Vector2(0f, radius);
            Vector2 left = center + new Vector2(-radius, 0f);
            DrawLine(top, right, color, 2f);
            DrawLine(right, bottom, color, 2f);
            DrawLine(bottom, left, color, 2f);
            DrawLine(left, top, color, 2f);
        }

        private static void DrawSquare(Vector2 center, float radius, Color color)
        {
            Rect rect = new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f);
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, CombatManagerTheme.GridTexture);
            GUI.color = old;
        }

        private static void DrawArrow(Vector2 start, Vector2 direction, Color color, float width)
        {
            if (direction.sqrMagnitude < 0.01f)
                return;

            Vector2 end = start + direction;
            DrawLine(start, end, color, width);
            Vector2 normal = direction.normalized;
            Vector2 left = new Vector2(-normal.y, normal.x);
            DrawLine(end, end - normal * 8f + left * 5f, color, width);
            DrawLine(end, end - normal * 8f - left * 5f, color, width);
        }

        private static void DrawLine(Vector2 from, Vector2 to, Color color, float width)
        {
            Vector2 delta = to - from;
            if (delta.sqrMagnitude < 0.01f)
                return;

            Color oldColor = GUI.color;
            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.color = color;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            GUIUtility.RotateAroundPivot(angle, from);
            GUI.DrawTexture(new Rect(from.x, from.y - width * 0.5f, delta.magnitude, width), CombatManagerTheme.GridTexture);
            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
        }

        private struct GridTransform
        {
            internal Rect Rect;
            internal Vector3 CenterWorld;
            internal float MetersPerPixel;

            internal static GridTransform For(Rect rect, MainframeIntentSnapshot snapshot)
            {
                var points = new List<Vector3> { snapshot.CraftPosition };
                if (snapshot.HasLiveTarget || snapshot.UsesSandboxTarget)
                    points.Add(snapshot.TargetPosition);
                if (snapshot.Prediction != null && snapshot.Prediction.HasPoint)
                    points.Add(snapshot.Prediction.DesiredPoint);

                Vector3 center = Vector3.zero;
                foreach (Vector3 point in points)
                    center += point;
                center /= Mathf.Max(1, points.Count);

                float radius = 250f;
                foreach (Vector3 point in points)
                    radius = Mathf.Max(radius, PlanarMath.GroundDistance(center, point) * 1.35f + 80f);

                return new GridTransform
                {
                    Rect = rect,
                    CenterWorld = center,
                    MetersPerPixel = radius * 2f / Mathf.Max(1f, Mathf.Min(rect.width, rect.height))
                };
            }

            internal Vector2 WorldToScreen(Vector3 world)
            {
                Vector3 delta = world - CenterWorld;
                float pixelsPerMeter = 1f / Mathf.Max(0.001f, MetersPerPixel);
                return new Vector2(
                    Rect.center.x + delta.x * pixelsPerMeter,
                    Rect.center.y - delta.z * pixelsPerMeter);
            }

            internal Vector3 ScreenToWorld(Vector2 screen, float y)
            {
                float metersPerPixel = Mathf.Max(0.001f, MetersPerPixel);
                Vector2 delta = screen - Rect.center;
                return new Vector3(
                    CenterWorld.x + delta.x * metersPerPixel,
                    y,
                    CenterWorld.z - delta.y * metersPerPixel);
            }

            internal Vector2 DirectionToScreen(Vector3 direction, float pixels)
            {
                Vector3 flat = PlanarMath.Flatten(direction);
                if (flat.sqrMagnitude < 0.0001f)
                    return Vector2.zero;
                Vector2 xz = new Vector2(flat.x, -flat.z).normalized;
                return xz * pixels;
            }
        }
    }
}
