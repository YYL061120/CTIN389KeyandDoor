using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Six-slot, stack-by-item-type inventory. It also owns hotbar selection and
/// the visual model attached to the player's hand.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    [Serializable]
    public class InventorySlot
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField] private int quantity;

        public ItemDefinition Item => item;
        public int Quantity => quantity;
        public bool IsEmpty => item == null || quantity <= 0;

        internal void Set(ItemDefinition newItem, int newQuantity)
        {
            item = newItem;
            quantity = newQuantity;
        }

        internal void Add(int amount) => quantity += amount;

        internal int Remove(int amount)
        {
            int removed = Mathf.Min(amount, quantity);
            quantity -= removed;
            if (quantity <= 0)
            {
                item = null;
                quantity = 0;
            }
            return removed;
        }
    }

    [Header("Capacity")]
    [Range(1, 10)] [SerializeField] private int maximumUniqueItemTypes = 6;
    [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();

    [Header("Selection")]
    [SerializeField] private int selectedSlotIndex;
    [SerializeField] private bool invertMouseWheel;

    [Header("Held Item")]
    [Tooltip("Create a child Transform under PlayerCamera and assign it here.")]
    [SerializeField] private Transform heldItemAnchor;

    private GameObject heldItemInstance;
    private ItemDefinition displayedHeldItem;
    private bool missingAnchorWarningShown;

    public IReadOnlyList<InventorySlot> Slots => slots;
    public int Capacity => maximumUniqueItemTypes;
    public int SelectedSlotIndex => selectedSlotIndex;
    public InventorySlot SelectedSlot => slots.Count > 0 ? slots[selectedSlotIndex] : null;
    public ItemDefinition SelectedItem => SelectedSlot != null && !SelectedSlot.IsEmpty ? SelectedSlot.Item : null;

    public event Action InventoryChanged;
    public event Action<int> SelectionChanged;

    private void Awake()
    {
        EnsureSlotCount();
        selectedSlotIndex = Mathf.Clamp(selectedSlotIndex, 0, slots.Count - 1);
        RefreshHeldItem();
    }

    private void Start()
    {
        InventoryHotbarUI.EnsureRuntimeHotbar(this);
    }

    private void Update()
    {
        float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.01f)
            return;

        int direction = scroll > 0f ? -1 : 1;
        if (invertMouseWheel)
            direction *= -1;
        SelectSlot(WrapIndex(selectedSlotIndex + direction));
    }

    public bool TryAdd(ItemDefinition item, int quantity = 1)
    {
        if (item == null || quantity <= 0)
            return false;

        InventorySlot existingSlot = FindSlot(item);
        if (existingSlot != null)
        {
            existingSlot.Add(quantity);
            NotifyInventoryChanged();
            return true;
        }

        InventorySlot emptySlot = slots.Find(slot => slot.IsEmpty);
        if (emptySlot == null)
            return false;

        emptySlot.Set(item, quantity);
        NotifyInventoryChanged();
        return true;
    }

    public bool Contains(ItemDefinition item, int quantity = 1)
    {
        InventorySlot slot = FindSlot(item);
        return slot != null && slot.Quantity >= quantity;
    }

    public bool TryRemove(ItemDefinition item, int quantity = 1)
    {
        if (item == null || quantity <= 0)
            return false;

        InventorySlot slot = FindSlot(item);
        if (slot == null || slot.Quantity < quantity)
            return false;

        slot.Remove(quantity);
        NotifyInventoryChanged();
        return true;
    }

    public bool TryRemoveSelected(int quantity = 1)
    {
        return SelectedItem != null && TryRemove(SelectedItem, quantity);
    }

    public void SelectSlot(int index)
    {
        if (slots.Count == 0)
            return;

        index = Mathf.Clamp(index, 0, slots.Count - 1);
        if (selectedSlotIndex == index)
            return;

        selectedSlotIndex = index;
        RefreshHeldItem();
        SelectionChanged?.Invoke(selectedSlotIndex);
    }

    public void SetHeldItemAnchor(Transform anchor)
    {
        heldItemAnchor = anchor;
        missingAnchorWarningShown = false;
        RefreshHeldItem();
    }

    private InventorySlot FindSlot(ItemDefinition item)
    {
        return slots.Find(slot => !slot.IsEmpty && slot.Item == item);
    }

    private void NotifyInventoryChanged()
    {
        RefreshHeldItem();
        InventoryChanged?.Invoke();
    }

    private void RefreshHeldItem()
    {
        ItemDefinition selectedItem = SelectedItem;
        if (selectedItem == displayedHeldItem && (selectedItem == null || heldItemInstance != null))
            return;

        if (heldItemInstance != null)
        {
            heldItemInstance.SetActive(false);
            Destroy(heldItemInstance);
        }
        displayedHeldItem = selectedItem;

        if (selectedItem == null || selectedItem.HeldPrefab == null)
            return;

        if (heldItemAnchor == null)
        {
            if (!missingAnchorWarningShown)
            {
                Debug.LogWarning("PlayerInventory needs a Held Item Anchor before it can display selected items.", this);
                missingAnchorWarningShown = true;
            }
            return;
        }

        heldItemInstance = Instantiate(selectedItem.HeldPrefab, heldItemAnchor);
        heldItemInstance.name = $"Held {selectedItem.DisplayName}";
        Transform heldTransform = heldItemInstance.transform;
        heldTransform.localPosition = selectedItem.HeldLocalPosition;
        heldTransform.localRotation = Quaternion.Euler(selectedItem.HeldLocalEulerAngles);
        heldTransform.localScale = selectedItem.HeldLocalScale;

        foreach (Collider itemCollider in heldItemInstance.GetComponentsInChildren<Collider>(true))
            itemCollider.enabled = false;
        foreach (Rigidbody body in heldItemInstance.GetComponentsInChildren<Rigidbody>(true))
        {
            body.isKinematic = true;
            body.useGravity = false;
        }
        foreach (WorldItem worldItem in heldItemInstance.GetComponentsInChildren<WorldItem>(true))
            worldItem.enabled = false;
    }

    private int WrapIndex(int index)
    {
        if (index < 0)
            return slots.Count - 1;
        if (index >= slots.Count)
            return 0;
        return index;
    }

    private void EnsureSlotCount()
    {
        maximumUniqueItemTypes = Mathf.Max(1, maximumUniqueItemTypes);
        while (slots.Count < maximumUniqueItemTypes)
            slots.Add(new InventorySlot());
        while (slots.Count > maximumUniqueItemTypes)
            slots.RemoveAt(slots.Count - 1);
    }

    private void OnValidate()
    {
        EnsureSlotCount();
        if (slots.Count > 0)
            selectedSlotIndex = Mathf.Clamp(selectedSlotIndex, 0, slots.Count - 1);
    }
}
