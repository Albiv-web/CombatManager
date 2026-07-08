using UnityEngine;

namespace CombatManager.Ui
{
    internal readonly struct CombatManagerEditorLayout
    {
        internal CombatManagerEditorLayout(
            Rect root,
            Rect toolbar,
            Rect toolbarLeft,
            Rect toolbarMiddle,
            Rect toolbarRight,
            Rect warning,
            Rect bluePanel,
            Rect blueTabContent,
            Rect grid,
            Rect redPanel,
            Rect redTabContent,
            float sidePanelWidth)
        {
            Root = root;
            Toolbar = toolbar;
            ToolbarLeft = toolbarLeft;
            ToolbarMiddle = toolbarMiddle;
            ToolbarRight = toolbarRight;
            Warning = warning;
            BluePanel = bluePanel;
            BlueTabContent = blueTabContent;
            Grid = grid;
            RedPanel = redPanel;
            RedTabContent = redTabContent;
            SidePanelWidth = sidePanelWidth;
        }

        internal Rect Root { get; }
        internal Rect Toolbar { get; }
        internal Rect ToolbarLeft { get; }
        internal Rect ToolbarMiddle { get; }
        internal Rect ToolbarRight { get; }
        internal Rect Warning { get; }
        internal Rect BluePanel { get; }
        internal Rect BlueTabContent { get; }
        internal Rect Grid { get; }
        internal Rect RedPanel { get; }
        internal Rect RedTabContent { get; }
        internal float SidePanelWidth { get; }

        internal static CombatManagerEditorLayout For(float screenWidth, float screenHeight)
        {
            float width = Mathf.Max(1f, screenWidth);
            float height = Mathf.Max(1f, screenHeight);
            float margin = 8f;
            float gap = 8f;
            float toolbarHeight = 74f;
            float warningHeight = 24f;
            float minimumGridWidth = 560f;

            float desiredSide = width >= 1700f ? 380f : width >= 1500f ? 360f : 300f;
            float maxSideForGrid = (width - (margin * 2f) - (gap * 2f) - minimumGridWidth) * 0.5f;
            float sideWidth = Mathf.Clamp(Mathf.Min(desiredSide, maxSideForGrid), 200f, 380f);

            Rect root = new Rect(0f, 0f, width, height);
            Rect toolbar = new Rect(margin, 4f, Mathf.Max(1f, width - (margin * 2f)), toolbarHeight);
            Rect toolbarInner = Inset(toolbar, 6f, 6f);
            float middleWidth = Mathf.Min(222f, toolbarInner.width * 0.2f);
            float leftWidth = Mathf.Clamp(toolbarInner.width * 0.34f, 430f, 560f);
            float rightWidth = Mathf.Max(1f, toolbarInner.width - leftWidth - middleWidth - (gap * 2f));
            Rect toolbarLeft = new Rect(toolbarInner.x, toolbarInner.y, leftWidth, toolbarInner.height);
            Rect toolbarMiddle = new Rect(toolbarLeft.xMax + gap, toolbarInner.y, middleWidth, toolbarInner.height);
            Rect toolbarRight = new Rect(toolbarMiddle.xMax + gap, toolbarInner.y, rightWidth, toolbarInner.height);
            Rect warning = new Rect(margin, toolbar.yMax + 4f, toolbar.width, warningHeight);
            float bodyY = warning.yMax + 6f;
            float bodyHeight = Mathf.Max(1f, height - bodyY - margin);

            Rect bluePanel = new Rect(margin, bodyY, sideWidth, bodyHeight);
            Rect redPanel = new Rect(width - margin - sideWidth, bodyY, sideWidth, bodyHeight);
            float gridX = bluePanel.xMax + gap;
            Rect grid = new Rect(gridX, bodyY, Mathf.Max(1f, redPanel.x - gridX - gap), bodyHeight);
            Rect blueContent = TabContentFor(bluePanel);
            Rect redContent = TabContentFor(redPanel);

            return new CombatManagerEditorLayout(root, toolbar, toolbarLeft, toolbarMiddle, toolbarRight, warning, bluePanel, blueContent, grid, redPanel, redContent, sideWidth);
        }

        internal static Rect TabContentFor(Rect panel)
        {
            const float padding = 10f;
            const float titleHeight = 28f;
            const float tabHeight = 30f;
            float y = panel.y + padding + titleHeight + 6f + tabHeight + 8f;
            return new Rect(panel.x + padding, y, Mathf.Max(1f, panel.width - (padding * 2f)), Mathf.Max(1f, panel.yMax - y - padding));
        }

        private static Rect Inset(Rect rect, float x, float y)
        {
            return new Rect(rect.x + x, rect.y + y, Mathf.Max(1f, rect.width - (x * 2f)), Mathf.Max(1f, rect.height - (y * 2f)));
        }
    }
}
