using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class SceneSetup
{
    private const string ScenePath = "Assets/Scenes/Arena.unity";
    private const string MatFolder = "Assets/Materials";
    private const string VersionKey = "FartyFarm.SceneVersion";
    private const int SceneVersion = 2;
    private const float ArenaHalf = 40f;
    private const int TreeCount = 110;

    static SceneSetup()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!System.IO.File.Exists(ScenePath) || EditorPrefs.GetInt(VersionKey, 0) < SceneVersion)
                CreateArenaScene(true);
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
        CreateForest();
        CreateCharacter(CharacterType.Boi, new Vector3(-3f, 0.9f, 0f));
        Transform vaca = CreateCharacter(CharacterType.Vaca, new Vector3(3f, 0.9f, 0f));
        SetupCamera(vaca);
        new GameObject("GameManager").AddComponent<GameManager>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        EditorPrefs.SetInt(VersionKey, SceneVersion);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Farty Farm] Arena criada em " + ScenePath + (auto ? " (automatico)." : "."));
    }

    // ---------- materiais ----------

    private static Material MakeMat(string name, Color color, string shader = "Standard")
    {
        string path = MatFolder + "/" + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;
        var mat = new Material(Shader.Find(shader)) { color = color };
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static Material ParticleMat()
    {
        var builtin = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");
        return builtin != null ? builtin : MakeMat("Mat_Particle", Color.white, "Sprites/Default");
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

    // ---------- arena ----------

    private static void CreateGround()
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        float s = ArenaHalf * 2f / 10f; // Plane base = 10x10
        ground.transform.localScale = new Vector3(s, 1f, s);
        ground.GetComponent<Renderer>().sharedMaterial = MakeMat("Mat_Grass", new Color(0.35f, 0.8f, 0.3f));
        ground.GetComponent<Collider>().sharedMaterial = MakeSlippery();
    }

    private static void CreateWalls()
    {
        var mat = MakeMat("Mat_Wall", new Color(0.6f, 0.45f, 0.3f));
        var slippery = MakeSlippery();
        float len = ArenaHalf * 2f + 2f;
        float off = ArenaHalf + 0.5f;
        MakeWall("Wall_Front", new Vector3(0, 1f, off), new Vector3(len, 2, 1), mat, slippery);
        MakeWall("Wall_Back", new Vector3(0, 1f, -off), new Vector3(len, 2, 1), mat, slippery);
        MakeWall("Wall_Left", new Vector3(-off, 1f, 0), new Vector3(1, 2, len), mat, slippery);
        MakeWall("Wall_Right", new Vector3(off, 1f, 0), new Vector3(1, 2, len), mat, slippery);
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

    // ---------- floresta ----------

    private static void CreateForest()
    {
        var forest = new GameObject("Forest").transform;
        var trunk = MakeMat("Mat_Trunk", new Color(0.4f, 0.25f, 0.12f));
        var leaves = MakeMat("Mat_Leaves", new Color(0.18f, 0.62f, 0.2f));
        var burnt = MakeMat("Mat_BurntLeaves", new Color(0.12f, 0.1f, 0.1f));
        var fireMat = ParticleMat();

        var placed = new List<Vector2>();
        // Arvores de teste perto dos personagens
        Vector2[] testSpots = { new Vector2(-7, 4), new Vector2(-6, -5), new Vector2(7, 4), new Vector2(8, -4), new Vector2(0, 9) };
        foreach (Vector2 p in testSpots)
        {
            MakeTree(forest, p, 1f, trunk, leaves, burnt, fireMat);
            placed.Add(p);
        }

        var rng = new System.Random(1234);
        int attempts = 0;
        while (placed.Count < TreeCount && attempts++ < 8000)
        {
            float x = (float)(rng.NextDouble() * 2.0 - 1.0) * (ArenaHalf - 3f);
            float z = (float)(rng.NextDouble() * 2.0 - 1.0) * (ArenaHalf - 3f);
            var p = new Vector2(x, z);
            if (p.magnitude < 6f) continue;

            bool free = true;
            foreach (Vector2 q in placed)
                if ((q - p).sqrMagnitude < 3.5f * 3.5f) { free = false; break; }
            if (!free) continue;

            float scale = 0.9f + (float)rng.NextDouble() * 0.4f;
            MakeTree(forest, p, scale, trunk, leaves, burnt, fireMat);
            placed.Add(p);
        }
    }

    private static void MakeTree(Transform parent, Vector2 pos, float scale, Material trunk, Material leaves, Material burnt, Material fireMat)
    {
        var root = new GameObject("Tree");
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(pos.x, 0f, pos.y);
        root.transform.localScale = Vector3.one * scale;

        var col = root.AddComponent<CapsuleCollider>();
        col.height = 1.8f;
        col.radius = 0.28f;
        col.center = new Vector3(0, 0.9f, 0);

        Part(root.transform, "Trunk", PrimitiveType.Cylinder, new Vector3(0, 0.8f, 0), new Vector3(0.4f, 0.8f, 0.4f), trunk);
        var f1 = Part(root.transform, "Foliage1", PrimitiveType.Sphere, new Vector3(0, 2.2f, 0), new Vector3(2.0f, 1.8f, 2.0f), leaves);
        var f2 = Part(root.transform, "Foliage2", PrimitiveType.Sphere, new Vector3(0, 3.1f, 0), new Vector3(1.5f, 1.4f, 1.5f), leaves);

        var fireGO = new GameObject("Fire");
        fireGO.transform.SetParent(root.transform, false);
        fireGO.transform.localPosition = new Vector3(0, 2.4f, 0);
        var ps = fireGO.AddComponent<ParticleSystem>();
        ConfigureFire(ps, fireMat);

        var tree = root.AddComponent<FlammableTree>();
        var so = new SerializedObject(tree);
        so.FindProperty("fire").objectReferenceValue = ps;
        so.FindProperty("burntMaterial").objectReferenceValue = burnt;
        var foliage = so.FindProperty("foliage");
        foliage.arraySize = 2;
        foliage.GetArrayElementAtIndex(0).objectReferenceValue = f1.GetComponent<Renderer>();
        foliage.GetArrayElementAtIndex(1).objectReferenceValue = f2.GetComponent<Renderer>();
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureFire(ParticleSystem ps, Material mat)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.1f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
        main.startColor = new Color(1f, 0.6f, 0.15f, 0.9f);
        main.gravityModifier = -0.3f;

        var emission = ps.emission;
        emission.rateOverTime = 30f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.8f;

        var colorLife = ps.colorOverLifetime;
        colorLife.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.9f, 0.3f), 0f),
                new GradientColorKey(new Color(1f, 0.4f, 0.1f), 0.5f),
                new GradientColorKey(new Color(0.2f, 0.2f, 0.2f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(0.7f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            });
        colorLife.color = g;

        var sizeLife = ps.sizeOverLifetime;
        sizeLife.enabled = true;
        sizeLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1f, 1, 0.3f));

        ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
    }

    // ---------- personagens ----------

    private static Transform CreateCharacter(CharacterType type, Vector3 pos)
    {
        var go = new GameObject(type == CharacterType.Vaca ? "Vaca" : "Boi");
        go.transform.position = pos;

        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        var col = go.AddComponent<CapsuleCollider>();
        col.height = 1.5f;
        col.radius = 0.6f;
        col.center = new Vector3(0, -0.05f, 0);
        col.sharedMaterial = MakeSlippery();

        var particleMat = ParticleMat();
        if (type == CharacterType.Vaca)
        {
            BuildCow(go.transform);
            var milk = CreateBurstParticles(go.transform, "MilkParticles", new Vector3(0, 0.35f, 1.35f), new Vector3(-8f, 0, 0),
                new Color(1f, 1f, 1f, 0.95f), 35, 5f, 0.55f, 1.0f, 12f, new Vector2(0.15f, 0.3f), particleMat);
            var vaca = go.AddComponent<VacaController>();
            var so = new SerializedObject(vaca);
            so.FindProperty("milkParticles").objectReferenceValue = milk;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            BuildBull(go.transform);
            var flame = CreateBurstParticles(go.transform, "FartParticles", new Vector3(0, 0.1f, -1.0f), new Vector3(0, 180f, 0),
                new Color(1f, 0.55f, 0.1f, 0.9f), 22, 2.5f, 0.7f, -0.05f, 25f, new Vector2(0.4f, 0.8f), particleMat);
            var boi = go.AddComponent<BoiController>();
            var so = new SerializedObject(boi);
            so.FindProperty("fartParticles").objectReferenceValue = flame;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        return go.transform;
    }

    private static ParticleSystem CreateBurstParticles(Transform parent, string name, Vector3 localPos, Vector3 euler,
        Color color, int count, float speed, float lifetime, float gravity, float coneAngle, Vector2 size, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(euler);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.5f;
        main.startLifetime = lifetime;
        main.startSpeed = speed;
        main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
        main.startColor = color;
        main.gravityModifier = gravity;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = coneAngle;
        shape.radius = 0.1f;

        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
        return ps;
    }

    private static void SetupCamera(Transform target)
    {
        var camGO = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGO.tag = "MainCamera";
        var cam = camGO.GetComponent<TopDownCamera>() ?? camGO.AddComponent<TopDownCamera>();
        cam.target = target;
        EditorUtility.SetDirty(cam);
    }

    // ---------- modelos ----------

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
        var visual = new GameObject("Visual").transform;
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
        var visual = new GameObject("Visual").transform;
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
}
