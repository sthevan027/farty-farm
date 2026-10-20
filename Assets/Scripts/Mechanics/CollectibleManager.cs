using System.Collections.Generic;
using UnityEngine;

/// <summary>Cria as ondas de moedas, as setas estilo GTA V e cuida da pontuacao.</summary>
public class CollectibleManager : MonoBehaviour
{
    public static int Score { get; private set; }
    public static int Wave { get; private set; }

    [Header("Materiais (criados pelo SceneSetup)")]
    [SerializeField] private Material coinMaterial;
    [SerializeField] private Material arrowMaterial;
    [SerializeField] private Material outlineMaterial;
    [SerializeField] private Material particleMaterial;

    [Header("Ondas")]
    [SerializeField] private int coinsPerWave = 12;
    [SerializeField] private float arenaHalf = 36f;
    [Tooltip("Gas/leite devolvido ao pegar uma moeda")]
    [SerializeField] private float gasBonus = 25f;

    private Mesh arrowMesh;
    private Mesh outlineMesh;
    private ParticleSystem burst;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Score = 0;
        Wave = 0;
    }

    private void Start()
    {
        arrowMesh = BuildArrowMesh(false);
        outlineMesh = BuildArrowMesh(true);
        CreateBurst();
        SpawnWave();
    }

    private void SpawnWave()
    {
        Wave++;
        var taken = new List<Vector3>();
        int attempts = 0;
        while (taken.Count < coinsPerWave && attempts++ < 3000)
        {
            Vector3 p = new Vector3(Random.Range(-arenaHalf, arenaHalf), 0f, Random.Range(-arenaHalf, arenaHalf));
            if (p.magnitude < 5f) continue;

            bool ok = true;
            foreach (FlammableTree t in FlammableTree.All)
            {
                Vector3 d = t.transform.position - p;
                d.y = 0f;
                if (d.sqrMagnitude < 2.6f * 2.6f) { ok = false; break; }
            }
            if (!ok) continue;
            foreach (Vector3 q in taken)
                if ((q - p).sqrMagnitude < 7f * 7f) { ok = false; break; }
            if (!ok) continue;

            taken.Add(p);
            CreateCoin(p);
        }
    }

    private void CreateCoin(Vector3 pos)
    {
        var root = new GameObject("Coin");
        root.transform.SetParent(transform, false);
        root.transform.position = pos;

        var trigger = root.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 1.2f;
        trigger.center = new Vector3(0f, 0.9f, 0f);

        // moeda
        var spinner = new GameObject("Spinner").transform;
        spinner.SetParent(root.transform, false);
        var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "CoinMesh";
        disc.transform.SetParent(spinner, false);
        disc.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        disc.transform.localScale = new Vector3(0.9f, 0.07f, 0.9f);
        Destroy(disc.GetComponent<Collider>());
        disc.GetComponent<Renderer>().sharedMaterial = coinMaterial;
        var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rim.name = "CoinCenter";
        rim.transform.SetParent(spinner, false);
        rim.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        rim.transform.localScale = new Vector3(0.55f, 0.09f, 0.55f);
        Destroy(rim.GetComponent<Collider>());
        rim.GetComponent<Renderer>().sharedMaterial = arrowMaterial;

        // seta estilo GTA V
        var marker = new GameObject("Arrow").transform;
        marker.SetParent(root.transform, false);
        AddMesh(marker, "ArrowBody", arrowMesh, arrowMaterial, 1f);
        AddMesh(marker, "ArrowOutline", outlineMesh, outlineMaterial, 1.18f);

        // brilhinhos
        var sparkGO = new GameObject("Sparkles");
        sparkGO.transform.SetParent(root.transform, false);
        sparkGO.transform.localPosition = new Vector3(0f, 0.3f, 0f);
        ConfigureSparkles(sparkGO.AddComponent<ParticleSystem>());

        root.AddComponent<Collectible>().Init(this, spinner, marker);
    }

    private static void AddMesh(Transform parent, string name, Mesh mesh, Material mat, float scale)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localScale = Vector3.one * scale;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    public void OnCollected(Collectible coin, CharacterBase who)
    {
        Score++;
        who.RefillGas(gasBonus);

        if (burst != null)
        {
            burst.transform.position = coin.transform.position + Vector3.up;
            burst.Emit(30);
        }

        // ultima moeda da onda? comeca outra
        if (Collectible.All.Count <= 1) Invoke(nameof(SpawnWave), 1.5f);
    }

    // ---------- visuais ----------

    private void ConfigureSparkles(ParticleSystem ps)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
        main.startColor = new Color(1f, 0.9f, 0.35f, 0.9f);
        main.gravityModifier = -0.15f;

        var emission = ps.emission;
        emission.rateOverTime = 7f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.55f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var colorLife = ps.colorOverLifetime;
        colorLife.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(new Color(1f, 0.95f, 0.5f), 0f), new GradientColorKey(new Color(1f, 0.7f, 0.2f), 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
        colorLife.color = g;

        if (particleMaterial != null) ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = particleMaterial;
        ps.Play();
    }

    private void CreateBurst()
    {
        var go = new GameObject("CollectBurst");
        go.transform.SetParent(transform, false);
        burst = go.AddComponent<ParticleSystem>();
        burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = burst.main;
        main.playOnAwake = false;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.22f);
        main.startColor = new Color(1f, 0.85f, 0.25f, 1f);
        main.gravityModifier = 1.2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = burst.emission;
        emission.rateOverTime = 0f;

        var shape = burst.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        var colorLife = burst.colorOverLifetime;
        colorLife.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(new Color(1f, 0.95f, 0.5f), 0f), new GradientColorKey(new Color(1f, 0.6f, 0.1f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        colorLife.color = g;

        if (particleMaterial != null) burst.GetComponent<ParticleSystemRenderer>().sharedMaterial = particleMaterial;
    }

    // Seta apontando para baixo (ponta = (0,-0.6)), com espessura e uma entalhe no topo.
    // "inverted" gera o casco invertido usado como contorno.
    private static Mesh BuildArrowMesh(bool inverted)
    {
        Vector2[] poly =
        {
            new Vector2(0f, -0.6f),    // ponta
            new Vector2(0.55f, 0.15f), // asa direita
            new Vector2(0f, 0.02f),    // entalhe
            new Vector2(-0.55f, 0.15f) // asa esquerda
        };
        const float t = 0.1f;

        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var tris = new List<int>();

        // frente (+z) e tras (-z)
        int[][] faces = { new[] { 0, 1, 2 }, new[] { 0, 2, 3 } };
        foreach (int[] f in faces)
        {
            int b = verts.Count;
            for (int i = 0; i < 3; i++) { verts.Add(new Vector3(poly[f[i]].x, poly[f[i]].y, t)); norms.Add(Vector3.forward); }
            tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);

            b = verts.Count;
            for (int i = 0; i < 3; i++) { verts.Add(new Vector3(poly[f[i]].x, poly[f[i]].y, -t)); norms.Add(Vector3.back); }
            tris.Add(b); tris.Add(b + 2); tris.Add(b + 1);
        }

        // laterais
        for (int i = 0; i < poly.Length; i++)
        {
            Vector2 a = poly[i];
            Vector2 c = poly[(i + 1) % poly.Length];
            Vector3 outward = new Vector3(c.y - a.y, -(c.x - a.x), 0f).normalized;

            int b = verts.Count;
            verts.Add(new Vector3(a.x, a.y, t));
            verts.Add(new Vector3(c.x, c.y, t));
            verts.Add(new Vector3(c.x, c.y, -t));
            verts.Add(new Vector3(a.x, a.y, -t));
            for (int k = 0; k < 4; k++) norms.Add(outward);

            Vector3 n = Vector3.Cross(verts[b + 1] - verts[b], verts[b + 2] - verts[b]);
            if (Vector3.Dot(n, outward) >= 0f)
            {
                tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
                tris.Add(b); tris.Add(b + 2); tris.Add(b + 3);
            }
            else
            {
                tris.Add(b); tris.Add(b + 2); tris.Add(b + 1);
                tris.Add(b); tris.Add(b + 3); tris.Add(b + 2);
            }
        }

        if (inverted)
        {
            for (int i = 0; i < tris.Count; i += 3)
            {
                int tmp = tris[i + 1];
                tris[i + 1] = tris[i + 2];
                tris[i + 2] = tmp;
            }
            for (int i = 0; i < norms.Count; i++) norms[i] = -norms[i];
        }

        var mesh = new Mesh { name = inverted ? "ArrowOutline" : "Arrow" };
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }
}
