using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Runtime fallback UI for the twelve clock burger slots.</summary>
public class ClockInteractionPanelUI : MonoBehaviour
{
    private sealed class SlotView
    {
        public Image Background;
        public Image Icon;
        public Text Label;
    }

    private static ClockInteractionPanelUI instance;
    private readonly List<SlotView> slots = new List<SlotView>();
    private readonly List<ClockController> pausedClocks = new List<ClockController>();
    private readonly List<bool> previousPauseStates = new List<bool>();

    private CanvasGroup panelGroup;
    private RectTransform slotContainer;
    private Text statusText;
    private BurgerClockPuzzle puzzle;
    private PlayerInteractionContext context;
    private Vector3 previousCameraPosition;
    private Quaternion previousCameraRotation;
    private bool previousPlayerCanMove;
    private bool previousCameraCanMove;
    private CursorLockMode previousCursorLockMode;
    private bool previousCursorVisible;
    private bool targetWasAnchorLatched;

    public bool IsOpen { get; private set; }

    public static void OpenOrCreate(BurgerClockPuzzle target,
        PlayerInteractionContext playerContext)
    {
        if (instance == null)
            instance = FindFirstObjectByType<ClockInteractionPanelUI>();
        if (instance == null)
            instance = new GameObject("Clock Interaction UI").AddComponent<ClockInteractionPanelUI>();
        instance.Open(target, playerContext);
    }

    private void Awake()
    {
        if (instance == null)
            instance = this;
        EnsureUI();
        SetVisible(false);
    }

