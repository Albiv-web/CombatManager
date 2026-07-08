using UnityEngine;

namespace CombatManager.Ui
{
    internal static class CombatManagerTheme
    {
        private static bool _ready;
        private static Texture2D _panel;
        private static Texture2D _header;
        private static Texture2D _row;
        private static Texture2D _selected;
        private static Texture2D _field;
        private static Texture2D _grid;

        internal static GUIStyle Window { get; private set; }
        internal static GUIStyle Title { get; private set; }
        internal static GUIStyle Header { get; private set; }
        internal static GUIStyle Body { get; private set; }
        internal static GUIStyle BodyWrap { get; private set; }
        internal static GUIStyle Mini { get; private set; }
        internal static GUIStyle Button { get; private set; }
        internal static GUIStyle ActiveButton { get; private set; }
        internal static GUIStyle Row { get; private set; }
        internal static GUIStyle SelectedRow { get; private set; }
        internal static GUIStyle Panel { get; private set; }
        internal static GUIStyle Warning { get; private set; }
        internal static Texture2D WindowTexture => _panel;
        internal static Texture2D GridTexture => _grid;

        internal static readonly Color Cyan = new Color(0.05f, 0.9f, 1f, 1f);
        internal static readonly Color Green = new Color(0.35f, 1f, 0.45f, 1f);
        internal static readonly Color Amber = new Color(1f, 0.72f, 0.2f, 1f);
        internal static readonly Color Red = new Color(1f, 0.28f, 0.24f, 1f);
        internal static readonly Color Target = new Color(1f, 0.32f, 0.32f, 1f);
        internal static readonly Color Craft = new Color(0.2f, 0.75f, 1f, 1f);
        internal static readonly Color Intent = new Color(0.4f, 1f, 0.65f, 1f);

        internal static void Ensure()
        {
            if (_ready)
                return;

            _panel = Solid(new Color(0.01f, 0.035f, 0.045f, 1f), "CM panel");
            _header = Solid(new Color(0.025f, 0.18f, 0.22f, 1f), "CM header");
            _row = Solid(new Color(0.015f, 0.105f, 0.13f, 1f), "CM row");
            _selected = Solid(new Color(0.02f, 0.45f, 0.54f, 1f), "CM selected");
            _field = Solid(new Color(0.012f, 0.065f, 0.075f, 1f), "CM field");
            _grid = Solid(Color.white, "CM grid pixel");

            Window = new GUIStyle(GUI.skin.window)
            {
                normal = { background = _panel, textColor = Color.white },
                border = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                overflow = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(10, 10, 24, 10),
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            Title = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = Color.white },
                alignment = TextAnchor.UpperCenter,
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(0, 0, 2, 0)
            };
            Panel = new GUIStyle(GUI.skin.box)
            {
                normal = { background = _field, textColor = Color.white },
                border = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                overflow = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(8, 8, 8, 8)
            };
            Header = new GUIStyle(GUI.skin.label)
            {
                normal = { background = _header, textColor = Color.white },
                border = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 2, 2),
                overflow = new RectOffset(0, 0, 0, 0),
                alignment = TextAnchor.MiddleLeft,
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(6, 6, 3, 3)
            };
            Body = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = Color.white },
                fontSize = 12,
                wordWrap = false
            };
            BodyWrap = new GUIStyle(Body)
            {
                wordWrap = true
            };
            Mini = new GUIStyle(Body)
            {
                fontSize = 10,
                normal = { textColor = new Color(0.75f, 0.92f, 0.96f, 1f) }
            };
            Warning = new GUIStyle(BodyWrap)
            {
                normal = { textColor = Amber },
                fontStyle = FontStyle.Bold
            };
            Button = new GUIStyle(GUI.skin.button)
            {
                normal = { background = _row, textColor = Color.white },
                hover = { background = _selected, textColor = Color.white },
                active = { background = _selected, textColor = Color.white },
                border = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(1, 1, 1, 1),
                overflow = new RectOffset(0, 0, 0, 0),
                fontSize = 11,
                padding = new RectOffset(6, 6, 3, 3)
            };
            ActiveButton = new GUIStyle(Button)
            {
                normal = { background = _selected, textColor = Color.white },
                fontStyle = FontStyle.Bold
            };
            Row = new GUIStyle(Button)
            {
                alignment = TextAnchor.MiddleLeft,
                margin = new RectOffset(0, 0, 1, 1)
            };
            SelectedRow = new GUIStyle(Row)
            {
                normal = { background = _selected, textColor = Color.white },
                fontStyle = FontStyle.Bold
            };
            _ready = true;
        }

        private static Texture2D Solid(Color color, string name)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = name,
                hideFlags = HideFlags.DontUnloadUnusedAsset
            };
            texture.SetPixel(0, 0, color);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            return texture;
        }
    }
}
