using UnityEngine;

/// <summary>
/// Opens tagged notes when the center of the screen points at them within range.
/// Reading never disables player movement or camera control.
/// </summary>
public class PlayerNoteReader : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private NoteViewerUI noteViewer;
    [Min(0.1f)] [SerializeField] private float readingDistance = 3f;
    [SerializeField] private LayerMask readableLayers = ~0;
    [SerializeField] private string readableNoteTag = "Note";
    [SerializeField] private bool openAutomaticallyWhenAimedAt = true;
    [SerializeField] private KeyCode openKey = KeyCode.E;
    [SerializeField] private KeyCode closeKey = KeyCode.X;

    private ReadableNote suppressedUntilLookAway;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();
    }

    private void Start()
    {
        if (noteViewer == null)
            noteViewer = FindFirstObjectByType<NoteViewerUI>(FindObjectsInactive.Include);
        if (noteViewer != null)
            noteViewer.PrepareForRuntime();
    }

    private void Update()
    {
        if (noteViewer == null)
            return;

        if (noteViewer.IsOpen)
        {
            if (Input.GetKeyDown(closeKey))
            {
                suppressedUntilLookAway = noteViewer.CurrentNote;
                noteViewer.Close();
            }
            return;
        }

        ReadableNote aimedNote = FindAimedNote();
        if (aimedNote == null)
        {
            suppressedUntilLookAway = null;
            return;
        }

        if (aimedNote == suppressedUntilLookAway)
            return;

        if ((openAutomaticallyWhenAimedAt || Input.GetKeyDown(openKey)) && !noteViewer.Show(aimedNote))
            suppressedUntilLookAway = aimedNote;
    }

    private ReadableNote FindAimedNote()
    {
        if (playerCamera == null)
            return null;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, readingDistance, readableLayers,
                QueryTriggerInteraction.Collide))
            return null;

        ReadableNote note = hit.collider.GetComponentInParent<ReadableNote>();
        if (note == null)
            return null;

        bool correctTag = note.gameObject.tag == readableNoteTag
            || hit.collider.gameObject.tag == readableNoteTag;
        return correctTag ? note : null;
    }
}
