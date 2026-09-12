using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>Reusable burger-payment safe. Configure 2 and 10 on two instances.</summary>
public class BurgerSafe : MonoBehaviour, IPlayerInteractable
{
    [Header("Cost")]
    [SerializeField] private ItemDefinition burgerItem;
    [Min(1)] [SerializeField] private int requiredBurgers = 2;
    [SerializeField] private int depositedBurgers;

    [Header("Interaction")]
    [Min(0.1f)] [SerializeField] private float interactionDistance = 3f;
    [Tooltip("Optional fixed point used for distance checks. If empty, the safe object's transform is used.")]
    [SerializeField] private Transform interactionPoint;
    [Tooltip("Optional collider whose bounds center is used when no interaction point is assigned. It is never auto-selected.")]
    [SerializeField] private Collider interactionCollider;

    [Header("Opening Animation")]
    [Tooltip("Preferred: assign an Animator with a trigger parameter named below.")]
    [SerializeField] private Animator animator;
    [SerializeField] private string openTrigger = "Open";
    [Tooltip("Fallback when no Animator is assigned. Assign the Safe_Door transform.")]
    [SerializeField] private Transform fallbackDoor;
    [SerializeField] private Vector3 fallbackOpenEulerAngles = new Vector3(0f, -105f, 0f);
    [Min(0.05f)] [SerializeField] private float fallbackOpenDuration = 1.2f;

    [Header("Reward")]
    [SerializeField] private GameObject[] rewardObjects = new GameObject[0];
    [SerializeField] private UnityEvent opened;

    private bool isOpen;

    public string InteractionPrompt => "Press E to interact";
    public float InteractionDistance => interactionDistance;
    public int DepositedBurgers => depositedBurgers;
    public int RequiredBurgers => requiredBurgers;
    public bool IsOpen => isOpen;

    private void Awake()
    {
        isOpen = depositedBurgers >= requiredBurgers;
        SetRewardsActive(isOpen);
    }

    public bool CanInteract(PlayerInteractionContext context) => !isOpen;

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
        if (isOpen || context == null || context.Player == null)
            return;

        Vector3 playerPosition = context.Player.transform.position;
        if ((GetInteractionPoint(playerPosition) - playerPosition).sqrMagnitude
            > interactionDistance * interactionDistance)
        {
            context.InteractionController?.ClearCurrentTarget();
            return;
        }

        bool holdingBurger = context.Inventory != null
            && context.Inventory.SelectedItem == burgerItem;
        if (holdingBurger && context.Inventory.TryRemove(burgerItem))
            depositedBurgers++;

        context.ShowMessage($"{depositedBurgers}/{requiredBurgers}", 2f);
        if (depositedBurgers >= requiredBurgers)
            OpenSafe();
    }

    [ContextMenu("Debug/Open Safe")]
    public void OpenSafe()
    {
        if (isOpen)
            return;
        isOpen = true;
        depositedBurgers = requiredBurgers;
        SetRewardsActive(true);

        if (animator != null && !string.IsNullOrEmpty(openTrigger))
            animator.SetTrigger(openTrigger);
        else if (fallbackDoor != null)
            StartCoroutine(OpenFallbackDoor());

        opened?.Invoke();
    }

    private IEnumerator OpenFallbackDoor()
    {
        Quaternion from = fallbackDoor.localRotation;
        Quaternion to = from * Quaternion.Euler(fallbackOpenEulerAngles);
        float elapsed = 0f;
        while (elapsed < fallbackOpenDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / fallbackOpenDuration);
            fallbackDoor.localRotation = Quaternion.Slerp(from, to, t);
            yield return null;
        }
        fallbackDoor.localRotation = to;
    }

    private void SetRewardsActive(bool active)
    {
        foreach (GameObject reward in rewardObjects)
        {
            if (reward != null)
                reward.SetActive(active);
        }
    }

    private void OnValidate()
    {
        requiredBurgers = Mathf.Max(1, requiredBurgers);
        depositedBurgers = Mathf.Clamp(depositedBurgers, 0, requiredBurgers);
        interactionDistance = Mathf.Max(0.1f, interactionDistance);
        fallbackOpenDuration = Mathf.Max(0.05f, fallbackOpenDuration);
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
