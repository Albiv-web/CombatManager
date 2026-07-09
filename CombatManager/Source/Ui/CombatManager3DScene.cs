using System.Collections.Generic;
using CombatManager.Ai;
using UnityEngine;

namespace CombatManager.Ui
{
    internal sealed class CombatManager3DScene
    {
        internal static readonly CombatManager3DScene Shared = new CombatManager3DScene();

        private static readonly Color Background = new Color(0.004f, 0.02f, 0.024f, 1f);
        private static readonly Color GridMinor = new Color(0.07f, 0.24f, 0.28f, 0.8f);
        private static readonly Color GridMajor = new Color(0.18f, 0.7f, 0.78f, 1f);
        private static readonly Color Blue = new Color(0.2f, 0.75f, 1f, 1f);
        private static readonly Color Red = new Color(1f, 0.32f, 0.32f, 1f);
        private static readonly Color BlueOrbit = new Color(1f, 0.86f, 0.22f, 0.95f);
        private static readonly Color RedOrbit = new Color(1f, 0.45f, 0.7f, 0.9f);
        private static readonly Color MotionPoint = new Color(1f, 0.72f, 0.2f, 1f);
        private static readonly Color Pillar = new Color(0.55f, 0.95f, 1f, 0.5f);
        private static readonly Color TrailBlue = new Color(0.35f, 1f, 0.75f, 0.65f);
        private static readonly Color TrailRed = new Color(1f, 0.34f, 0.34f, 0.65f);

        private RenderTexture _texture;
        private Material _material;
        private Camera _camera;

        internal Texture Render(Rect rect, AiSimulationState state, AiDuelFrame frame)
        {
            int width = Mathf.Max(1, Mathf.RoundToInt(rect.width));
            int height = Mathf.Max(1, Mathf.RoundToInt(rect.height));
            EnsureResources(width, height);

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = _texture;
            GL.PushMatrix();
            GL.LoadPixelMatrix(0f, width, height, 0f);
            GL.Clear(true, true, Background);
            _material.SetPass(0);

            var projection = AiSimulation3DProjection.For(new Rect(0f, 0f, width, height), state);
            DrawGrid(projection);
            DrawOrbit(projection, frame.Blue.TargetPosition, frame.Blue.Radius, BlueOrbit);
            DrawOrbit(projection, frame.Red.TargetPosition, frame.Red.Radius, RedOrbit);

            if (state.GraphDetailMode != AiGraphDetailMode.Clean && state.ShowTrail)
            {
                DrawTrail(projection, state.Blue.Trail, TrailBlue);
                DrawTrail(projection, state.Red.Trail, TrailRed);
            }

            if (state.GraphDetailMode != AiGraphDetailMode.Clean && state.ShowDesiredTrail)
            {
                DrawTrail(projection, state.Blue.IntentTrail, new Color(1f, 0.72f, 0.2f, 0.46f));
                DrawTrail(projection, state.Red.IntentTrail, new Color(1f, 0.38f, 0.92f, 0.42f));
            }

            DrawEntity(projection, frame.Blue, Blue, state);
            DrawEntity(projection, frame.Red, Red, state);
            DrawReticle(projection.WorldToScreen(state.Red.Position), Red);

            GL.PopMatrix();
            RenderTexture.active = previous;
            return _texture;
        }

        private void EnsureResources(int width, int height)
        {
            if (_texture == null || _texture.width != width || _texture.height != height)
            {
                if (_texture != null)
                    _texture.Release();
                _texture = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    antiAliasing = 1,
                    filterMode = FilterMode.Bilinear
                };
                _texture.Create();
            }

            if (_material == null)
            {
                Shader shader = Shader.Find("Hidden/Internal-Colored");
                _material = new Material(shader)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                _material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                _material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                _material.SetInt("_ZWrite", 0);
            }

            if (_camera == null)
            {
                var cameraObject = new GameObject("CombatManager Tactical Render Camera")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                Object.DontDestroyOnLoad(cameraObject);
                _camera = cameraObject.AddComponent<Camera>();
                _camera.enabled = false;
                _camera.orthographic = true;
                _camera.clearFlags = CameraClearFlags.SolidColor;
                _camera.backgroundColor = Background;
                _camera.targetTexture = _texture;
            }
            else
            {
                _camera.targetTexture = _texture;
            }
        }

        private static void DrawGrid(AiSimulation3DProjection projection)
        {
            float step = NiceLength(projection.VisibleRadius / 4f);
            float halfWidth = projection.Rect.width * projection.MetersPerPixel * 0.75f;
            float halfHeight = projection.Rect.height * projection.MetersPerPixel * 0.75f;
            int xLineCount = Mathf.CeilToInt(halfWidth / step);
            int zLineCount = Mathf.CeilToInt(halfHeight / step);

            for (int i = -zLineCount; i <= zLineCount; i++)
            {
                float offset = i * step;
                Color color = i == 0 ? GridMajor : GridMinor;
                DrawLine(
                    projection.RelativeToScreen(new Vector3(-halfWidth, 0f, offset)),
                    projection.RelativeToScreen(new Vector3(halfWidth, 0f, offset)),
                    color);
            }

            for (int i = -xLineCount; i <= xLineCount; i++)
            {
                float offset = i * step;
                Color color = i == 0 ? GridMajor : GridMinor;
                DrawLine(
                    projection.RelativeToScreen(new Vector3(offset, 0f, -halfHeight)),
                    projection.RelativeToScreen(new Vector3(offset, 0f, halfHeight)),
                    color);
            }
        }

