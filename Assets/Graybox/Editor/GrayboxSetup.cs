using System;
using System.Linq;
using Luvsic.Graybox;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class GrayboxSetup
{
    public const string CampPath = "Assets/Scenes/Camp.scene";
    public const string DungeonPath = "Assets/Scenes/Dungeon.scene";

    public static string Create()
    {
        if (EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Save the active scene and exit Play Mode before creating the graybox.");

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(CampPath) != null ||
            AssetDatabase.LoadAssetAtPath<SceneAsset>(DungeonPath) != null)
            throw new InvalidOperationException("Graybox scenes already exist.");

        const string materialFolder = "Assets/Graybox/Materials";
        if (!AssetDatabase.IsValidFolder(materialFolder))
            AssetDatabase.CreateFolder("Assets/Graybox", "Materials");

        var palette = new Palette(materialFolder);
        CreateCamp(palette);
        CreateDungeon(palette);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(CampPath, true),
            new EditorBuildSettingsScene(DungeonPath, true)
        }.Concat(EditorBuildSettings.scenes
            .Where(scene => scene.path != CampPath && scene.path != DungeonPath)
            .Select(scene => scene.path == "Assets/Scenes/SampleScene.scene"
                ? new EditorBuildSettingsScene(scene.path, false) : scene)).ToArray();
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(CampPath, OpenSceneMode.Single);
        return "Created and opened Camp. Saved Dungeon and registered both in Build Settings.";
    }

    private static void CreateCamp(Palette p)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Transform room = Root("Camp Architecture").transform;
        Shell(room, p, 18f, -7f, 8f);
        LightRoom(new Color(0.64f, 0.65f, 0.63f), 1.05f);

        Box("Central Walkway", room, new Vector3(0f, 0.012f, 0.6f), new Vector3(3.4f, 0.025f, 11.4f), p.Slate, false);
        Box("Stash Trail", room, new Vector3(-3.6f, 0.025f, 0.7f), new Vector3(0.24f, 0.03f, 4.7f), p.Gold, false);
        Box("Dungeon Trail", room, new Vector3(3.6f, 0.025f, 0.7f), new Vector3(0.24f, 0.03f, 4.7f), p.Teal, false);

        Transform stash = Interactable("Warehouse", new Vector3(-3.6f, 0f, 3.15f), GrayboxInteraction.Warehouse,
            new Vector3(0f, 0.87f, 0f), new Vector3(2.25f, 1.72f, 0.95f));
        Box("Counter", stash, new Vector3(0f, 0.47f, 0f), new Vector3(2.2f, 0.9f, 0.9f), p.Wood, false);
        Box("Amber Counter Edge", stash, new Vector3(0f, 0.91f, -0.04f), new Vector3(2.35f, 0.13f, 0.98f), p.Gold, false);
        Box("Storage Crate Left", stash, new Vector3(-0.58f, 1.13f, 0.04f), new Vector3(0.56f, 0.35f, 0.63f), p.Gold, false);
        Box("Storage Crate Right", stash, new Vector3(0.43f, 1.19f, 0.13f), new Vector3(0.74f, 0.49f, 0.65f), p.Wood, false);
        Sign("STASH", stash, new Vector3(0f, 1.8f, -0.47f), new Color(1f, 0.82f, 0.47f));

        Transform door = Interactable("Dungeon Entrance", new Vector3(3.6f, 0f, 3.15f), GrayboxInteraction.Departure,
            new Vector3(0f, 1.2f, 0f), new Vector3(1.85f, 2.4f, 0.62f));
        Gate(door, p);
        Sign("DUNGEON", door, new Vector3(0f, 2.86f, -0.39f), new Color(0.51f, 1f, 0.88f));

        Transform furniture = Root("Camp Props").transform;
        for (int index = 0; index < 3; index++)
        {
            float x = -6.9f + index * 0.76f;
            Box("Supply Crate", furniture, new Vector3(x, 0.33f, 6.35f), new Vector3(0.69f, 0.66f, 0.77f), p.Wood);
        }
        Box("Work Bench", furniture, new Vector3(6.76f, 0.45f, 5.6f), new Vector3(1.4f, 0.9f, 0.75f), p.Wood);
        CreatePlayerAndFlow(new Vector3(0f, 0.035f, -3.75f), false, null, null, null);
        Save(scene, CampPath);
    }

    private static void CreateDungeon(Palette p)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Transform room = Root("Dungeon Architecture").transform;
        Shell(room, p, 16f, -6.5f, 9f);
        LightRoom(new Color(0.49f, 0.54f, 0.57f), 0.88f);

        Box("Amber Trail", room, new Vector3(-2.35f, 0.025f, -0.9f), new Vector3(0.22f, 0.03f, 3.7f), p.Gold, false);
        Box("Exit Trail", room, new Vector3(3.65f, 0.025f, 2f), new Vector3(0.24f, 0.03f, 5.3f), p.Teal, false);
        Box("Left Rubble", room, new Vector3(-5.8f, 0.32f, 4.5f), new Vector3(1.55f, 0.64f, 1.52f), p.Stone)
            .localRotation = Quaternion.Euler(0f, 14f, 0f);
        Box("Right Rubble", room, new Vector3(6f, 0.28f, 0.3f), new Vector3(1.3f, 0.56f, 1.1f), p.Slate)
            .localRotation = Quaternion.Euler(0f, -16f, 0f);

        Transform chest = Interactable("Loot Chest", new Vector3(-2.35f, 0f, 1.65f), GrayboxInteraction.Chest,
            new Vector3(0f, 0.65f, 0f), new Vector3(1.65f, 1.28f, 1.12f));
        Box("Plinth", chest, new Vector3(0f, 0.16f, 0f), new Vector3(1.8f, 0.32f, 1.24f), p.Slate);
        Box("Lower Chest", chest, new Vector3(0f, 0.63f, 0f), new Vector3(1.48f, 0.63f, 0.91f), p.Wood, false);
        Box("Amber Rim", chest, new Vector3(0f, 0.88f, 0f), new Vector3(1.56f, 0.13f, 0.97f), p.Gold, false);
        Transform lid = Child("Chest Lid Hinge", chest, new Vector3(0f, 0.99f, 0.43f));
        Box("Chest Lid", lid, new Vector3(0f, 0.02f, -0.43f), new Vector3(1.55f, 0.21f, 0.93f), p.Gold, false);
        Box("Latch", lid, new Vector3(0f, -0.03f, -0.9f), new Vector3(0.2f, 0.22f, 0.08f), p.Dark, false);

        Transform scrap = Interactable("Iron Scrap", new Vector3(-2.35f, 1.55f, 1.28f), GrayboxInteraction.Loot,
            Vector3.zero, new Vector3(0.63f, 0.52f, 0.63f));
        Box("Scrap Mesh", scrap, Vector3.zero, new Vector3(0.42f, 0.32f, 0.42f), p.Highlight, false)
            .localRotation = Quaternion.Euler(22f, 38f, 15f);
        scrap.gameObject.SetActive(false);

        Transform exit = Interactable("Extraction Gate", new Vector3(3.65f, 0f, 7.75f), GrayboxInteraction.Extraction,
            new Vector3(0f, 1.2f, 0f), new Vector3(1.85f, 2.4f, 0.62f));
        Gate(exit, p);
        Sign("EXIT", exit, new Vector3(0f, 2.86f, -0.39f), new Color(0.51f, 1f, 0.88f));
        Transform pad = Root("Extraction Platform").transform;
        pad.position = new Vector3(3.65f, 0f, 6.08f);
        Box("Platform Marking", pad, new Vector3(0f, 0.027f, 0f), new Vector3(3.3f, 0.035f, 2.5f), p.Slate, false);
        Box("Near Stripe", pad, new Vector3(0f, 0.05f, -1.21f), new Vector3(3.3f, 0.035f, 0.12f), p.Teal, false);
        Box("Far Stripe", pad, new Vector3(0f, 0.05f, 1.21f), new Vector3(3.3f, 0.035f, 0.12f), p.Teal, false);
        Box("Left Stripe", pad, new Vector3(-1.58f, 0.05f, 0f), new Vector3(0.12f, 0.035f, 2.45f), p.Teal, false);
        Box("Right Stripe", pad, new Vector3(1.58f, 0.05f, 0f), new Vector3(0.12f, 0.035f, 2.45f), p.Teal, false);
        var zone = pad.gameObject.AddComponent<BoxCollider>();
        zone.center = new Vector3(0f, 1f, 0f);
        zone.size = new Vector3(3.3f, 2.4f, 2.5f);
        zone.isTrigger = true;

        CreatePlayerAndFlow(new Vector3(0f, 0.035f, -3.9f), true, zone, scrap.gameObject, lid);
        Save(scene, DungeonPath);
    }

    private static void CreatePlayerAndFlow(Vector3 spawn, bool dungeon, BoxCollider zone, GameObject loot, Transform lid)
    {
        var playerObject = Root("First Person Player");
        playerObject.layer = 2;
        playerObject.transform.position = spawn;
        var controller = playerObject.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.31f;
        controller.center = new Vector3(0f, 0.9f, 0f);
        controller.stepOffset = 0.22f;
        controller.skinWidth = 0.035f;

        Transform view = Child("First Person Camera", playerObject.transform, new Vector3(0f, 1.65f, 0f));
        view.gameObject.tag = "MainCamera";
        var camera = view.gameObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.09f, 0.12f, 0.13f);
        camera.fieldOfView = 70f;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 50f;
        view.gameObject.AddComponent<AudioListener>();
        var player = playerObject.AddComponent<GrayboxPlayer>();

        var system = Root("Graybox Flow");
        var flow = system.AddComponent<GrayboxFlow>();
        var hud = system.AddComponent<GrayboxHud>();
        var playerConfig = new SerializedObject(player);
        playerConfig.FindProperty("flow").objectReferenceValue = flow;
        playerConfig.FindProperty("viewCamera").objectReferenceValue = camera;
        playerConfig.ApplyModifiedPropertiesWithoutUndo();

        var flowConfig = new SerializedObject(flow);
        flowConfig.FindProperty("dungeon").boolValue = dungeon;
        flowConfig.FindProperty("player").objectReferenceValue = player;
        flowConfig.FindProperty("extractionZone").objectReferenceValue = zone;
        flowConfig.FindProperty("loot").objectReferenceValue = loot;
        flowConfig.FindProperty("chestLid").objectReferenceValue = lid;
        flowConfig.ApplyModifiedPropertiesWithoutUndo();

        var hudConfig = new SerializedObject(hud);
        hudConfig.FindProperty("flow").objectReferenceValue = flow;
        hudConfig.FindProperty("player").objectReferenceValue = player;
        hudConfig.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Shell(Transform room, Palette p, float width, float near, float far)
    {
        float length = far - near;
        float mid = (near + far) * 0.5f;
        float side = width * 0.5f;
        Box("Floor", room, new Vector3(0f, -0.2f, mid), new Vector3(width, 0.4f, length), p.Floor);
        Box("Left Wall", room, new Vector3(-side, 1.8f, mid), new Vector3(0.42f, 3.6f, length), p.Stone);
        Box("Right Wall", room, new Vector3(side, 1.8f, mid), new Vector3(0.42f, 3.6f, length), p.Stone);
        Box("Back Wall", room, new Vector3(0f, 1.8f, far), new Vector3(width, 3.6f, 0.45f), p.Stone);
        Box("Front Wall", room, new Vector3(0f, 1.8f, near), new Vector3(width, 3.6f, 0.45f), p.Stone);
        Box("Ceiling", room, new Vector3(0f, 3.74f, mid), new Vector3(width, 0.26f, length), p.Slate, false);
        for (int i = 0; i < 4; i++)
        {
            float z = near + 2.4f + i * (length - 4.8f) / 3f;
            Box("Left Pier", room, new Vector3(-side + 0.24f, 1.72f, z), new Vector3(0.62f, 3.44f, 0.72f), p.Slate, false);
            Box("Right Pier", room, new Vector3(side - 0.24f, 1.72f, z), new Vector3(0.62f, 3.44f, 0.72f), p.Slate, false);
        }
        Box("Back Trim", room, new Vector3(0f, 2.95f, far - 0.27f), new Vector3(width, 0.15f, 0.14f), p.Slate, false);
    }

    private static void Gate(Transform parent, Palette p)
    {
        Box("Door Panel", parent, new Vector3(0f, 1.16f, 0.01f), new Vector3(1.61f, 2.23f, 0.42f), p.Dark, false);
        Box("Left Jamb", parent, new Vector3(-0.93f, 1.22f, 0f), new Vector3(0.25f, 2.44f, 0.64f), p.Teal, false);
        Box("Right Jamb", parent, new Vector3(0.93f, 1.22f, 0f), new Vector3(0.25f, 2.44f, 0.64f), p.Teal, false);
        Box("Lintel", parent, new Vector3(0f, 2.44f, 0f), new Vector3(2.11f, 0.28f, 0.64f), p.Teal, false);
        Box("Access Plate", parent, new Vector3(0f, 1.32f, -0.24f), new Vector3(0.38f, 0.4f, 0.12f), p.Teal, false);
    }

    private static void LightRoom(Color ambient, float intensity)
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = ambient;
        GameObject gameObject = Root("Graybox Lighting");
        gameObject.transform.rotation = Quaternion.Euler(48f, 153f, 0f);
        var sun = gameObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.93f, 0.8f);
        sun.intensity = intensity;
    }

    private static Transform Interactable(string name, Vector3 position, GrayboxInteraction kind, Vector3 center, Vector3 size)
    {
        var go = Root(name);
        go.transform.position = position;
        var collider = go.AddComponent<BoxCollider>();
        collider.center = center;
        collider.size = size;
        var setup = new SerializedObject(go.AddComponent<GrayboxInteractable>());
        setup.FindProperty("kind").enumValueIndex = (int)kind;
        setup.ApplyModifiedPropertiesWithoutUndo();
        return go.transform;
    }

    private static void Sign(string text, Transform parent, Vector3 position, Color color)
    {
        Transform transform = Child(text + " Sign", parent, position);
        var mesh = transform.gameObject.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.fontSize = 56;
        mesh.characterSize = 0.155f;
        mesh.color = color;
    }

    private static GameObject Root(string name) => new GameObject(name);

    private static Transform Child(string name, Transform parent, Vector3 position)
    {
        Transform child = Root(name).transform;
        child.SetParent(parent, false);
        child.localPosition = position;
        return child;
    }

    private static Transform Box(string name, Transform parent, Vector3 position, Vector3 size, Material material, bool collidable = true)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent, false);
        box.transform.localPosition = position;
        box.transform.localScale = size;
        box.GetComponent<MeshRenderer>().sharedMaterial = material;
        if (!collidable)
            UnityEngine.Object.DestroyImmediate(box.GetComponent<BoxCollider>());
        return box.transform;
    }

    private static void Save(Scene scene, string path)
    {
        if (!EditorSceneManager.SaveScene(scene, path))
            throw new InvalidOperationException("Could not save " + path);
    }

    private sealed class Palette
    {
        public readonly Material Floor;
        public readonly Material Stone;
        public readonly Material Slate;
        public readonly Material Dark;
        public readonly Material Wood;
        public readonly Material Gold;
        public readonly Material Teal;
        public readonly Material Highlight;

        public Palette(string folder)
        {
            Floor = Make(folder, "Floor", new Color(0.28f, 0.31f, 0.32f));
            Stone = Make(folder, "Stone", new Color(0.37f, 0.40f, 0.41f));
            Slate = Make(folder, "Slate", new Color(0.22f, 0.27f, 0.29f));
            Dark = Make(folder, "Dark", new Color(0.12f, 0.18f, 0.20f));
            Wood = Make(folder, "Wood", new Color(0.52f, 0.35f, 0.21f));
            Gold = Make(folder, "Amber", new Color(0.94f, 0.66f, 0.29f));
            Teal = Make(folder, "Teal", new Color(0.27f, 0.77f, 0.68f));
            Highlight = Make(folder, "Loot", new Color(1f, 0.79f, 0.34f));
        }

        private static Material Make(string folder, string name, Color color)
        {
            string path = folder + "/" + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                throw new InvalidOperationException("The project's URP Lit shader could not be found.");
            var material = new Material(shader) { name = name, color = color };
            material.SetFloat("_Smoothness", 0.08f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
