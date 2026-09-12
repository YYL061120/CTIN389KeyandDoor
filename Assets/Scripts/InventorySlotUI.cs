using UnityEngine;
using UnityEngine.UI;

/// <summary>View for one manually-created hotbar slot.</summary>
public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private Text quantityText;
    [SerializeField] private RectTransform scaleTarget;
    [Min(1f)] [SerializeField] private float selectedScale = 1.2f;
    [Min(0f)] [SerializeField] private float scaleAnimationSpeed = 12f;

    private Vector3 targetScale = Vector3.one;

    private void Awake()
    {
        if (scaleTarget == null)
            scaleTarget = transform as RectTransform;
    }

    private void Update()
    {
        if (scaleTarget != null)
            scaleTarget.localScale = Vector3.Lerp(scaleTarget.localScale, targetScale,
                Time.unscaledDeltaTime * scaleAnimationSpeed);
    }

    public void Display(PlayerInventory.InventorySlot slot, bool selected)
    {
        bool hasItem = slot != null && !slot.IsEmpty;
        if (itemIcon != null)
        {
            itemIcon.enabled = hasItem;
            itemIcon.sprite = hasItem ? slot.Item.Icon : null;
            itemIcon.preserveAspect = true;
        }

        if (quantityText != null)
            quantityText.text = hasItem ? slot.Quantity.ToString() : string.Empty;

        targetScale = Vector3.one * (selected ? selectedScale : 1f);
    }
}
