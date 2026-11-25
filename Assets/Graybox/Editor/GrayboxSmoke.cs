using System;
using System.Reflection;
using System.Threading.Tasks;
using Luvsic.Graybox;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GrayboxSmoke
{
    public static string Status { get; private set; } = "Not started";
    private static readonly MethodInfo Interact = typeof(GrayboxPlayer).GetMethod("TryInteract", BindingFlags.NonPublic | BindingFlags.Instance);

    public static string Start()
    {
        if (!EditorApplication.isPlaying)
            throw new InvalidOperationException("Enter Play Mode first.");

        Status = "Running";
        Run();
        return Status;
    }

    private static async void Run()
    {
        try
        {
            var flow = await WaitForScene(GrayboxFlow.CampScene);
            Check(flow.WarehouseCount == 0, "New play session starts with an empty warehouse");
            LookAndInteract(GrayboxInteraction.Warehouse, new Vector3(-3.6f, 0.036f, 1.25f), new Vector3(-3.6f, 1f, 3.15f));
            Check(flow.WarehouseOpen, "Warehouse panel opens");
            flow.CloseWarehouse();

            Depart();
            flow = await WaitForScene(GrayboxFlow.DungeonScene);
            Check(!flow.CarryingLoot && GameObject.Find("Iron Scrap") == null, "Loot is hidden before the chest opens");
            OpenChest();
            Check(GameObject.Find("Iron Scrap") != null && !flow.CarryingLoot, "Opening the chest reveals loot without collecting it");
            LookAndInteract(GrayboxInteraction.Loot, new Vector3(-2.35f, 0.036f, -0.1f), new Vector3(-2.35f, 1.55f, 1.28f));
            Check(flow.CarryingLoot && GameObject.Find("Iron Scrap") == null && flow.WarehouseCount == 0, "Picking up loot removes it locally and credits only the run");
            Check(!(bool)Interact.Invoke(FindPlayer(), null), "Loot cannot be picked up twice");

            Aim(new Vector3(3.65f, 0.036f, 4.65f), new Vector3(3.65f, 1.35f, 7.75f));
            Check(!(bool)Interact.Invoke(FindPlayer(), null) && !flow.Extracting, "The gate cannot start extraction outside the platform");
            UseExit();
            Check(flow.Extracting, "Extraction starts inside the designated zone");
            Check(!(bool)Interact.Invoke(FindPlayer(), null), "Extraction cannot start twice");
            Aim(new Vector3(3.65f, 0.036f, 4.35f), new Vector3(3.65f, 1.35f, 7.75f));
            await WaitUntil(() => !flow.Extracting, "Leaving the platform interrupts extraction");
            Check(SceneManager.GetActiveScene().name == GrayboxFlow.DungeonScene && flow.CarryingLoot, "Interrupted extraction keeps the run and the loot");

            UseExit();
            flow = await WaitForScene(GrayboxFlow.CampScene);
            Check(flow.WarehouseCount == 1 && flow.CampReport.Contains(GrayboxFlow.LootName), "Successful extraction credits the warehouse exactly once");
            LookAndInteract(GrayboxInteraction.Warehouse, new Vector3(-3.6f, 0.036f, 1.25f), new Vector3(-3.6f, 1f, 3.15f));
            Check(flow.WarehouseOpen && flow.WarehouseCount == 1, "Warehouse panel opens with one returned item");
            ScreenCapture.CaptureScreenshot(@"E:\Unity-Luv\Luvsic\Temp\Luvsic-Graybox-Warehouse.png");
            await Task.Delay(400);
            flow.CloseWarehouse();

            Depart();
            flow = await WaitForScene(GrayboxFlow.DungeonScene);
            Check(!flow.CarryingLoot && flow.WarehouseCount == 1 && GameObject.Find("Iron Scrap") == null, "Second departure resets the chest and bag while preserving the warehouse");
            OpenChest();
            Check(GameObject.Find("Iron Scrap") != null, "Chest can be used again on the second visit");
            UseExit();
            flow = await WaitForScene(GrayboxFlow.CampScene);
            Check(flow.WarehouseCount == 1 && flow.CampReport.Contains("未带回"), "Returning empty does not add or remove stock");
            Status = "PASS. Two journeys, warehouse state, loot interaction, 5-second extraction, cancellation, duplicate protection and empty return.";
            Debug.Log("Luvsic Graybox Smoke: " + Status);
        }
        catch (Exception error)
        {
            Status = "FAIL. " + error;
            Debug.LogError("Luvsic Graybox Smoke: " + Status);
        }
    }

    private static void Depart() =>
        LookAndInteract(GrayboxInteraction.Departure, new Vector3(3.6f, 0.036f, 1.1f), new Vector3(3.6f, 1.35f, 3.15f));

    private static void OpenChest() =>
        LookAndInteract(GrayboxInteraction.Chest, new Vector3(-2.35f, 0.036f, -0.18f), new Vector3(-2.35f, 0.85f, 1.2f));

    private static void UseExit() =>
        LookAndInteract(GrayboxInteraction.Extraction, new Vector3(3.65f, 0.036f, 6.05f), new Vector3(3.65f, 1.35f, 7.75f));

    private static GrayboxPlayer FindPlayer() => UnityEngine.Object.FindObjectOfType<GrayboxPlayer>();

    private static void Aim(Vector3 feet, Vector3 point)
    {
        var player = FindPlayer();
        var controller = player.GetComponent<CharacterController>();
        controller.enabled = false;
        player.transform.SetPositionAndRotation(feet, Quaternion.identity);
        controller.enabled = true;
        var view = player.GetComponentInChildren<Camera>().transform;
        view.rotation = Quaternion.LookRotation(point - view.position);
        Physics.SyncTransforms();
    }

    private static void LookAndInteract(GrayboxInteraction kind, Vector3 feet, Vector3 point)
    {
        Aim(feet, point);
        var player = FindPlayer();
        Check(player.LookTarget() != null && player.LookTarget().Kind == kind, "Aim can target " + kind);
        Check((bool)Interact.Invoke(player, null), "E-key action succeeds for " + kind);
    }

    private static async Task<GrayboxFlow> WaitForScene(string name)
    {
        await WaitUntil(() => SceneManager.GetActiveScene().name == name &&
            UnityEngine.Object.FindObjectOfType<GrayboxFlow>() != null, "Loaded scene " + name);
        await Task.Delay(140);
        return UnityEngine.Object.FindObjectOfType<GrayboxFlow>();
    }

    private static async Task WaitUntil(Func<bool> predicate, string description)
    {
        double limit = EditorApplication.timeSinceStartup + 12;
        while (!predicate() && EditorApplication.timeSinceStartup < limit)
            await Task.Delay(60);
        Check(predicate(), description);
    }

    private static void Check(bool passed, string description)
    {
        if (!passed)
            throw new InvalidOperationException(description);
    }
}
