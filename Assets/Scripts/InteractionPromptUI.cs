using UnityEngine;
using UnityEngine.UI;

/// <summary>Bottom-center flashing interaction prompt plus short status messages.</summary>
public class InteractionPromptUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;
    [SerializeField] private Text promptText;
    [Min(0f)] [SerializeField] private float flashSpeed = 3.5f;

    private float transientUntil;
    private bool contextualVisible;

    private void Awake()
    {
        EnsureUI();
        Hide();
    }

    private void Update()
    {
        bool transient = Time.unscaledTime < transientUntil;
        bool visible = transient || contextualVisible;
        if (group == null)
            return;
        group.alpha = visible
            ? 0.62f + Mathf.Sin(Time.unscaledTime * flashSpeed) * 0.38f
            : 0f;
    }

    public void ShowContext(string message)
    {
        EnsureUI();
        contextualVisible = !string.IsNullOrWhiteSpace(message);
        if (contextualVisible && Time.unscaledTime >= transientUntil)
            promptText.text = message;
    }

    public void ShowTransient(string message, float duration)
    {
        EnsureUI();
        promptText.text = message;
        transientUntil = Time.unscaledTime + Mathf.Max(0.1f, duration);
    }

    public void Hide()
    {
        contextualVisible = false;
        if (group != null && Time.unscaledTime >= transientUntil)
            group.alpha = 0f;
    }

    private void EnsureUI()
    {
        if (group != null && promptText != null)
            return;

        GameObject canvasObject = new GameObject("Interaction Prompt Canvas", typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 700;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject textObject = new GameObject("Prompt Text", typeof(RectTransform),
            typeof(CanvasGroup), typeof(Text));
        textObject.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.2f, 0.10f);
        rect.anchorMax = new Vector2(0.8f, 0.19f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        group = textObject.GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        promptText = textObject.GetComponent<Text>();
        promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        promptText.fontSize = 28;
        promptText.fontStyle = FontStyle.Bold;
        promptText.alignment = TextAnchor.MiddleCenter;
        promptText.color = Color.white;
        promptText.raycastTarget = false;
    }
}
