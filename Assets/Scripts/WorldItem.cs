using UnityEngine;

/// <summary>A collectible instance placed in the game world.</summary>
public class WorldItem : MonoBehaviour
{
    [SerializeField] private ItemDefinition item;
    [Min(1)] [SerializeField] private int quantity = 1;
    [SerializeField] private bool canBeCollected = true;
    [Tooltip("Adds the item on every interaction without destroying this world object.")]
    [SerializeField] private bool infiniteSupply;

    private bool collectionInProgress;

    public ItemDefinition Item => item;
    public int Quantity => quantity;
    public bool CanBeCollected => isActiveAndEnabled && canBeCollected
        && !collectionInProgress && item != null;
    public bool IsInfiniteSupply => infiniteSupply;

    public bool TryCollect(PlayerInventory inventory)
    {
        if (!CanBeCollected || inventory == null || !inventory.TryAdd(item, quantity))
            return false;

        if (!infiniteSupply)
        {
            collectionInProgress = true;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
        return true;
    }

    public void Configure(ItemDefinition definition, int itemQuantity = 1,
        bool collectible = true, bool supplyIsInfinite = false)
    {
        item = definition;
        quantity = Mathf.Max(1, itemQuantity);
        canBeCollected = collectible;
        infiniteSupply = supplyIsInfinite;
        collectionInProgress = false;
    }

    public void SetCanBeCollected(bool collectible)
    {
        canBeCollected = collectible;
        collectionInProgress = false;
    }

    public Vector3 GetClosestPoint(Vector3 position)
    {
        Collider itemCollider = GetComponentInChildren<Collider>();
        return itemCollider != null ? itemCollider.ClosestPoint(position) : transform.position;
    }

    private void OnValidate()
    {
        quantity = Mathf.Max(1, quantity);
    }
}
