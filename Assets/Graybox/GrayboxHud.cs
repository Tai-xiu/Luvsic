using UnityEngine;

namespace Luvsic.Graybox
{
    public sealed class GrayboxHud : MonoBehaviour
    {
        private static readonly Color PanelColor = new Color(0.035f, 0.065f, 0.078f, 0.96f);
        private static readonly Color Row = new Color(0.09f, 0.15f, 0.17f);
        private static readonly Color Text = new Color(0.92f, 0.95f, 0.94f);
        private static readonly Color Muted = new Color(0.69f, 0.78f, 0.79f);
        private static readonly Color Accent = new Color(0.42f, 0.93f, 0.81f);
        private static readonly Color Danger = new Color(0.96f, 0.53f, 0.39f);

        [SerializeField] private GrayboxFlow flow;
        [SerializeField] private GrayboxPlayer player;

        private Font font;
        private GUIStyle title;
        private GUIStyle body;
        private GUIStyle centered;
        private GUIStyle subtle;
        private GUIStyle button;
        private bool confirmAbandon;

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
            float scale = Mathf.Clamp(Mathf.Min(Screen.width / 960f, Screen.height / 540f), 0.3f, 2f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;
            float height = Screen.height / scale;
            float x = width * 0.5f;

            DrawStatus(width);
            if (flow.Panel != DemoPanel.Pause)
                confirmAbandon = false;

            if (flow.Panel == DemoPanel.None)
                DrawExplore(width, height);
            else
                DrawPanel(width, height);

            if (!string.IsNullOrEmpty(flow.Notice))
            {
                Fill(new Rect(x - 246f, height - 54f, 492f, 34f), PanelColor);
                GUI.Label(new Rect(x - 246f, height - 54f, 492f, 34f), flow.Notice, centered);
            }

            GUI.matrix = previousMatrix;
        }

        private void DrawStatus(float width)
        {
            Fill(new Rect(24f, 24f, 424f, 78f), PanelColor);
            Fill(new Rect(24f, 24f, 3f, 78f), Accent);
            string location = flow.InDungeon ? (flow.Practice ? "地牢练习" : "地牢测试房") : "回收营地";
            GUI.Label(new Rect(41f, 32f, 380f, 27f), location, title);
            GUI.Label(new Rect(42f, 65f, 394f, 23f), flow.Objective, body);

            float left = width - 228f;
            Fill(new Rect(left, 24f, 204f, flow.InDungeon ? 62f : 42f), PanelColor);
            if (flow.InDungeon)
            {
                GUI.Label(new Rect(left + 10f, 26f, 188f, 26f), $"{flow.WeaponName}   ·   生命 {flow.Health}", centered);
                Fill(new Rect(left + 12f, 54f, 180f, 5f), Row);
                Fill(new Rect(left + 12f, 54f, 180f * flow.Health / GrayboxFlow.MaxHealth, 5f), flow.Health <= 30 ? Danger : Accent);
                GUI.Label(new Rect(left + 10f, 59f, 188f, 22f), $"背包 {flow.PackSlots}/{GrayboxFlow.PackCapacity}    药水 {flow.InPack(DemoItem.Potion)}", centered);
            }
            else
            {
                GUI.Label(new Rect(left, 24f, 204f, 42f), $"金币  ·  {flow.Coins}", centered);
            }
        }

