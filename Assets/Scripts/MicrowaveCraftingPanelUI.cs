using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime burger recipe panel. Its recipe slot count comes from the station,
/// so a later recipe can add/remove ingredients without rewriting this UI.
/// </summary>
public class MicrowaveCraftingPanelUI : MonoBehaviour
{
    private sealed class SlotView
    {
        public Button Button;
        public Image Background;
        public Image Icon;
        public Text Label;
    }

    [Header("Optional Custom UI")]
    [Tooltip("Leave empty to build a complete four-slot fallback panel at runtime.")]
    [SerializeField] private CanvasGroup panelGroup;
    [SerializeField] private RectTransform slotContainer;
    [SerializeField] private Button startButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Text statusText;

    [Header("Appearance")]
    [SerializeField] private Color emptySlotColor = new Color(0.16f, 0.16f, 0.18f, 0.96f);
    [SerializeField] private Color filledSlotColor = new Color(0.25f, 0.55f, 0.25f, 0.98f);

    private readonly List<SlotView> slots = new List<SlotView>();
    private BurgerMicrowaveStation station;
    private PlayerInventory inventory;
    private FirstPersonController movementController;
    private bool[] deposited;
    private bool previousPlayerCanMove;
    private bool previousCameraCanMove;
    private CursorLockMode previousCursorLockMode;
    private bool previousCursorVisible;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        EnsureUI();
        SetVisible(false);
    }

    private void Update()
    {
        if (!IsOpen)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
            CloseAndRefund();
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            StartCooking();
    }

    public bool Open(BurgerMicrowaveStation targetStation, PlayerInventory playerInventory,
        FirstPersonController controller)
    {
        if (IsOpen || targetStation == null || playerInventory == null || targetStation.IsCooking)
            return false;

        station = targetStation;
        inventory = playerInventory;
        movementController = controller;
        deposited = new bool[station.RequiredIngredients.Length];
        RebuildSlots();
        LockPlayer();
        SetVisible(true);
        IsOpen = true;
        Refresh();
        return true;
    }

    public void CloseAndRefund()
    {
        if (!IsOpen)
            return;

        for (int index = 0; index < deposited.Length; index++)
        {
            if (deposited[index] && station.RequiredIngredients[index] != null)
                inventory.TryAdd(station.RequiredIngredients[index]);
        }

        FinishClosing();
    }

    private void ToggleIngredient(int index)
    {
        if (!IsOpen || index < 0 || index >= deposited.Length)
            return;

        ItemDefinition ingredient = station.RequiredIngredients[index];
        if (ingredient == null)
            return;

        if (!deposited[index])
        {
            if (inventory.TryRemove(ingredient))
                deposited[index] = true;
        }
        else if (inventory.TryAdd(ingredient))
        {
            deposited[index] = false;
        }

        Refresh();
    }

    private void StartCooking()
    {
        if (!IsOpen || !AllIngredientsDeposited())
            return;

        // Items were removed as the player filled the slots. Closing without a
        // refund commits those ingredients to this cooking cycle.
        if (station.TryStartCooking())
            FinishClosing();
    }

    private bool AllIngredientsDeposited()
    {
        if (deposited == null || deposited.Length == 0)
            return false;

        foreach (bool isDeposited in deposited)
        {
            if (!isDeposited)
                return false;
        }
        return true;
    }

    private void Refresh()
    {
        bool allFilled = AllIngredientsDeposited();
        for (int index = 0; index < slots.Count; index++)
        {
            ItemDefinition ingredient = station.RequiredIngredients[index];
            bool isFilled = deposited[index];
            SlotView view = slots[index];
            view.Background.color = isFilled ? filledSlotColor : emptySlotColor;
            view.Icon.sprite = ingredient != null ? ingredient.Icon : null;
            view.Icon.enabled = ingredient != null && ingredient.Icon != null;
            string itemName = ingredient != null ? ingredient.DisplayName : "Missing recipe item";
            view.Label.text = isFilled ? itemName + "\nREADY" : itemName + "\nClick to add";
        }

        startButton.interactable = allFilled;
        statusText.text = allFilled
            ? $"Recipe ready — cooking takes {station.CookingDuration:0.#} seconds"
            : "Place one correct ingredient in each slot";
    }

    private void RebuildSlots()
    {
        foreach (SlotView slot in slots)
            Destroy(slot.Button.gameObject);
        slots.Clear();

        for (int index = 0; index < station.RequiredIngredients.Length; index++)
        {
            int capturedIndex = index;
            GameObject slotObject = new GameObject($"Ingredient Slot {index + 1}",
                typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            slotObject.transform.SetParent(slotContainer, false);
            LayoutElement layout = slotObject.GetComponent<LayoutElement>();
            layout.preferredWidth = 190f;
            layout.preferredHeight = 210f;

            Image background = slotObject.GetComponent<Image>();
            Button button = slotObject.GetComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(() => ToggleIngredient(capturedIndex));

            GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(slotObject.transform, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.2f, 0.36f);
            iconRect.anchorMax = new Vector2(0.8f, 0.9f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            Image icon = iconObject.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            Text label = CreateText("Label", slotObject.transform, 22, TextAnchor.MiddleCenter);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0.05f, 0.03f);
            labelRect.anchorMax = new Vector2(0.95f, 0.38f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            slots.Add(new SlotView
            {
                Button = button,
                Background = background,
                Icon = icon,
                Label = label
            });
        }
    }

    private void LockPlayer()
    {
        if (movementController != null)
        {
            previousPlayerCanMove = movementController.playerCanMove;
            previousCameraCanMove = movementController.cameraCanMove;
            movementController.playerCanMove = false;
            movementController.cameraCanMove = false;
            Rigidbody body = movementController.GetComponent<Rigidbody>();
            if (body != null)
                body.linearVelocity = Vector3.zero;
        }

        previousCursorLockMode = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void UnlockPlayer()
    {
        if (movementController != null)
        {
            movementController.playerCanMove = previousPlayerCanMove;
            movementController.cameraCanMove = previousCameraCanMove;
        }
        Cursor.lockState = previousCursorLockMode;
        Cursor.visible = previousCursorVisible;
    }

    private void FinishClosing()
    {
        IsOpen = false;
        SetVisible(false);
        UnlockPlayer();
        station = null;
        inventory = null;
        movementController = null;
        deposited = null;
    }

    private void SetVisible(bool visible)
    {
        if (panelGroup == null)
            return;
        panelGroup.alpha = visible ? 1f : 0f;
        panelGroup.interactable = visible;
        panelGroup.blocksRaycasts = visible;
    }

    private void EnsureUI()
    {
        if (panelGroup != null && slotContainer != null && startButton != null
            && closeButton != null && statusText != null)
        {
            startButton.onClick.AddListener(StartCooking);
            closeButton.onClick.AddListener(CloseAndRefund);
            return;
        }

        GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 750;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject shade = new GameObject("Dim Background", typeof(RectTransform), typeof(Image),
            typeof(CanvasGroup));
        shade.transform.SetParent(canvasObject.transform, false);
        Stretch(shade.GetComponent<RectTransform>());
        shade.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

        panelGroup = shade.GetComponent<CanvasGroup>();

        GameObject panel = new GameObject("Burger Recipe Panel", typeof(RectTransform),
            typeof(Image));
        panel.transform.SetParent(shade.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(1040f, 520f);
        panel.GetComponent<Image>().color = new Color(0.055f, 0.055f, 0.065f, 0.98f);

        Text title = CreateText("Title", panel.transform, 38, TextAnchor.MiddleCenter);
        SetRect(title.rectTransform, new Vector2(0.08f, 0.82f), new Vector2(0.92f, 0.98f));
        title.text = "BURGER ASSEMBLY";

        GameObject slotsObject = new GameObject("Ingredient Slots", typeof(RectTransform),
            typeof(HorizontalLayoutGroup));
        slotsObject.transform.SetParent(panel.transform, false);
        slotContainer = slotsObject.GetComponent<RectTransform>();
        SetRect(slotContainer, new Vector2(0.08f, 0.31f), new Vector2(0.92f, 0.80f));
        HorizontalLayoutGroup layout = slotsObject.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 20f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        statusText = CreateText("Status", panel.transform, 23, TextAnchor.MiddleCenter);
        SetRect(statusText.rectTransform, new Vector2(0.1f, 0.18f), new Vector2(0.9f, 0.29f));

        startButton = CreateButton("Enter Button", panel.transform, "ENTER", new Color(0.72f, 0.24f, 0.12f));
        SetRect(startButton.GetComponent<RectTransform>(), new Vector2(0.36f, 0.035f), new Vector2(0.64f, 0.17f));
        startButton.onClick.AddListener(StartCooking);

        closeButton = CreateButton("Close Button", panel.transform, "X", new Color(0.25f, 0.25f, 0.28f));
        SetRect(closeButton.GetComponent<RectTransform>(), new Vector2(0.92f, 0.88f), new Vector2(0.975f, 0.965f));
        closeButton.onClick.AddListener(CloseAndRefund);
    }

    private static Button CreateButton(string name, Transform parent, string label, Color color)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.GetComponent<Image>();
        image.color = color;
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        Text text = CreateText("Text", buttonObject.transform, 25, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform);
        text.text = label;
        return button;
    }

    private static Text CreateText(string name, Transform parent, int size, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.alignment = alignment;
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

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void OnDisable()
    {
        if (IsOpen)
            CloseAndRefund();
    }
}
