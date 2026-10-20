using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Malhas das arvores geradas por codigo e salvas em Assets/Meshes.
/// Uma malha unica de tronco e 3 variacoes de copa sao compartilhadas por todas as arvores.
/// </summary>
public static class TreeMeshFactory
{
    private const string Folder = "Assets/Meshes";

    // ------------------------------------------------------------------
    // Tronco: cilindro afunilado, com base alargada (raizes) e leve irregularidade.
    // ------------------------------------------------------------------

    public static Mesh CreateTrunk()
    {
        const int seg = 14;
        float[] ys = { 0f, 0.10f, 0.35f, 0.9f, 1.6f, 2.3f, 2.9f };
        float[] rs = { 0.46f, 0.34f, 0.24f, 0.20f, 0.16f, 0.12f, 0.07f };
        int rings = ys.Length;

        var verts = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();

        for (int r = 0; r < rings; r++)
        {
            int prev = Mathf.Max(r - 1, 0);
            int next = Mathf.Min(r + 1, rings - 1);
            float slope = (rs[next] - rs[prev]) / (ys[next] - ys[prev]);

            for (int s = 0; s <= seg; s++)
            {
                float a = s / (float)seg * Mathf.PI * 2f;
                float cos = Mathf.Cos(a);
                float sin = Mathf.Sin(a);

                // irregularidade que fecha perfeitamente na emenda (multiplo inteiro de 2*pi)
                float rr = rs[r] * (1f + 0.07f * Mathf.Sin(a * 3f + r * 1.7f));

                verts.Add(new Vector3(cos * rr, ys[r], sin * rr));
                normals.Add(new Vector3(cos, -slope, sin).normalized);
                uvs.Add(new Vector2(s / (float)seg, ys[r] * 0.7f));
            }
        }

        for (int r = 0; r < rings - 1; r++)
            for (int s = 0; s < seg; s++)
            {
                int a = r * (seg + 1) + s;
                int b = a + 1;
                int c = a + (seg + 1);
                int d = c + 1;
                tris.Add(a); tris.Add(c); tris.Add(d);
                tris.Add(a); tris.Add(d); tris.Add(b);
            }

        var mesh = new Mesh { name = "TreeTrunk" };
        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        return Save(mesh);
    }

    // ------------------------------------------------------------------
    // Copa: varias esferas "amassadas" por ruido, fundidas numa malha so.
    // ------------------------------------------------------------------

    public static Mesh CreateFoliage(int seed)
    {
        var rng = new System.Random(seed);
        var clumps = new List<Vector4>(); // x, y, z, raio

        clumps.Add(new Vector4(0f, 2.65f, 0f, 1.15f));
        int extra = 5 + rng.Next(0, 3);
        for (int i = 0; i < extra; i++)
        {
            float ang = (float)(rng.NextDouble() * Mathf.PI * 2.0);
            float dist = 0.55f + (float)rng.NextDouble() * 0.55f;
            float y = 2.1f + (float)rng.NextDouble() * 1.4f;
            float r = 0.70f + (float)rng.NextDouble() * 0.40f;
            clumps.Add(new Vector4(Mathf.Cos(ang) * dist, y, Mathf.Sin(ang) * dist, r));
        }
        clumps.Add(new Vector4(0.05f, 3.75f, -0.05f, 0.72f));

        Vector3 canopyCenter = new Vector3(0f, 2.9f, 0f);
        const int rings = 12;
        const int segs = 20;

        var verts = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();

        float noiseSeed = seed * 3.17f;

        foreach (Vector4 cl in clumps)
        {
            Vector3 center = new Vector3(cl.x, cl.y, cl.z);
            float radius = cl.w;
            float uOff = (float)rng.NextDouble();
            float vOff = (float)rng.NextDouble();
            int baseIndex = verts.Count;

            for (int j = 0; j <= rings; j++)
            {
                float phi = Mathf.PI * j / rings;
                for (int k = 0; k <= segs; k++)
                {
                    float theta = Mathf.PI * 2f * k / segs;
                    Vector3 dir = new Vector3(
                        Mathf.Sin(phi) * Mathf.Cos(theta),
                        Mathf.Cos(phi),
                        Mathf.Sin(phi) * Mathf.Sin(theta));

                    // lumpiness depende so da posicao => sem rachadura na emenda
                    Vector3 p = center + dir * radius;
                    float n = Perlin3(p.x * 1.7f + noiseSeed, p.y * 1.7f, p.z * 1.7f + noiseSeed);
                    float rr = radius * (0.80f + 0.40f * n);

                    Vector3 pos = center + new Vector3(dir.x * rr, dir.y * rr * 0.88f, dir.z * rr);
                    Vector3 outward = (pos - canopyCenter).normalized;
                    Vector3 normal = (dir * 0.5f + outward * 0.5f).normalized;

                    verts.Add(pos);
                    normals.Add(normal);
                    uvs.Add(new Vector2(k / (float)segs * 3f + uOff, j / (float)rings * 2f + vOff));
                }
            }

            for (int j = 0; j < rings; j++)
                for (int k = 0; k < segs; k++)
                {
                    int a = baseIndex + j * (segs + 1) + k; // topo-esq
                    int b = a + 1;                          // topo-dir
                    int c = a + (segs + 1);                 // baixo-esq
                    int d = c + 1;                          // baixo-dir
                    tris.Add(a); tris.Add(b); tris.Add(d);
                    tris.Add(a); tris.Add(d); tris.Add(c);
                }
        }

        var mesh = new Mesh { name = "TreeFoliage_" + seed };
        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return Save(mesh);
    }

    // ------------------------------------------------------------------

    private static float Perlin3(float x, float y, float z)
    {
        float ab = Mathf.PerlinNoise(x, y);
        float bc = Mathf.PerlinNoise(y, z);
        float ca = Mathf.PerlinNoise(z, x);
        return (ab + bc + ca) / 3f;
    }

    private static Mesh Save(Mesh mesh)
    {
        System.IO.Directory.CreateDirectory(Folder);
        string path = Folder + "/" + mesh.name + ".asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh, path);
        return AssetDatabase.LoadAssetAtPath<Mesh>(path);
    }
}