        private void DrawExplore(float width, float height)
        {
            float x = width * 0.5f;
            float y = height * 0.5f;
            if (player.HitFlash > 0f)
            {
                Color flash = player.LastHitBlocked ? Accent : Danger;
                flash.a = player.HitFlash * 0.3f;
                Fill(new Rect(0f, 0f, width, 10f), flash);
                Fill(new Rect(0f, height - 10f, width, 10f), flash);
            }
            Fill(new Rect(x - 6f, y - 1f, 12f, 2f), Text);
            Fill(new Rect(x - 1f, y - 6f, 2f, 12f), Text);

            string hint = Cursor.lockState == CursorLockMode.Locked ? flow.Prompt(player.LookTarget()) : "点击画面锁定鼠标";
            if (!string.IsNullOrEmpty(hint))
            {
                Fill(new Rect(x - 150f, height - 101f, 300f, 39f), PanelColor);
                GUI.Label(new Rect(x - 150f, height - 101f, 300f, 39f), hint, centered);
            }

            if (flow.Extracting)
            {
                Fill(new Rect(x - 132f, height - 106f, 264f, 8f), PanelColor);
                Fill(new Rect(x - 132f, height - 106f, 264f * flow.ExtractionProgress, 8f), Accent);
            }

            if (!string.IsNullOrEmpty(flow.CampReport))
            {
                Fill(new Rect(x - 244f, 116f, 488f, 34f), PanelColor);
                GUI.Label(new Rect(x - 244f, 116f, 488f, 34f), flow.CampReport, centered);
            }

            string controls = flow.InDungeon
                ? "WASD 移动   Shift 冲刺   E 交互   Tab 背包   Q 治疗   Esc 暂停"
                : "WASD 移动   鼠标 视角   E 交互   Esc 暂停";
            Fill(new Rect(24f, height - 43f, 490f, 26f), PanelColor);
            GUI.Label(new Rect(34f, height - 43f, 478f, 26f), controls, subtle);
        }

        private void DrawPanel(float width, float height)
        {
            Fill(new Rect(0f, 0f, width, height), new Color(0.01f, 0.025f, 0.03f, 0.64f));
            DemoPanel active = flow.Panel;
            float x = (width - 654f) * 0.5f;
            float y = (height - 326f) * 0.5f;
            Fill(new Rect(x, y, 654f, 326f), PanelColor);
            Fill(new Rect(x, y, 654f, 3f), Accent);

            switch (active)
            {
                case DemoPanel.Merchant:
                case DemoPanel.Warehouse:
                case DemoPanel.Pack:
                    DrawItemPanel(x, y, active);
                    break;
                case DemoPanel.Departure:
                    Header(x, y, "出征整备", $"武器 {flow.WeaponName}    护甲 {(flow.HasArmor ? "皮甲" : "无")}    背包 {flow.PackSlots}/{GrayboxFlow.PackCapacity}");
                    Label(x + 26f, y + 115f, PackSummary());
                    Label(x + 26f, y + 167f, "死亡或放弃会丢失本局携带的物品与付费装备。");
                    if (Button(x + 472f, y + 268f, 154f, "E  ·  确认出发")) flow.Depart();
                    if (Button(x + 26f, y + 268f, 112f, "返回")) flow.ClosePanel();
                    break;
                case DemoPanel.Pause:
                    Header(x, y, "暂停", confirmAbandon ? "放弃会丢失本局携带的物品和付费装备。" : "Esc 返回游戏");
                    if (flow.InDungeon && Button(x + 26f, y + 122f, 166f, confirmAbandon ? "确认放弃本局" : "放弃本局"))
                    {
                        if (confirmAbandon) flow.Abandon();
                        else confirmAbandon = true;
                    }

                    if (Button(x + 26f, y + 268f, 140f, "继续")) flow.ClosePanel();
                    break;
                case DemoPanel.Settlement:
                    Header(x, y, flow.SettlementTitle, flow.SettlementSaved ? "结算完成" : "等待保存");
                    GUI.Label(new Rect(x + 26f, y + 114f, 600f, 90f), flow.SettlementDetails, body);
                    if (flow.SettlementSaved)
                    {
                        if (Button(x + 472f, y + 268f, 154f, "E  ·  返回营地")) flow.ReturnToCamp();
                    }
                    else if (Button(x + 472f, y + 268f, 154f, "重试保存")) flow.RetrySettlement();
                    break;
                case DemoPanel.Recovery:
                    Header(x, y, "存档恢复", flow.CanResetSave ? "两个存档均无法读取。" : "上次出征等待恢复。");
                    if (Button(x + 26f, y + 130f, 234f, flow.CanResetSave ? "保留旧文件并创建新档" : "重试保存"))
                    {
                        if (flow.CanResetSave) flow.ResetInvalidSave();
                        else flow.RetryRecovery();
                    }
                    break;
            }
        }

