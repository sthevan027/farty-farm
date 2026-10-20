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
    private const int SceneVersion = 5;
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

        ProceduralTextures.Set tex = null;
        try
        {
            tex = ProceduralTextures.GenerateAll();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        SetupLighting();
        CreateGround(tex);
        CreateWalls(tex);
        CreateForest(tex);

        EditorUtility.DisplayProgressBar("Farty Farm", "Esculpindo os animais...", 0.5f);
        AnimalAssets animals;
        try
        {
            animals = new AnimalAssets
            {
                cow = AnimalMeshFactory.Cow(),
                bull = AnimalMeshFactory.Bull(),
                udder = AnimalMeshFactory.Udder(),
                skatePink = AnimalMeshFactory.Skate(false),
                skateBlue = AnimalMeshFactory.Skate(true),
                cowMat = MakeAnimalMat("Mat_AnimalCow", tex.cow, 2.0f),
                bullMat = MakeAnimalMat("Mat_AnimalBull", tex.bull, 2.0f),
                plainMat = MakeAnimalMat("Mat_AnimalPlain", null, 2.0f)
            };
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        CreateCharacter(CharacterType.Boi, new Vector3(-3f, 0.9f, 0f), animals);
        Transform vaca = CreateCharacter(CharacterType.Vaca, new Vector3(3f, 0.9f, 0f), animals);
        SetupCamera(vaca);
        CreateCollectibles();

        var gm = new GameObject("GameManager");
        gm.AddComponent<GameManager>();
        gm.AddComponent<CollectibleHUD>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        EditorPrefs.SetInt(VersionKey, SceneVersion);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Farty Farm] Arena criada em " + ScenePath + (auto ? " (automatico)." : "."));
    }

    // ---------- iluminacao / ceu ----------

    private static void SetupLighting()
    {
        Light sun = Object.FindFirstObjectByType<Light>();
        if (sun != null)
        {
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.95f, 0.84f);
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.78f;
            sun.transform.rotation = Quaternion.Euler(52f, -32f, 0f);
        }

        Shader skyShader = Shader.Find("Skybox/Procedural");
        if (skyShader != null)
        {
            var sky = MakeMat("Mat_Sky", Color.white, "Skybox/Procedural");
            sky.SetColor("_SkyTint", new Color(0.42f, 0.62f, 0.95f));
            sky.SetColor("_GroundColor", new Color(0.55f, 0.6f, 0.5f));
            sky.SetFloat("_AtmosphereThickness", 0.85f);
            sky.SetFloat("_Exposure", 1.25f);
            RenderSettings.skybox = sky;
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.62f, 0.78f, 0.98f);
        RenderSettings.ambientEquatorColor = new Color(0.78f, 0.80f, 0.70f);
        RenderSettings.ambientGroundColor = new Color(0.34f, 0.40f, 0.28f);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.74f, 0.87f, 0.98f);
        RenderSettings.fogStartDistance = 38f;
        RenderSettings.fogEndDistance = 105f;
    }

    // ---------- materiais ----------

    private static Material MakeMat(string name, Color color, string shader = "Standard", float smoothness = 0.08f)
    {
        string path = MatFolder + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find(shader));
            AssetDatabase.CreateAsset(mat, path);
        }
        if (mat.HasProperty("_Color")) mat.color = color;
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material MakeTexMat(string name, Texture2D tex, Color tint, Vector2 tiling,
        Texture2D normal = null, float smoothness = 0.08f)
    {
        var mat = MakeMat(name, tint, "Standard", smoothness);
        mat.mainTexture = tex;
        mat.mainTextureScale = tiling;
        if (normal != null)
        {
            mat.SetTexture("_BumpMap", normal);
            mat.SetFloat("_BumpScale", 1f);
            mat.EnableKeyword("_NORMALMAP");
        }
        mat.enableInstancing = true;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material ParticleMat()
    {
        var builtin = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");
        return builtin != null ? builtin : MakeMat("Mat_Particle", Color.white, "Sprites/Default");
    }

    private static Material MakeAnimalMat(string name, Texture2D pattern, float patternScale)
    {
        Shader shader = Shader.Find("FartyFarm/Animal");
        if (shader == null)
        {
            Debug.LogWarning("[Farty Farm] Shader FartyFarm/Animal nao encontrado; usando Standard (sem cores por vertice).");
            shader = Shader.Find("Standard");
        }

        string path = MatFolder + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.shader = shader;
        if (mat.HasProperty("_Pattern")) mat.SetTexture("_Pattern", pattern);
        if (mat.HasProperty("_PatternScale")) mat.SetFloat("_PatternScale", patternScale);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.1f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // Personagens: sem atrito, MUITO quicantes (batem nas arvores/paredes e ricocheteiam).
    private static PhysicsMaterial MakeBouncy()
    {
        string path = MatFolder + "/Bouncy.physicsMaterial";
        var pm = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
        if (pm == null)
        {
            pm = new PhysicsMaterial("Bouncy");
            AssetDatabase.CreateAsset(pm, path);
        }
        pm.dynamicFriction = 0f;
        pm.staticFriction = 0f;
        pm.frictionCombine = PhysicsMaterialCombine.Minimum;
        pm.bounciness = 0.6f;
        pm.bounceCombine = PhysicsMaterialCombine.Maximum;
        EditorUtility.SetDirty(pm);
        return pm;
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

    private static void CreateGround(ProceduralTextures.Set tex)
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        float s = ArenaHalf * 2f / 10f; // Plane base = 10x10
        ground.transform.localScale = new Vector3(s, 1f, s);
        var grass = MakeTexMat("Mat_Grass", tex.grass, Color.white, new Vector2(16f, 16f), null, 0.02f);
        // camada de detalhe em escala grande: quebra a repeticao da grama
        grass.SetTexture("_DetailAlbedoMap", tex.grassDetail);
        grass.SetTextureScale("_DetailAlbedoMap", new Vector2(2.5f, 2.5f));
        grass.EnableKeyword("_DETAIL_MULX2");
        EditorUtility.SetDirty(grass);
        ground.GetComponent<Renderer>().sharedMaterial = grass;
        ground.GetComponent<Collider>().sharedMaterial = MakeSlippery();
    }

    private static void CreateWalls(ProceduralTextures.Set tex)
    {
        var mat = MakeTexMat("Mat_Wall", tex.wood, Color.white, new Vector2(24f, 1f), null, 0.05f);
        var bouncy = MakeBouncy();
        float len = ArenaHalf * 2f + 2f;
        float off = ArenaHalf + 0.5f;
        MakeWall("Wall_Front", new Vector3(0, 1f, off), new Vector3(len, 2, 1), mat, bouncy);
        MakeWall("Wall_Back", new Vector3(0, 1f, -off), new Vector3(len, 2, 1), mat, bouncy);
        MakeWall("Wall_Left", new Vector3(-off, 1f, 0), new Vector3(1, 2, len), mat, bouncy);
        MakeWall("Wall_Right", new Vector3(off, 1f, 0), new Vector3(1, 2, len), mat, bouncy);
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

    private class TreeAssets
    {
        public Mesh trunk;
        public Mesh[] foliage;
        public Material trunkMat, trunkBurntMat, burntLeaves, fire;
        public Material[] leaves;
    }

    private static void CreateForest(ProceduralTextures.Set tex)
    {
        var forest = new GameObject("Forest").transform;

        var assets = new TreeAssets
        {
            trunk = TreeMeshFactory.CreateTrunk(),
            foliage = new[] { TreeMeshFactory.CreateFoliage(11), TreeMeshFactory.CreateFoliage(22), TreeMeshFactory.CreateFoliage(33) },
            trunkMat = MakeTexMat("Mat_Bark", tex.barkAlbedo, Color.white, Vector2.one, tex.barkNormal, 0.05f),
            trunkBurntMat = MakeTexMat("Mat_BarkBurnt", tex.barkAlbedo, new Color(0.2f, 0.18f, 0.18f), Vector2.one, tex.barkNormal, 0.05f),
            burntLeaves = MakeTexMat("Mat_BurntLeaves", tex.leavesBurnt, Color.white, Vector2.one),
            leaves = new[]
            {
                MakeTexMat("Mat_Leaves_A", tex.leaves, Color.white, Vector2.one, tex.leavesNormal),
                MakeTexMat("Mat_Leaves_B", tex.leaves, new Color(0.95f, 1f, 0.72f), Vector2.one, tex.leavesNormal),
                MakeTexMat("Mat_Leaves_C", tex.leaves, new Color(0.78f, 0.95f, 0.88f), Vector2.one, tex.leavesNormal)
            },
            fire = ParticleMat()
        };

        var rng = new System.Random(1234);
        var placed = new List<Vector2>();

        // Arvores de teste perto dos personagens
        Vector2[] testSpots = { new Vector2(-7, 4), new Vector2(-6, -5), new Vector2(7, 4), new Vector2(8, -4), new Vector2(0, 9) };
        foreach (Vector2 p in testSpots)
        {
            MakeTree(forest, p, 1f, (float)rng.NextDouble() * 360f, assets, rng.Next(3), rng.Next(3));
            placed.Add(p);
        }

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
            MakeTree(forest, p, scale, (float)rng.NextDouble() * 360f, assets, rng.Next(3), rng.Next(3));
            placed.Add(p);
        }
    }

    private static MeshRenderer MeshPart(Transform parent, string name, Mesh mesh, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        return mr;
    }

    private static void MakeTree(Transform parent, Vector2 pos, float scale, float yaw, TreeAssets a, int shape, int tint)
    {
        var root = new GameObject("Tree");
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(pos.x, 0f, pos.y);
        root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        root.transform.localScale = Vector3.one * scale;

        var col = root.AddComponent<CapsuleCollider>();
        col.height = 2.4f;
        col.radius = 0.3f;
        col.center = new Vector3(0, 1.2f, 0);

        MeshRenderer trunk = MeshPart(root.transform, "Trunk", a.trunk, a.trunkMat);
        MeshRenderer foliage = MeshPart(root.transform, "Foliage", a.foliage[shape], a.leaves[tint]);

        var fireGO = new GameObject("Fire");
        fireGO.transform.SetParent(root.transform, false);
        fireGO.transform.localPosition = new Vector3(0, 2.9f, 0);
        var ps = fireGO.AddComponent<ParticleSystem>();
        ConfigureFire(ps, a.fire);

        var tree = root.AddComponent<FlammableTree>();
        var so = new SerializedObject(tree);
        so.FindProperty("fire").objectReferenceValue = ps;
        so.FindProperty("burntMaterial").objectReferenceValue = a.burntLeaves;
        so.FindProperty("burntTrunkMaterial").objectReferenceValue = a.trunkBurntMat;

        var fol = so.FindProperty("foliage");
        fol.arraySize = 1;
        fol.GetArrayElementAtIndex(0).objectReferenceValue = foliage;

        var trk = so.FindProperty("trunk");
        trk.arraySize = 1;
        trk.GetArrayElementAtIndex(0).objectReferenceValue = trunk;

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
        shape.radius = 1.0f;

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

    private class AnimalAssets
    {
        public Mesh cow, bull, udder, skatePink, skateBlue;
        public Material cowMat, bullMat, plainMat;
    }

    private static Transform CreateCharacter(CharacterType type, Vector3 pos, AnimalAssets a)
    {
        var go = new GameObject(type == CharacterType.Vaca ? "Vaca" : "Boi");
        go.transform.position = pos;

        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.linearDamping = 0f;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        var col = go.AddComponent<CapsuleCollider>();
        col.height = 1.5f;
        col.radius = 0.6f;
        col.center = new Vector3(0, -0.05f, 0);
        col.sharedMaterial = MakeBouncy();

        if (type == CharacterType.Vaca)
        {
            BuildCow(go.transform, a);
            go.AddComponent<VacaController>();
        }
        else
        {
            BuildBull(go.transform, a);
            var flame = CreateFartParticles(go.transform, ParticleMat());
            var boi = go.AddComponent<BoiController>();
            var so = new SerializedObject(boi);
            so.FindProperty("fartParticles").objectReferenceValue = flame;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        return go.transform;
    }

    private static void CreateCollectibles()
    {
        var coinMat = MakeMat("Mat_Coin", new Color(1f, 0.78f, 0.12f), "Standard", 0.75f);
        coinMat.SetFloat("_Metallic", 0.6f);

        var arrowMat = MakeMat("Mat_Arrow", new Color(1f, 0.82f, 0.1f), "Standard", 0.5f);
        arrowMat.EnableKeyword("_EMISSION");
        arrowMat.SetColor("_EmissionColor", new Color(1f, 0.65f, 0.05f) * 0.9f);
        arrowMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

        var outline = MakeMat("Mat_ArrowOutline", new Color(0.12f, 0.06f, 0.0f), "Unlit/Color");

        var go = new GameObject("Collectibles");
        var mgr = go.AddComponent<CollectibleManager>();
        var so = new SerializedObject(mgr);
        so.FindProperty("coinMaterial").objectReferenceValue = coinMat;
        so.FindProperty("arrowMaterial").objectReferenceValue = arrowMat;
        so.FindProperty("outlineMaterial").objectReferenceValue = outline;
        so.FindProperty("particleMaterial").objectReferenceValue = ParticleMat();
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(arrowMat);
    }

    // Peido de segurar: sistema continuo (loop) com chama + nuvem de gas esverdeada.
    private static ParticleSystem CreateFartParticles(Transform parent, Material mat)
    {
        var go = new GameObject("FartParticles");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 0.1f, -1.0f);
        go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // +Z do emissor aponta para tras do boi

        var flame = go.AddComponent<ParticleSystem>();
        flame.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        {
            var main = flame.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 3.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
            main.startColor = new Color(1f, 0.6f, 0.15f, 0.9f);
            main.gravityModifier = -0.05f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = flame.emission;
            emission.rateOverTime = 45f;

            var shape = flame.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 14f;
            shape.radius = 0.08f;

            var colorLife = flame.colorOverLifetime;
            colorLife.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.95f, 0.45f), 0f),
                    new GradientColorKey(new Color(1f, 0.45f, 0.1f), 0.5f),
                    new GradientColorKey(new Color(0.25f, 0.2f, 0.18f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.95f, 0f),
                    new GradientAlphaKey(0.7f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorLife.color = g;

            var sizeLife = flame.sizeOverLifetime;
            sizeLife.enabled = true;
            sizeLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1f, 1, 0.25f));

            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
        }

        var gasGO = new GameObject("FartGas");
        gasGO.transform.SetParent(go.transform, false);
        var gas = gasGO.AddComponent<ParticleSystem>();
        gas.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        {
            var main = gas.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startColor = new Color(0.6f, 0.78f, 0.25f, 0.4f);
            main.gravityModifier = -0.1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = gas.emission;
            emission.rateOverTime = 22f;

            var shape = gas.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 28f;
            shape.radius = 0.12f;

            var colorLife = gas.colorOverLifetime;
            colorLife.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.7f, 0.85f, 0.3f), 0f),
                    new GradientColorKey(new Color(0.45f, 0.6f, 0.2f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.5f, 0f),
                    new GradientAlphaKey(0.3f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorLife.color = g;

            var sizeLife = gas.sizeOverLifetime;
            sizeLife.enabled = true;
            sizeLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.5f, 1, 1.7f));

            gasGO.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
        }

        return flame;
    }

    // Jato de leite de uma teta (apontado e ligado pelo CowMilkAnimator).
    private static ParticleSystem CreateMilkSpray(Transform parent, Vector3 localPos, Material mat)
    {
        var go = new GameObject("TeatSpray");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.65f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4.5f, 5.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.13f);
        main.startColor = new Color(1f, 1f, 1f, 0.95f);
        main.gravityModifier = 1f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 300;

        var emission = ps.emission;
        emission.rateOverTime = 55f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 4f;
        shape.radius = 0.01f;

        var colorLife = ps.colorOverLifetime;
        colorLife.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(0.92f, 0.95f, 1f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.95f, 0.75f),
                new GradientAlphaKey(0f, 1f)
            });
        colorLife.color = g;

        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
        return ps;
    }

    private static void SetupCamera(Transform target)
    {
        var camGO = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGO.tag = "MainCamera";
        var cam = camGO.GetComponent<ThirdPersonCamera>() ?? camGO.AddComponent<ThirdPersonCamera>();
        cam.target = target;
        EditorUtility.SetDirty(cam);
    }

    // ---------- modelos ----------

    private static MeshRenderer MeshOnly(Transform parent, string name, Mesh mesh, Material mat, Vector3 pos)
    {
        var mr = MeshPart(parent, name, mesh, mat);
        mr.transform.localPosition = pos;
        return mr;
    }

    private static void BuildCow(Transform root, AnimalAssets a)
    {
        var visual = new GameObject("Visual").transform;
        visual.SetParent(root, false);

        MeshOnly(visual, "Body", a.cow, a.cowMat, Vector3.zero);

        // cabeca vazia so para agrupar os olhos (o CharacterBase os encontra por "Visual/Head/EyeLeft")
        var head = new GameObject("Head").transform;
        head.SetParent(visual, false);
        head.localPosition = new Vector3(0f, 0.58f, 1.02f);
        Eye(head, "EyeLeft", new Vector3(-0.22f, 0.09f, 0.34f), new Vector3(0.25f, 0.29f, 0.17f));
        Eye(head, "EyeRight", new Vector3(0.22f, 0.09f, 0.34f), new Vector3(0.25f, 0.29f, 0.17f));

        // ubere (separado para poder "pulsar") + 4 jatos de leite nas pontas das tetas
        var udder = MeshOnly(visual, "Udder", a.udder, a.plainMat, new Vector3(0f, -0.42f, -0.45f)).transform;
        var sprays = new List<ParticleSystem>();
        var particleMat = ParticleMat();
        foreach (float sx in new[] { -0.09f, 0.09f })
            foreach (float sz in new[] { -0.09f, 0.09f })
                sprays.Add(CreateMilkSpray(visual, new Vector3(sx, -0.71f, -0.45f + sz), particleMat));

        BuildTail(visual, new Vector3(0f, 0.3f, -0.86f), MakeMat("Mat_CowTail", new Color(0.95f, 0.93f, 0.88f)),
            MakeMat("Mat_Black", new Color(0.1f, 0.09f, 0.1f)), 0.09f);

        BuildSkates(root, visual, a.skatePink, a.plainMat, new[] { -0.34f, 0.34f }, new[] { -0.5f, 0.5f });

        var anim = root.gameObject.AddComponent<CowMilkAnimator>();
        var so = new SerializedObject(anim);
        so.FindProperty("visual").objectReferenceValue = visual;
        so.FindProperty("udder").objectReferenceValue = udder;
        var arr = so.FindProperty("teatSprays");
        arr.arraySize = sprays.Count;
        for (int i = 0; i < sprays.Count; i++)
            arr.GetArrayElementAtIndex(i).objectReferenceValue = sprays[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildBull(Transform root, AnimalAssets a)
    {
        var visual = new GameObject("Visual").transform;
        visual.SetParent(root, false);

        MeshOnly(visual, "Body", a.bull, a.bullMat, Vector3.zero);

        var head = new GameObject("Head").transform;
        head.SetParent(visual, false);
        head.localPosition = new Vector3(0f, 0.52f, 1.10f);
        Eye(head, "EyeLeft", new Vector3(-0.24f, 0.10f, 0.36f), new Vector3(0.24f, 0.27f, 0.17f));
        Eye(head, "EyeRight", new Vector3(0.24f, 0.10f, 0.36f), new Vector3(0.24f, 0.27f, 0.17f));

        var darkFur = MakeMat("Mat_BullDark", new Color(0.25f, 0.12f, 0.07f));
        BuildTail(visual, new Vector3(0f, 0.3f, -0.87f), darkFur, darkFur, 0.1f);

        BuildSkates(root, visual, a.skateBlue, a.plainMat, new[] { -0.38f, 0.38f }, new[] { -0.55f, 0.5f });
    }

    private static void BuildSkates(Transform root, Transform visual, Mesh bootMesh, Material bootMat, float[] xs, float[] zs)
    {
        var wheelMat = MakeMat("Mat_Wheel", new Color(1f, 0.55f, 0.1f), "Standard", 0.3f);
        var hubMat = MakeMat("Mat_WheelHub", new Color(0.15f, 0.15f, 0.18f));
        var wheels = new List<Transform>();

        foreach (float x in xs)
            foreach (float z in zs)
            {
                float zc = z + 0.03f;
                MeshOnly(visual, "Skate", bootMesh, bootMat, new Vector3(x, -0.52f, zc));
                foreach (float dz in new[] { -0.15f, 0.15f })
                {
                    var wheel = Part(visual, "Wheel", PrimitiveType.Cylinder, new Vector3(x, -0.65f, zc + dz),
                        new Vector3(0.28f, 0.05f, 0.28f), wheelMat, new Vector3(0, 0, 90));
                    Part(wheel, "Hub", PrimitiveType.Cube, Vector3.zero, new Vector3(0.9f, 1.4f, 0.14f), hubMat);
                    wheels.Add(wheel);
                }
            }

        var sw = root.gameObject.AddComponent<SkateWheels>();
        var so = new SerializedObject(sw);
        var arr = so.FindProperty("wheels");
        arr.arraySize = wheels.Count;
        for (int i = 0; i < wheels.Count; i++)
            arr.GetArrayElementAtIndex(i).objectReferenceValue = wheels[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Rabinho de 3 juntas + tufo. O TailAnimator na 1a junta balanca tudo.
    private static void BuildTail(Transform visual, Vector3 attach, Material tailMat, Material tuftMat, float thickness)
    {
        const int count = 3;
        const float segLen = 0.28f;
        var joints = new Transform[count];
        Transform parent = visual;

        for (int i = 0; i < count; i++)
        {
            var joint = new GameObject(i == 0 ? "Tail" : "TailJoint" + i).transform;
            joint.SetParent(parent, false);
            joint.localPosition = i == 0 ? attach : new Vector3(0f, -segLen, 0f);
            joint.localRotation = Quaternion.Euler(i == 0 ? 28f : 9f, 0f, 0f);

            Part(joint, "TailMesh", PrimitiveType.Capsule, new Vector3(0f, -segLen * 0.5f, 0f),
                new Vector3(thickness, segLen * 0.55f, thickness), tailMat);

            joints[i] = joint;
            parent = joint;
        }

        Part(joints[count - 1], "TailTuft", PrimitiveType.Sphere, new Vector3(0f, -segLen - 0.04f, 0f),
            new Vector3(0.2f, 0.34f, 0.2f), tuftMat);

        var anim = joints[0].gameObject.AddComponent<TailAnimator>();
        var so = new SerializedObject(anim);
        var arr = so.FindProperty("segments");
        arr.arraySize = count;
        for (int i = 0; i < count; i++)
            arr.GetArrayElementAtIndex(i).objectReferenceValue = joints[i];
        so.ApplyModifiedPropertiesWithoutUndo();
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
        var eye = Part(head, name, PrimitiveType.Sphere, pos, scale, MakeMat("Mat_EyeWhite", Color.white, "Standard", 0.6f));
        var pupil = Part(eye, "Pupil", PrimitiveType.Sphere, new Vector3(0, -0.05f, 0.4f), new Vector3(0.5f, 0.5f, 0.4f), MakeMat("Mat_Pupil", new Color(0.04f, 0.03f, 0.04f), "Standard", 0.8f));
        Part(pupil, "Glint", PrimitiveType.Sphere, new Vector3(0.22f, 0.26f, 0.42f), new Vector3(0.28f, 0.28f, 0.2f), MakeMat("Mat_Glint", Color.white, "Standard", 0.9f));
    }
}
