using CombatManager.Ai;
using UnityEngine;

namespace CombatManager.Ui
{
    internal static class CombatManagerGridRenderer
    {
        private static readonly Color GridMinor = new Color(0.08f, 0.28f, 0.32f, 0.7f);
        private static readonly Color GridMajor = new Color(0.25f, 0.78f, 0.86f, 0.9f);
        private static readonly Color Orbit = new Color(1f, 0.86f, 0.22f, 0.95f);
        private static readonly Color Radial = new Color(0.6f, 0.9f, 1f, 0.45f);
        private static readonly Color Alternate = new Color(0.55f, 0.85f, 1f, 0.34f);
        private static readonly Color TargetPath = new Color(1f, 0.34f, 0.34f, 0.55f);
        private static readonly Color DesiredPoint = new Color(1f, 0.72f, 0.2f, 1f);

        internal static void Draw(Rect rect, AiSimulationState state)
        {
            CombatManagerTheme.Ensure();
            GUI.Box(rect, GUIContent.none, CombatManagerTheme.Panel);

            GUI.BeginGroup(rect);
            Rect grid = new Rect(8f, 8f, rect.width - 16f, rect.height - 16f);
            AiSimulationGridProjection projection = AiSimulationGridProjection.For(grid, state);
            AiSimulationFrame frame = state.BuildFrame();

            DrawGrid(projection);
            DrawOrbit(projection, frame.TargetPosition, frame.Radius);

            if (state.ShowTargetPath)
                DrawPath(projection, state.BuildTargetFuturePath(26, 1f), TargetPath, 2f);

            if (state.ShowTrail)
                DrawTrail(projection, state.Trail, new Color(0.35f, 1f, 0.65f, 0.58f));

            if (state.ShowDesiredTrail)
                DrawTrail(projection, state.DesiredTrail, new Color(1f, 0.72f, 0.2f, 0.48f));

            Vector2 target = projection.WorldToScreen(frame.TargetPosition);
            DrawTargetReticle(target);
            DrawArrow(target, projection.DirectionToScreen(frame.TargetVelocity, 52f), CombatManagerTheme.Target, 2f);
            DrawLabel(target + new Vector2(12f, 8f), $"{frame.TargetProfile} {frame.TargetSpeed:0.#}m/s");

            if (frame.HasDesiredPoint)
            {
                Vector3 point = frame.HasMotionPoint ? frame.MotionPoint : frame.DesiredPoint;
                Vector2 desired = projection.WorldToScreen(point);
                DrawPointMarker(desired, DesiredPoint);
                DrawLine(projection.WorldToScreen(frame.CraftPosition), desired, DesiredPoint, 1f);
                DrawLabel(desired + new Vector2(12f, -20f), "AI motion point");
            }

            Vector2 craft = projection.WorldToScreen(frame.CraftPosition);
            DrawLine(craft, target, Radial, 1f);
            DrawShip(craft, projection.DirectionToScreen(frame.CraftHeading, 1f), CombatManagerTheme.Craft);
            DrawArrow(craft, projection.DirectionToScreen(frame.CraftHeading, 48f), CombatManagerTheme.Craft, 3f);

            if (frame.DesiredTravel.sqrMagnitude > 0.001f)
            {
                Vector2 travel = projection.DirectionToScreen(frame.DesiredTravel, 44f);
                Vector2 offset = new Vector2(-travel.y, travel.x).normalized * 12f;
                DrawArrow(craft + offset, travel, CombatManagerTheme.Intent, 2f);

                if (state.Side == AiSimulationSide.Both && state.Preset == AiSimulationPreset.Circle)
                    DrawArrow(craft - offset, -travel, Alternate, 2f);
            }

            DrawLabel(craft + new Vector2(12f, -26f), $"craft range {frame.GroundRange:0.#}m");
            DrawLabel(new Vector2(grid.x + 8f, grid.y + 8f), $"{frame.Summary} | {frame.AiState}");
            DrawLabel(new Vector2(grid.x + 8f, grid.y + 26f), $"range {frame.GroundRange:0.#}m | azimuth {frame.Azimuth:0.#} deg{(frame.Approximate ? " | approx pathfinding" : string.Empty)}");
            DrawLabel(new Vector2(grid.x + 8f, grid.yMax - 22f), $"{projection.MetersPerPixel:0.##} m/px  |  zoom {state.GridZoom:0.#}x");

            if (state.Preset == AiSimulationPreset.Broadside || state.Preset == AiSimulationPreset.NavalBroadside)
                DrawLabel(craft + new Vector2(12f, -8f), $"broadside {frame.BroadsideAngle:0.#} deg");

            if (state.ShowLegend)
                DrawLegend(grid);

            DrawBorder(grid);
            GUI.EndGroup();
        }

