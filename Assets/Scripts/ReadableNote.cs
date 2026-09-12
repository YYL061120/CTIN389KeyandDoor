using UnityEngine;

/// <summary>Content carried by a world-space note object.</summary>
public class ReadableNote : MonoBehaviour
{
    [SerializeField] private Sprite contentImage;

    public Sprite ContentImage => contentImage;
}
