using UnityEngine;

/// <summary>Finds the nearest microwave in range and opens its recipe UI.</summary>
public class PlayerMicrowaveInteractor : MonoBehaviour
{
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private FirstPersonController movementController;
    [SerializeField] private MicrowaveCraftingPanelUI craftingUI;
    [SerializeField] private bool autoCreateUI = true;

    private void Awake()
    {
        if (inventory == null)
            inventory = GetComponent<PlayerInventory>();
        if (movementController == null)
            movementController = GetComponent<FirstPersonController>();
        if (craftingUI == null)
            craftingUI = FindFirstObjectByType<MicrowaveCraftingPanelUI>();

        if (craftingUI == null && autoCreateUI)
        {
            GameObject uiObject = new GameObject("Microwave Crafting UI");
            craftingUI = uiObject.AddComponent<MicrowaveCraftingPanelUI>();
        }
    }

    /// <summary>
    /// Returns true when a microwave claimed the E press, including while that
    /// nearby station is already cooking.
    /// </summary>
    public bool TryInteract()
    {
        if (craftingUI == null || craftingUI.IsOpen)
            return craftingUI != null && craftingUI.IsOpen;

        BurgerMicrowaveStation station = FindNearestStation();
        if (station == null)
            return false;

        if (station.IsCooking)
            return true;

        return craftingUI.Open(station, inventory, movementController);
    }

    public bool HasStationInRange() => FindNearestStation() != null;

    private BurgerMicrowaveStation FindNearestStation()
    {
        BurgerMicrowaveStation nearest = null;
        float nearestSqrDistance = float.PositiveInfinity;

        foreach (BurgerMicrowaveStation station in
                 FindObjectsByType<BurgerMicrowaveStation>(FindObjectsSortMode.None))
        {
            Vector3 closestPoint = station.GetClosestPoint(transform.position);
            if (float.IsNaN(closestPoint.x) || float.IsNaN(closestPoint.y)
                || float.IsNaN(closestPoint.z) || float.IsInfinity(closestPoint.x)
                || float.IsInfinity(closestPoint.y) || float.IsInfinity(closestPoint.z))
                continue;
            float sqrDistance = (closestPoint - transform.position).sqrMagnitude;
            if (sqrDistance <= station.InteractionDistance * station.InteractionDistance
                && sqrDistance < nearestSqrDistance)
            {
                nearest = station;
                nearestSqrDistance = sqrDistance;
            }
        }

        return nearest;
    }
}
