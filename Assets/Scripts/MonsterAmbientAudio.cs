using UnityEngine;

/// <summary>Looping 3D monster ambience whose volume falls with distance.</summary>
public class MonsterAmbientAudio : MonoBehaviour
{
    [SerializeField] private AudioClip roarLoop;
    [SerializeField] private AudioSource audioSource;
    [Range(0f, 1f)] [SerializeField] private float volume = 0.65f;
    [Min(0.01f)] [SerializeField] private float minDistance = 2f;
    [Min(0.1f)] [SerializeField] private float maxDistance = 24f;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.clip = roarLoop;
        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.volume = volume;
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        audioSource.dopplerLevel = 0f;
    }

    private void Start()
    {
        if (roarLoop != null)
            audioSource.Play();
    }

    private void OnValidate()
    {
        minDistance = Mathf.Max(0.01f, minDistance);
        maxDistance = Mathf.Max(minDistance, maxDistance);
    }
}
