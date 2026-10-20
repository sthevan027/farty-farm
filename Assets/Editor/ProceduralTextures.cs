using UnityEditor;
using UnityEngine;

/// <summary>
/// Gera todas as texturas do jogo por codigo (sem precisar de arquivos externos).
/// Todo o ruido e "tileavel": nao aparece emenda nas esferas nem no chao repetido.
/// </summary>
public static class ProceduralTextures
{
    public class Set
    {
        public Texture2D cow, bull, barkAlbedo, barkNormal, leaves, leavesNormal, leavesBurnt, grass, grassDetail, wood;
    }

    private const string Folder = "Assets/Textures";
    private const int S = 512;
    private const int Steps = 9;

    private static int step;

    // ------------------------------------------------------------------
    // Entrada
    // ------------------------------------------------------------------

    public static Set GenerateAll()
    {
        System.IO.Directory.CreateDirectory(Folder);
        AssetDatabase.DeleteAsset(Folder + "/CowSpots.png"); // texturas antigas
        AssetDatabase.DeleteAsset(Folder + "/CowFur.png");
        AssetDatabase.DeleteAsset(Folder + "/BullFur.png");

        var set = new Set();
        step = 0;

        Progress("pelagem da vaca");
        set.cow = Make("CowPattern", CowPixel);

        Progress("pelagem do boi");
        set.bull = Make("BullPattern", BullPixel);

        Progress("casca das arvores");
        MakeBark(out set.barkAlbedo, out set.barkNormal);

        Progress("folhas");
        MakeLeaves(out set.leaves, out set.leavesNormal);

        Progress("folhas queimadas");
        set.leavesBurnt = Make("LeavesBurnt", BurntLeavesPixel);

        Progress("grama");
        set.grass = Make("Grass", GrassPixel);

        Progress("detalhe do chao");
        set.grassDetail = Make("GroundDetail", GroundDetailPixel);

        Progress("madeira");
        set.wood = Make("Wood", WoodPixel);

        EditorUtility.ClearProgressBar();
        return set;
    }

    private static void Progress(string label)
    {
        EditorUtility.DisplayProgressBar("Farty Farm", "Gerando textura: " + label, step / (float)Steps);
        step++;
    }

    // ------------------------------------------------------------------
    // Texturas
    // ------------------------------------------------------------------

    // Padrao MULTIPLICATIVO: o shader multiplica pela cor base do animal (creme / marrom).
    private static Color CowPixel(float u, float v)
    {
        Color black = new Color(0.10f, 0.09f, 0.10f);
        Color brown = new Color(0.34f, 0.20f, 0.14f);

        float shade = 1f - Fbm(u, v, 5, 5, 3, 5) * 0.12f;
        float spot = Sstep(0.56f, 0.58f, Fbm(u, v, 3, 3, 4, 11));
        Color spotColor = Color.Lerp(black, brown, Sstep(0.45f, 0.75f, Fbm(u, v, 9, 9, 2, 3)) * 0.55f);
        Color c = Color.Lerp(new Color(shade, shade, shade), spotColor, spot);

        float grain = Fbm(u, v, 80, 80, 2, 77) - 0.5f;
        return Mul(c, 1f + grain * 0.22f);
    }

    private static Color BullPixel(float u, float v)
    {
        float low = Fbm(u, v, 4, 4, 4, 41);
        float l = 0.75f + 0.35f * Sstep(0.3f, 0.7f, low);
        float patch = Sstep(0.56f, 0.62f, Fbm(u, v, 3, 3, 3, 88));
        l *= 1f - patch * 0.25f;

        float hair = Fbm(u, v, 150, 30, 2, 53) - 0.5f;
        float hair2 = Fbm(u, v, 220, 60, 1, 19) - 0.5f;
        l *= 1f + hair * 0.5f + hair2 * 0.2f;
        return new Color(l, l, l, 1f);
    }

