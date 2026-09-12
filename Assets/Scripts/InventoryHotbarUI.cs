using UnityEngine;
using System;

/// <summary>Connects a manually-created hotbar to PlayerInventory.</summary>
public class InventoryHotbarUI : MonoBehaviour
{
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private InventorySlotUI[] slotViews = new InventorySlotUI[6];

    private void Awake()
    {
        if (inventory == null)
            inventory = FindFirstObjectByType<PlayerInventory>();
        ResolveSlotViews();
    }

    private void OnEnable()
    {
        if (inventory == null)
            return;
        inventory.InventoryChanged += Refresh;
        inventory.SelectionChanged += HandleSelectionChanged;
        Refresh();
    }

    private void OnDisable()
    {
        if (inventory == null)
            return;
        inventory.InventoryChanged -= Refresh;
        inventory.SelectionChanged -= HandleSelectionChanged;
    }

    public void Refresh()
    {
        if (inventory == null)
            return;

        for (int index = 0; index < slotViews.Length; index++)
        {
            if (slotViews[index] == null)
                continue;
            PlayerInventory.InventorySlot slot = index < inventory.Slots.Count
                ? inventory.Slots[index]
                : null;
            slotViews[index].Display(slot, index == inventory.SelectedSlotIndex);
        }
    }

    private void HandleSelectionChanged(int selectedIndex) => Refresh();

    public void Bind(PlayerInventory playerInventory)
    {
        if (inventory == playerInventory)
        {
            ResolveSlotViews();
            Refresh();
            return;
        }

        if (isActiveAndEnabled && inventory != null)
        {
            inventory.InventoryChanged -= Refresh;
            inventory.SelectionChanged -= HandleSelectionChanged;
        }

        inventory = playerInventory;
        ResolveSlotViews();

        if (isActiveAndEnabled && inventory != null)
        {
            inventory.InventoryChanged += Refresh;
            inventory.SelectionChanged += HandleSelectionChanged;
        }
        Refresh();
    }

    public static void EnsureRuntimeHotbar(PlayerInventory playerInventory)
    {
        InventoryHotbarUI hotbar = FindFirstObjectByType<InventoryHotbarUI>(FindObjectsInactive.Include);
        if (hotbar == null)
        {
            GameObject hotbarObject = GameObject.Find("Hotbar");
            if (hotbarObject != null)
                hotbar = hotbarObject.AddComponent<InventoryHotbarUI>();
        }

        if (hotbar != null)
            hotbar.Bind(playerInventory);
        else
            Debug.LogWarning("No Hotbar GameObject was found for PlayerInventory.", playerInventory);
    }

    private void ResolveSlotViews()
    {
        int expectedSlotCount = inventory != null ? inventory.Capacity : 6;
        bool needsResolution = slotViews == null || slotViews.Length != expectedSlotCount
            || Array.Exists(slotViews, slot => slot == null);
        if (!needsResolution)
            return;

        slotViews = GetComponentsInChildren<InventorySlotUI>(true);
        Array.Sort(slotViews, (left, right) =>
            left.transform.GetSiblingIndex().CompareTo(right.transform.GetSiblingIndex()));
    }
}
