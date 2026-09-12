using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>Interactive 12-hour burger anchor puzzle used by both clocks.</summary>
public class BurgerClockPuzzle : MonoBehaviour, IPlayerInteractable
{
    public enum ClockPurpose { Production, MonsterFeeding }

    [Header("Role")]
    [SerializeField] private ClockPurpose purpose;
    [SerializeField] private ClockController clock;
    [SerializeField] private BurgerMicrowaveStation microwaveStation;
    [SerializeField] private GameManager gameManager;

    [Header("Interaction")]
    [Min(0.1f)] [SerializeField] private float interactionDistance = 3f;
    [Tooltip("Optional fixed point used for distance checks. If empty, the clock object's transform is used.")]
    [SerializeField] private Transform interactionPoint;
    [Tooltip("Optional collider whose bounds center is used when no interaction point is assigned. It is never auto-selected.")]
    [SerializeField] private Collider interactionCollider;
    [Tooltip("Camera position and rotation while the 12-slot panel is open.")]
    [SerializeField] private Transform interactionCameraPose;

    [Header("Burger Anchors (0 = 12 o'clock, 1 = 1 o'clock, etc.)")]
    [SerializeField] private ItemDefinition burgerItem;
    [SerializeField] private GameObject placedBurgerPrefab;
    [SerializeField] private Transform[] burgerSlotAnchors = new Transform[12];

    [Header("Removable Hour Hand")]
    [SerializeField] private bool requiresHourHand;
    [SerializeField] private bool hourHandInstalled = true;
    [SerializeField] private ItemDefinition hourHandItem;
    [SerializeField] private GameObject hourHandVisual;
    [SerializeField] private bool stopClockUntilHandInstalled = true;

    [Header("Final Feeding Puzzle")]
    [Range(0, 11)] [SerializeField] private int victoryDialHour = 6;
    [Min(0f)] [SerializeField] private float victoryDelay = 5f;
    [SerializeField] private UnityEvent burgerAnchorTriggered;
    [SerializeField] private UnityEvent hourHandWasInstalled;

    private readonly bool[] occupiedHours = new bool[12];
    private readonly GameObject[] placedBurgerObjects = new GameObject[12];
    private bool anchorLatched;
    private int latchedHour = -1;
    private bool victorySequenceStarted;

    public string InteractionPrompt => "Press E to interact";
    public float InteractionDistance => interactionDistance;
    public Transform InteractionCameraPose => interactionCameraPose;
    public ClockController Clock => clock;
    public ItemDefinition BurgerItem => burgerItem;
    public bool ShouldRemainPaused => anchorLatched;
    public bool HourHandInstalled => !requiresHourHand || hourHandInstalled;

    private void Awake()
    {
        if (clock == null)
            clock = GetComponent<ClockController>();
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();
        if (purpose == ClockPurpose.Production && microwaveStation == null)
            microwaveStation = FindFirstObjectByType<BurgerMicrowaveStation>();

        if (requiresHourHand)
        {
            SetHourHandVisual(hourHandInstalled);
            if (stopClockUntilHandInstalled && clock != null)
                clock.SetTimeRunning(hourHandInstalled);
        }
    }

    private void OnEnable()
    {
        if (clock != null)
            clock.HourAdvanced += HandleHourAdvanced;
    }

    private void OnDisable()
    {
        if (clock != null)
            clock.HourAdvanced -= HandleHourAdvanced;
    }

    public bool CanInteract(PlayerInteractionContext context) => !victorySequenceStarted;

    public Vector3 GetInteractionPoint(Vector3 playerPosition)
    {
        if (interactionPoint != null)
            return interactionPoint.position;
        if (interactionCollider != null)
            return interactionCollider.bounds.center;
        return transform.position;
    }

    public void Interact(PlayerInteractionContext context)
    {
        if (context == null || context.Player == null)
            return;

        Vector3 playerPosition = context.Player.transform.position;
        Vector3 point = GetInteractionPoint(playerPosition);
        if ((point - playerPosition).sqrMagnitude > interactionDistance * interactionDistance)
        {
            context.InteractionController?.ClearCurrentTarget();
            return;
        }

        if (!HourHandInstalled)
        {
            if (context.Inventory != null && context.Inventory.SelectedItem == hourHandItem
                && context.Inventory.TryRemove(hourHandItem))
            {
                hourHandInstalled = true;
                SetHourHandVisual(true);
                if (clock != null)
                    clock.SetTimeRunning(true);
                hourHandWasInstalled?.Invoke();
                context.ShowMessage("Clock hand installed", 2f);
            }
            else
            {
                context.ShowMessage("Select the clock hand, then press E", 2f);
            }
            return;
        }

        ClockInteractionPanelUI.OpenOrCreate(this, context);
    }

    public bool IsHourOccupied(int dialHour)
    {
        return dialHour >= 0 && dialHour < occupiedHours.Length && occupiedHours[dialHour];
    }