    private static Color LeavesPixel(float u, float v)
    {
        Color deep = new Color(0.06f, 0.26f, 0.09f);
        Color mid = new Color(0.20f, 0.55f, 0.16f);
        Color light = new Color(0.50f, 0.82f, 0.26f);
        Color yellow = new Color(0.62f, 0.78f, 0.22f);

        // deforma as coordenadas para os tufos ficarem organicos (nao parecerem escamas)
        float wu = u + (Fbm(u, v, 8, 8, 2, 101) - 0.5f) * 0.14f;
        float wv = v + (Fbm(u, v, 8, 8, 2, 202) - 0.5f) * 0.14f;

        float f1, f2, id;
        Worley(wu, wv, 10, 3, out f1, out f2, out id);

        float shade = Mathf.Clamp01(1f - f1 * 1.15f);
        Color c = Color.Lerp(deep, mid, shade);
        c = Color.Lerp(c, light, Sstep(0.5f, 1f, shade) * (0.3f + id * 0.7f));
        c = Color.Lerp(c, yellow, id * 0.22f);

        // costuras suaves entre os tufos de folha
        c = Mul(c, 0.72f + 0.28f * Sstep(0f, 0.3f, f2 - f1));

        // folhinhas menores por cima
        float g1, g2, gid;
        Worley(wu, wv, 26, 71, out g1, out g2, out gid);
        c = Mul(c, 0.8f + 0.3f * Mathf.Clamp01(1f - g1 * 1.3f));

        // manchas grandes de claro/escuro (esconde a repeticao)
        c = Mul(c, 0.8f + 0.4f * Fbm(u, v, 3, 3, 3, 55));

        float fine = Fbm(u, v, 40, 40, 2, 9) - 0.5f;
        return Mul(c, 1f + fine * 0.3f);
    }

    private static Color BurntLeavesPixel(float u, float v)
    {
        Color deep = new Color(0.03f, 0.03f, 0.035f);
        Color mid = new Color(0.11f, 0.10f, 0.10f);
        Color ash = new Color(0.28f, 0.26f, 0.25f);

        float wu = u + (Fbm(u, v, 8, 8, 2, 101) - 0.5f) * 0.14f;
        float wv = v + (Fbm(u, v, 8, 8, 2, 202) - 0.5f) * 0.14f;

        float f1, f2, id;
        Worley(wu, wv, 10, 3, out f1, out f2, out id);

        float shade = Mathf.Clamp01(1f - f1 * 1.15f);
        Color c = Color.Lerp(deep, mid, shade);
        c = Color.Lerp(c, ash, Sstep(0.6f, 1f, shade) * id * 0.7f);
        c = Mul(c, Mathf.Lerp(0.5f, 1f, Sstep(0f, 0.22f, f2 - f1)));

        float fine = Fbm(u, v, 40, 40, 2, 9) - 0.5f;
        return Mul(c, 1f + fine * 0.4f);
    }

    private static Color GrassPixel(float u, float v)
    {
        Color a = new Color(0.20f, 0.52f, 0.16f);
        Color b = new Color(0.42f, 0.78f, 0.28f);
        Color clover = new Color(0.15f, 0.44f, 0.14f);

        Color c = Color.Lerp(a, b, Sstep(0.3f, 0.7f, Fbm(u, v, 4, 4, 3, 5)));
        c = Color.Lerp(c, clover, Sstep(0.58f, 0.66f, Fbm(u, v, 6, 6, 3, 13)) * 0.7f);

        float blades = Fbm(u, v, 128, 128, 2, 9) - 0.5f;
        float blades2 = Fbm(u, v, 40, 160, 1, 21) - 0.5f;
        return Mul(c, 1f + blades * 0.35f + blades2 * 0.25f);
    }

    // Variacao em grande escala por cima da grama (quebra a repeticao). Neutra = 0.5 cinza.
    private static Color GroundDetailPixel(float u, float v)
    {
        float big = Fbm(u, v, 3, 3, 4, 201);
        float dirt = Sstep(0.62f, 0.72f, Fbm(u, v, 5, 5, 3, 305));
        float l = 0.5f + (big - 0.5f) * 0.9f;
        l = Mathf.Lerp(l, 0.58f, dirt * 0.5f);
        return new Color(l, l * 0.97f, l * 0.9f, 1f);
    }