    private void Update()
    {
        if (IsOpen && (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Escape)))
            Close();
    }

    private void Open(BurgerClockPuzzle target, PlayerInteractionContext playerContext)
    {
        if (IsOpen || target == null || playerContext == null)
            return;
        puzzle = target;
        context = playerContext;
        targetWasAnchorLatched = puzzle.ShouldRemainPaused;
        PauseAllClocks();
        LockPlayerAndMoveCamera();
        SetVisible(true);
        IsOpen = true;
        Refresh();
    }

    public void Close()
    {
        if (!IsOpen)
            return;
        RestoreClocks();
        RestorePlayerAndCamera();
        SetVisible(false);
        IsOpen = false;
        context?.InteractionController?.ClearCurrentTarget();
        puzzle = null;
        context = null;
    }

    public void PlaceBurger(int index)
    {
        if (puzzle != null && puzzle.TryPlaceBurgerAtHour(index, context))
            Refresh();
    }

    public void RemoveBurger(int index)
    {
        if (puzzle != null && puzzle.TryRemoveBurgerAtHour(index, context))
            Refresh();
    }

    private void Refresh()
    {
        for (int index = 0; index < slots.Count; index++)
        {
            bool occupied = puzzle.IsHourOccupied(index);
            SlotView slot = slots[index];
            slot.Icon.sprite = puzzle.BurgerItem != null ? puzzle.BurgerItem.Icon : null;
            slot.Icon.enabled = occupied && slot.Icon.sprite != null;
            string hour = index == 0 ? "12" : index.ToString();
            slot.Label.text = occupied ? hour + "\nBURGER" : hour;
            slot.Background.color = occupied
                ? new Color(0.55f, 0.25f, 0.08f, 0.98f)
                : new Color(0.12f, 0.12f, 0.14f, 0.96f);
        }
        statusText.text = puzzle.GetStatusMessage()
            + "\nLeft-click: place · Right-click: remove · X: close";
    }

    private void PauseAllClocks()
    {
        pausedClocks.Clear();
        previousPauseStates.Clear();
        foreach (ClockController candidate in
                 FindObjectsByType<ClockController>(FindObjectsSortMode.None))
        {
            pausedClocks.Add(candidate);
            previousPauseStates.Add(candidate.IsPaused);
            candidate.SetPaused(true);
        }
    }

    private void RestoreClocks()
    {
        for (int index = 0; index < pausedClocks.Count; index++)
        {
            ClockController candidate = pausedClocks[index];
            if (candidate == null)
                continue;
            bool isTargetClock = puzzle != null && candidate == puzzle.Clock;
            bool anchorWasRemoved = isTargetClock && targetWasAnchorLatched
                && !puzzle.ShouldRemainPaused;
            bool remainPaused = isTargetClock && puzzle.ShouldRemainPaused;
            candidate.SetPaused(remainPaused
                || (previousPauseStates[index] && !anchorWasRemoved));
        }
        pausedClocks.Clear();
        previousPauseStates.Clear();
    }

    private void LockPlayerAndMoveCamera()
    {
        if (context.Controller != null)
        {
            previousPlayerCanMove = context.Controller.playerCanMove;
            previousCameraCanMove = context.Controller.cameraCanMove;
            context.Controller.playerCanMove = false;
            context.Controller.cameraCanMove = false;
            Rigidbody body = context.Controller.GetComponent<Rigidbody>();
            if (body != null)
                body.linearVelocity = Vector3.zero;
        }

        if (context.Camera != null)
        {
            previousCameraPosition = context.Camera.transform.position;
            previousCameraRotation = context.Camera.transform.rotation;
            if (puzzle.InteractionCameraPose != null)
                context.Camera.transform.SetPositionAndRotation(
                    puzzle.InteractionCameraPose.position,
                    puzzle.InteractionCameraPose.rotation);
        }

        previousCursorLockMode = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void RestorePlayerAndCamera()
    {
        if (context.Camera != null)
            context.Camera.transform.SetPositionAndRotation(previousCameraPosition,
                previousCameraRotation);
        if (context.Controller != null)
        {
            context.Controller.playerCanMove = previousPlayerCanMove;
            context.Controller.cameraCanMove = previousCameraCanMove;
        }
        Cursor.lockState = previousCursorLockMode;
        Cursor.visible = previousCursorVisible;
    }

    private void EnsureUI()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 800;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject shade = new GameObject("Dim Background", typeof(RectTransform),
            typeof(Image), typeof(CanvasGroup));
        shade.transform.SetParent(canvasObject.transform, false);
        Stretch(shade.GetComponent<RectTransform>());
        shade.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.70f);
        panelGroup = shade.GetComponent<CanvasGroup>();

        GameObject panel = new GameObject("Clock Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(shade.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(1100f, 520f);
        panel.GetComponent<Image>().color = new Color(0.045f, 0.04f, 0.035f, 0.98f);

        Text title = CreateText("Title", panel.transform, 38);
        title.text = "CLOCK ANCHOR";
        SetRect(title.rectTransform, new Vector2(0.1f, 0.84f), new Vector2(0.9f, 0.98f));

        GameObject container = new GameObject("Twelve Slots", typeof(RectTransform),
            typeof(GridLayoutGroup));
        container.transform.SetParent(panel.transform, false);
        slotContainer = container.GetComponent<RectTransform>();
        SetRect(slotContainer, new Vector2(0.08f, 0.25f), new Vector2(0.92f, 0.82f));
        GridLayoutGroup grid = container.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(135f, 120f);
        grid.spacing = new Vector2(18f, 18f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 6;
        grid.childAlignment = TextAnchor.MiddleCenter;

        for (int index = 0; index < 12; index++)
        {
            int captured = index;
            GameObject slotObject = new GameObject("Hour Slot " + (index == 0 ? 12 : index),
                typeof(RectTransform), typeof(Image), typeof(ClockSlotPointerHandler));
            slotObject.transform.SetParent(slotContainer, false);
            ClockSlotPointerHandler pointerHandler = slotObject.GetComponent<ClockSlotPointerHandler>();
            pointerHandler.Configure(this, captured);

            GameObject iconObject = new GameObject("Burger Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(slotObject.transform, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            SetRect(iconRect, new Vector2(0.25f, 0.24f), new Vector2(0.75f, 0.90f));
            Image icon = iconObject.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            Text label = CreateText("Hour", slotObject.transform, 22);
            SetRect(label.rectTransform, new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.30f));
            slots.Add(new SlotView
            {
                Background = slotObject.GetComponent<Image>(),
                Icon = icon,
                Label = label
            });
        }

        statusText = CreateText("Status", panel.transform, 22);
        SetRect(statusText.rectTransform, new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.21f));
    }

    private void SetVisible(bool visible)
    {
        panelGroup.alpha = visible ? 1f : 0f;
        panelGroup.interactable = visible;
        panelGroup.blocksRaycasts = visible;
    }

    private static Text CreateText(string name, Transform parent, int fontSize)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
