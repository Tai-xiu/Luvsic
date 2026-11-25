using UnityEngine;
using UnityEngine.SceneManagement;

namespace Luvsic.Graybox
{
    public sealed class GrayboxFlow : MonoBehaviour
    {
        public const string CampScene = "Camp";
        public const string DungeonScene = "Dungeon";
        public const string LootName = "粗铁废料";

        private const float ExtractionDuration = 5f;
        private static int warehouseCount;
        private static bool runActive;
        private static bool runLoot;
        private static bool? lastExtractionHadLoot;

        [SerializeField] private bool dungeon;
        [SerializeField] private GrayboxPlayer player;
        [SerializeField] private BoxCollider extractionZone;
        [SerializeField] private GameObject loot;
        [SerializeField] private Transform chestLid;

        private bool changingScene;
        private bool chestOpened;
        private float extractionTime = -1f;

        public bool InDungeon => dungeon;
        public bool WarehouseOpen { get; private set; }
        public bool CarryingLoot => dungeon && runLoot;
        public int WarehouseCount => warehouseCount;
        public bool Extracting => extractionTime >= 0f;
        public float ExtractionProgress => Mathf.Clamp01(extractionTime / ExtractionDuration);
        public bool InExtractionZone => dungeon && extractionZone.bounds.Contains(player.transform.position + Vector3.up * 0.5f);

        public string Objective
        {
            get
            {
                if (!dungeon)
                    return "黄色货架是仓库，青色门通往地牢。";
                if (Extracting)
                    return $"撤离中，留在平台内 · {Mathf.Max(1, Mathf.CeilToInt(ExtractionDuration - extractionTime))} 秒";
                if (CarryingLoot)
                    return "已拿到战利品。前往房间后方的青色撤离台。";

                return chestOpened ? "对准箱子上方的战利品，按 E 拾取。" : "找到黄色宝箱，靠近后按 E 打开。";
            }
        }

        public string CampReport
        {
            get
            {
                if (dungeon || !lastExtractionHadLoot.HasValue)
                    return string.Empty;

                return lastExtractionHadLoot.Value ? $"撤离成功 · {LootName} ×1 已入库" : "撤离成功 · 本次未带回物品";
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            warehouseCount = 0;
            runActive = false;
            runLoot = false;
            lastExtractionHadLoot = null;
        }

        private void Awake()
        {
            if (dungeon && !runActive)
                BeginRun();
        }

        private void Update()
        {
            if (!Extracting || changingScene)
                return;

            if (!InExtractionZone)
            {
                extractionTime = -1f;
                return;
            }

            extractionTime += Time.deltaTime;
            if (extractionTime >= ExtractionDuration)
                FinishExtraction();
        }

        public string Prompt(GrayboxInteractable target)
        {
            if (changingScene || WarehouseOpen || Extracting)
                return string.Empty;

            if (target != null)
            {
                switch (target.Kind)
                {
                    case GrayboxInteraction.Warehouse: return "E  ·  查看仓库";
                    case GrayboxInteraction.Departure: return "E  ·  进入地牢";
                    case GrayboxInteraction.Chest: return chestOpened ? "宝箱已打开" : "E  ·  打开宝箱";
                    case GrayboxInteraction.Loot: return $"E  ·  拾取{LootName}";
                    case GrayboxInteraction.Extraction:
                        return InExtractionZone ? "E  ·  开始撤离" : "走上青色平台后按 E 撤离";
                }
            }

            return InExtractionZone ? "E  ·  开始撤离" : string.Empty;
        }

        public bool Interact(GrayboxInteractable target)
        {
            if (changingScene || WarehouseOpen || Extracting)
                return false;

            if (target == null)
                return StartExtraction();

            switch (target.Kind)
            {
                case GrayboxInteraction.Warehouse:
                    if (dungeon)
                        return false;
                    WarehouseOpen = true;
                    player.ReleaseCursor();
                    return true;
                case GrayboxInteraction.Departure:
                    if (dungeon)
                        return false;
                    BeginRun();
                    changingScene = true;
                    SceneManager.LoadScene(DungeonScene);
                    return true;
                case GrayboxInteraction.Chest:
                    if (!dungeon || chestOpened)
                        return false;
                    chestOpened = true;
                    chestLid.localRotation = Quaternion.Euler(64f, 0f, 0f);
                    loot.SetActive(true);
                    return true;
                case GrayboxInteraction.Loot:
                    if (!dungeon || !chestOpened || runLoot)
                        return false;
                    runLoot = true;
                    loot.SetActive(false);
                    return true;
                case GrayboxInteraction.Extraction:
                    return StartExtraction();
                default:
                    return false;
            }
        }

        public void CloseWarehouse()
        {
            WarehouseOpen = false;
            player.CaptureCursor();
        }

        private bool StartExtraction()
        {
            if (!InExtractionZone || !runActive)
                return false;

            extractionTime = 0f;
            return true;
        }

        private static void BeginRun()
        {
            runLoot = false;
            runActive = true;
            lastExtractionHadLoot = null;
        }

        private void FinishExtraction()
        {
            if (!runActive)
                return;

            changingScene = true;
            runActive = false;
            lastExtractionHadLoot = runLoot;

            if (runLoot)
                warehouseCount++;

            runLoot = false;
            SceneManager.LoadScene(CampScene);
        }
    }
}
