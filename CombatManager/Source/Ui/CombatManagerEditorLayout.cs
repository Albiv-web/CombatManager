using UnityEngine;

namespace CombatManager.Ui
{
    internal readonly struct CombatManagerEditorLayout
    {
        internal CombatManagerEditorLayout(Rect root, Rect toolbar, Rect warning, Rect bluePanel, Rect grid, Rect redPanel, float sidePanelWidth)
        {
            Root = root;
            Toolbar = toolbar;
            Warning = warning;
            BluePanel = bluePanel;
            Grid = grid;
            RedPanel = redPanel;
            SidePanelWidth = sidePanelWidth;
        }

        internal Rect Root { get; }
        internal Rect Toolbar { get; }
        internal Rect Warning { get; }
        internal Rect BluePanel { get; }
        internal Rect Grid { get; }
        internal Rect RedPanel { get; }
        internal float SidePanelWidth { get; }

        internal static CombatManagerEditorLayout For(float screenWidth, float screenHeight)
        {
            float width = Mathf.Max(1f, screenWidth);
            float height = Mathf.Max(1f, screenHeight);
            float margin = 8f;
            float gap = 8f;
            float toolbarHeight = 58f;
            float warningHeight = 24f;
            float minimumGridWidth = 420f;

            float desiredSide = width >= 1500f ? 320f : 260f;
            float maxSideForGrid = (width - (margin * 2f) - (gap * 2f) - minimumGridWidth) * 0.5f;
            float sideWidth = Mathf.Clamp(Mathf.Min(desiredSide, maxSideForGrid), 160f, 320f);

            Rect root = new Rect(0f, 0f, width, height);
            Rect toolbar = new Rect(margin, 4f, Mathf.Max(1f, width - (margin * 2f)), toolbarHeight);
            Rect warning = new Rect(margin, toolbar.yMax + 4f, toolbar.width, warningHeight);
            float bodyY = warning.yMax + 6f;
            float bodyHeight = Mathf.Max(1f, height - bodyY - margin);

            Rect bluePanel = new Rect(margin, bodyY, sideWidth, bodyHeight);
            Rect redPanel = new Rect(width - margin - sideWidth, bodyY, sideWidth, bodyHeight);
            float gridX = bluePanel.xMax + gap;
            Rect grid = new Rect(gridX, bodyY, Mathf.Max(1f, redPanel.x - gridX - gap), bodyHeight);

            return new CombatManagerEditorLayout(root, toolbar, warning, bluePanel, grid, redPanel, sideWidth);
        }
    }
}
