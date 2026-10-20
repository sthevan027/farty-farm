using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Uma forma da escultura: elipsoide, capsula (cone arredondado) ou torus.</summary>
public struct SdfShape
{
    public int type;                 // 0 = elipsoide, 1 = capsula, 2 = torus
    public Vector3 c, a, b, radii;
    public Vector3 row0, row1, row2; // rotacao inversa (mundo -> local)
    public float r0, r1, k;
    public Color color;              // rgb = cor da regiao, a = quanto o padrao de pelagem se aplica

    public static SdfShape Ellipsoid(Vector3 c, Vector3 r, Vector3 row0, Vector3 row1, Vector3 row2, float k, Color color)
    {
        return new SdfShape { type = 0, c = c, radii = r, row0 = row0, row1 = row1, row2 = row2, k = k, color = color };
    }

    public static SdfShape Capsule(Vector3 a, Vector3 b, float ra, float rb, float k, Color color)
    {
        return new SdfShape { type = 1, a = a, b = b, r0 = ra, r1 = rb, k = k, color = color };
    }

    public static SdfShape Torus(Vector3 c, Vector3 row0, Vector3 row1, Vector3 row2, float major, float minor, float k, Color color)
    {
        return new SdfShape { type = 2, c = c, row0 = row0, row1 = row1, row2 = row2, r0 = major, r1 = minor, k = k, color = color };
    }

    public float Dist(Vector3 p)
    {
        if (type == 0)
        {
            Vector3 d = p - c;
            float qx = row0.x * d.x + row0.y * d.y + row0.z * d.z;
            float qy = row1.x * d.x + row1.y * d.y + row1.z * d.z;
            float qz = row2.x * d.x + row2.y * d.y + row2.z * d.z;
            float ax = qx / radii.x, ay = qy / radii.y, az = qz / radii.z;
            float bx = ax / radii.x, by = ay / radii.y, bz = az / radii.z;
            float k0 = Mathf.Sqrt(ax * ax + ay * ay + az * az);
            float k1 = Mathf.Sqrt(bx * bx + by * by + bz * bz);
            return k0 * (k0 - 1f) / Mathf.Max(k1, 1e-6f);
        }
        if (type == 1)
        {
            Vector3 ba = b - a;
            Vector3 pa = p - a;
            float h = Mathf.Clamp01(Vector3.Dot(pa, ba) / Vector3.Dot(ba, ba));
            return (pa - ba * h).magnitude - Mathf.Lerp(r0, r1, h);
        }
        else
        {
            Vector3 d = p - c;
            float qx = row0.x * d.x + row0.y * d.y + row0.z * d.z;
            float qy = row1.x * d.x + row1.y * d.y + row1.z * d.z;
            float qz = row2.x * d.x + row2.y * d.y + row2.z * d.z;
            float ring = Mathf.Sqrt(qx * qx + qz * qz) - r0;
            return Mathf.Sqrt(ring * ring + qy * qy) - r1;
        }
    }
}

/// <summary>Esfera de tinta: pinta uma regiao (narinas, etc.) por cima das cores das formas.</summary>
public struct SdfPaint
{
    public Vector3 center;
    public float radius, soft;
    public Color color;

    public SdfPaint(Vector3 center, float radius, float soft, Color color)
    {
        this.center = center;
        this.radius = radius;
        this.soft = soft;
        this.color = color;
    }
}

/// <summary>
/// Transforma uma uniao suave de formas numa malha lisa (surface nets).
/// Cor por vertice = regiao + oclusao ambiente. Sem UV: o shader usa mapeamento triplanar.
/// </summary>
public static class SdfMesher
{
    private static readonly int[,] Corners =
    {
        {0,0,0},{1,0,0},{0,1,0},{1,1,0},{0,0,1},{1,0,1},{0,1,1},{1,1,1}
    };

    private static readonly int[,] Edges =
    {
        {0,1},{2,3},{4,5},{6,7},{0,2},{1,3},{4,6},{5,7},{0,4},{1,5},{2,6},{3,7}
    };