    public bool TryPlaceBurgerAtHour(int dialHour, PlayerInteractionContext context)
    {
        if (dialHour < 0 || dialHour >= 12 || context.Inventory == null)
            return false;

        if (occupiedHours[dialHour])
        {
            context.ShowMessage("Right-click this slot to remove its burger", 1.5f);
            return false;
        }

        if (purpose == ClockPurpose.Production
            && (microwaveStation == null || !microwaveStation.IsCooking))
        {
            context.ShowMessage("The microwave must be working first", 1.5f);
            return false;
        }

        if (context.Inventory.SelectedItem != burgerItem
            || !context.Inventory.TryRemove(burgerItem))
        {
            context.ShowMessage("Select a burger in the hotbar first", 1.5f);
            return false;
        }

        occupiedHours[dialHour] = true;
        SpawnAnchoredBurger(dialHour);
        return true;
    }

    public bool TryRemoveBurgerAtHour(int dialHour, PlayerInteractionContext context)
    {
        if (dialHour < 0 || dialHour >= 12 || context.Inventory == null
            || !occupiedHours[dialHour])
            return false;

        if (!context.Inventory.TryAdd(burgerItem))
        {
            context.ShowMessage("Inventory is full", 1.5f);
            return false;
        }

        occupiedHours[dialHour] = false;
        if (placedBurgerObjects[dialHour] != null)
            Destroy(placedBurgerObjects[dialHour]);
        placedBurgerObjects[dialHour] = null;

        if (latchedHour == dialHour)
        {
            anchorLatched = false;
            latchedHour = -1;
            context.ShowMessage("Burger removed — the clock will continue", 1.5f);
        }
        return true;
    }

    public string GetStatusMessage()
    {
        if (purpose == ClockPurpose.Production && microwaveStation != null
            && microwaveStation.IsCooking)
        {
            int hour = microwaveStation.ExpectedCompletionDialHour;
            return hour >= 0
                ? $"Microwave completion: {(hour == 0 ? 12 : hour)} o'clock"
                : "Microwave is cooking";
        }

        return purpose == ClockPurpose.MonsterFeeding
            ? $"Final feeding anchor: {(victoryDialHour == 0 ? 12 : victoryDialHour)} o'clock"
            : "Place a burger before the hand reaches its hour";
    }

    private void SpawnAnchoredBurger(int dialHour)
    {
        GameObject prefab = placedBurgerPrefab != null
            ? placedBurgerPrefab
            : burgerItem != null ? burgerItem.HeldPrefab : null;
        Transform anchor = burgerSlotAnchors != null && dialHour < burgerSlotAnchors.Length
            ? burgerSlotAnchors[dialHour]
            : null;
        if (prefab == null || anchor == null)
            return;

        GameObject burger = Instantiate(prefab, anchor);
        burger.name = $"Clock Burger - {(dialHour == 0 ? 12 : dialHour)} o'clock";
        burger.transform.localPosition = Vector3.zero;
        burger.transform.localRotation = Quaternion.identity;
        foreach (Collider itemCollider in burger.GetComponentsInChildren<Collider>(true))
            itemCollider.enabled = false;
        foreach (Rigidbody body in burger.GetComponentsInChildren<Rigidbody>(true))
        {
            body.isKinematic = true;
            body.useGravity = false;
        }
        foreach (WorldItem item in burger.GetComponentsInChildren<WorldItem>(true))
            item.SetCanBeCollected(false);
        placedBurgerObjects[dialHour] = burger;
    }

    private void HandleHourAdvanced(int currentHour, int totalElapsedHours)
    {
        int dialHour = currentHour % 12;
        if (!occupiedHours[dialHour])
            return;

        anchorLatched = true;
        latchedHour = dialHour;
        clock.SetPaused(true);
        burgerAnchorTriggered?.Invoke();

        if (purpose == ClockPurpose.Production && microwaveStation != null
            && microwaveStation.ExpectedCompletionDialHour == dialHour)
        {
            microwaveStation.EnableInfiniteProductionMode();
        }
        else if (purpose == ClockPurpose.MonsterFeeding
                 && dialHour == victoryDialHour && !victorySequenceStarted)
        {
            StartCoroutine(CompleteAfterDelay());
        }
    }

    private IEnumerator CompleteAfterDelay()
    {
        victorySequenceStarted = true;
        yield return new WaitForSecondsRealtime(victoryDelay);
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
            gameManager.CompleteGame();
    }

    private void SetHourHandVisual(bool visible)
    {
        if (hourHandVisual != null)
            hourHandVisual.SetActive(visible);
        else if (clock != null)
            clock.SetHourHandVisible(visible);
    }

    private void OnValidate()
    {
        interactionDistance = Mathf.Max(0.1f, interactionDistance);
        victoryDelay = Mathf.Max(0f, victoryDelay);
        if (burgerSlotAnchors == null || burgerSlotAnchors.Length != 12)
            Array.Resize(ref burgerSlotAnchors, 12);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = interactionPoint != null
            ? interactionPoint.position
            : interactionCollider != null ? interactionCollider.bounds.center : transform.position;
        Gizmos.color = new Color(0.25f, 0.85f, 1f, 0.35f);
        Gizmos.DrawWireSphere(center, Mathf.Max(0.1f, interactionDistance));
    }
}
