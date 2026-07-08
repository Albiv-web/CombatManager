using CombatManager.Ai;
using UnityEngine;

namespace CombatManager.Ui
{
    internal static class CombatManagerGridRenderer
    {
        internal static void Draw(Rect rect, AiSimulationState state)
        {
            CombatManagerTheme.Ensure();
            GUI.Box(rect, GUIContent.none, CombatManagerTheme.Panel);

            Rect grid = new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, rect.height - 16f);
            AiSimulationGridProjection projection = AiSimulationGridProjection.For(grid, state);
            AiSimulationFrame frame = state.BuildFrame();

            DrawGrid(projection);
            DrawCircle(projection, Vector3.zero, frame.Radius, new Color(0.9f, 0.78f, 0.18f, 0.55f));
            DrawTrail(projection, state);

            Vector2 target = projection.WorldToScreen(Vector3.zero);
            DrawDiamond(target, 8f, CombatManagerTheme.Target);
            DrawLabel(target + new Vector2(10f, 8f), "target origin");

            Vector2 craft = projection.WorldToScreen(frame.CraftPosition);
            DrawLine(craft, target, new Color(0.75f, 0.95f, 1f, 0.32f), 1f);
            DrawSquare(craft, 7f, CombatManagerTheme.Craft);
            DrawArrow(craft, projection.DirectionToScreen(frame.Heading, 44f), CombatManagerTheme.Craft, 3f);

            if (frame.DesiredTravel.sqrMagnitude > 0.001f)
                DrawArrow(craft + new Vector2(0f, 17f), projection.DirectionToScreen(frame.DesiredTravel, 40f), CombatManagerTheme.Intent, 2f);

            DrawLabel(craft + new Vector2(10f, -24f), $"craft {PlanarMath.GroundDistance(Vector3.zero, frame.CraftPosition):0.#}m");
            DrawLabel(new Vector2(grid.x + 8f, grid.y + 8f), frame.Summary);
            DrawLabel(new Vector2(grid.x + 8f, grid.yMax - 22f), $"{projection.MetersPerPixel:0.#} m/px  |  X/Z target-centered sandbox");

            if (state.Preset == AiSimulationPreset.Broadside)
                DrawLabel(craft + new Vector2(10f, -6f), $"broadside {frame.BroadsideAngle:0.#} deg");
        }

        private static void DrawGrid(AiSimulationGridProjection projection)
        {
            float step = NiceGridStep(projection.VisibleRadius / 4f);
            int lineCount = Mathf.CeilToInt(projection.VisibleRadius / step);

            for (int i = -lineCount; i <= lineCount; i++)
            {
                float offset = i * step;
                Color color = i == 0
                    ? new Color(0.32f, 0.55f, 0.6f, 0.82f)
                    : new Color(0.18f, 0.34f, 0.38f, 0.48f);

                DrawLine(
                    projection.WorldToScreen(new Vector3(-projection.VisibleRadius, 0f, offset)),
                    projection.WorldToScreen(new Vector3(projection.VisibleRadius, 0f, offset)),
                    color,
                    i == 0 ? 2f : 1f);
                DrawLine(
                    projection.WorldToScreen(new Vector3(offset, 0f, -projection.VisibleRadius)),
                    projection.WorldToScreen(new Vector3(offset, 0f, projection.VisibleRadius)),
                    color,
                    i == 0 ? 2f : 1f);
            }
        }

        private static float NiceGridStep(float raw)
        {
            if (raw <= 25f)
                return 25f;
            if (raw <= 50f)
                return 50f;
            if (raw <= 100f)
                return 100f;
            if (raw <= 200f)
                return 200f;
            return 500f;
        }

        private static void DrawTrail(AiSimulationGridProjection projection, AiSimulationState state)
        {
            if (state.Trail.Count < 2)
                return;

            for (int i = 1; i < state.Trail.Count; i++)
            {
                float alpha = Mathf.Lerp(0.08f, 0.5f, i / (float)state.Trail.Count);
                DrawLine(
                    projection.WorldToScreen(state.Trail[i - 1]),
                    projection.WorldToScreen(state.Trail[i]),
                    new Color(0.35f, 1f, 0.65f, alpha),
                    2f);
            }
        }

        private static void DrawCircle(AiSimulationGridProjection projection, Vector3 center, float radius, Color color)
        {
            const int segments = 96;
            Vector2 previous = Vector2.zero;
            for (int i = 0; i <= segments; i++)
            {
                float angle = (Mathf.PI * 2f * i) / segments;
                Vector3 point = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Vector2 current = projection.WorldToScreen(point);
                if (i > 0)
                    DrawLine(previous, current, color, 1f);
                previous = current;
            }
        }

        private static void DrawLabel(Vector2 position, string label)
        {
            GUI.Label(new Rect(position.x, position.y, 260f, 18f), label, CombatManagerTheme.Mini);
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
    }
}