        private static void DrawGrid(AiSimulationGridProjection projection)
        {
            float step = NiceGridStep(projection.VisibleRadius / 4f);
            int xLineCount = Mathf.CeilToInt(projection.VisibleHalfWidth / step);
            int zLineCount = Mathf.CeilToInt(projection.VisibleHalfHeight / step);

            for (int i = -zLineCount; i <= zLineCount; i++)
            {
                float offset = i * step;
                Color color = i == 0 ? GridMajor : GridMinor;
                DrawLine(
                    projection.RelativeToScreen(new Vector3(-projection.VisibleHalfWidth, 0f, offset)),
                    projection.RelativeToScreen(new Vector3(projection.VisibleHalfWidth, 0f, offset)),
                    color,
                    i == 0 ? 2f : 1f);
            }

            for (int i = -xLineCount; i <= xLineCount; i++)
            {
                float offset = i * step;
                Color color = i == 0 ? GridMajor : GridMinor;
                DrawLine(
                    projection.RelativeToScreen(new Vector3(offset, 0f, -projection.VisibleHalfHeight)),
                    projection.RelativeToScreen(new Vector3(offset, 0f, projection.VisibleHalfHeight)),
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

        private static void DrawOrbit(AiSimulationGridProjection projection, Vector3 center, float radius)
        {
            DrawCircle(projection, center, radius, Orbit, 2f);

            const int ticks = 16;
            for (int i = 0; i < ticks; i++)
            {
                float angle = (Mathf.PI * 2f * i) / ticks;
                Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector2 ring = projection.WorldToScreen(center + radial * radius);
                DrawLine(ring, ring + projection.DirectionToScreen(radial, i % 4 == 0 ? 12f : 7f), Orbit, 2f);
            }

            DrawLabel(projection.WorldToScreen(center + Vector3.forward * radius) + new Vector2(8f, -18f), $"{radius:0.#}m");
            DrawLabel(projection.WorldToScreen(center + Vector3.right * radius) + new Vector2(8f, -8f), $"{radius:0.#}m");
        }

        private static void DrawTrail(AiSimulationGridProjection projection, System.Collections.Generic.List<Vector3> points, Color color)
        {
            if (points.Count < 2)
                return;

            for (int i = 1; i < points.Count; i++)
            {
                float alpha = Mathf.Lerp(0.08f, color.a, i / (float)points.Count);
                DrawLine(
                    projection.WorldToScreen(points[i - 1]),
                    projection.WorldToScreen(points[i]),
                    new Color(color.r, color.g, color.b, alpha),
                    2f);
            }
        }

        private static void DrawPath(AiSimulationGridProjection projection, System.Collections.Generic.List<Vector3> points, Color color, float width)
        {
            if (points.Count < 2)
                return;

            for (int i = 1; i < points.Count; i++)
            {
                float alpha = Mathf.Lerp(color.a * 0.25f, color.a, i / (float)points.Count);
                DrawLine(
                    projection.WorldToScreen(points[i - 1]),
                    projection.WorldToScreen(points[i]),
                    new Color(color.r, color.g, color.b, alpha),
                    width);
            }
        }

        private static void DrawCircle(AiSimulationGridProjection projection, Vector3 center, float radius, Color color, float width)
        {
            const int segments = 128;
            Vector2 previous = Vector2.zero;
            for (int i = 0; i <= segments; i++)
            {
                float angle = (Mathf.PI * 2f * i) / segments;
                Vector3 point = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Vector2 current = projection.WorldToScreen(point);
                if (i > 0)
                    DrawLine(previous, current, color, width);
                previous = current;
            }
        }

        private static void DrawTargetReticle(Vector2 center)
        {
            DrawLine(center + new Vector2(-18f, 0f), center + new Vector2(-7f, 0f), CombatManagerTheme.Target, 2f);
            DrawLine(center + new Vector2(7f, 0f), center + new Vector2(18f, 0f), CombatManagerTheme.Target, 2f);
            DrawLine(center + new Vector2(0f, -18f), center + new Vector2(0f, -7f), CombatManagerTheme.Target, 2f);
            DrawLine(center + new Vector2(0f, 7f), center + new Vector2(0f, 18f), CombatManagerTheme.Target, 2f);
            DrawDiamond(center, 7f, CombatManagerTheme.Target);
        }

        private static void DrawShip(Vector2 center, Vector2 direction, Color color)
        {
            Vector2 forward = direction.sqrMagnitude < 0.001f ? new Vector2(0f, -1f) : direction.normalized;
            Vector2 side = new Vector2(-forward.y, forward.x);
            Vector2 bow = center + forward * 14f;
            Vector2 stern = center - forward * 10f;
            Vector2 left = stern + side * 8f;
            Vector2 right = stern - side * 8f;

            DrawLine(bow, left, color, 2f);
            DrawLine(left, stern, color, 2f);
            DrawLine(stern, right, color, 2f);
            DrawLine(right, bow, color, 2f);
            DrawLine(stern, bow, color, 1f);
        }

        private static void DrawLegend(Rect grid)
        {
            Rect legend = new Rect(grid.xMax - 156f, grid.y + 10f, 146f, 92f);
            DrawFilledRect(legend, new Color(0.008f, 0.04f, 0.05f, 0.96f));
            DrawBorder(legend);
            GUI.Label(new Rect(legend.x + 8f, legend.y + 6f, 120f, 18f), "Legend", CombatManagerTheme.Mini);
            DrawLegendRow(legend.x + 8f, legend.y + 26f, CombatManagerTheme.Craft, "Heading");
            DrawLegendRow(legend.x + 8f, legend.y + 44f, CombatManagerTheme.Intent, "Travel");
            DrawLegendRow(legend.x + 8f, legend.y + 62f, DesiredPoint, "AI motion");
            DrawLegendRow(legend.x + 8f, legend.y + 80f, TargetPath, "Target path");
        }

        private static void DrawLegendRow(float x, float y, Color color, string label)
        {
            DrawFilledRect(new Rect(x, y + 4f, 18f, 3f), color);
            GUI.Label(new Rect(x + 25f, y - 2f, 100f, 18f), label, CombatManagerTheme.Mini);
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

        private static void DrawPointMarker(Vector2 center, Color color)
        {
            DrawLine(center + new Vector2(-8f, -8f), center + new Vector2(8f, 8f), color, 2f);
            DrawLine(center + new Vector2(-8f, 8f), center + new Vector2(8f, -8f), color, 2f);
            DrawDiamond(center, 6f, color);
        }

        private static void DrawBorder(Rect rect)
        {
            DrawLine(new Vector2(rect.x, rect.y), new Vector2(rect.xMax, rect.y), GridMajor, 1f);
            DrawLine(new Vector2(rect.xMax, rect.y), new Vector2(rect.xMax, rect.yMax), GridMajor, 1f);
            DrawLine(new Vector2(rect.xMax, rect.yMax), new Vector2(rect.x, rect.yMax), GridMajor, 1f);
            DrawLine(new Vector2(rect.x, rect.yMax), new Vector2(rect.x, rect.y), GridMajor, 1f);
        }

        private static void DrawFilledRect(Rect rect, Color color)
        {
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
