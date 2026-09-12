using UnityEngine;

/// <summary>
/// Shared data for one item type. Create assets from Assets/Create/Key and Door/Item Definition.
/// World objects and inventory stacks reference the same asset to establish item identity.
/// </summary>
[CreateAssetMenu(fileName = "New Item", menuName = "Key and Door/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    [Header("Identity and UI")]
    [SerializeField] private string displayName = "New Item";
    [SerializeField] private Sprite icon;

    [Header("Held View")]
    [Tooltip("A visual-only prefab displayed under the player's Held Item Anchor.")]
    [SerializeField] private GameObject heldPrefab;
    [SerializeField] private Vector3 heldLocalPosition;
    [SerializeField] private Vector3 heldLocalEulerAngles;
    [SerializeField] private Vector3 heldLocalScale = Vector3.one;

    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public GameObject HeldPrefab => heldPrefab;
    public Vector3 HeldLocalPosition => heldLocalPosition;
    public Vector3 HeldLocalEulerAngles => heldLocalEulerAngles;
    public Vector3 HeldLocalScale => heldLocalScale;

    private void OnValidate()
    {
        if (heldLocalScale == Vector3.zero)
            heldLocalScale = Vector3.one;
    }
}
