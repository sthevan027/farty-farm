using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private BoiController boi;

    private GUIStyle labelStyle;
    private GUIStyle centerStyle;
    private Texture2D white;

    private void Start()
    {
        if (boi == null) boi = FindObjectOfType<BoiController>();
        white = new Texture2D(1, 1);
        white.SetPixel(0, 0, Color.white);
        white.Apply();
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
    }

    private void OnGUI()
    {
        if (boi == null) return;

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            labelStyle.normal.textColor = Color.white;
            centerStyle = new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 18 };
        }

        float pct = boi.GasPercent;
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(20, 20, 260, 28), white);
        GUI.color = pct > 0.2f ? new Color(0.4f, 0.9f, 0.3f) : new Color(0.95f, 0.3f, 0.2f);
        GUI.DrawTexture(new Rect(22, 22, 256 * pct, 24), white);
        GUI.color = Color.white;

        GUI.Label(new Rect(20, 52, 400, 30), "Gas: " + Mathf.RoundToInt(pct * 100f) + "%", labelStyle);
        GUI.Label(new Rect(20, 80, 400, 30), "Velocidade: " + boi.Speed.ToString("F1"), labelStyle);

        if (Time.time - boi.NoGasMessageTime < 1f)
            GUI.Label(new Rect(0, Screen.height * 0.3f, Screen.width, 40), "SEM GAS!", centerStyle);

        GUI.Label(new Rect(0, Screen.height - 50, Screen.width, 40),
            "WASD = mover | Espaco = peido | Esc = sair", centerStyle);
    }
}