    private static Color WoodPixel(float u, float v)
    {
        // tabuas horizontais com veios
        const int planks = 4;
        float pv = v * planks;
        int plank = Mathf.FloorToInt(pv);
        float inPlank = pv - plank;
        float tone = Hash(plank, 7, 3);

        float grain = Fbm(u + tone, v, 2, 24, 3, 91 + plank);
        float rings = 0.5f + 0.5f * Mathf.Sin((grain * 14f + tone * 6f));
        Color a = new Color(0.55f, 0.36f, 0.20f);
        Color b = new Color(0.40f, 0.25f, 0.13f);
        Color c = Color.Lerp(a, b, rings * 0.7f + grain * 0.3f);
        c = Mul(c, 0.85f + tone * 0.3f);

        float gap = Sstep(0.0f, 0.04f, inPlank) * Sstep(0f, 0.04f, 1f - inPlank);
        return Mul(c, Mathf.Lerp(0.35f, 1f, gap));
    }

    private static void MakeLeaves(out Texture2D albedo, out Texture2D normal)
    {
        albedo = Make("Leaves", LeavesPixel);

        // mapa de normal a partir do brilho dos tufos
        var height = new float[S * S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = (x + 0.5f) / S;
                float v = (y + 0.5f) / S;
                float wu = u + (Fbm(u, v, 8, 8, 2, 101) - 0.5f) * 0.14f;
                float wv = v + (Fbm(u, v, 8, 8, 2, 202) - 0.5f) * 0.14f;
                float f1, f2, id;
                Worley(wu, wv, 10, 3, out f1, out f2, out id);
                height[y * S + x] = Mathf.Clamp01(1f - f1 * 1.15f) * 0.8f + Sstep(0f, 0.3f, f2 - f1) * 0.2f;
            }

        var px = new Color32[S * S];
        const float strength = 3f;
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float l = height[y * S + Wrap(x - 1, S)];
                float r = height[y * S + Wrap(x + 1, S)];
                float d = height[Wrap(y - 1, S) * S + x];
                float t = height[Wrap(y + 1, S) * S + x];
                float nx = (l - r) * strength;
                float ny = (d - t) * strength;
                float len = Mathf.Sqrt(nx * nx + ny * ny + 1f);
                px[y * S + x] = new Color(nx / len * 0.5f + 0.5f, ny / len * 0.5f + 0.5f, 1f / len * 0.5f + 0.5f, 1f);
            }
        normal = Save("LeavesNormal", px, true);
    }

    // Casca: gera albedo e mapa de normal a partir do mesmo mapa de altura.
    private static void MakeBark(out Texture2D albedo, out Texture2D normal)
    {
        var height = new float[S * S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = (x + 0.5f) / S;
                float v = (y + 0.5f) / S;
                float ridge = 1f - Mathf.Clamp01(Mathf.Abs(Fbm(u, v, 10, 3, 4, 7) - 0.5f) * 3.5f);
                float ridge2 = 1f - Mathf.Clamp01(Mathf.Abs(Fbm(u, v, 24, 6, 3, 23) - 0.5f) * 4f);
                float fine = Fbm(u, v, 48, 12, 2, 19);
                height[y * S + x] = ridge * 0.5f + ridge2 * 0.3f + fine * 0.2f;
            }

        Color dark = new Color(0.20f, 0.13f, 0.08f);
        Color light = new Color(0.50f, 0.35f, 0.22f);
        Color moss = new Color(0.30f, 0.33f, 0.18f);

        var albedoPx = new Color32[S * S];
        var normalPx = new Color32[S * S];
        const float strength = 4f;

        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = (x + 0.5f) / S;
                float v = (y + 0.5f) / S;
                float h = height[y * S + x];

                Color c = Color.Lerp(dark, light, Sstep(0.25f, 0.9f, h));
                float mossAmount = Sstep(0.58f, 0.75f, Fbm(u, v, 5, 5, 3, 31)) * 0.35f;
                c = Color.Lerp(c, moss, mossAmount);
                c.a = 1f;
                albedoPx[y * S + x] = c;

                float l = height[y * S + Wrap(x - 1, S)];
                float r = height[y * S + Wrap(x + 1, S)];
                float d = height[Wrap(y - 1, S) * S + x];
                float t = height[Wrap(y + 1, S) * S + x];
                float nx = (l - r) * strength;
                float ny = (d - t) * strength;
                float len = Mathf.Sqrt(nx * nx + ny * ny + 1f);
                normalPx[y * S + x] = new Color(nx / len * 0.5f + 0.5f, ny / len * 0.5f + 0.5f, 1f / len * 0.5f + 0.5f, 1f);
            }

        albedo = Save("BarkAlbedo", albedoPx, false);
        normal = Save("BarkNormal", normalPx, true);
    }

    // ------------------------------------------------------------------
    // Ajudantes de arquivo
    // ------------------------------------------------------------------

    private static Texture2D Make(string name, System.Func<float, float, Color> pixel)
    {
        var px = new Color32[S * S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                Color c = pixel((x + 0.5f) / S, (y + 0.5f) / S);
                c.a = 1f;
                px[y * S + x] = c;
            }
        return Save(name, px, false);
    }

    private static Texture2D Save(string name, Color32[] pixels, bool isNormalMap)
    {
        string path = Folder + "/" + name + ".png";

        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.SetPixels32(pixels);
        tex.Apply();
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = isNormalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = true;
            importer.anisoLevel = 4;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // ------------------------------------------------------------------
    // Matematica / ruido tileavel
    // ------------------------------------------------------------------

    private static Color Mul(Color c, float k)
    {
        return new Color(c.r * k, c.g * k, c.b * k, 1f);
    }

    private static float Sstep(float a, float b, float x)
    {
        float t = Mathf.Clamp01((x - a) / (b - a));
        return t * t * (3f - 2f * t);
    }

    private static int Wrap(int v, int period)
    {
        int r = v % period;
        return r < 0 ? r + period : r;
    }

    private static float Hash(int x, int y, int seed)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFFFFu) / (float)0x1000000;
        }
    }

    // Ruido de valor periodico: px x py celulas por textura.
    private static float ValueNoise(float x, float y, int px, int py, int seed)
    {
        int x0 = Mathf.FloorToInt(x);
        int y0 = Mathf.FloorToInt(y);
        float fx = x - x0;
        float fy = y - y0;
        float sx = fx * fx * (3f - 2f * fx);
        float sy = fy * fy * (3f - 2f * fy);

        int xa = Wrap(x0, px), xb = Wrap(x0 + 1, px);
        int ya = Wrap(y0, py), yb = Wrap(y0 + 1, py);

        float a = Hash(xa, ya, seed);
        float b = Hash(xb, ya, seed);
        float c = Hash(xa, yb, seed);
        float d = Hash(xb, yb, seed);
        return Mathf.Lerp(Mathf.Lerp(a, b, sx), Mathf.Lerp(c, d, sx), sy);
    }

    // Soma de oitavas. u e v em [0,1).
    private static float Fbm(float u, float v, int baseX, int baseY, int octaves, int seed)
    {
        float sum = 0f, amp = 0.5f, norm = 0f;
        int fx = baseX, fy = baseY;
        for (int i = 0; i < octaves; i++)
        {
            sum += amp * ValueNoise(u * fx, v * fy, fx, fy, seed + i * 17);
            norm += amp;
            amp *= 0.5f;
            fx *= 2;
            fy *= 2;
        }
        return sum / norm;
    }

    // Ruido celular periodico: f1/f2 = distancias ao 1o e 2o ponto mais proximos.
    private static void Worley(float u, float v, int cells, int seed, out float f1, out float f2, out float id)
    {
        float x = u * cells;
        float y = v * cells;
        int xi = Mathf.FloorToInt(x);
        int yi = Mathf.FloorToInt(y);

        f1 = 9f;
        f2 = 9f;
        id = 0f;

        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                int cx = xi + dx;
                int cy = yi + dy;
                int wx = Wrap(cx, cells);
                int wy = Wrap(cy, cells);

                float px = cx + Hash(wx, wy, seed);
                float py = cy + Hash(wx, wy, seed + 101);
                float ddx = px - x;
                float ddy = py - y;
                float d = Mathf.Sqrt(ddx * ddx + ddy * ddy);

                if (d < f1)
                {
                    f2 = f1;
                    f1 = d;
                    id = Hash(wx, wy, seed + 202);
                }
                else if (d < f2)
                {
                    f2 = d;
                }
            }
    }
}
