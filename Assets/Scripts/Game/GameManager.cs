using UnityEngine;

public enum GameMode { Classico, Dupla, Multi }

/// <summary>
/// Fluxo do jogo: tela de selecao de MODO -> (Classico: selecao de personagem) -> Jogando.
///
/// Modos:
///  - Classico: 1 jogador local escolhe Vaca ou Boi; o outro bicho fica de IA (como sempre foi).
///  - Dupla: mesmo jogo do Classico, mas os DOIS bichos sao jogadores humanos ao mesmo tempo
///           (cooperativo, 1 placar so). Hoje e local/mesmo teclado (P1=WASD+Espaco na Vaca,
///           P2=Setas+Enter no Boi) como PLACEHOLDER ate existir multiplayer em rede de verdade
///           (2 PCs via LAN) -- isso e trabalho futuro, nao implementado aqui.
///  - Multi: dupla contra dupla. Clona a Vaca+Boi originais (Time A) e cria uma 2a dupla
///           (Time B) do outro lado da arena, com placar separado (CollectibleManager.ScoreA/B)
///           e vence quem chegar primeiro a MultiTargetScore. Tambem local (P1 controla
///           VacaA, P2 controla VacaB) ate o multiplayer em rede existir.
///
/// NAO VERIFICADO NO UNITY (ambiente sem Editor instalado ao gerar isto) -- validar os 3
/// modos no Play Mode antes de considerar pronto, principalmente a clonagem do Time B
/// (materiais/tinta, posicionamento, camera larga) no modo Multi.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static bool Playing { get; private set; }

    private enum State { ModeSelect, Selecting, Playing, MultiVictory }
    private State state = State.ModeSelect;
    private GameMode mode = GameMode.Classico;

    private const int MultiTargetScore = 25;
    private const float TeamSpawnOffsetX = 22f;

    // Time A (sempre existente na cena, construida pelo SceneSetup)
    private BoiController boi;
    private VacaController vaca;
    // Time B (so existe no modo Multi; clonado em runtime a partir do Time A)
    private BoiController boiB;
    private VacaController vacaB;

    private ThirdPersonCamera topCamera;
    private Transform arenaCenter;
    private CharacterBase player;
    private CharacterType chosen = CharacterType.Vaca;

    private GUIStyle labelStyle, centerStyle, titleStyle, buttonStyle, smallButtonStyle;
    private Texture2D white;

    private void Start()
    {
        Playing = false;
        boi = FindFirstObjectByType<BoiController>();
        vaca = FindFirstObjectByType<VacaController>();
        topCamera = FindFirstObjectByType<ThirdPersonCamera>();
        arenaCenter = new GameObject("ArenaCenterLookAt").transform;

        white = new Texture2D(1, 1);
        white.SetPixel(0, 0, Color.white);
        white.Apply();

        if (boi != null) boi.enabled = false;
        if (vaca != null) vaca.enabled = false;
        state = State.ModeSelect;
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

        if (state == State.Selecting && player != null)
        {
            player.transform.Rotate(0f, 40f * Time.deltaTime, 0f);
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) Select(CharacterType.Vaca);
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) Select(CharacterType.Boi);
            if (Input.GetKeyDown(KeyCode.Return)) StartPlaying();
        }

        if (state == State.Playing && mode == GameMode.Multi)
        {
            if (CollectibleManager.ScoreA >= MultiTargetScore || CollectibleManager.ScoreB >= MultiTargetScore)
                state = State.MultiVictory;
        }
    }

    // ---------- escolha de modo ----------

    private void ChooseMode(GameMode m)
    {
        mode = m;
        switch (m)
        {
            case GameMode.Classico:
                state = State.Selecting;
                Select(CharacterType.Vaca);
                break;
            case GameMode.Dupla:
                StartDupla();
                break;
            case GameMode.Multi:
                StartMulti();
                break;
        }
    }

    private void StartDupla()
    {
        // Cooperativo: os dois bichos sao jogadores ao mesmo tempo, 1 placar so.
        // P1 = Vaca (WASD + Espaco), P2 = Boi (Setas + Enter) -- mesmo PC (placeholder
        // local ate existir multiplayer em rede de verdade entre 2 PCs via LAN).
        if (vaca != null)
        {
            vaca.SetInputScheme(KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D, KeyCode.Space);
            vaca.SetPlayer(true);
            vaca.enabled = true;
        }
        if (boi != null)
        {
            boi.SetInputScheme(KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.Return);
            boi.SetPlayer(true);
            boi.enabled = true;
        }
        SetupWideCamera();
        Playing = true;
        state = State.Playing;
    }

    private void StartMulti()
    {
        // Time A = Vaca/Boi originais (P1 = Vaca, WASD+Espaco). Time B = clones
        // posicionados do outro lado da arena (P2 = VacaB, Setas+Enter), tingidos
        // de outra cor pra dar pra distinguir os times na mesma tela.
        if (vaca != null)
        {
            vaca.SetTeam(0);
            vaca.SetInputScheme(KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D, KeyCode.Space);
            vaca.SetPlayer(true);
            vaca.enabled = true;
        }
        if (boi != null)
        {
            boi.SetTeam(0);
            boi.SetPlayer(false); // IA ajuda o time A
            boi.enabled = true;
        }

        vacaB = CloneCharacter(vaca, "VacaB_TimeB", new Vector3(TeamSpawnOffsetX, 0f, 0f), TeamTint(1));
        boiB = CloneCharacter(boi, "BoiB_TimeB", new Vector3(TeamSpawnOffsetX, 0f, 6f), TeamTint(1));
        TintCharacter(vaca, TeamTint(0));
        TintCharacter(boi, TeamTint(0));

        if (vacaB != null)
        {
            vacaB.SetTeam(1);
            vacaB.SetInputScheme(KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.Return);
            vacaB.SetPlayer(true);
            vacaB.enabled = true;
        }
        if (boiB != null)
        {
            boiB.SetTeam(1);
            boiB.SetPlayer(false); // IA ajuda o time B
            boiB.enabled = true;
        }

        SetupWideCamera();
        Playing = true;
        state = State.Playing;
    }

    private void SetupWideCamera()
    {
        arenaCenter.position = Vector3.zero;
        if (topCamera != null) topCamera.SetWideOverview(arenaCenter, 30f);
    }

    private static Color TeamTint(int team) => team == 0
        ? new Color(1f, 0.75f, 0.8f)   // Time A: rosado (tom da vaca original)
        : new Color(0.55f, 0.75f, 1f); // Time B: azulado

    // Clona um personagem ja existente na cena (malha/material/animators inclusos),
    // move pra posicao do outro time e devolve o componente correspondente.
    private static T CloneCharacter<T>(T source, string name, Vector3 offset, Color tint) where T : CharacterBase
    {
        if (source == null) return null;
        var clone = Object.Instantiate(source.gameObject, source.transform.position + offset, source.transform.rotation);
        clone.name = name;
        var comp = clone.GetComponent<T>();
        TintCharacter(comp, tint);
        return comp;
    }

    // Tinge os MeshRenderers do personagem com uma cor (material de instancia, nao
    // altera o material compartilhado original -- se nao, o outro time mudaria junto).
    private static void TintCharacter(CharacterBase character, Color tint)
    {
        if (character == null) return;
        foreach (var mr in character.GetComponentsInChildren<MeshRenderer>())
        {
            if (mr.sharedMaterial == null) continue;
            var mat = new Material(mr.sharedMaterial);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", tint);
            mr.material = mat;
        }
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
        smallButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 16, wordWrap = true };
    }

    private void OnGUI()
    {
        EnsureStyles();

        if (state == State.ModeSelect) { DrawModeSelect(); return; }
        if (player == null && mode == GameMode.Classico) return;

        if (state == State.Selecting) DrawSelection();
        else if (state == State.MultiVictory) DrawMultiVictory();
        else DrawHud();
    }

    private void DrawModeSelect()
    {
        float w = Screen.width;
        float h = Screen.height;
        GUI.Label(new Rect(0, h * 0.08f, w, 70), "FARTY FARM", titleStyle);
        GUI.Label(new Rect(0, h * 0.08f + 70, w, 40), "Escolha o modo de jogo", centerStyle);

        float btnW = 260, btnH = 150, gap = 30;
        float totalW = btnW * 3 + gap * 2;
        float x0 = w * 0.5f - totalW * 0.5f;
        float y = h * 0.5f - btnH * 0.5f;

        if (GUI.Button(new Rect(x0, y, btnW, btnH),
            "CLASSICO\n\n1 jogador\nescolhe Vaca ou Boi", smallButtonStyle))
            ChooseMode(GameMode.Classico);

        if (GUI.Button(new Rect(x0 + btnW + gap, y, btnW, btnH),
            "DUPLA\n\nClassico jogado por\n2 pessoas (coop)\nP1=WASD/Espaco P2=Setas/Enter", smallButtonStyle))
            ChooseMode(GameMode.Dupla);

        if (GUI.Button(new Rect(x0 + (btnW + gap) * 2, y, btnW, btnH),
            "MULTI\n\nDupla x Dupla (placar\npor time ate " + MultiTargetScore + ")\nP1=WASD/Espaco P2=Setas/Enter", smallButtonStyle))
            ChooseMode(GameMode.Multi);

        GUI.Label(new Rect(0, y + btnH + 20, w, 60),
            "Dupla e Multi hoje rodam no mesmo PC/teclado como teste local --\n" +
            "multiplayer em rede (PCs diferentes via LAN) fica para uma proxima etapa.", centerStyle);
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
        if (mode == GameMode.Multi)
        {
            DrawMultiHud();
            return;
        }

        if (player == null) return;
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

        string hint = mode == GameMode.Dupla
            ? "P1 (Vaca): WASD + Espaco | P2 (Boi): Setas + Enter | Esc = sair"
            : chosen == CharacterType.Vaca
                ? "WASD = andar | Espaco = leite (apaga fogo) | Esc = sair"
                : "WASD = direcao | SEGURE Espaco = peido (move e incendeia) | Esc = sair";
        GUI.Label(new Rect(0, Screen.height - 50, Screen.width, 40), hint, centerStyle);
    }

    private void DrawMultiHud()
    {
        float w = Screen.width;
        GUI.Label(new Rect(0, 20, w, 40),
            "TIME A (rosa, P1): " + CollectibleManager.ScoreA + "   x   TIME B (azul, P2): " + CollectibleManager.ScoreB,
            centerStyle);
        GUI.Label(new Rect(0, Screen.height - 50, w, 40),
            "Meta: " + MultiTargetScore + " moedas | P1=WASD/Espaco | P2=Setas/Enter | Esc = sair", centerStyle);
    }

    private void DrawMultiVictory()
    {
        bool teamAWon = CollectibleManager.ScoreA >= CollectibleManager.ScoreB;
        GUI.Label(new Rect(0, Screen.height * 0.4f, Screen.width, 80),
            (teamAWon ? "TIME A" : "TIME B") + " VENCEU!", titleStyle);
        GUI.Label(new Rect(0, Screen.height * 0.4f + 70, Screen.width, 40),
            CollectibleManager.ScoreA + " x " + CollectibleManager.ScoreB, centerStyle);
    }
}
