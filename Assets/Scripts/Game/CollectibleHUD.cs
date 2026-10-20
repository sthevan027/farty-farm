using UnityEngine;

/// <summary>
/// HUD estilo GTA V: radar circular com blips (moedas amarelas, fogo vermelho) e uma seta na
/// borda da tela apontando para a moeda mais proxima quando ela esta fora de vista.
/// </summary>
public class CollectibleHUD : MonoBehaviour
{
    [SerializeField] private float radarRadius = 95f;
    [SerializeField] private float radarRange = 32f;

    private Texture2D disc, arrow, square;
    private GUIStyle distStyle;
    private CharacterBase player;
    private float nextFind;

    private void Awake()
    {
        disc = MakeDisc(128);
        arrow = MakeArrow(64);
        square = new Texture2D(1, 1);
        square.SetPixel(0, 0, Color.white);
        square.Apply();
    }

    private CharacterBase FindPlayer()
    {
        if (player != null && player.IsPlayer) return player;
        if (Time.time < nextFind) return null;
        nextFind = Time.time + 0.5f;
        foreach (CharacterBase c in FindObjectsByType<CharacterBase>(FindObjectsSortMode.None))
            if (c.IsPlayer) { player = c; break; }
        return player;
    }

    private void OnGUI()
    {
        if (!GameManager.Playing) return;
        CharacterBase p = FindPlayer();
        Camera cam = Camera.main;
        if (p == null || cam == null) return;

        if (distStyle == null)
        {
            distStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            distStyle.normal.textColor = Color.white;
        }

        Collectible nearest = NearestCoin(p.transform.position);
        FlammableTree nearestFire = NearestFire(p.transform.position);
        DrawEdgeArrow(cam, nearest != null ? nearest.transform.position + Vector3.up * 1.5f : (Vector3?)null,
            new Color(0f, 0f, 0f, 0.85f), new Color(1f, 0.82f, 0.1f, 1f), p.transform.position);
        DrawEdgeArrow(cam, nearestFire != null ? nearestFire.transform.position + Vector3.up * 2.5f : (Vector3?)null,
            new Color(0f, 0f, 0f, 0.85f), new Color(1f, 0.3f, 0.1f, 1f), p.transform.position);
        DrawRadar(cam, p, nearest);
    }

    private static Collectible NearestCoin(Vector3 from)
    {
        Collectible best = null;
        float bestD = float.MaxValue;
        foreach (Collectible c in Collectible.All)
        {
            float d = (c.transform.position - from).sqrMagnitude;
            if (d < bestD) { bestD = d; best = c; }
        }
        return best;
    }

    private static FlammableTree NearestFire(Vector3 from)
    {
        FlammableTree best = null;
        float bestD = float.MaxValue;
        foreach (FlammableTree t in FlammableTree.All)
        {
            if (!t.IsBurning) continue;
            float d = (t.transform.position - from).sqrMagnitude;
            if (d < bestD) { bestD = d; best = t; }
        }
        return best;
    }

    // ---------- seta na borda da tela (moeda ou fogo, conforme as cores passadas) ----------

    private void DrawEdgeArrow(Camera cam, Vector3? targetWorld, Color outline, Color fill, Vector3 playerPos)
    {
        if (targetWorld == null) return;
        Vector3 world = targetWorld.Value;
        Vector3 sp = cam.WorldToScreenPoint(world);
        float w = Screen.width, h = Screen.height;
        const float margin = 70f;

        if (sp.z < 0f) { sp.x = w - sp.x; sp.y = h - sp.y; sp.z = 1f; sp.x = Mathf.Clamp(sp.x, -w, 2f * w); }

        bool onScreen = sp.z > 0f && sp.x > margin && sp.x < w - margin && sp.y > margin && sp.y < h - margin;
        if (onScreen) return;

        Vector2 center = new Vector2(w * 0.5f, h * 0.5f);
        Vector2 dir = new Vector2(sp.x, sp.y) - center;
        if (dir.sqrMagnitude < 1f) dir = Vector2.up;

        float sx = dir.x != 0f ? (center.x - margin) / Mathf.Abs(dir.x) : float.MaxValue;
        float sy = dir.y != 0f ? (center.y - margin) / Mathf.Abs(dir.y) : float.MaxValue;
        Vector2 pos = center + dir * Mathf.Min(sx, sy);

        float angle = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg; // 0 = para cima, horario
        Vector2 gui = new Vector2(pos.x, h - pos.y);

        float pulse = 1f + 0.12f * Mathf.Sin(Time.time * 6f);
        DrawArrow(gui, angle, 46f * pulse, outline);
        DrawArrow(gui, angle, 36f * pulse, fill);

        float meters = Vector3.Distance(playerPos, world);
        Vector2 labelPos = gui - new Vector2(dir.x, -dir.y).normalized * 44f;
        GUI.Label(new Rect(labelPos.x - 50, labelPos.y - 14, 100, 28), Mathf.RoundToInt(meters) + " m", distStyle);
    }