        private static void DrawOrbit(AiSimulation3DProjection projection, Vector3 center, float radius, Color color)
        {
            const int segments = 128;
            Vector2 previous = Vector2.zero;
            for (int i = 0; i <= segments; i++)
            {
                float angle = (Mathf.PI * 2f * i) / segments;
                Vector3 point = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Vector2 current = projection.WorldToScreen(point);
                if (i > 0)
                    DrawLine(previous, current, color);
                previous = current;
            }
        }

        private static void DrawTrail(AiSimulation3DProjection projection, List<Vector3> points, Color color)
        {
            if (points.Count < 2)
                return;

            for (int i = 1; i < points.Count; i++)
            {
                float alpha = Mathf.Lerp(0.08f, color.a, i / (float)points.Count);
                DrawLine(
                    projection.WorldToScreen(points[i - 1]),
                    projection.WorldToScreen(points[i]),
                    new Color(color.r, color.g, color.b, alpha));
            }
        }

        private static void DrawEntity(AiSimulation3DProjection projection, AiSimulationFrame frame, Color color, AiSimulationState state)
        {
            Vector2 craft = projection.WorldToScreen(frame.CraftPosition);
            Vector2 ground = projection.WorldToScreen(new Vector3(frame.CraftPosition.x, 0f, frame.CraftPosition.z));
            DrawLine(craft, ground, Pillar);

            if (state.GraphDetailMode != AiGraphDetailMode.Clean && state.ShowMotionPoint && frame.HasMotionPoint)
            {
                Vector2 motion = projection.WorldToScreen(frame.MotionPoint);
                Vector2 motionGround = projection.WorldToScreen(new Vector3(frame.MotionPoint.x, 0f, frame.MotionPoint.z));
                DrawLine(motion, motionGround, new Color(MotionPoint.r, MotionPoint.g, MotionPoint.b, 0.45f));
                DrawLine(craft, motion, MotionPoint);
                DrawCross(motion, MotionPoint, 6f);
            }

            Vector2 forward = projection.DirectionToScreen(frame.CraftHeading, 1f);
            DrawShip(craft, forward, color);
            DrawArrow(craft, projection.DirectionToScreen(frame.CraftHeading, 42f), color);
        }

        private static void DrawReticle(Vector2 center, Color color)
        {
            DrawLine(center + new Vector2(-18f, 0f), center + new Vector2(-7f, 0f), color);
            DrawLine(center + new Vector2(7f, 0f), center + new Vector2(18f, 0f), color);
            DrawLine(center + new Vector2(0f, -18f), center + new Vector2(0f, -7f), color);
            DrawLine(center + new Vector2(0f, 7f), center + new Vector2(0f, 18f), color);
        }

        private static void DrawShip(Vector2 center, Vector2 direction, Color color)
        {
            Vector2 forward = direction.sqrMagnitude < 0.001f ? new Vector2(0f, -1f) : direction.normalized;
            Vector2 side = new Vector2(-forward.y, forward.x);
            Vector2 bow = center + forward * 14f;
            Vector2 stern = center - forward * 10f;
            DrawTriangle(bow, stern + side * 8f, stern - side * 8f, color);
            DrawLine(stern, bow, new Color(0f, 0f, 0f, 0.65f));
        }

        private static void DrawArrow(Vector2 start, Vector2 delta, Color color)
        {
            if (delta.sqrMagnitude < 0.001f)
                return;
            Vector2 end = start + delta;
            DrawLine(start, end, color);
            Vector2 direction = delta.normalized;
            Vector2 side = new Vector2(-direction.y, direction.x);
            DrawLine(end, end - direction * 9f + side * 5f, color);
            DrawLine(end, end - direction * 9f - side * 5f, color);
        }

        private static void DrawCross(Vector2 center, Color color, float size)
        {
            DrawLine(center + new Vector2(-size, -size), center + new Vector2(size, size), color);
            DrawLine(center + new Vector2(-size, size), center + new Vector2(size, -size), color);
        }

        private static void DrawLine(Vector2 a, Vector2 b, Color color)
        {
            GL.Begin(GL.LINES);
            GL.Color(color);
            GL.Vertex3(a.x, a.y, 0f);
            GL.Vertex3(b.x, b.y, 0f);
            GL.End();
        }

        private static void DrawTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            GL.Begin(GL.TRIANGLES);
            GL.Color(color);
            GL.Vertex3(a.x, a.y, 0f);
            GL.Vertex3(b.x, b.y, 0f);
            GL.Vertex3(c.x, c.y, 0f);
            GL.End();
        }

        private static float NiceLength(float raw)
        {
            raw = Mathf.Max(1f, raw);
            float exponent = Mathf.Pow(10f, Mathf.Floor(Mathf.Log10(raw)));
            float fraction = raw / exponent;
            float nice;
            if (fraction <= 1f)
                nice = 1f;
            else if (fraction <= 2f)
                nice = 2f;
            else if (fraction <= 5f)
                nice = 5f;
            else
                nice = 10f;
            return nice * exponent;
        }
    }
}
