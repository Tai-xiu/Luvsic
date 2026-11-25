using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Luvsic.Graybox
{
    public enum DemoPanel
    {
        None, Warehouse, Merchant, Pack, Departure, Pause, Settlement, Recovery
    }

    public sealed class GrayboxFlow : MonoBehaviour
    {
        public const string CampScene = "Camp";
        public const string DungeonScene = "Dungeon";
        public const int PackCapacity = 8;
        public const int MaxHealth = 100;

        private const float ExtractionDuration = 5f;
        private static DemoSaveStore store;
        private static DemoProfile profile;
        private static Run run;
        private static string campReport;

        [SerializeField] private bool dungeon;
        [SerializeField] private GrayboxPlayer player;
        [SerializeField] private BoxCollider extractionZone;
        [SerializeField] private GameObject loot;
        [SerializeField] private Transform chestLid;
        [SerializeField] private DemoItemCatalog catalog;

        private bool changingScene;
        private bool chestOpened;
        private float extractionTime = -1f;
        private DemoProfile settlementProfile;
        private bool settlementSaved;

        public bool InDungeon => dungeon;
        public DemoPanel Panel { get; private set; }
        public bool Paused => Panel == DemoPanel.Pause;
        public bool Simulating => dungeon && !changingScene && !Paused && Panel != DemoPanel.Settlement && Panel != DemoPanel.Recovery;
        public bool Extracting => extractionTime >= 0f;
        public float ExtractionProgress => Mathf.Clamp01(extractionTime / ExtractionDuration);
        public int ExtractionSeconds => Mathf.Max(0, Mathf.CeilToInt(ExtractionDuration - extractionTime));
        public int Health { get; private set; } = MaxHealth;
        public bool HasSword => dungeon ? run != null && run.sword : profile != null && profile.sword;
        public bool HasArmor => dungeon ? run != null && run.armor : profile != null && profile.armor;
        public string WeaponName => HasSword ? catalog[DemoItem.Sword].name : "旧剑";
        public int Coins => profile?.coins ?? 0;
        public int PackSlots => CurrentPack?.Slots(catalog) ?? 0;
        public string Notice { get; private set; }
        public string CampReport => dungeon ? null : campReport;
        public string SettlementTitle { get; private set; }
        public string SettlementDetails { get; private set; }
        public bool SettlementSaved => settlementSaved;
        public bool CanResetSave => profile == null;
        public bool Practice => dungeon && run != null && string.IsNullOrEmpty(run.id);

        private DemoInventory CurrentPack => dungeon ? run?.pack : profile?.prepared;
        private bool InExtractionZone => dungeon &&
            extractionZone.bounds.Contains(player.transform.position + Vector3.up * 0.5f);

        public string Objective
        {
            get
            {
                if (!dungeon)
                    return "商人在左侧。整理装备后，从青色门出发。";
                if (Extracting)
                    return $"留在撤离平台内 · {ExtractionSeconds} 秒";
                if (chestOpened && loot.activeSelf)
                    return "对准箱子上方的战利品，按 E 拾取。";
                if (chestOpened)
                    return "前往房间后方的青色撤离平台。";
                return "找到黄色宝箱。左键攻击，按住右键格挡。";
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            store = null;
            profile = null;
            run = null;
            campReport = null;
        }

        private void Awake()
        {
            if (store == null)
            {
                store = new DemoSaveStore(Path.Combine(Application.persistentDataPath, "LuvsicDemo"));
                profile = store.Load(out string notice);
                campReport = notice;
                RecoverInterruptedRun();
            }

            if (profile == null || (run == null && !string.IsNullOrEmpty(profile.pendingRun)))
                SetPanel(DemoPanel.Recovery);
            else if (dungeon && run == null)
                run = new Run("", new DemoProfile());
        }

        private void LateUpdate()
        {
            if (!Simulating || !Extracting)
                return;

            if (!InExtractionZone)
            {
                extractionTime = -1f;
                return;
            }

            extractionTime += Time.deltaTime;
            if (extractionTime >= ExtractionDuration && Health > 0)
                EndRun(true, "撤离成功");
        }

        public DemoItemCatalog.Definition Item(DemoItem item) => catalog[item];
        public int InWarehouse(DemoItem item) => profile?.warehouse.Count(item) ?? 0;
        public int InPack(DemoItem item) => CurrentPack?.Count(item) ?? 0;
        public bool HasFreeSlot(DemoItem item) => CurrentPack != null && CurrentPack.CanAdd(item, catalog);

        public string Prompt(GrayboxInteractable target)
        {
            if (changingScene || Panel != DemoPanel.None || Extracting)
                return null;

            if (target != null)
            {
                switch (target.Kind)
                {
                    case GrayboxInteraction.Merchant: return "E  ·  与回收商人交易";
                    case GrayboxInteraction.Warehouse: return "E  ·  打开仓库与整备";
                    case GrayboxInteraction.Departure: return "E  ·  检查装备并出发";
                    case GrayboxInteraction.Chest: return chestOpened ? "宝箱已打开" : "E  ·  打开宝箱";
                    case GrayboxInteraction.Loot:
                        return HasFreeSlot(target.Item) ? $"E  ·  拾取{catalog[target.Item].name}" : "背包已满";
                    case GrayboxInteraction.Extraction:
                        return InExtractionZone ? "E  ·  开始撤离" : "走上青色平台后按 E 撤离";
                }
            }

            return InExtractionZone ? "E  ·  开始撤离" : null;
        }

        public bool Interact(GrayboxInteractable target)
        {
            if (changingScene || Panel != DemoPanel.None || Extracting || profile == null)
                return false;

            if (target == null)
                return StartExtraction();

            switch (target.Kind)
            {
                case GrayboxInteraction.Merchant:
                    if (dungeon) return false;
                    SetPanel(DemoPanel.Merchant);
                    return true;
                case GrayboxInteraction.Warehouse:
                    if (dungeon) return false;
                    SetPanel(DemoPanel.Warehouse);
                    return true;
                case GrayboxInteraction.Departure:
                    if (!HasUsableCamp()) return false;
                    SetPanel(DemoPanel.Departure);
                    return true;
                case GrayboxInteraction.Chest:
                    if (!dungeon || chestOpened) return false;
                    chestOpened = true;
                    chestLid.localRotation = Quaternion.Euler(64f, 0f, 0f);
                    loot.SetActive(true);
                    return true;
                case GrayboxInteraction.Loot:
                    if (!dungeon || !target.isActiveAndEnabled || !HasFreeSlot(target.Item)) return false;
                    run.pack.Add(target.Item, 1);
                    target.Collect();
                    return true;
                case GrayboxInteraction.Extraction:
                    return StartExtraction();
                default:
                    return false;
            }
        }

        public void TogglePack()
        {
            if (Panel == DemoPanel.Pack)
                SetPanel(DemoPanel.None);
            else if (Panel == DemoPanel.None && dungeon)
                SetPanel(DemoPanel.Pack);
        }

        public void Escape()
        {
            switch (Panel)
            {
                case DemoPanel.Settlement:
                case DemoPanel.Recovery:
                    return;
                case DemoPanel.None:
                    SetPanel(DemoPanel.Pause);
                    return;
                default:
                    SetPanel(DemoPanel.None);
                    return;
            }
        }

        public void ClosePanel() => SetPanel(DemoPanel.None);

        public bool Buy(DemoItem item)
        {
            int price = catalog[item].buyPrice;
            return HasUsableCamp() && price >= 0 && Coins >= price && Commit(next =>
            {
                next.coins -= price;
                next.warehouse.Add(item, 1);
            });
        }

        public bool Sell(DemoItem item)
        {
            int price = catalog[item].sellPrice;
            return HasUsableCamp() && price >= 0 && InWarehouse(item) > 0 && Coins <= int.MaxValue - price && Commit(next =>
            {
                next.warehouse.Add(item, -1);
                next.coins += price;
            });
        }

        public bool Transfer(DemoItem item, bool intoPack)
        {
            if (!HasUsableCamp() || (intoPack ? InWarehouse(item) == 0 || !HasFreeSlot(item) : InPack(item) == 0))
                return false;

            return Commit(next =>
            {
                int amount = intoPack ? 1 : -1;
                next.prepared.Add(item, amount);
                next.warehouse.Add(item, -amount);
            });
        }

        public bool Equip(DemoItem item, bool equip)
        {
            if (!HasUsableCamp() || (item != DemoItem.Sword && item != DemoItem.Armor))
                return false;

            bool equipped = item == DemoItem.Sword ? HasSword : HasArmor;
            if (equipped == equip || (equip && InWarehouse(item) == 0))
                return false;

            return Commit(next =>
            {
                next.warehouse.Add(item, equip ? -1 : 1);
                if (item == DemoItem.Sword)
                    next.sword = equip;
                else
                    next.armor = equip;
            });
        }

        public bool Depart()
        {
            if (!HasUsableCamp() || Panel != DemoPanel.Departure)
                return false;

            var nextRun = new Run(Guid.NewGuid().ToString("N"), profile);
            if (!Commit(next =>
            {
                next.pendingRun = nextRun.id;
                next.prepared = new DemoInventory();
                next.sword = false;
                next.armor = false;
            }))
                return false;

            run = nextRun;
            campReport = null;
            changingScene = true;
            SceneManager.LoadScene(DungeonScene);
            return true;
        }

        public bool UsePotion()
        {
            if (!Simulating || Health >= MaxHealth || InPack(DemoItem.Potion) == 0)
                return false;

            run.pack.Add(DemoItem.Potion, -1);
            Health = Mathf.Min(MaxHealth, Health + 35);
            return true;
        }

        public bool Drop(DemoItem item)
        {
            if (!Simulating || InPack(item) == 0)
                return false;

            Vector3 origin = player.transform.position + Vector3.up * 0.5f;
            Vector3 forward = player.transform.forward;
            if (Physics.SphereCast(origin, 0.16f, forward, out _, 1.15f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                Notice = "前方没有可用空间。";
                return false;
            }

            Material material = loot.GetComponentInChildren<MeshRenderer>(true).sharedMaterial;
            GrayboxInteractable.SpawnDrop(item, origin + forward * 1.15f - Vector3.up * 0.3f, material);
            run.pack.Add(item, -1);
            Notice = null;
            return true;
        }

        public void TakeHit(int damage, Vector3 source)
        {
            if (!Simulating || damage <= 0)
                return;

            Vector3 toward = source - player.transform.position;
            toward.y = 0f;
            bool blocked = player.Guarding && Vector3.Dot(player.transform.forward, toward.normalized) >= 0.45f;
            int reduced = Mathf.Max(1, Mathf.RoundToInt(damage * (blocked ? 0.4f : 1f) * (HasArmor ? 0.8f : 1f)));
            Health = Mathf.Max(0, Health - reduced);
            player.ShowHit(blocked);

            if (Health == 0)
                EndRun(false, "出征失败 · 已死亡");
        }

        public void Abandon()
        {
            if (dungeon && run != null && Panel == DemoPanel.Pause)
                EndRun(false, "出征失败 · 已放弃");
        }

        public void RetrySettlement()
        {
            if (Panel != DemoPanel.Settlement || settlementSaved || settlementProfile == null)
                return;

            if (!store.Save(settlementProfile, out string error))
            {
                Notice = error;
                return;
            }

            profile = settlementProfile;
            settlementSaved = true;
            Notice = null;
        }

        public void ReturnToCamp()
        {
            if (Panel != DemoPanel.Settlement || !settlementSaved || changingScene)
                return;

            campReport = SettlementTitle;
            changingScene = true;
            run = null;
            SceneManager.LoadScene(CampScene);
        }

        public void RetryRecovery()
        {
            if (Panel != DemoPanel.Recovery || profile == null)
                return;

            RecoverInterruptedRun();
            if (string.IsNullOrEmpty(profile.pendingRun))
                FinishRecovery();
        }

        public void ResetInvalidSave()
        {
            if (Panel != DemoPanel.Recovery || profile != null)
                return;

            var fresh = new DemoProfile();
            if (!store.CreateFresh(fresh, out string error))
            {
                Notice = error;
                return;
            }

            profile = fresh;
            campReport = "已创建新档。原存档文件已保留。";
            FinishRecovery();
        }

        private void FinishRecovery()
        {
            Notice = null;
            SetPanel(DemoPanel.None);
            if (dungeon && run == null)
                run = new Run("", new DemoProfile());
        }

        private void RecoverInterruptedRun()
        {
            if (profile == null || string.IsNullOrEmpty(profile.pendingRun) || run != null)
                return;

            if (Commit(next => next.pendingRun = ""))
                campReport = "上次出征未结算，已按放弃处理。";
        }

        private bool StartExtraction()
        {
            if (!Simulating || !InExtractionZone || run == null)
                return false;

            extractionTime = 0f;
            return true;
        }

        private void EndRun(bool extracted, string title)
        {
            if (run == null || Panel == DemoPanel.Settlement)
                return;

            extractionTime = -1f;
            SettlementTitle = title;
            string summary = PackSummary();
            if (extracted)
            {
                SettlementDetails = summary.Length == 0 ? "本次没有携带物品入库。" : "带回 · " + summary;
                if (!Practice && (run.sword || run.armor))
                    SettlementDetails += "\n穿戴装备已随你返回。";
            }
            else
            {
                string equipment = (run.sword ? "铁短剑 " : "") + (run.armor ? "皮甲 " : "");
                SettlementDetails = (equipment + summary).Trim();
                SettlementDetails = SettlementDetails.Length == 0 ? "本次没有付费装备或物品损失。" : "损失 · " + SettlementDetails;
            }

            SetPanel(DemoPanel.Settlement);
            if (Practice)
            {
                SettlementDetails += "\n地牢练习局，营地资产保持原样。";
                settlementSaved = true;
                return;
            }

            settlementProfile = profile.Copy();
            settlementProfile.pendingRun = "";
            if (extracted)
            {
                settlementProfile.sword = run.sword;
                settlementProfile.armor = run.armor;
                foreach (DemoItem item in DemoItemCatalog.Items)
                    settlementProfile.warehouse.Add(item, run.pack.Count(item));
            }

            RetrySettlement();
        }

        private string PackSummary()
        {
            var entries = new System.Collections.Generic.List<string>();
            foreach (DemoItem item in DemoItemCatalog.Items)
            {
                int count = run.pack.Count(item);
                if (count > 0)
                    entries.Add($"{catalog[item].name} ×{count}");
            }

            return string.Join("、", entries);
        }

        private bool HasUsableCamp() => !dungeon && !changingScene && profile != null && string.IsNullOrEmpty(profile.pendingRun);

        private bool Commit(Action<DemoProfile> change)
        {
            var next = profile.Copy();
            change(next);
            if (!store.Save(next, out string error))
            {
                Notice = error;
                return false;
            }

            profile = next;
            Notice = null;
            return true;
        }

        private void SetPanel(DemoPanel value)
        {
            Panel = value;
            if (value == DemoPanel.None)
                player.CaptureCursor();
            else
                player.ReleaseCursor();
        }

        private sealed class Run
        {
            public readonly string id;
            public readonly DemoInventory pack;
            public readonly bool sword;
            public readonly bool armor;

            public Run(string id, DemoProfile source)
            {
                this.id = id;
                pack = source.prepared.Copy();
                sword = source.sword;
                armor = source.armor;
            }
        }
    }
}
