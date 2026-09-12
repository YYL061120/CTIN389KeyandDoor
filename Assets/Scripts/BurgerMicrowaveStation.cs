using System;
using System.Collections;
using OOLaboratories.Microwave;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Owns the burger recipe, microwave work cycle, and physical result. UI and
/// player-input code talk to this component instead of depending on the asset.
/// </summary>
public class BurgerMicrowaveStation : MonoBehaviour
{
    public enum ProductionMode
    {
        Normal,
        Infinite
    }

    [Header("Interaction")]
    [Min(0.1f)] [SerializeField] private float interactionDistance = 3.5f;
    [Tooltip("Optional fixed point used for player interaction distance. If empty, the microwave object's transform is used.")]
    [SerializeField] private Transform interactionPoint;

    [Header("Burger Recipe")]
    [Tooltip("The UI creates one slot for every entry. The current recipe has four entries.")]
    [SerializeField] private ItemDefinition[] requiredIngredients = Array.Empty<ItemDefinition>();

    [Header("Cooking")]
    [SerializeField] private MicrowaveController microwaveController;
    [SerializeField] private ClockController gameClock;
    [Tooltip("Used only when no ClockController can be found.")]
    [Min(0.1f)] [SerializeField] private float fallbackCookingSeconds = 5f;

    [Header("Production Mode")]
    [SerializeField] private ProductionMode startingMode = ProductionMode.Normal;
    [Min(0.1f)] [SerializeField] private float infiniteOutputInterval = 2f;
    [SerializeField] private DiningAreaBurgerReceiver diningAreaReceiver;

    [Header("Output")]
    [SerializeField] private GameObject burgerOutputPrefab;
    [SerializeField] private Transform outputPoint;
    [Tooltip("Upward launch speed in metres per second. This is independent of burger mass.")]
    [FormerlySerializedAs("upwardImpulse")]
    [Min(0f)] [SerializeField] private float upwardLaunchSpeed = 4.5f;
    [Tooltip("Forward launch speed in metres per second. This is independent of burger mass.")]
    [FormerlySerializedAs("forwardImpulse")]
    [Min(0f)] [SerializeField] private float forwardLaunchSpeed = 0.8f;
    [Min(0f)] [SerializeField] private float launchSpin = 2.5f;
    [Min(0.1f)] [SerializeField] private float outputLifetime = 2f;

    public float InteractionDistance => interactionDistance;
    public ItemDefinition[] RequiredIngredients => requiredIngredients;
    public bool IsCooking { get; private set; }
    public ProductionMode CurrentMode { get; private set; }
    public float CookingDuration => activeCookingDuration > 0f
        ? activeCookingDuration
        : CalculateCookingDuration();
    public int ExpectedCompletionHour { get; private set; } = -1;
    public int ExpectedCompletionDialHour => ExpectedCompletionHour < 0
        ? -1
        : ExpectedCompletionHour % 12;

    public event Action CookingStarted;
    public event Action<GameObject> BurgerProduced;

    private Coroutine normalCookingRoutine;
    private Coroutine infiniteProductionRoutine;
    private float activeCookingDuration;

    private void Awake()
    {
        if (microwaveController == null)
            microwaveController = GetComponent<MicrowaveController>();
        if (gameClock == null)
            gameClock = FindFirstObjectByType<ClockController>();
        if (diningAreaReceiver == null)
            diningAreaReceiver = FindFirstObjectByType<DiningAreaBurgerReceiver>();

        CurrentMode = ProductionMode.Normal;
    }

    private void Start()
    {
        if (startingMode == ProductionMode.Infinite)
            SetInfiniteProductionMode(true);
    }

    public Vector3 GetClosestPoint(Vector3 position)
    {
        return interactionPoint != null ? interactionPoint.position : transform.position;
    }

    public bool TryStartCooking()
    {
        if (IsCooking || CurrentMode == ProductionMode.Infinite || burgerOutputPrefab == null)
            return false;

        activeCookingDuration = CalculateCookingDuration();
        ExpectedCompletionHour = gameClock != null
            ? gameClock.GetCookingCompletionHourFromCurrentPhase()
            : -1;
        normalCookingRoutine = StartCoroutine(CookBurger());
        return true;
    }

    private IEnumerator CookBurger()
    {
        IsCooking = true;
        CookingStarted?.Invoke();

        float duration = Mathf.Max(0.1f, CookingDuration);
        if (microwaveController != null)
        {
            microwaveController.doorAngle = 0f;
            // The supplied controller accepts whole seconds, so ceil keeps its
            // effect alive while our exact phase-aligned timer is authoritative.
            microwaveController.StartMicrowave(Mathf.Max(1, Mathf.CeilToInt(duration)));
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (microwaveController.isCooking)
            {
                microwaveController.StopMicrowave();
                if (microwaveController.isCooking)
                    microwaveController.StopMicrowave();
            }
        }
        else
        {
            Debug.LogWarning("BurgerMicrowaveStation has no MicrowaveController; using timer-only fallback.", this);
            yield return new WaitForSeconds(duration);
        }

        GameObject burger = SpawnVisualBurger();
        if (diningAreaReceiver != null)
            diningAreaReceiver.SpawnCollectibleBurger();
        else
            Debug.LogWarning("No DiningAreaBurgerReceiver was found; collectible burger was not spawned.", this);

        IsCooking = false;
        normalCookingRoutine = null;
        activeCookingDuration = 0f;
        BurgerProduced?.Invoke(burger);
    }

    private float CalculateCookingDuration()
    {
        return gameClock != null
            ? gameClock.GetCookingDurationFromCurrentPhase()
            : fallbackCookingSeconds;
    }

