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
        boi.transform.position = new Vector3(0, 0.6f, 0);

        var rb = boi.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        var col = boi.AddComponent<CapsuleCollider>();
        col.height = 1.6f;
        col.radius = 0.5f;
        col.center = new Vector3(0, 0.3f, 0);
        col.sharedMaterial = MakeSlippery();

        var body = Part(boi, "Body", PrimitiveType.Cube, new Vector3(0, 0, 0), new Vector3(1f, 0.7f, 1.3f), MakeMat("Mat_BoiBody", new Color(1f, 0.62f, 0.2f)));
        var head = Part(boi, "Head", PrimitiveType.Sphere, new Vector3(0, 0.7f, 0.15f), new Vector3(0.85f, 0.85f, 0.85f), MakeMat("Mat_BoiHead", new Color(0.95f, 0.5f, 0.12f)));

        var eyeMat = MakeMat("Mat_EyeWhite", Color.white);
        var pupilMat = MakeMat("Mat_Pupil", Color.black);
        Transform eyeL = Eye(head, "EyeLeft", new Vector3(-0.22f, 0.12f, 0.42f), eyeMat, pupilMat);
        Transform eyeR = Eye(head, "EyeRight", new Vector3(0.22f, 0.12f, 0.42f), eyeMat, pupilMat);

        // Patins: dois cubos escuros embaixo do corpo
        var skateMat = MakeMat("Mat_Skate", new Color(0.2f, 0.2f, 0.25f));
        Part(boi, "SkateL", PrimitiveType.Cube, new Vector3(-0.3f, -0.4f, 0), new Vector3(0.25f, 0.12f, 1.2f), skateMat);
        Part(boi, "SkateR", PrimitiveType.Cube, new Vector3(0.3f, -0.4f, 0), new Vector3(0.25f, 0.12f, 1.2f), skateMat);

        var ps = CreateFartParticles(boi);

        var controller = boi.AddComponent<BoiController>();
        var so = new SerializedObject(controller);
        so.FindProperty("eyeLeft").objectReferenceValue = eyeL;
        so.FindProperty("eyeRight").objectReferenceValue = eyeR;
        so.FindProperty("fartParticles").objectReferenceValue = ps;
        so.ApplyModifiedPropertiesWithoutUndo();

        return boi.transform;
    }

    private static Transform Part(GameObject parent, string name, PrimitiveType type, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        return go.transform;
    }

    private static Transform Eye(Transform head, string name, Vector3 pos, Material eyeMat, Material pupilMat)
    {
        var eye = Part(head.gameObject, name, PrimitiveType.Sphere, pos, new Vector3(0.32f, 0.32f, 0.22f), eyeMat);
        Part(eye.gameObject, "Pupil", PrimitiveType.Sphere, new Vector3(0, 0, 0.4f), new Vector3(0.5f, 0.5f, 0.4f), pupilMat);
        return eye;
    }

    private static ParticleSystem CreateFartParticles(GameObject boi)
    {
        var go = new GameObject("FartParticles");
        go.transform.SetParent(boi.transform, false);
        go.transform.localPosition = new Vector3(0, 0.1f, -0.7f);
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
        so.FindProperty("boi").objectReferenceValue = boi.GetComponent<BoiController>();
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
