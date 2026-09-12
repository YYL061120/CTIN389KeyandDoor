using UnityEngine;

/// <summary>Routes E to nearby puzzle objects and drives the shared prompt.</summary>
public class PlayerInteractionController : MonoBehaviour
{
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private FirstPersonController movementController;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerMicrowaveInteractor microwaveInteractor;
    [SerializeField] private InteractionPromptUI promptUI;
    [Min(0.05f)] [SerializeField] private float targetRefreshInterval = 0.15f;

    private PlayerInteractionContext context;
    private IPlayerInteractable currentTarget;
    private float nextRefreshTime;

    private void Awake()
    {
        if (inventory == null)
            inventory = GetComponent<PlayerInventory>();
        if (movementController == null)
            movementController = GetComponent<FirstPersonController>();
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();
        if (microwaveInteractor == null)
            microwaveInteractor = GetComponent<PlayerMicrowaveInteractor>();
        if (promptUI == null)
        {
            promptUI = FindFirstObjectByType<InteractionPromptUI>();
            if (promptUI == null)
                promptUI = new GameObject("Interaction Prompt UI").AddComponent<InteractionPromptUI>();
        }

        context = new PlayerInteractionContext(gameObject, inventory, movementController,
            playerCamera, this);
    }

    private void Update()
    {
        if (movementController != null
            && (!movementController.playerCanMove || !movementController.cameraCanMove))
        {
            promptUI.Hide();
            return;
        }

        if (Time.unscaledTime >= nextRefreshTime)
        {
            nextRefreshTime = Time.unscaledTime + targetRefreshInterval;
            currentTarget = FindNearestTarget();
        }

        if (currentTarget != null)
            promptUI.ShowContext(currentTarget.InteractionPrompt);
        else if (microwaveInteractor != null && microwaveInteractor.HasStationInRange())
            promptUI.ShowContext("Press E to interact");
        else
            promptUI.Hide();
    }

    public bool TryInteract()
    {
        currentTarget = FindNearestTarget();
        if (currentTarget != null)
        {
            currentTarget.Interact(context);
            return true;
        }

        return microwaveInteractor != null && microwaveInteractor.TryInteract();
    }

    public void ShowMessage(string message, float duration = 2f)
    {
        promptUI.ShowTransient(message, duration);
    }

    public void ClearCurrentTarget()
    {
        currentTarget = null;
        nextRefreshTime = 0f;
        if (promptUI != null)
            promptUI.Hide();
    }

    private IPlayerInteractable FindNearestTarget()
    {
        IPlayerInteractable nearest = null;
        float nearestSqrDistance = float.PositiveInfinity;
        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (!(behaviour is IPlayerInteractable interactable)
                || !behaviour.isActiveAndEnabled || !interactable.CanInteract(context))
                continue;

            Vector3 point = interactable.GetInteractionPoint(transform.position);
            if (float.IsNaN(point.x) || float.IsNaN(point.y) || float.IsNaN(point.z)
                || float.IsInfinity(point.x) || float.IsInfinity(point.y)
                || float.IsInfinity(point.z))
                continue;
            float sqrDistance = (point - transform.position).sqrMagnitude;
            float range = interactable.InteractionDistance;
            if (sqrDistance <= range * range && sqrDistance < nearestSqrDistance)
            {
                nearest = interactable;
                nearestSqrDistance = sqrDistance;
            }
        }
        return nearest;
    }
}
