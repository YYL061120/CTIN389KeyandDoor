using System;
using UnityEngine;

/// <summary>
/// E first collects the centered item. If none is centered, it collects the
/// nearest eligible item within pickupDistance.
/// </summary>
public class PlayerItemCollector : MonoBehaviour
{
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private FirstPersonController movementController;
    [SerializeField] private PlayerMicrowaveInteractor microwaveInteractor;
    [SerializeField] private PlayerInteractionController interactionController;
    [Min(0.1f)] [SerializeField] private float pickupDistance = 3f;
    [SerializeField] private KeyCode pickupKey = KeyCode.E;
    [SerializeField] private LayerMask collectibleLayers = ~0;

    [Header("Pickup Audio")]
    [Tooltip("One clip is enough. If several are assigned, one is chosen randomly after each successful pickup.")]
    [SerializeField] private AudioClip[] pickupSounds = Array.Empty<AudioClip>();
    [Tooltip("Optional. When empty, a dedicated AudioSource is created on the player at runtime.")]
    [SerializeField] private AudioSource pickupAudioSource;
    [Range(0f, 1f)] [SerializeField] private float pickupVolume = 0.8f;
    [Range(0f, 1f)] [SerializeField] private float pickupSpatialBlend;

    public event Action<WorldItem> ItemCollected;
    public event Action<WorldItem> CollectionFailed;

    private void Awake()
    {
        if (inventory == null)
            inventory = GetComponent<PlayerInventory>();
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();
        if (movementController == null)
            movementController = GetComponent<FirstPersonController>();
        if (microwaveInteractor == null)
            microwaveInteractor = GetComponent<PlayerMicrowaveInteractor>();
        if (interactionController == null)
        {
            interactionController = GetComponent<PlayerInteractionController>();
            if (interactionController == null)
                interactionController = gameObject.AddComponent<PlayerInteractionController>();
        }

        ResolvePickupAudioSource();
    }

    private void Update()
    {
        if ((movementController == null || (movementController.playerCanMove && movementController.cameraCanMove))
            && Input.GetKeyDown(pickupKey))
        {
            // World interactions get first refusal so E never opens a station and
            // collects a nearby loose ingredient on the same frame.
            if (interactionController != null && interactionController.TryInteract())
                return;

            TryCollectBestCandidate();
        }
    }

    public bool TryCollectBestCandidate()
    {
        WorldItem candidate = FindCenteredItem();
        if (candidate == null)
            candidate = FindNearestItem();
        if (candidate == null)
            return false;

        if (candidate.TryCollect(inventory))
        {
            PlayPickupSound();
            ItemCollected?.Invoke(candidate);
            return true;
        }

        CollectionFailed?.Invoke(candidate);
        return false;
    }

    private WorldItem FindCenteredItem()
    {
        if (playerCamera == null)
            return null;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit[] hits = Physics.RaycastAll(ray, pickupDistance, collectibleLayers,
            QueryTriggerInteraction.Collide);
        Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        foreach (RaycastHit hit in hits)
        {
            WorldItem item = hit.collider.GetComponentInParent<WorldItem>();
            if (IsEligible(item))
                return item;
        }
        return null;
    }

    private WorldItem FindNearestItem()
    {
        WorldItem nearest = null;
        float nearestSqrDistance = pickupDistance * pickupDistance;
        WorldItem[] worldItems = FindObjectsByType<WorldItem>(FindObjectsSortMode.None);

        foreach (WorldItem item in worldItems)
        {
            if (!IsEligible(item))
                continue;

            float sqrDistance = (item.GetClosestPoint(transform.position) - transform.position).sqrMagnitude;
            if (sqrDistance <= nearestSqrDistance)
            {
                nearest = item;
                nearestSqrDistance = sqrDistance;
            }
        }
        return nearest;
    }

    private bool IsEligible(WorldItem item)
    {
        if (item == null || !item.CanBeCollected || !IsLayerIncluded(item.gameObject.layer))
            return false;
        return (item.GetClosestPoint(transform.position) - transform.position).sqrMagnitude
            <= pickupDistance * pickupDistance;
    }

    private bool IsLayerIncluded(int layer)
    {
        return (collectibleLayers.value & (1 << layer)) != 0;
    }

    private void ResolvePickupAudioSource()
    {
        if (pickupAudioSource == null && pickupSounds != null && pickupSounds.Length > 0)
            pickupAudioSource = gameObject.AddComponent<AudioSource>();

        if (pickupAudioSource == null)
            return;

        pickupAudioSource.playOnAwake = false;
        pickupAudioSource.loop = false;
        pickupAudioSource.spatialBlend = pickupSpatialBlend;
        pickupAudioSource.dopplerLevel = 0f;
    }

    private void PlayPickupSound()
    {
        if (pickupAudioSource == null || pickupSounds == null || pickupSounds.Length == 0)
            return;

        int startIndex = UnityEngine.Random.Range(0, pickupSounds.Length);
        for (int offset = 0; offset < pickupSounds.Length; offset++)
        {
            AudioClip clip = pickupSounds[(startIndex + offset) % pickupSounds.Length];
            if (clip == null)
                continue;

            pickupAudioSource.PlayOneShot(clip, pickupVolume);
            return;
        }
    }

    private void OnValidate()
    {
        pickupVolume = Mathf.Clamp01(pickupVolume);
        pickupSpatialBlend = Mathf.Clamp01(pickupSpatialBlend);
        if (pickupAudioSource != null)
            pickupAudioSource.spatialBlend = pickupSpatialBlend;
    }
}
