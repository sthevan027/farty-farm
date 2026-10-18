using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class SceneSetup
{
    [MenuItem("Farty Farm/Setup Arena Scene")]
    public static void CreateArenaScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateGround();
        CreateWalls();
        var boiTransform = CreateBoi();
        CreateCamera(boiTransform);
        CreateGameManager(boiTransform);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Arena.unity");
        AssetDatabase.Refresh();
        Debug.Log("Arena scene criada com sucesso: Assets/Scenes/Arena.unity");
    }

    private static void CreateGround()
    {
        var groundGO = GameObject.CreatePrimitive(PrimitiveType.Plane);
        groundGO.name = "Ground";
        groundGO.transform.position = Vector3.zero;
        groundGO.transform.localScale = new Vector3(10, 1, 10);

        DestroyImmediate(groundGO.GetComponent<Collider>());
        var groundCollider = groundGO.AddComponent<BoxCollider>();
        groundCollider.size = new Vector3(20, 0.1f, 20);

        var groundMat = new Material(Shader.Find("Standard"));
        groundMat.color = new Color(0.3f, 0.8f, 0.3f);
        groundGO.GetComponent<Renderer>().material = groundMat;
    }

    private static void CreateWalls()
    {
        CreateWall("Wall_Front", new Vector3(0, 0.5f, 10), new Vector3(20, 1, 1));
        CreateWall("Wall_Back", new Vector3(0, 0.5f, -10), new Vector3(20, 1, 1));
        CreateWall("Wall_Left", new Vector3(-10, 0.5f, 0), new Vector3(1, 1, 20));
        CreateWall("Wall_Right", new Vector3(10, 0.5f, 0), new Vector3(1, 1, 20));
    }

    private static void CreateWall(string name, Vector3 position, Vector3 scale)
    {
        var wallGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallGO.name = name;
        wallGO.transform.position = position;
        wallGO.transform.localScale = scale;

        var wallMat = new Material(Shader.Find("Standard"));
        wallMat.color = new Color(0.5f, 0.5f, 0.5f);
        wallGO.GetComponent<Renderer>().material = wallMat;
    }

    private static Transform CreateBoi()
    {
        var boiGO = new GameObject("Boi");
        boiGO.transform.position = new Vector3(0, 0.5f, 0);

        var rb = boiGO.AddComponent<Rigidbody>();
        rb.mass = 1;
        rb.drag = 0;
        rb.angularDrag = 0.1f;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        var capsuleCollider = boiGO.AddComponent<CapsuleCollider>();
        capsuleCollider.height = 1.6f;
        capsuleCollider.radius = 0.5f;
        capsuleCollider.center = new Vector3(0, 0.3f, 0);

        CreateBodyPart(boiGO, "Body", Vector3.zero, new Vector3(1, 0.6f, 1), new Color(1f, 0.6f, 0.2f));

        var headGO = CreateBodyPart(boiGO, "Head", new Vector3(0, 0.7f, 0), new Vector3(0.8f, 0.8f, 0.8f), new Color(0.9f, 0.5f, 0.1f));
        CreateEye(headGO, "EyeLeft", new Vector3(-0.25f, 0.2f, 0.35f));
        CreateEye(headGO, "EyeRight", new Vector3(0.25f, 0.2f, 0.35f));

        var particlesGO = new GameObject("FartParticles");
        particlesGO.transform.parent = boiGO.transform;
        particlesGO.transform.localPosition = new Vector3(0, 0, -0.5f);
        var ps = particlesGO.AddComponent<ParticleSystem>();
        ps.playOnAwake = false;

        var psRenderer = particlesGO.GetComponent<ParticleSystemRenderer>();
        var particleMat = new Material(Shader.Find("Standard"));
        particleMat.color = new Color(0.6f, 0.6f, 0.5f, 0.7f);
        psRenderer.material = particleMat;

        var boiController = boiGO.AddComponent<BoiController>();
        EditorUtility.SetDirty(boiController);

        return boiGO.transform;
    }

    private static GameObject CreateBodyPart(GameObject parent, string name, Vector3 localPos, Vector3 scale, Color color)
    {
        var partGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        if (name == "Head")
        {
            DestroyImmediate(partGO);
            partGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        }

        partGO.name = name;
        partGO.transform.parent = parent.transform;
        partGO.transform.localPosition = localPos;
        partGO.transform.localScale = scale;

        DestroyImmediate(partGO.GetComponent<Collider>());
        var mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        partGO.GetComponent<Renderer>().material = mat;

        return partGO;
    }

    private static void CreateEye(GameObject headGO, string eyeName, Vector3 localPos)
    {
        var eyeGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eyeGO.name = eyeName;
        eyeGO.transform.parent = headGO.transform;
        eyeGO.transform.localPosition = localPos;
        eyeGO.transform.localScale = new Vector3(0.2f, 0.2f, 0.15f);

        DestroyImmediate(eyeGO.GetComponent<Collider>());
        var eyeMat = new Material(Shader.Find("Standard"));
        eyeMat.color = Color.white;
        eyeGO.GetComponent<Renderer>().material = eyeMat;

        var pupilGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pupilGO.name = "Pupil";
        pupilGO.transform.parent = eyeGO.transform;
        pupilGO.transform.localPosition = new Vector3(0, 0, 0.15f);
        pupilGO.transform.localScale = new Vector3(0.12f, 0.12f, 0.08f);

        DestroyImmediate(pupilGO.GetComponent<Collider>());
        var pupilMat = new Material(Shader.Find("Standard"));
        pupilMat.color = Color.black;
        pupilGO.GetComponent<Renderer>().material = pupilMat;
    }

    private static void CreateCamera(Transform boiTransform)
    {
        var cameraGO = new GameObject("MainCamera");
        cameraGO.tag = "MainCamera";
        var camera = cameraGO.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.1f, 0.1f, 0.1f);

        cameraGO.AddComponent<AudioListener>();
        var topDownCamera = cameraGO.AddComponent<TopDownCamera>();
        topDownCamera.target = boiTransform;

        EditorUtility.SetDirty(topDownCamera);
    }

    private static void CreateGameManager(Transform boiTransform)
    {
        var gmGO = new GameObject("GameManager");
        var gm = gmGO.AddComponent<GameManager>();

        var boiController = boiTransform.GetComponent<BoiController>();
        if (boiController != null)
        {
            var boiField = typeof(GameManager).GetField("boiController", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (boiField != null)
                boiField.SetValue(gm, boiController);
        }

        EditorUtility.SetDirty(gm);
    }
}
