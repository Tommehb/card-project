using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GamePauseMenu : MonoBehaviour
{
    private const int PauseSortingOrder = 1000;

    private GameHandler gameHandler;
    private GameObject panelRoot;
    private Button resumeButton;
    private Button restartButton;
    private TMP_Text restartButtonText;
    private bool isOpen;
    private float previousTimeScale = 1f;

    public static bool IsPauseOpen { get; private set; }

    public static void ResetGlobalState()
    {
        IsPauseOpen = false;
    }

    private void Awake()
    {
        gameHandler = GetComponent<GameHandler>();
    }

    private void Start()
    {
        BuildMenu();
        panelRoot.SetActive(false);
        isOpen = false;
        IsPauseOpen = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }

        RefreshRestartState();
    }

    public void TogglePause()
    {
        if (isOpen)
        {
            Resume();
        }
        else
        {
            SetOpen(true);
        }
    }

    public void Resume()
    {
        SetOpen(false);
    }

    public void Restart()
    {
        SetOpen(false);
        if (gameHandler != null)
        {
            gameHandler.RestartGame();
        }
    }

    public void QuitToTitle()
    {
        SetOpen(false);
        if (gameHandler != null)
        {
            gameHandler.ExitToTitle();
        }
    }

    private void SetOpen(bool open)
    {
        if (panelRoot == null)
        {
            return;
        }

        if (isOpen == open)
        {
            panelRoot.SetActive(open);
            IsPauseOpen = open;
            return;
        }

        isOpen = open;
        IsPauseOpen = open;
        panelRoot.SetActive(open);

        if (open)
        {
            previousTimeScale = Time.timeScale;
            if (!IsNetworkSessionActive())
            {
                Time.timeScale = 0f;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            EventSystem.current?.SetSelectedGameObject(resumeButton != null ? resumeButton.gameObject : null);
            RefreshRestartState();
        }
        else
        {
            if (!IsNetworkSessionActive())
            {
                Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }
    }

    private void RefreshRestartState()
    {
        if (restartButton == null)
        {
            return;
        }

        bool canRestart = !IsNetworkSessionActive() || (LanSessionManager.Instance != null && LanSessionManager.Instance.IsServer);
        restartButton.interactable = canRestart;

        if (restartButtonText != null)
        {
            restartButtonText.text = canRestart ? "Restart" : "Restart (Host Only)";
        }
    }

    private bool IsNetworkSessionActive()
    {
        return NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
    }

    private void BuildMenu()
    {
        if (panelRoot != null)
        {
            return;
        }

        Canvas canvas = CreateCanvas();

        panelRoot = new GameObject("Pause Panel", typeof(RectTransform), typeof(Image));
        panelRoot.transform.SetParent(canvas.transform, false);

        var panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var backdrop = panelRoot.GetComponent<Image>();
        backdrop.color = new Color(0f, 0f, 0f, 0.72f);

        GameObject menuBox = new GameObject("Pause Menu", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        menuBox.transform.SetParent(panelRoot.transform, false);

        var boxRect = menuBox.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.5f, 0.5f);
        boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.pivot = new Vector2(0.5f, 0.5f);
        boxRect.anchoredPosition = Vector2.zero;
        boxRect.sizeDelta = new Vector2(360f, 300f);

        var boxImage = menuBox.GetComponent<Image>();
        boxImage.color = new Color(0.04f, 0.045f, 0.05f, 0.94f);

        var layout = menuBox.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(34, 34, 28, 34);
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        CreateLabel(menuBox.transform, "Paused");
        resumeButton = CreateButton(menuBox.transform, "Resume", Resume);
        restartButton = CreateButton(menuBox.transform, "Restart", Restart);
        restartButtonText = restartButton.GetComponentInChildren<TMP_Text>();
        CreateButton(menuBox.transform, "Quit to Title", QuitToTitle);
    }

    private Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("Pause Menu Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = PauseSortingOrder;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private static void CreateLabel(Transform parent, string text)
    {
        GameObject labelObject = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        labelObject.transform.SetParent(parent, false);

        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(0f, 54f);

        var layout = labelObject.GetComponent<LayoutElement>();
        layout.preferredHeight = 54f;

        var label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 38f;
        label.color = Color.white;
    }

    private static Button CreateButton(Transform parent, string text, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(text + " Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);

        var rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 48f);

        var layout = buttonObject.GetComponent<LayoutElement>();
        layout.preferredHeight = 48f;

        var image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.82f, 0.82f, 0.78f, 1f);

        var button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(action);

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);

        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 24f;
        label.color = new Color(0.08f, 0.08f, 0.075f, 1f);

        return button;
    }

    private void OnDestroy()
    {
        if (isOpen && !IsNetworkSessionActive())
        {
            Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
        }

        if (isOpen)
        {
            IsPauseOpen = false;
        }
    }
}
