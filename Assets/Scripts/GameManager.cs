using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Coordinates the monster feeding loop and high-level game flow.
/// Timekeeping and clock visuals belong to <see cref="ClockController"/>.
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum FeedingResult { Fed, NotFed }

    [Serializable]
    private class BurgerPartSpawnEntry
    {
        public GameObject prefab = null;
        [Min(0)] public int amount = 1;
    }

    [Header("Monster Check")]
    [Tooltip("Master debug switch. When disabled, clock time still passes but the monster never checks for food.")]
    [SerializeField] private bool monsterChecksEnabled = true;
    [SerializeField] private ClockController gameClock;

    [Header("Dining Area Detection")]
    [SerializeField] private Transform diningArea;
    [SerializeField] private DiningAreaBurgerReceiver diningAreaBurgerReceiver;
    [Tooltip("Local-space center of the food detection box, relative to DiningArea.")]
    [SerializeField] private Vector3 diningAreaCenter = new Vector3(0f, 0.5f, 0f);
    [Tooltip("Half size of the food detection box. Adjust this with the yellow gizmo in the Scene view.")]
    [SerializeField] private Vector3 diningAreaHalfExtents = new Vector3(1.5f, 0.75f, 1.5f);
    [SerializeField] private LayerMask foodDetectionLayers = ~0;
    [SerializeField] private bool consumeBurgerWhenFed = true;

    [Header("Feeding Debug Override")]
    [Tooltip("When enabled, the value below replaces DiningArea detection.")]
    [SerializeField] private bool useManualFeedingValue;
    [Tooltip("Change this during Play Mode to test the success/failure UI.")]
    [SerializeField] private bool manualPlayerHasFed;

    [Header("Player")]
    [SerializeField] private FirstPersonController playerController;

    [Header("Result UI (optional - auto-created if empty)")]
    [SerializeField] private CanvasGroup resultOverlay;
    [SerializeField] private Text resultText;
    [Min(0f)] [SerializeField] private float fadeDuration = 1.25f;
    [Min(0f)] [SerializeField] private float successMessageDuration = 2.5f;
    [TextArea] [SerializeField] private string fedMessage = "The creature accepts your offering.\nYou have one more full rotation.";
    [TextArea] [SerializeField] private string notFedMessage = "You did not feed the creature.\n\nPress Enter to restart.";

    [Header("Victory")]
    [TextArea] [SerializeField] private string victoryMessage =
        "THE CLOCK IS JAMMED AT FEEDING HOUR.\n\n" +
        "The burgers will never stop coming.\n" +
        "The creature will never stop eating—or come for you.\n\n" +
        "YOU SURVIVED.";
    [Tooltip("How long the final game-over screen takes to fade completely to black.")]
    [Min(0f)] [SerializeField] private float gameEndFadeDuration = 2f;

    [Header("Burger Part Spawning")]
    [SerializeField] private bool spawnBurgerPartsOnStart = true;
    [SerializeField] private BurgerPartSpawnEntry[] burgerPartSpawns = Array.Empty<BurgerPartSpawnEntry>();
    [Tooltip("Optional orientation/origin for the cyan spawn box. When empty, Center is interpreted in world space.")]
    [SerializeField] private Transform burgerPartSpawnArea;
    [SerializeField] private Vector3 burgerPartSpawnCenter;
    [SerializeField] private Vector3 burgerPartSpawnSize = new Vector3(10f, 4f, 10f);
    [Tooltip("Objects are cast downward inside the box and placed on these surfaces.")]
    [SerializeField] private LayerMask burgerPartSpawnSurfaces = ~0;
    [Min(1)] [SerializeField] private int spawnAttemptsPerPart = 12;
    [Min(0f)] [SerializeField] private float spawnSurfaceOffset = 0.08f;
    [SerializeField] private bool randomizeSpawnYaw = true;
    [SerializeField] private Transform spawnedBurgerPartParent;

    private readonly System.Collections.Generic.List<GameObject> spawnedBurgerParts =
        new System.Collections.Generic.List<GameObject>();

    private bool resolvingCheck;
    private bool previousPlayerCanMove;
    private bool previousCameraCanMove;

    public bool MonsterChecksEnabled { get => monsterChecksEnabled; set => monsterChecksEnabled = value; }
    public bool ManualPlayerHasFed { get => manualPlayerHasFed; set => manualPlayerHasFed = value; }
    public bool GameCompleted { get; private set; }
    public event Action<FeedingResult> FeedingCheckResolved;

    private void Awake()
    {
        ResolveSceneReferences();
        EnsureResultUI();
        SetOverlayImmediate(0f, false);
    }

    private void OnEnable()
    {
        if (gameClock != null)
            gameClock.MonsterCheckReached += HandleMonsterCheckReached;
    }

    private void Start()
    {
        // Handles script execution order when the clock was not available during OnEnable.
        if (gameClock == null)
        {
            gameClock = FindFirstObjectByType<ClockController>();
            if (gameClock != null)
                gameClock.MonsterCheckReached += HandleMonsterCheckReached;
        }

        if (spawnBurgerPartsOnStart)
            SpawnBurgerParts();
    }

    private void OnDisable()
    {
        if (gameClock != null)
            gameClock.MonsterCheckReached -= HandleMonsterCheckReached;
    }

    private void ResolveSceneReferences()
    {
        if (gameClock == null)
            gameClock = FindFirstObjectByType<ClockController>();

        if (diningArea == null)
        {
            GameObject diningAreaObject = GameObject.Find("DiningArea");
            if (diningAreaObject != null)
                diningArea = diningAreaObject.transform;
        }

        if (diningAreaBurgerReceiver == null)
        {
            if (diningArea != null)
                diningAreaBurgerReceiver = diningArea.GetComponent<DiningAreaBurgerReceiver>();
            if (diningAreaBurgerReceiver == null)
                diningAreaBurgerReceiver = FindFirstObjectByType<DiningAreaBurgerReceiver>();
        }

        if (playerController == null)
            playerController = FindFirstObjectByType<FirstPersonController>();
    }

    private void HandleMonsterCheckReached(int totalGameHours)
    {
        // HourAdvanced is raised first, so a burger that jams the hand exactly
        // at feeding time wins instead of also starting the normal failure check.
        BurgerClockPuzzle[] clockPuzzles =
            FindObjectsByType<BurgerClockPuzzle>(FindObjectsSortMode.None);
        foreach (BurgerClockPuzzle puzzle in clockPuzzles)
        {
            if (puzzle.IsFinalFeedingJamActive)
                return;
        }

        if (monsterChecksEnabled && !resolvingCheck)
            StartCoroutine(ResolveFeedingCheck());
    }

    /// <summary>Public debug hook for a UnityEvent or future debug menu.</summary>
    [ContextMenu("Debug/Run Monster Check Now")]
    public void RunMonsterCheckNow()
    {
        if (!resolvingCheck)
            StartCoroutine(ResolveFeedingCheck());
    }

    public void CompleteGame()
    {
        if (GameCompleted)
            return;

        GameCompleted = true;
        resolvingCheck = true;
        StopAllCoroutines();
        StartCoroutine(ShowVictory());
    }

    private IEnumerator ShowVictory()
    {
        SetPlayerControl(false);
        foreach (ClockController clock in
                 FindObjectsByType<ClockController>(FindObjectsSortMode.None))
            clock.SetPaused(true);

        resultText.text = victoryMessage;
        resultText.enabled = false;
        resultOverlay.gameObject.SetActive(true);
        resultOverlay.alpha = 0f;
        yield return FadeOverlay(0f, 1f, gameEndFadeDuration);
        resultText.enabled = true;
    }

    [ContextMenu("Debug/Respawn Burger Parts")]
    public void SpawnBurgerParts()
    {
        ClearSpawnedBurgerParts();

        foreach (BurgerPartSpawnEntry entry in burgerPartSpawns)
        {
            if (entry == null || entry.prefab == null || entry.amount <= 0)
                continue;

            int spawnedCount = 0;
            for (int index = 0; index < entry.amount; index++)
            {
                if (!TryFindBurgerPartSpawnPoint(out Vector3 position, out Quaternion surfaceRotation))
                    continue;

                Quaternion rotation = surfaceRotation;
                if (randomizeSpawnYaw)
                    rotation = Quaternion.AngleAxis(UnityEngine.Random.Range(0f, 360f), GetSpawnUp()) * rotation;

                GameObject instance = Instantiate(entry.prefab, position, rotation, spawnedBurgerPartParent);
                instance.name = $"{entry.prefab.name} (Generated)";
                spawnedBurgerParts.Add(instance);
                spawnedCount++;
            }

            if (spawnedCount < entry.amount)
                Debug.LogWarning(
                    $"Spawned {spawnedCount}/{entry.amount} instances of {entry.prefab.name}. " +
                    "Move/resize the cyan spawn box or include the floor layer in Spawn Surfaces.", this);
        }
    }

    private bool TryFindBurgerPartSpawnPoint(out Vector3 position, out Quaternion rotation)
    {
        Vector3 size = Abs(burgerPartSpawnSize);
        Vector3 up = GetSpawnUp();
        Vector3 right = burgerPartSpawnArea != null ? burgerPartSpawnArea.right : Vector3.right;
        Vector3 forward = burgerPartSpawnArea != null ? burgerPartSpawnArea.forward : Vector3.forward;
        Vector3 worldCenter = burgerPartSpawnArea != null
            ? burgerPartSpawnArea.TransformPoint(burgerPartSpawnCenter)
            : burgerPartSpawnCenter;

        Vector3 scale = burgerPartSpawnArea != null ? Abs(burgerPartSpawnArea.lossyScale) : Vector3.one;
        float width = size.x * scale.x;
        float depth = size.z * scale.z;
        float height = Mathf.Max(0.1f, size.y * scale.y);
        float rayPadding = Mathf.Max(0.25f, spawnSurfaceOffset + 0.1f);

        for (int attempt = 0; attempt < spawnAttemptsPerPart; attempt++)
        {
            Vector3 rayOrigin = worldCenter
                + right * UnityEngine.Random.Range(-width * 0.5f, width * 0.5f)
                + forward * UnityEngine.Random.Range(-depth * 0.5f, depth * 0.5f)
                + up * (height * 0.5f + rayPadding);

            // RaycastAll is intentional: previously a spawned pickup could hide the floor
            // beneath it, making later items incorrectly report that no surface existed.
            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, -up, height + rayPadding * 2f,
                burgerPartSpawnSurfaces, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (left, rightHit) => left.distance.CompareTo(rightHit.distance));

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.GetComponentInParent<FirstPersonController>() != null
                    || hit.collider.GetComponentInParent<WorldItem>() != null)
                    continue;

                position = hit.point + hit.normal * spawnSurfaceOffset;
                rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                return true;
            }
        }

        position = default;
        rotation = Quaternion.identity;
        return false;
    }

    private Vector3 GetSpawnUp()
    {
        return burgerPartSpawnArea != null ? burgerPartSpawnArea.up : Vector3.up;
    }

    private void ClearSpawnedBurgerParts()
    {
        foreach (GameObject spawnedPart in spawnedBurgerParts)
        {
            if (spawnedPart != null)
            {
                spawnedPart.SetActive(false);
                Destroy(spawnedPart);
            }
        }
        spawnedBurgerParts.Clear();
    }

    private IEnumerator ResolveFeedingCheck()
    {
        resolvingCheck = true;
        SetPlayerControl(false);
        bool clockWasAlreadyPaused = gameClock != null && gameClock.IsPaused;
        if (gameClock != null)
            gameClock.SetPaused(true);

        BurgerMarker detectedBurger = null;
        bool playerHasFed = useManualFeedingValue
            ? manualPlayerHasFed
            : TryFindBurgerInDiningArea(out detectedBurger);

        resultText.text = playerHasFed ? fedMessage : notFedMessage;
        resultText.enabled = false;
        resultOverlay.gameObject.SetActive(true);
        yield return FadeOverlay(0f, 1f);

        resultText.enabled = true;
        FeedingResult result = playerHasFed ? FeedingResult.Fed : FeedingResult.NotFed;
        FeedingCheckResolved?.Invoke(result);

        if (playerHasFed)
        {
            if (consumeBurgerWhenFed && detectedBurger != null && detectedBurger.ConsumableByMonster)
            {
                bool receiverConsumedBurger = diningAreaBurgerReceiver != null
                    && diningAreaBurgerReceiver.TryConsumeNormalBurger(detectedBurger);
                if (!receiverConsumedBurger)
                    Destroy(detectedBurger.gameObject);
            }

            yield return WaitRealtime(successMessageDuration);
            resultText.enabled = false;
            yield return FadeOverlay(1f, 0f);
            resultOverlay.gameObject.SetActive(false);

            if (gameClock != null)
                gameClock.SetPaused(clockWasAlreadyPaused);

            SetPlayerControl(true);
            resolvingCheck = false;
            yield break;
        }

        while (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter))
            yield return null;

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private bool TryFindBurgerInDiningArea(out BurgerMarker burger)
    {
        burger = null;

        // Infinite microwave mode replaces normal rewards with a permanent
        // supply. Treat it as food for every 12-hour check without relying on
        // the mountain's collider position and without consuming the mountain.
        if (diningAreaBurgerReceiver != null
            && diningAreaBurgerReceiver.InfiniteSupplyActive)
            return true;

        if (diningArea == null)
        {
            Debug.LogWarning("GameManager cannot check feeding because DiningArea is not assigned.", this);
            return false;
        }

        Vector3 center = diningArea.TransformPoint(diningAreaCenter);
        Vector3 scaledHalfExtents = Vector3.Scale(diningAreaHalfExtents, Abs(diningArea.lossyScale));
        Collider[] overlaps = Physics.OverlapBox(center, scaledHalfExtents, diningArea.rotation,
            foodDetectionLayers, QueryTriggerInteraction.Collide);

        foreach (Collider candidate in overlaps)
        {
            if (candidate.transform.IsChildOf(diningArea))
                continue;

            burger = candidate.GetComponentInParent<BurgerMarker>();
            if (burger != null)
                return true;

            // Prototype fallback until every microwave output prefab has BurgerMarker.
            Transform burgerOwner = FindBurgerOwner(candidate.transform);
            if (burgerOwner != null || candidate.gameObject.tag == "Burger")
            {
                burgerOwner ??= candidate.attachedRigidbody != null
                    ? candidate.attachedRigidbody.transform
                    : candidate.transform;
                burger = burgerOwner.GetComponent<BurgerMarker>();
                if (burger == null)
                    burger = burgerOwner.gameObject.AddComponent<BurgerMarker>();
                return true;
            }
        }

        return false;
    }

    private static bool IsBurgerName(string objectName)
    {
        return objectName.IndexOf("burger", StringComparison.OrdinalIgnoreCase) >= 0
            || objectName.IndexOf("hamburger", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static Transform FindBurgerOwner(Transform candidate)
    {
        Transform current = candidate;
        while (current != null)
        {
            if (IsBurgerName(current.name))
                return current;
            current = current.parent;
        }
        return null;
    }

    private void SetPlayerControl(bool enabled)
    {
        if (playerController == null)
            return;

        if (!enabled)
        {
            previousPlayerCanMove = playerController.playerCanMove;
            previousCameraCanMove = playerController.cameraCanMove;
            playerController.playerCanMove = false;
            playerController.cameraCanMove = false;
            Rigidbody body = playerController.GetComponent<Rigidbody>();
            if (body != null)
                body.linearVelocity = Vector3.zero;
        }
        else
        {
            playerController.playerCanMove = previousPlayerCanMove;
            playerController.cameraCanMove = previousCameraCanMove;
        }
    }

    private void EnsureResultUI()
    {
        if (resultOverlay != null && resultText != null)
            return;

        GameObject canvasObject = new GameObject("Monster Result Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject overlayObject = new GameObject("Black Overlay", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        overlayObject.transform.SetParent(canvasObject.transform, false);
        RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        Image overlayImage = overlayObject.GetComponent<Image>();
        overlayImage.color = Color.black;
        overlayImage.raycastTarget = false;
        resultOverlay = overlayObject.GetComponent<CanvasGroup>();
        resultOverlay.interactable = false;
        resultOverlay.blocksRaycasts = false;

        GameObject textObject = new GameObject("Result Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(overlayObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.15f, 0.25f);
        textRect.anchorMax = new Vector2(0.85f, 0.75f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        resultText = textObject.GetComponent<Text>();
        resultText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        resultText.fontSize = 42;
        resultText.alignment = TextAnchor.MiddleCenter;
        resultText.color = Color.white;
        resultText.raycastTarget = false;
    }

    private IEnumerator FadeOverlay(float from, float to)
    {
        yield return FadeOverlay(from, to, fadeDuration);
    }

    private IEnumerator FadeOverlay(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            resultOverlay.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            resultOverlay.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        resultOverlay.alpha = to;
    }

    private static IEnumerator WaitRealtime(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void SetOverlayImmediate(float alpha, bool active)
    {
        resultOverlay.alpha = alpha;
        resultText.enabled = false;
        resultOverlay.gameObject.SetActive(active);
    }

    private static Vector3 Abs(Vector3 value)
    {
        return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
    }

    private void OnDrawGizmosSelected()
    {
        Matrix4x4 previousMatrix = Gizmos.matrix;
        if (diningArea != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.matrix = Matrix4x4.TRS(diningArea.TransformPoint(diningAreaCenter), diningArea.rotation,
                Abs(diningArea.lossyScale));
            Gizmos.DrawWireCube(Vector3.zero, diningAreaHalfExtents * 2f);
        }

        Gizmos.color = Color.cyan;
        if (burgerPartSpawnArea != null)
        {
            Gizmos.matrix = Matrix4x4.TRS(
                burgerPartSpawnArea.TransformPoint(burgerPartSpawnCenter),
                burgerPartSpawnArea.rotation,
                Abs(burgerPartSpawnArea.lossyScale));
        }
        else
        {
            Gizmos.matrix = Matrix4x4.TRS(burgerPartSpawnCenter, Quaternion.identity, Vector3.one);
        }
        Gizmos.DrawWireCube(Vector3.zero, Abs(burgerPartSpawnSize));
        Gizmos.matrix = previousMatrix;
    }
}
