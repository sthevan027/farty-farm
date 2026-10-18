using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    [SerializeField] private BoiController boiController;
    [SerializeField] private Canvas canvas;

    private TextMeshProUGUI gasText;
    private TextMeshProUGUI speedText;
    private TextMeshProUGUI statusText;

    private void Start()
    {
        if (boiController == null)
            boiController = FindObjectOfType<BoiController>();

        CreateUI();
    }

    private void CreateUI()
    {
        if (canvas == null)
        {
            var canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<GraphicRaycaster>();

            var canvasScaler = canvasGO.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        }

        // Gas text
        var gasGO = new GameObject("GasText");
        gasGO.transform.SetParent(canvas.transform);
        gasText = gasGO.AddComponent<TextMeshProUGUI>();
        var gasRect = gasGO.GetComponent<RectTransform>();
        gasRect.anchorMin = new Vector2(0, 1);
        gasRect.anchorMax = new Vector2(0, 1);
        gasRect.offsetMin = Vector2.zero;
        gasRect.offsetMax = new Vector2(200, -20);
        gasText.text = "Gás: 100%";
        gasText.fontSize = 36;
        gasText.color = Color.white;

        // Speed text
        var speedGO = new GameObject("SpeedText");
        speedGO.transform.SetParent(canvas.transform);
        speedText = speedGO.AddComponent<TextMeshProUGUI>();
        var speedRect = speedGO.GetComponent<RectTransform>();
        speedRect.anchorMin = new Vector2(0, 0.5f);
        speedRect.anchorMax = new Vector2(0, 0.5f);
        speedRect.offsetMin = Vector2.zero;
        speedRect.offsetMax = new Vector2(200, 50);
        speedText.text = "Velocidade: 0";
        speedText.fontSize = 36;
        speedText.color = Color.white;

        // Status text
        var statusGO = new GameObject("StatusText");
        statusGO.transform.SetParent(canvas.transform);
        statusText = statusGO.AddComponent<TextMeshProUGUI>();
        var statusRect = statusGO.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.5f, 0);
        statusRect.anchorMax = new Vector2(0.5f, 0);
        statusRect.offsetMin = Vector2.zero;
        statusRect.offsetMax = new Vector2(400, 100);
        statusText.text = "WASD = Mover | Espaço = Peido\n(Pressione ESC para fechar)";
        statusText.fontSize = 24;
        statusText.alignment = TextAlignmentOptions.BottomCenter;
        statusText.color = Color.white;
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

        if (boiController != null && gasText != null)
        {
            gasText.text = $"Gás: {boiController.GetGasPercent() * 100f:F0}%";
            speedText.text = $"Vel: {boiController.GetCurrentSpeed():F1}";
        }
    }
}
