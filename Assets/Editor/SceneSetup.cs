using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class SceneSetup
{
    private const string ScenePath = "Assets/Scenes/Arena.unity";
    private const string MatFolder = "Assets/Materials";

    static SceneSetup()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!System.IO.File.Exists(ScenePath)) CreateArenaScene(true);
        };
    }

    [MenuItem("Farty Farm/Setup Arena Scene")]
    public static void CreateArenaMenu()
    {
        if (System.IO.File.Exists(ScenePath) &&
            !EditorUtility.DisplayDialog("Farty Farm", "Arena.unity ja existe. Recriar e sobrescrever?", "Recriar", "Cancelar"))
            return;
        CreateArenaScene(false);
    }

    private static void CreateArenaScene(bool auto)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        System.IO.Directory.CreateDirectory(MatFolder);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        CreateGround();
        CreateWalls();
        Transform boi = CreateBoi();
        SetupCamera(boi);
        CreateGameManager(boi);

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Farty Farm] Arena criada em " + ScenePath + (auto ? " (automatico na primeira abertura)." : "."));
    }

    private static Material MakeMat(string name, Color color, string shader = "Standard")
    {
        string path = MatFolder + "/" + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;
        var mat = new Material(Shader.Find(shader)) { color = color };
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static PhysicsMaterial MakeSlippery()
    {
        string path = MatFolder + "/Slippery.physicsMaterial";
        var existing = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
        if (existing != null) return existing;
        var pm = new PhysicsMaterial("Slippery")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounciness = 0.1f
        };
        AssetDatabase.CreateAsset(pm, path);
        return pm;
    }

    private static void CreateGround()
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(2f, 1f, 2f); // Plane base = 10x10 -> arena 20x20
        ground.GetComponent<Renderer>().sharedMaterial = MakeMat("Mat_Grass", new Color(0.35f, 0.8f, 0.3f));
        ground.GetComponent<Collider>().sharedMaterial = MakeSlippery();
    }

    private static void CreateWalls()
    {
        var mat = MakeMat("Mat_Wall", new Color(0.6f, 0.45f, 0.3f));
        var slippery = MakeSlippery();
        MakeWall("Wall_Front", new Vector3(0, 1f, 10.5f), new Vector3(22, 2, 1), mat, slippery);
        MakeWall("Wall_Back", new Vector3(0, 1f, -10.5f), new Vector3(22, 2, 1), mat, slippery);
        MakeWall("Wall_Left", new Vector3(-10.5f, 1f, 0), new Vector3(1, 2, 22), mat, slippery);
        MakeWall("Wall_Right", new Vector3(10.5f, 1f, 0), new Vector3(1, 2, 22), mat, slippery);
    }

    private static void MakeWall(string name, Vector3 pos, Vector3 scale, Material mat, PhysicsMaterial pm)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.position = pos;
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().sharedMaterial = mat;
        wall.GetComponent<Collider>().sharedMaterial = pm;
    }

    private static Transform CreateBoi()
    {
        var boi = new GameObject("Boi");
        boi.transform.position = new Vector3(0, 0.9f, 0);

        var rb = boi.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        var col = boi.AddComponent<CapsuleCollider>();
        col.height = 1.5f;
        col.radius = 0.6f;
        col.center = new Vector3(0, -0.05f, 0);
        col.sharedMaterial = MakeSlippery();

        BuildCow(boi.transform);
        BuildBull(boi.transform);
        CreateFartParticles(boi);

        boi.AddComponent<BoiController>();
        return boi.transform;
    }

    private static Texture2D MakeSpotsTexture()
    {
        const string path = "Assets/Textures/CowSpots.png";
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null) return existing;

        System.IO.Directory.CreateDirectory("Assets/Textures");
        const int size = 512;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var white = new Color(0.97f, 0.96f, 0.92f);
        var black = new Color(0.1f, 0.09f, 0.1f);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float n = Mathf.PerlinNoise(x / (float)size * 4f + 3.1f, y / (float)size * 4f + 7.7f);
                tex.SetPixel(x, y, n > 0.58f ? black : white);
            }
        tex.Apply();
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static Material MakeTexturedMat(string name, Texture2D tex)
    {
        var mat = MakeMat(name, Color.white);
        mat.mainTexture = tex;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void BuildCow(Transform root)
    {
        var visual = new GameObject("Visual_Vaca").transform;
        visual.SetParent(root, false);

        var fur = MakeTexturedMat("Mat_CowFur", MakeSpotsTexture());
        var pink = MakeMat("Mat_Pink", new Color(1f, 0.55f, 0.6f));
        var cream = MakeMat("Mat_Horn", new Color(0.95f, 0.88f, 0.7f));
        var gold = MakeMat("Mat_Gold", new Color(1f, 0.8f, 0.15f));
        var dark = MakeMat("Mat_Dark", new Color(0.15f, 0.08f, 0.08f));
        var black = MakeMat("Mat_Black", new Color(0.1f, 0.09f, 0.1f));

        Part(visual, "Body", PrimitiveType.Sphere, new Vector3(0, 0.1f, -0.05f), new Vector3(1.2f, 1.0f, 1.5f), fur);
        Part(visual, "Udder", PrimitiveType.Sphere, new Vector3(0, -0.4f, -0.45f), new Vector3(0.45f, 0.3f, 0.45f), pink);

        var head = Part(visual, "Head", PrimitiveType.Sphere, new Vector3(0, 0.6f, 0.8f), new Vector3(1.0f, 0.95f, 0.95f), fur);
        Part(head, "Muzzle", PrimitiveType.Sphere, new Vector3(0, -0.2f, 0.4f), new Vector3(0.6f, 0.4f, 0.4f), pink);
        Part(head, "NostrilL", PrimitiveType.Sphere, new Vector3(-0.1f, -0.2f, 0.58f), new Vector3(0.08f, 0.1f, 0.05f), dark);
        Part(head, "NostrilR", PrimitiveType.Sphere, new Vector3(0.1f, -0.2f, 0.58f), new Vector3(0.08f, 0.1f, 0.05f), dark);
        Part(head, "EarL", PrimitiveType.Sphere, new Vector3(-0.55f, 0.15f, -0.05f), new Vector3(0.38f, 0.2f, 0.28f), pink, new Vector3(0, 0, 25));
        Part(head, "EarR", PrimitiveType.Sphere, new Vector3(0.55f, 0.15f, -0.05f), new Vector3(0.38f, 0.2f, 0.28f), pink, new Vector3(0, 0, -25));
        Part(head, "HornL", PrimitiveType.Capsule, new Vector3(-0.28f, 0.5f, -0.05f), new Vector3(0.12f, 0.18f, 0.12f), cream, new Vector3(0, 0, 30));
        Part(head, "HornR", PrimitiveType.Capsule, new Vector3(0.28f, 0.5f, -0.05f), new Vector3(0.12f, 0.18f, 0.12f), cream, new Vector3(0, 0, -30));
        Eye(head, "EyeLeft", new Vector3(-0.22f, 0.15f, 0.4f), new Vector3(0.4f, 0.45f, 0.25f));
        Eye(head, "EyeRight", new Vector3(0.22f, 0.15f, 0.4f), new Vector3(0.4f, 0.45f, 0.25f));
        Part(visual, "Collar", PrimitiveType.Sphere, new Vector3(0, 0.25f, 0.6f), new Vector3(0.9f, 0.3f, 0.5f), MakeMat("Mat_RedCollar", new Color(0.85f, 0.1f, 0.15f)));
        Part(visual, "Bell", PrimitiveType.Sphere, new Vector3(0, 0.05f, 0.8f), new Vector3(0.22f, 0.22f, 0.22f), gold);

        Part(visual, "Tail", PrimitiveType.Capsule, new Vector3(0, 0.25f, -0.85f), new Vector3(0.07f, 0.3f, 0.07f), fur, new Vector3(-30, 0, 0));
        Part(visual, "TailTuft", PrimitiveType.Sphere, new Vector3(0, 0.0f, -1.05f), new Vector3(0.22f, 0.3f, 0.22f), black);

        Skates(visual, new Color(1f, 0.5f, 0.65f));
    }

    private static void BuildBull(Transform root)
    {
        var visual = new GameObject("Visual_Boi").transform;
        visual.SetParent(root, false);

        var fur = MakeMat("Mat_BullFur", new Color(0.42f, 0.22f, 0.12f));
        var snout = MakeMat("Mat_BullSnout", new Color(0.85f, 0.55f, 0.4f));
        var cream = MakeMat("Mat_Horn", new Color(0.95f, 0.88f, 0.7f));
        var gold = MakeMat("Mat_Gold", new Color(1f, 0.8f, 0.15f));
        var dark = MakeMat("Mat_Dark", new Color(0.15f, 0.08f, 0.08f));
        var darkFur = MakeMat("Mat_BullDark", new Color(0.25f, 0.12f, 0.07f));

        Part(visual, "Body", PrimitiveType.Sphere, new Vector3(0, 0.1f, -0.05f), new Vector3(1.3f, 1.1f, 1.6f), fur);
        Part(visual, "Hump", PrimitiveType.Sphere, new Vector3(0, 0.6f, 0.25f), new Vector3(0.9f, 0.6f, 0.8f), fur);

        var head = Part(visual, "Head", PrimitiveType.Sphere, new Vector3(0, 0.6f, 0.85f), new Vector3(1.05f, 1.0f, 1.0f), fur);
        Part(head, "Muzzle", PrimitiveType.Sphere, new Vector3(0, -0.2f, 0.4f), new Vector3(0.65f, 0.42f, 0.42f), snout);
        Part(head, "NostrilL", PrimitiveType.Sphere, new Vector3(-0.1f, -0.2f, 0.6f), new Vector3(0.08f, 0.1f, 0.05f), dark);
        Part(head, "NostrilR", PrimitiveType.Sphere, new Vector3(0.1f, -0.2f, 0.6f), new Vector3(0.08f, 0.1f, 0.05f), dark);
        Part(head, "NoseRing", PrimitiveType.Sphere, new Vector3(0, -0.4f, 0.55f), new Vector3(0.16f, 0.16f, 0.04f), gold);
        Part(head, "EarL", PrimitiveType.Sphere, new Vector3(-0.58f, 0.1f, -0.05f), new Vector3(0.35f, 0.2f, 0.28f), darkFur, new Vector3(0, 0, 20));
        Part(head, "EarR", PrimitiveType.Sphere, new Vector3(0.58f, 0.1f, -0.05f), new Vector3(0.35f, 0.2f, 0.28f), darkFur, new Vector3(0, 0, -20));
        Part(head, "HornL", PrimitiveType.Capsule, new Vector3(-0.5f, 0.38f, -0.05f), new Vector3(0.2f, 0.3f, 0.2f), cream, new Vector3(0, 0, 65));
        Part(head, "HornR", PrimitiveType.Capsule, new Vector3(0.5f, 0.38f, -0.05f), new Vector3(0.2f, 0.3f, 0.2f), cream, new Vector3(0, 0, -65));
        Eye(head, "EyeLeft", new Vector3(-0.23f, 0.15f, 0.4f), new Vector3(0.4f, 0.45f, 0.25f));
        Eye(head, "EyeRight", new Vector3(0.23f, 0.15f, 0.4f), new Vector3(0.4f, 0.45f, 0.25f));
        Part(visual, "Collar", PrimitiveType.Sphere, new Vector3(0, 0.25f, 0.65f), new Vector3(1.0f, 0.3f, 0.5f), MakeMat("Mat_BlueCollar", new Color(0.15f, 0.4f, 0.9f)));
        Part(visual, "Bell", PrimitiveType.Sphere, new Vector3(0, 0.05f, 0.85f), new Vector3(0.22f, 0.22f, 0.22f), gold);

        Part(visual, "Tail", PrimitiveType.Capsule, new Vector3(0, 0.25f, -0.9f), new Vector3(0.07f, 0.3f, 0.07f), fur, new Vector3(-30, 0, 0));
        Part(visual, "TailTuft", PrimitiveType.Sphere, new Vector3(0, 0.0f, -1.1f), new Vector3(0.22f, 0.3f, 0.22f), darkFur);

        Skates(visual, new Color(0.2f, 0.45f, 0.95f));
    }

    private static void Skates(Transform visual, Color bootColor)
    {
        var boot = MakeMat("Mat_Boot_" + ColorUtility.ToHtmlStringRGB(bootColor), bootColor);
        var wheel = MakeMat("Mat_Wheel", new Color(1f, 0.55f, 0.1f));
        float[] xs = { -0.4f, 0.4f };
        float[] zs = { -0.45f, 0.5f };
        foreach (float x in xs)
            foreach (float z in zs)
            {
                Part(visual, "Skate", PrimitiveType.Cube, new Vector3(x, -0.55f, z), new Vector3(0.32f, 0.2f, 0.45f), boot);
                Part(visual, "WheelA", PrimitiveType.Cylinder, new Vector3(x, -0.65f, z - 0.15f), new Vector3(0.28f, 0.05f, 0.28f), wheel, new Vector3(0, 0, 90));
                Part(visual, "WheelB", PrimitiveType.Cylinder, new Vector3(x, -0.65f, z + 0.15f), new Vector3(0.28f, 0.05f, 0.28f), wheel, new Vector3(0, 0, 90));
            }
    }

    private static Transform Part(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 scale, Material mat, Vector3? euler = null)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
        go.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        return go.transform;
    }

    private static void Eye(Transform head, string name, Vector3 pos, Vector3 scale)
    {
        var eye = Part(head, name, PrimitiveType.Sphere, pos, scale, MakeMat("Mat_EyeWhite", Color.white));
        Part(eye, "Pupil", PrimitiveType.Sphere, new Vector3(0, -0.05f, 0.4f), new Vector3(0.45f, 0.45f, 0.4f), MakeMat("Mat_Pupil", Color.black));
    }

    private static ParticleSystem CreateFartParticles(GameObject boi)
    {
        var go = new GameObject("FartParticles");
        go.transform.SetParent(boi.transform, false);
        go.transform.localPosition = new Vector3(0, 0.1f, -1.0f);
        go.transform.localRotation = Quaternion.Euler(0, 180f, 0); // emite para tras

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.5f;
        main.startLifetime = 0.9f;
        main.startSpeed = 3f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startColor = new Color(0.75f, 0.85f, 0.4f, 0.8f);
        main.gravityModifier = -0.1f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;
        shape.radius = 0.1f;

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.5f, 1, 1.6f));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = MakeMat("Mat_Fart", Color.white, "Sprites/Default");
        return ps;
    }

    private static void SetupCamera(Transform boi)
    {
        var camGO = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGO.tag = "MainCamera";
        var cam = camGO.GetComponent<TopDownCamera>() ?? camGO.AddComponent<TopDownCamera>();
        cam.target = boi;
        EditorUtility.SetDirty(cam);
    }

    private static void CreateGameManager(Transform boi)
    {
        var go = new GameObject("GameManager");
        var gm = go.AddComponent<GameManager>();
        var so = new SerializedObject(gm);
        so.FindProperty("player").objectReferenceValue = boi.GetComponent<BoiController>();
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