    /// <summary>UnityEvent-friendly entry point for the clock-hand puzzle.</summary>
    public void EnableInfiniteProductionMode() => SetInfiniteProductionMode(true);

    public void DisableInfiniteProductionMode() => SetInfiniteProductionMode(false);

    public void SetInfiniteProductionMode(bool enabled)
    {
        ProductionMode requestedMode = enabled ? ProductionMode.Infinite : ProductionMode.Normal;
        if (CurrentMode == requestedMode)
            return;

        CurrentMode = requestedMode;
        if (enabled)
        {
            if (normalCookingRoutine != null)
            {
                StopCoroutine(normalCookingRoutine);
                normalCookingRoutine = null;
            }
            activeCookingDuration = 0f;

            IsCooking = true;
            if (diningAreaReceiver == null)
                diningAreaReceiver = FindFirstObjectByType<DiningAreaBurgerReceiver>();
            if (diningAreaReceiver != null)
                diningAreaReceiver.ReplaceBurgersWithInfiniteMountain();

            StartContinuousMicrowaveEffect();
            infiniteProductionRoutine = StartCoroutine(ProduceForever());
        }
        else
        {
            if (infiniteProductionRoutine != null)
            {
                StopCoroutine(infiniteProductionRoutine);
                infiniteProductionRoutine = null;
            }
            StopMicrowaveEffect();
            if (diningAreaReceiver != null)
                diningAreaReceiver.RemoveInfiniteMountain();
            IsCooking = false;
        }
    }

    [ContextMenu("Debug/Enable Infinite Production")]
    private void DebugEnableInfiniteProduction() => SetInfiniteProductionMode(true);

    [ContextMenu("Debug/Disable Infinite Production")]
    private void DebugDisableInfiniteProduction() => SetInfiniteProductionMode(false);

    private IEnumerator ProduceForever()
    {
        WaitForSeconds interval = new WaitForSeconds(Mathf.Max(0.1f, infiniteOutputInterval));
        while (CurrentMode == ProductionMode.Infinite)
        {
            yield return interval;
            GameObject burger = SpawnVisualBurger();
            BurgerProduced?.Invoke(burger);

            // Keep the supplied asset controller in its animated cooking state.
            if (microwaveController != null && !microwaveController.isCooking)
                StartContinuousMicrowaveEffect();
        }
    }

    private void StartContinuousMicrowaveEffect()
    {
        if (microwaveController == null)
            return;

        microwaveController.doorAngle = 0f;
        microwaveController.StartMicrowave(60 * 99 + 59);
    }

    private void StopMicrowaveEffect()
    {
        if (microwaveController == null || !microwaveController.isCooking)
            return;

        // The asset's first stop pauses and its second stop clears the timer.
        microwaveController.StopMicrowave();
        if (microwaveController.isCooking)
            microwaveController.StopMicrowave();
    }

    private GameObject SpawnVisualBurger()
    {
        Vector3 position = outputPoint != null
            ? outputPoint.position
            : transform.TransformPoint(new Vector3(0f, 0.45f, 0f));
        Quaternion rotation = outputPoint != null ? outputPoint.rotation : transform.rotation;
        GameObject burger = Instantiate(burgerOutputPrefab, position, rotation);
        burger.name = "Microwave Burger (Visual Only)";

        foreach (WorldItem worldItem in burger.GetComponentsInChildren<WorldItem>(true))
            worldItem.SetCanBeCollected(false);
        foreach (BurgerMarker marker in burger.GetComponentsInChildren<BurgerMarker>(true))
            marker.SetConsumableByMonster(false);

        Rigidbody body = burger.GetComponentInChildren<Rigidbody>();
        if (body == null)
            body = burger.AddComponent<Rigidbody>();
        body.isKinematic = false;
        body.useGravity = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;

        Vector3 forward = outputPoint != null ? outputPoint.forward : transform.forward;
        Vector3 launchVelocity = Vector3.up * upwardLaunchSpeed + forward * forwardLaunchSpeed;
        body.AddForce(launchVelocity, ForceMode.VelocityChange);
        body.AddTorque(UnityEngine.Random.onUnitSphere * launchSpin, ForceMode.VelocityChange);

        Collider[] burgerColliders = burger.GetComponentsInChildren<Collider>(true);
        Collider[] microwaveColliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider burgerCollider in burgerColliders)
        {
            foreach (Collider microwaveCollider in microwaveColliders)
            {
                if (burgerCollider != null && microwaveCollider != null)
                    Physics.IgnoreCollision(burgerCollider, microwaveCollider, true);
            }
        }

        Destroy(burger, outputLifetime);
        return burger;
    }

    private void OnValidate()
    {
        interactionDistance = Mathf.Max(0.1f, interactionDistance);
        fallbackCookingSeconds = Mathf.Max(0.1f, fallbackCookingSeconds);
        infiniteOutputInterval = Mathf.Max(0.1f, infiniteOutputInterval);
        upwardLaunchSpeed = Mathf.Max(0f, upwardLaunchSpeed);
        forwardLaunchSpeed = Mathf.Max(0f, forwardLaunchSpeed);
        outputLifetime = Mathf.Max(0.1f, outputLifetime);
        launchSpin = Mathf.Max(0f, launchSpin);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.55f, 0.1f, 0.75f);
        Gizmos.DrawWireSphere(
            interactionPoint != null ? interactionPoint.position : transform.position,
            interactionDistance);
        Gizmos.DrawRay(outputPoint != null ? outputPoint.position : transform.position,
            Vector3.up * Mathf.Max(0.5f, upwardLaunchSpeed * 0.25f));
    }
}
