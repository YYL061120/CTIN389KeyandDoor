using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns burger presentation inside DiningArea: random normal rewards and the
/// persistent infinite-supply mountain used by the final production mode.
/// </summary>
public class DiningAreaBurgerReceiver : MonoBehaviour
{
    [Header("Burger Assets")]
    [SerializeField] private GameObject collectibleBurgerPrefab;
    [Tooltip("Required for infinite mode. Exactly one authored mountain prefab is spawned.")]
    [SerializeField] private GameObject burgerMountainPrefab;
    [SerializeField] private ItemDefinition burgerItem;

    [Header("Local Spawn Region")]
    [SerializeField] private Vector3 localSpawnCenter = new Vector3(0f, 0.5f, 0f);
    [SerializeField] private Vector3 localSpawnSize = new Vector3(2.5f, 0f, 2.5f);
    [SerializeField] private bool randomizeYaw = true;

    private readonly List<GameObject> normalBurgers = new List<GameObject>();
    private GameObject infiniteMountain;

    public bool InfiniteSupplyActive => infiniteMountain != null;
    public int NormalBurgerCount
    {
        get
        {
            RemoveMissingNormalBurgers();
            return normalBurgers.Count;
        }
    }

    public GameObject SpawnCollectibleBurger()
    {
        if (collectibleBurgerPrefab == null)
        {
            Debug.LogWarning("DiningAreaBurgerReceiver needs a Collectible Burger Prefab.", this);
            return null;
        }

        Vector3 halfSize = Abs(localSpawnSize) * 0.5f;
        Vector3 localPosition = localSpawnCenter + new Vector3(
            Random.Range(-halfSize.x, halfSize.x),
            Random.Range(-halfSize.y, halfSize.y),
            Random.Range(-halfSize.z, halfSize.z));
        Quaternion rotation = randomizeYaw
            ? transform.rotation * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f)
            : transform.rotation;

        GameObject burger = Instantiate(collectibleBurgerPrefab,
            transform.TransformPoint(localPosition), rotation);
        burger.name = "Dining Area Burger (Collectible)";
        ConfigureBurger(burger, true, false, true);
        normalBurgers.Add(burger);
        return burger;
    }

    public void ReplaceBurgersWithInfiniteMountain()
    {
        ClearNormalBurgers();
        RemoveInfiniteMountain();

        if (burgerMountainPrefab == null)
        {
            Debug.LogWarning(
                "Infinite mode needs a Burger Mountain Prefab on DiningAreaBurgerReceiver.", this);
            return;
        }

        Vector3 position = transform.TransformPoint(localSpawnCenter);
        infiniteMountain = Instantiate(burgerMountainPrefab, position, transform.rotation);

        infiniteMountain.name = "Infinite Burger Mountain";
        foreach (Rigidbody body in infiniteMountain.GetComponentsInChildren<Rigidbody>(true))
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.useGravity = false;
            body.isKinematic = true;
        }
        ConfigureBurger(infiniteMountain, true, true, false);
        if (infiniteMountain.GetComponentInChildren<Collider>(true) == null)
            Debug.LogWarning(
                "Burger Mountain Prefab needs at least one Collider so the player can collect from it.",
                infiniteMountain);
    }

    /// <summary>
    /// Removes exactly one normal-mode burger. The preferred marker may be on
    /// either the burger root or one of its children.
    /// </summary>
    public bool TryConsumeNormalBurger(BurgerMarker preferredMarker)
    {
        if (InfiniteSupplyActive)
            return false;

        RemoveMissingNormalBurgers();
        for (int index = 0; index < normalBurgers.Count; index++)
        {
            GameObject candidate = normalBurgers[index];
            if (preferredMarker != null
                && !preferredMarker.transform.IsChildOf(candidate.transform)
                && !candidate.transform.IsChildOf(preferredMarker.transform))
                continue;

            normalBurgers.RemoveAt(index);
            candidate.SetActive(false);
            Destroy(candidate);
            return true;
        }

        return false;
    }

    public void RemoveInfiniteMountain()
    {
        if (infiniteMountain == null)
            return;

        infiniteMountain.SetActive(false);
        Destroy(infiniteMountain);
        infiniteMountain = null;
    }

    private void ConfigureBurger(GameObject target, bool collectible, bool infinite,
        bool monsterCanConsume)
    {
        WorldItem rootItem = target.GetComponent<WorldItem>();
        if (rootItem == null && collectible)
            rootItem = target.AddComponent<WorldItem>();

        foreach (WorldItem worldItem in target.GetComponentsInChildren<WorldItem>(true))
        {
            if (infinite)
                worldItem.Configure(burgerItem, 1, true, true);
            else
                worldItem.SetCanBeCollected(false);
        }
        if (rootItem != null)
            rootItem.Configure(burgerItem, 1, collectible, infinite);

        foreach (BurgerMarker marker in target.GetComponentsInChildren<BurgerMarker>(true))
            marker.SetConsumableByMonster(monsterCanConsume);
        BurgerMarker rootMarker = target.GetComponent<BurgerMarker>();
        if (rootMarker == null)
            rootMarker = target.AddComponent<BurgerMarker>();
        rootMarker.SetConsumableByMonster(monsterCanConsume);
    }

    private void ClearNormalBurgers()
    {
        foreach (GameObject burger in normalBurgers)
        {
            if (burger != null)
            {
                burger.SetActive(false);
                Destroy(burger);
            }
        }
        normalBurgers.Clear();
    }

    private void RemoveMissingNormalBurgers()
    {
        normalBurgers.RemoveAll(burger => burger == null);
    }

    private static Vector3 Abs(Vector3 value)
    {
        return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
    }

    private void OnValidate()
    {
        localSpawnSize = Abs(localSpawnSize);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.95f, 0.45f, 0.1f, 0.8f);
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(transform.TransformPoint(localSpawnCenter),
            transform.rotation, Abs(transform.lossyScale));
        Gizmos.DrawWireCube(Vector3.zero, Abs(localSpawnSize));
        Gizmos.matrix = oldMatrix;
    }
}
