using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private BoiController player;

    private enum State { Selecting, Playing }
    private State state = State.Selecting;

    private GUIStyle labelStyle, centerStyle, titleStyle, buttonStyle;
    private Texture2D white;

    private void Start()
    {
        if (player == null) player = FindFirstObjectByType<BoiController>();
        white = new Texture2D(1, 1);
        white.SetPixel(0, 0, Color.white);
        white.Apply();
        if (player != null) player.enabled = false;
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
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) player.SetCharacter(CharacterType.Vaca);
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) player.SetCharacter(CharacterType.Boi);
            if (Input.GetKeyDown(KeyCode.Return)) StartPlaying();
        }
    }

    private void StartPlaying()
    {
        state = State.Playing;
        player.enabled = true;
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

        bool vaca = player.Current == CharacterType.Vaca;
        GUI.color = vaca ? new Color(1f, 0.6f, 0.8f) : Color.white;
        if (GUI.Button(new Rect(w * 0.5f - 270, h - 170, 250, 60), "VACA", buttonStyle)) player.SetCharacter(CharacterType.Vaca);
        GUI.color = !vaca ? new Color(0.6f, 0.75f, 1f) : Color.white;
        if (GUI.Button(new Rect(w * 0.5f + 20, h - 170, 250, 60), "BOI", buttonStyle)) player.SetCharacter(CharacterType.Boi);
        GUI.color = new Color(0.6f, 1f, 0.5f);
        if (GUI.Button(new Rect(w * 0.5f - 125, h - 95, 250, 60), "JOGAR", buttonStyle)) StartPlaying();
        GUI.color = Color.white;

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

        GUI.Label(new Rect(20, 52, 400, 30), "Gas: " + Mathf.RoundToInt(pct * 100f) + "%", labelStyle);
        GUI.Label(new Rect(20, 80, 400, 30), "Velocidade: " + player.Speed.ToString("F1"), labelStyle);

        if (Time.time - player.NoGasMessageTime < 1f)
            GUI.Label(new Rect(0, Screen.height * 0.3f, Screen.width, 40), "SEM GAS!", centerStyle);

        GUI.Label(new Rect(0, Screen.height - 50, Screen.width, 40),
            "WASD = direcao | Espaco = peido (move) | Esc = sair", centerStyle);
    }
}
