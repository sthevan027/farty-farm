using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static bool Playing { get; private set; }

    private enum State { Selecting, Playing }
    private State state = State.Selecting;

    private BoiController boi;
    private VacaController vaca;
    private ThirdPersonCamera topCamera;
    private CharacterBase player;
    private CharacterType chosen = CharacterType.Vaca;

    private GUIStyle labelStyle, centerStyle, titleStyle, buttonStyle;
    private Texture2D white;

    private void Start()
    {
        Playing = false;
        boi = FindFirstObjectByType<BoiController>();
        vaca = FindFirstObjectByType<VacaController>();
        topCamera = FindFirstObjectByType<ThirdPersonCamera>();

        white = new Texture2D(1, 1);
        white.SetPixel(0, 0, Color.white);
        white.Apply();

        if (boi != null) boi.enabled = false;
        if (vaca != null) vaca.enabled = false;
        Select(CharacterType.Vaca);
    }

    private void Select(CharacterType type)
    {
        chosen = type;
        player = type == CharacterType.Vaca ? (CharacterBase)vaca : boi;
        if (topCamera != null && player != null) topCamera.target = player.transform;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        if (state != State.Selecting || player == null) return;

        player.transform.Rotate(0f, 40f * Time.deltaTime, 0f);
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) Select(CharacterType.Vaca);
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) Select(CharacterType.Boi);
        if (Input.GetKeyDown(KeyCode.Return)) StartPlaying();
    }

    private void StartPlaying()
    {
        state = State.Playing;
        Playing = true;
        if (topCamera != null) topCamera.BeginChase();
        if (boi != null)
        {
            boi.SetPlayer(chosen == CharacterType.Boi);
            boi.enabled = true;
        }
        if (vaca != null)
        {
            vaca.SetPlayer(chosen == CharacterType.Vaca);
            vaca.enabled = true;
        }
    }

    private void EnsureStyles()
    {
        if (labelStyle != null) return;
        labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
        labelStyle.normal.textColor = Color.white;
        centerStyle = new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 18 };
        titleStyle = new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 48 };
        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 26, fontStyle = FontStyle.Bold };
    }

    private void OnGUI()
    {
        if (player == null) return;
        EnsureStyles();

        if (state == State.Selecting) DrawSelection();
        else DrawHud();
    }

    private void DrawSelection()
    {
        float w = Screen.width;
        float h = Screen.height;
        GUI.Label(new Rect(0, h * 0.06f, w, 70), "FARTY FARM", titleStyle);
        GUI.Label(new Rect(0, h * 0.06f + 70, w, 40), "Escolha seu personagem", centerStyle);

        bool isVaca = chosen == CharacterType.Vaca;
        GUI.color = isVaca ? new Color(1f, 0.6f, 0.8f) : Color.white;
        if (GUI.Button(new Rect(w * 0.5f - 270, h - 170, 250, 60), "VACA", buttonStyle)) Select(CharacterType.Vaca);
        GUI.color = !isVaca ? new Color(0.6f, 0.75f, 1f) : Color.white;
        if (GUI.Button(new Rect(w * 0.5f + 20, h - 170, 250, 60), "BOI", buttonStyle)) Select(CharacterType.Boi);
        GUI.color = new Color(0.6f, 1f, 0.5f);
        if (GUI.Button(new Rect(w * 0.5f - 125, h - 95, 250, 60), "JOGAR", buttonStyle)) StartPlaying();
        GUI.color = Color.white;

        string role = isVaca ? "Vaca: anda com WASD e solta leite (Espaco) para apagar o fogo."
                             : "Boi: WASD gira, SEGURE Espaco para peidar e se mover. Cuidado com o fogo!";
        GUI.Label(new Rect(0, h * 0.06f + 105, w, 30), role, centerStyle);
        GUI.Label(new Rect(0, h - 28, w, 28), "A / D = trocar | Enter = jogar", centerStyle);
    }

    private void DrawHud()
    {
        float pct = player.GasPercent;
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(20, 20, 260, 28), white);
        GUI.color = pct > 0.2f ? new Color(0.4f, 0.9f, 0.3f) : new Color(0.95f, 0.3f, 0.2f);
        GUI.DrawTexture(new Rect(22, 22, 256 * pct, 24), white);
        GUI.color = Color.white;

        string resource = chosen == CharacterType.Vaca ? "Leite" : "Gas";
        GUI.Label(new Rect(20, 52, 400, 30), resource + ": " + Mathf.RoundToInt(pct * 100f) + "%", labelStyle);
        GUI.Label(new Rect(20, 80, 400, 30), "Velocidade: " + player.Speed.ToString("F1"), labelStyle);

        GUI.Label(new Rect(20, 112, 600, 30), "Incendios ativos: " + FlammableTree.ActiveFires, labelStyle);
        GUI.Label(new Rect(20, 140, 600, 30), "Arvores queimadas: " + FlammableTree.BurntCount, labelStyle);
        GUI.Label(new Rect(20, 168, 600, 30), "Incendios apagados: " + FlammableTree.ExtinguishedCount, labelStyle);
        GUI.Label(new Rect(20, 196, 600, 30), "Moedas: " + CollectibleManager.Score + "  (restam " + Collectible.All.Count + ")", labelStyle);

        if (Time.time - player.NoGasMessageTime < 1f)
            GUI.Label(new Rect(0, Screen.height * 0.3f, Screen.width, 40),
                chosen == CharacterType.Vaca ? "SEM LEITE!" : "SEM GAS!", centerStyle);

        string hint = chosen == CharacterType.Vaca
            ? "WASD = andar | Espaco = leite (apaga fogo) | Esc = sair"
            : "WASD = direcao | SEGURE Espaco = peido (move e incendeia) | Esc = sair";
        GUI.Label(new Rect(0, Screen.height - 50, Screen.width, 40), hint, centerStyle);
    }
}