    public static float SmoothMin(float a, float b, float k)
    {
        if (k <= 1e-5f) return Mathf.Min(a, b);
        float h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / k);
        return Mathf.Lerp(b, a, h) - k * h * (1f - h);
    }

    public static float Eval(SdfShape[] shapes, Vector3 p)
    {
        float d = 0f;
        for (int i = 0; i < shapes.Length; i++)
        {
            float di = shapes[i].Dist(p);
            d = i == 0 ? di : SmoothMin(d, di, shapes[i].k);
        }
        return d;
    }

    private static Vector3 Gradient(SdfShape[] shapes, Vector3 p, float e)
    {
        float dx = Eval(shapes, p + new Vector3(e, 0, 0)) - Eval(shapes, p - new Vector3(e, 0, 0));
        float dy = Eval(shapes, p + new Vector3(0, e, 0)) - Eval(shapes, p - new Vector3(0, e, 0));
        float dz = Eval(shapes, p + new Vector3(0, 0, e)) - Eval(shapes, p - new Vector3(0, 0, e));
        Vector3 g = new Vector3(dx, dy, dz);
        return g.sqrMagnitude > 1e-12f ? g.normalized : Vector3.up;
    }

    private static Color ColorAt(SdfShape[] shapes, SdfPaint[] paints, Vector3 p)
    {
        float r = 0f, g = 0f, b = 0f, fur = 0f, den = 0f;
        for (int i = 0; i < shapes.Length; i++)
        {
            float d = shapes[i].Dist(p);
            float w = Mathf.Exp(-Mathf.Max(d, 0f) * 45f) + 1e-6f;
            Color c = shapes[i].color;
            r += c.r * w; g += c.g * w; b += c.b * w; fur += c.a * w; den += w;
        }
        Color result = new Color(r / den, g / den, b / den, fur / den);

        for (int i = 0; i < paints.Length; i++)
        {
            float dd = (p - paints[i].center).magnitude;
            float t = 1f - Mathf.Clamp01((dd - (paints[i].radius - paints[i].soft)) / Mathf.Max(paints[i].soft, 1e-4f));
            t = t * t * (3f - 2f * t);
            result = Color.Lerp(result, paints[i].color, t);
        }
        return result;
    }

    public static Mesh Build(string name, SdfShape[] shapes, SdfPaint[] paints, Vector3 min, Vector3 max, float cell)
    {
        int nx = Mathf.CeilToInt((max.x - min.x) / cell) + 1;
        int ny = Mathf.CeilToInt((max.y - min.y) / cell) + 1;
        int nz = Mathf.CeilToInt((max.z - min.z) / cell) + 1;

        var s = new float[nx * ny * nz];
        for (int z = 0; z < nz; z++)
            for (int y = 0; y < ny; y++)
                for (int x = 0; x < nx; x++)
                    s[x + nx * (y + ny * z)] = Eval(shapes, min + new Vector3(x, y, z) * cell);

        var cellVert = new int[nx * ny * nz];
        for (int i = 0; i < cellVert.Length; i++) cellVert[i] = -1;

        var verts = new List<Vector3>();
        var v = new float[8];

        for (int z = 0; z < nz - 1; z++)
            for (int y = 0; y < ny - 1; y++)
                for (int x = 0; x < nx - 1; x++)
                {
                    int neg = 0;
                    for (int c = 0; c < 8; c++)
                    {
                        v[c] = s[(x + Corners[c, 0]) + nx * ((y + Corners[c, 1]) + ny * (z + Corners[c, 2]))];
                        if (v[c] < 0f) neg++;
                    }
                    if (neg == 0 || neg == 8) continue;

                    Vector3 sum = Vector3.zero;
                    int count = 0;
                    for (int e = 0; e < 12; e++)
                    {
                        int ca = Edges[e, 0], cb = Edges[e, 1];
                        if ((v[ca] < 0f) == (v[cb] < 0f)) continue;
                        float t = v[ca] / (v[ca] - v[cb]);
                        Vector3 pa = new Vector3(Corners[ca, 0], Corners[ca, 1], Corners[ca, 2]);
                        Vector3 pb = new Vector3(Corners[cb, 0], Corners[cb, 1], Corners[cb, 2]);
                        sum += pa + (pb - pa) * t;
                        count++;
                    }
                    Vector3 local = sum / count;
                    cellVert[x + nx * (y + ny * z)] = verts.Count;
                    verts.Add(min + (new Vector3(x, y, z) + local) * cell);
                }

        var tris = new List<int>();

        for (int z = 0; z < nz; z++)
            for (int y = 0; y < ny; y++)
                for (int x = 0; x < nx; x++)
                {
                    float s0 = s[x + nx * (y + ny * z)];

                    if (x < nx - 1 && y > 0 && z > 0 && y < ny - 1 && z < nz - 1)
                    {
                        float s1 = s[(x + 1) + nx * (y + ny * z)];
                        if ((s0 < 0f) != (s1 < 0f))
                            AddQuad(verts, tris,
                                cellVert[x + nx * ((y - 1) + ny * (z - 1))], cellVert[x + nx * (y + ny * (z - 1))],
                                cellVert[x + nx * (y + ny * z)], cellVert[x + nx * ((y - 1) + ny * z)],
                                s0 >= 0f);
                    }
                    if (y < ny - 1 && x > 0 && z > 0 && x < nx - 1 && z < nz - 1)
                    {
                        float s1 = s[x + nx * ((y + 1) + ny * z)];
                        if ((s0 < 0f) != (s1 < 0f))
                            AddQuad(verts, tris,
                                cellVert[(x - 1) + nx * (y + ny * (z - 1))], cellVert[x + nx * (y + ny * (z - 1))],
                                cellVert[x + nx * (y + ny * z)], cellVert[(x - 1) + nx * (y + ny * z)],
                                s0 < 0f);
                    }
                    if (z < nz - 1 && x > 0 && y > 0 && x < nx - 1 && y < ny - 1)
                    {
                        float s1 = s[x + nx * (y + ny * (z + 1))];
                        if ((s0 < 0f) != (s1 < 0f))
                            AddQuad(verts, tris,
                                cellVert[(x - 1) + nx * ((y - 1) + ny * z)], cellVert[x + nx * ((y - 1) + ny * z)],
                                cellVert[x + nx * (y + ny * z)], cellVert[(x - 1) + nx * (y + ny * z)],
                                s0 >= 0f);
                    }
                }

        int n = verts.Count;
        var normals = new List<Vector3>(n);
        var colors = new List<Color>(n);
        var uvs = new List<Vector2>(n);
        var tangents = new List<Vector4>(n);

        for (int i = 0; i < n; i++)
        {
            Vector3 p = verts[i];

            // 1 passo de Newton: encosta o vertice na superficie
            Vector3 g0 = Gradient(shapes, p, cell * 0.6f);
            p -= g0 * Eval(shapes, p);
            verts[i] = p;

            Vector3 nrm = Gradient(shapes, p, cell * 0.6f);
            normals.Add(nrm);

            // oclusao ambiente barata
            float occ = 0f;
            float[] hs = { 0.03f, 0.07f, 0.13f, 0.22f };
            foreach (float h in hs) occ += (h - Eval(shapes, p + nrm * h)) / h;
            float ao = Mathf.Clamp(1f - 0.18f * occ, 0.35f, 1f);

            Color c = ColorAt(shapes, paints, p);
            colors.Add(new Color(c.r * ao, c.g * ao, c.b * ao, c.a));

            uvs.Add(new Vector2(p.x, p.z));
            Vector3 t = Vector3.Cross(nrm, Mathf.Abs(nrm.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            tangents.Add(new Vector4(t.x, t.y, t.z, 1f));
        }

        var mesh = new Mesh { name = name };
        if (n > 65000) mesh.indexFormat = IndexFormat.UInt32;
        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetColors(colors);
        mesh.SetUVs(0, uvs);
        mesh.SetTangents(tangents);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    // A ordem dos 4 vertices ja tem uma orientacao fixa por eixo (testada); "flip" inverte quando preciso.
    private static void AddQuad(List<Vector3> verts, List<int> tris, int a, int b, int c, int d, bool flip)
    {
        if (a < 0 || b < 0 || c < 0 || d < 0) return;
        if (!flip)
        {
            tris.Add(a); tris.Add(b); tris.Add(c);
            tris.Add(a); tris.Add(c); tris.Add(d);
        }
        else
        {
            tris.Add(a); tris.Add(c); tris.Add(b);
            tris.Add(a); tris.Add(d); tris.Add(c);
        }
    }
}