    private void DrawArrow(Vector2 center, float angle, float size, Color color)
    {
        Matrix4x4 old = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, center);
        GUI.color = color;
        GUI.DrawTexture(new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size), arrow);
        GUI.color = Color.white;
        GUI.matrix = old;
    }

    // ---------- radar ----------

    private void DrawRadar(Camera cam, CharacterBase p, Collectible nearest)
    {
        float r = radarRadius;
        Vector2 c = new Vector2(30f + r, Screen.height - 70f - r);

        GUI.color = new Color(0.05f, 0.08f, 0.1f, 0.65f);
        GUI.DrawTexture(new Rect(c.x - r, c.y - r, r * 2f, r * 2f), disc);
        GUI.color = new Color(1f, 1f, 1f, 0.9f);
        DrawRing(c, r);
        GUI.color = Color.white;

        float camYaw = cam.transform.eulerAngles.y;
        float scale = (r - 8f) / radarRange;

        foreach (FlammableTree t in FlammableTree.All)
            if (t.IsBurning) DrawBlip(c, r, scale, camYaw, p.transform.position, t.transform.position, new Color(1f, 0.25f, 0.15f), 8f, true);

        foreach (Collectible coin in Collectible.All)
        {
            bool isNearest = coin == nearest;
            float size = isNearest ? 11f + Mathf.Sin(Time.time * 6f) * 1.5f : 8f;
            DrawBlip(c, r, scale, camYaw, p.transform.position, coin.transform.position, new Color(1f, 0.85f, 0.1f), size, false);
        }

        // jogador no centro: triangulo branco apontando para onde ele olha
        DrawArrow(c, p.transform.eulerAngles.y - camYaw, 22f, Color.black);
        DrawArrow(c, p.transform.eulerAngles.y - camYaw, 16f, Color.white);

        GUI.Label(new Rect(c.x - 60, c.y + r + 2, 120, 24), "MOEDAS " + CollectibleManager.Score, distStyle);
    }

    private void DrawBlip(Vector2 c, float r, float scale, float camYaw, Vector3 from, Vector3 to, Color color, float size, bool diamond)
    {
        Vector3 rel = Quaternion.Euler(0f, -camYaw, 0f) * (to - from);
        Vector2 off = new Vector2(rel.x, rel.z) * scale;
        float max = r - 8f;
        bool clamped = off.magnitude > max;
        if (clamped) off = off.normalized * max;

        Vector2 pos = new Vector2(c.x + off.x, c.y - off.y);
        float s = clamped ? size * 0.75f : size;

        Matrix4x4 old = GUI.matrix;
        if (diamond) GUIUtility.RotateAroundPivot(45f, pos);
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(pos.x - s * 0.5f - 2f, pos.y - s * 0.5f - 2f, s + 4f, s + 4f), square);
        GUI.color = color;
        GUI.DrawTexture(new Rect(pos.x - s * 0.5f, pos.y - s * 0.5f, s, s), square);
        GUI.color = Color.white;
        GUI.matrix = old;
    }

    private void DrawRing(Vector2 c, float r)
    {
        const int dots = 48;
        for (int i = 0; i < dots; i++)
        {
            float a = i / (float)dots * Mathf.PI * 2f;
            GUI.DrawTexture(new Rect(c.x + Mathf.Cos(a) * (r - 1f) - 1.5f, c.y + Mathf.Sin(a) * (r - 1f) - 1.5f, 3f, 3f), square);
        }
    }

    // ---------- texturas geradas ----------

    private static Texture2D MakeDisc(int size)
    {
        var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half));
                float a = Mathf.Clamp01((half - d) / 1.5f);
                t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        t.Apply();
        t.filterMode = FilterMode.Bilinear;
        return t;
    }

    // Seta tipo "chevron" apontando para cima.
    private static Texture2D MakeArrow(int size)
    {
        var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float mid = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float fy = y / (float)(size - 1);            // 0 = baixo, 1 = topo
                float half = (1f - fy) * mid * 0.95f;         // largura diminui ate a ponta
                bool inside = Mathf.Abs(x - mid) <= half && fy > 0.04f;
                float notch = (0.38f - fy) * 0.7f * mid;      // entalhe na base
                if (fy < 0.38f && Mathf.Abs(x - mid) < notch) inside = false;
                t.SetPixel(x, y, new Color(1f, 1f, 1f, inside ? 1f : 0f));
            }
        t.Apply();
        t.filterMode = FilterMode.Bilinear;
        return t;
    }
}
