using UnityEngine;

namespace Luvsic.Graybox
{
    public sealed class GrayboxHud : MonoBehaviour
    {
        private static readonly Color Panel = new Color(0.035f, 0.065f, 0.078f, 0.94f);
        private static readonly Color Text = new Color(0.92f, 0.95f, 0.94f);
        private static readonly Color Muted = new Color(0.69f, 0.78f, 0.79f);
        private static readonly Color Accent = new Color(0.42f, 0.93f, 0.81f);

        [SerializeField] private GrayboxFlow flow;
        [SerializeField] private GrayboxPlayer player;

        private Font font;
        private GUIStyle title;
        private GUIStyle body;
        private GUIStyle centered;
        private GUIStyle subtle;
        private GUIStyle item;

        private void Awake()
        {
            font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Microsoft YaHei UI", "SimHei", "Arial" }, 20);
        }

        private void OnDestroy()
        {
            if (font != null)
                Destroy(font);
        }

        private void OnGUI()
        {
            if (title == null)
                PrepareStyles();

            Matrix4x4 previousMatrix = GUI.matrix;
            float scale = Mathf.Clamp(Mathf.Min(Screen.width / 960f, Screen.height / 540f), 0.45f, 2f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;
            float height = Screen.height / scale;

            Fill(new Rect(24f, 24f, 388f, 82f), Panel);
            Fill(new Rect(24f, 24f, 3f, 82f), Accent);
            GUI.Label(new Rect(41f, 33f, 355f, 27f), flow.InDungeon ? "地牢测试房" : "回收营地", title);
            GUI.Label(new Rect(42f, 66f, 360f, 25f), flow.Objective, body);

            string inventory = flow.InDungeon
                ? (flow.CarryingLoot ? $"携带  ·  {GrayboxFlow.LootName} ×1" : "携带  ·  空")
                : $"仓库  ·  {flow.WarehouseCount} 件";
            Fill(new Rect(width - 206f, 24f, 182f, 42f), Panel);
            GUI.Label(new Rect(width - 206f, 24f, 182f, 42f), inventory, centered);

            if (flow.WarehouseOpen)
                DrawWarehouse(width, height);
            else
                DrawPlayHud(width, height);

            GUI.matrix = previousMatrix;
        }

        private void DrawPlayHud(float width, float height)
        {
            float x = width * 0.5f;
            float y = height * 0.5f;
            Fill(new Rect(x - 6f, y - 1f, 12f, 2f), Text);
            Fill(new Rect(x - 1f, y - 6f, 2f, 12f), Text);

            bool captured = Cursor.lockState == CursorLockMode.Locked;
            string prompt = captured ? flow.Prompt(player.LookTarget()) : "点击游戏画面以控制视角";
            if (!string.IsNullOrEmpty(prompt))
            {
                Fill(new Rect(x - 150f, height - 96f, 300f, 40f), Panel);
                GUI.Label(new Rect(x - 150f, height - 96f, 300f, 40f), prompt, centered);
            }

            if (flow.Extracting)
            {
                Fill(new Rect(x - 120f, height - 118f, 240f, 8f), Panel);
                Fill(new Rect(x - 120f, height - 118f, 240f * flow.ExtractionProgress, 8f), Accent);
            }

            if (!string.IsNullOrEmpty(flow.CampReport))
            {
                Fill(new Rect(x - 181f, 118f, 362f, 38f), Panel);
                GUI.Label(new Rect(x - 181f, 118f, 362f, 38f), flow.CampReport, centered);
            }

            Fill(new Rect(24f, height - 45f, 340f, 28f), Panel);
            GUI.Label(new Rect(34f, height - 45f, 327f, 28f), "WASD 移动   鼠标 视角   E 交互   Esc 释放鼠标", subtle);
        }

        private void DrawWarehouse(float width, float height)
        {
            Fill(new Rect(0f, 0f, width, height), new Color(0.01f, 0.025f, 0.03f, 0.62f));
            float x = (width - 390f) * 0.5f;
            float y = (height - 198f) * 0.5f;
            Fill(new Rect(x, y, 390f, 198f), Panel);
            Fill(new Rect(x, y, 390f, 3f), Accent);
            GUI.Label(new Rect(x + 25f, y + 22f, 330f, 36f), "营地仓库", title);
            Fill(new Rect(x + 25f, y + 76f, 340f, 46f), new Color(0.09f, 0.15f, 0.17f));
            string contents = flow.WarehouseCount == 0 ? "仓库为空" : $"{GrayboxFlow.LootName}  ×{flow.WarehouseCount}";
            GUI.Label(new Rect(x + 39f, y + 78f, 312f, 42f), contents, item);
            GUI.Label(new Rect(x + 25f, y + 151f, 330f, 27f), "E 或 Esc 关闭", subtle);
        }

        private void PrepareStyles()
        {
            title = MakeStyle(26, Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            body = MakeStyle(15, Text, TextAnchor.MiddleLeft);
            centered = MakeStyle(16, Text, TextAnchor.MiddleCenter);
            subtle = MakeStyle(13, Muted, TextAnchor.MiddleLeft);
            item = MakeStyle(19, Text, TextAnchor.MiddleLeft);
        }

        private GUIStyle MakeStyle(int size, Color color, TextAnchor alignment, FontStyle weight = FontStyle.Normal)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = size,
                fontStyle = weight,
                alignment = alignment,
                padding = new RectOffset()
            };
            style.normal.textColor = color;
            return style;
        }

        private static void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