        private void DrawItemPanel(float x, float y, DemoPanel active)
        {
            bool trading = active == DemoPanel.Merchant;
            bool warehouse = active == DemoPanel.Warehouse;
            string label = trading ? "回收商人" : warehouse ? "仓库与整备" : "局内背包";
            string detail = trading ? $"金币  {flow.Coins}    买入直接进入仓库，卖出从仓库扣除。"
                : $"背包 {flow.PackSlots}/{GrayboxFlow.PackCapacity}    武器 {flow.WeaponName}    护甲 {(flow.HasArmor ? "皮甲" : "无")}";
            Header(x, y, label, detail);

            float rowY = y + 92f;
            foreach (DemoItem id in DemoItemCatalog.Items)
            {
                var item = flow.Item(id);
                int inWarehouse = flow.InWarehouse(id);
                int inPack = flow.InPack(id);
                string quantity = trading ? $"库存 {inWarehouse}" : warehouse ? $"仓库 {inWarehouse}   带入 {inPack}" : $"数量 {inPack}";
                Fill(new Rect(x + 24f, rowY, 606f, 36f), Row);
                GUI.Label(new Rect(x + 34f, rowY, 235f, 36f), item.name + "    " + quantity, body);

                if (trading)
                {
                    if (item.buyPrice >= 0 && Button(x + 376f, rowY + 2f, 112f, $"买入 {item.buyPrice}", flow.Coins >= item.buyPrice)) flow.Buy(id);
                    if (Button(x + 508f, rowY + 2f, 112f, $"卖出 {item.sellPrice}", inWarehouse > 0)) flow.Sell(id);
                }
                else if (warehouse)
                {
                    if (Button(x + 316f, rowY + 2f, 92f, "带入", inWarehouse > 0 && flow.HasFreeSlot(id))) flow.Transfer(id, true);
                    if (Button(x + 422f, rowY + 2f, 92f, "入库", inPack > 0)) flow.Transfer(id, false);
                    if (id == DemoItem.Sword || id == DemoItem.Armor)
                    {
                        bool equipped = id == DemoItem.Sword ? flow.HasSword : flow.HasArmor;
                        if (Button(x + 528f, rowY + 2f, 92f, equipped ? "卸下" : "装备", equipped || inWarehouse > 0)) flow.Equip(id, !equipped);
                    }
                }
                else if (Button(x + 506f, rowY + 2f, 110f, "丢到地上", inPack > 0))
                {
                    flow.Drop(id);
                }

                rowY += 42f;
            }

            if (Button(x + 492f, y + 268f, 132f, active == DemoPanel.Pack ? "Tab  ·  关闭" : "E  ·  关闭"))
                flow.ClosePanel();
        }

        private string PackSummary()
        {
            var entries = new System.Collections.Generic.List<string>();
            foreach (DemoItem item in DemoItemCatalog.Items)
            {
                int count = flow.InPack(item);
                if (count > 0) entries.Add($"{flow.Item(item).name} ×{count}");
            }

            return entries.Count == 0 ? "背包为空，可带旧剑出发。" : string.Join("、", entries);
        }

        private void Header(float x, float y, string heading, string detail)
        {
            GUI.Label(new Rect(x + 26f, y + 20f, 600f, 35f), heading, title);
            GUI.Label(new Rect(x + 26f, y + 56f, 600f, 24f), detail, subtle);
        }

        private void Label(float x, float y, string text)
        {
            GUI.Label(new Rect(x, y, 600f, 36f), text, body);
        }

        private bool Button(float x, float y, float width, string label, bool enabled = true)
        {
            bool previous = GUI.enabled;
            GUI.enabled = previous && enabled;
            bool clicked = GUI.Button(new Rect(x, y, width, 32f), label, button);
            GUI.enabled = previous;
            return clicked;
        }

        private void PrepareStyles()
        {
            title = MakeStyle(GUI.skin.label, 26, Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            body = MakeStyle(GUI.skin.label, 15, Text, TextAnchor.MiddleLeft);
            centered = MakeStyle(GUI.skin.label, 16, Text, TextAnchor.MiddleCenter);
            subtle = MakeStyle(GUI.skin.label, 13, Muted, TextAnchor.MiddleLeft);
            button = MakeStyle(GUI.skin.button, 14, Text, TextAnchor.MiddleCenter);
            button.hover.textColor = Accent;
            button.active.textColor = Accent;
        }

        private GUIStyle MakeStyle(GUIStyle template, int size, Color color, TextAnchor anchor, FontStyle weight = FontStyle.Normal)
        {
            var style = new GUIStyle(template)
            {
                font = font,
                fontSize = size,
                fontStyle = weight,
                alignment = anchor,
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
