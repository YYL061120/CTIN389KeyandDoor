using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Routes left/right pointer clicks from one clock-hour slot.</summary>
public class ClockSlotPointerHandler : MonoBehaviour, IPointerClickHandler
{
    private ClockInteractionPanelUI owner;
    private int slotIndex;

    public void Configure(ClockInteractionPanelUI panel, int index)
    {
        owner = panel;
        slotIndex = index;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (owner == null)
            return;

        if (eventData.button == PointerEventData.InputButton.Left)
            owner.PlaceBurger(slotIndex);
        else if (eventData.button == PointerEventData.InputButton.Right)
            owner.RemoveBurger(slotIndex);
    }
}
