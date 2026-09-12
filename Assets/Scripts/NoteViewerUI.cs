using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Displays note art over a manually-created dimmed full-screen UI.</summary>
public class NoteViewerUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup rootCanvasGroup;
    [SerializeField] private Image dimmedBackground;
    [SerializeField] private Image noteImage;
    [SerializeField] private Color backgroundColor = new Color(0.16f, 0.16f, 0.16f, 0.72f);

    public bool IsOpen { get; private set; }
    public ReadableNote CurrentNote { get; private set; }

    public event Action<ReadableNote> NoteOpened;
    public event Action<ReadableNote> NoteClosed;

    private void Awake()
    {
        if (rootCanvasGroup == null)
            rootCanvasGroup = GetComponent<CanvasGroup>();
        if (dimmedBackground != null)
            dimmedBackground.color = backgroundColor;
        HideImmediate();
    }

    /// <summary>
    /// Allows the active player to initialize a viewer placed below a disabled
    /// Canvas. This keeps the Canvas hidden by alpha instead of disabling the
    /// hierarchy that owns this component.
    /// </summary>
    public void PrepareForRuntime()
    {
        Canvas parentCanvas = GetComponentInParent<Canvas>(true);
        if (parentCanvas != null && !parentCanvas.gameObject.activeSelf)
            parentCanvas.gameObject.SetActive(true);

        if (rootCanvasGroup == null)
            rootCanvasGroup = GetComponent<CanvasGroup>();
        if (dimmedBackground != null)
            dimmedBackground.color = backgroundColor;
        HideImmediate();
    }

    public bool Show(ReadableNote note)
    {
        if (note == null || note.ContentImage == null || rootCanvasGroup == null || noteImage == null)
        {
            Debug.LogWarning("NoteViewerUI is missing a note image or UI reference.", this);
            return false;
        }

        CurrentNote = note;
        noteImage.sprite = note.ContentImage;
        noteImage.preserveAspect = true;
        noteImage.enabled = true;
        rootCanvasGroup.alpha = 1f;
        rootCanvasGroup.interactable = false;
        rootCanvasGroup.blocksRaycasts = false;
        IsOpen = true;
        NoteOpened?.Invoke(note);
        return true;
    }

    public void Close()
    {
        if (!IsOpen)
            return;

        ReadableNote closedNote = CurrentNote;
        HideImmediate();
        NoteClosed?.Invoke(closedNote);
    }

    private void HideImmediate()
    {
        IsOpen = false;
        CurrentNote = null;
        if (noteImage != null)
        {
            noteImage.enabled = false;
            noteImage.sprite = null;
        }
        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = 0f;
            rootCanvasGroup.interactable = false;
            rootCanvasGroup.blocksRaycasts = false;
        }
    }
}
