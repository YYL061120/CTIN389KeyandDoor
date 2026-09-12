using System;
using UnityEngine;

/// <summary>
/// Owns game-time progression, the visible hour hand, and its two audio layers.
/// Other systems subscribe to its events instead of maintaining separate timers.
/// </summary>
public class ClockController : MonoBehaviour
{
    [Header("Time")]
    [Min(0.01f)] [SerializeField] private float realSecondsPerGameHour = 5f;
    [Range(0, 23)] [SerializeField] private int startingHour;
    [Tooltip("Monster checks after one complete 12-hour rotation.")]
    [Min(1)] [SerializeField] private int monsterCheckIntervalHours = 12;
    [SerializeField] private bool timeRunning = true;

    [Header("Hour Hand")]
    [SerializeField] private Transform hourHand;
    [Tooltip("Used only when Hour Hand is not assigned. This prefab's short hand is Arrow_3.")]
    [SerializeField] private string hourHandChildName = "Arrow_3";
    [SerializeField] private Vector3 localRotationAxis = Vector3.forward;
    [SerializeField] private bool clockwise = true;
    [SerializeField] private bool moveInDiscreteHourSteps = true;

    [Header("Clock Audio")]
    [Tooltip("Quiet looping mechanical ticking ambience.")]
    [SerializeField] private AudioClip ambientTickingLoop;
    [Tooltip("Louder single tick/chime played whenever a new hour begins.")]
    [SerializeField] private AudioClip newHourTick;
    [Range(0f, 1f)] [SerializeField] private float ambientTickingVolume = 0.08f;
    [Range(0f, 1f)] [SerializeField] private float newHourTickVolume = 0.35f;
    [Range(0f, 1f)] [SerializeField] private float audioSpatialBlend = 0.25f;
    [Min(0.01f)] [SerializeField] private float audioMinDistance = 1.5f;
    [Min(0.1f)] [SerializeField] private float audioMaxDistance = 12f;
    [SerializeField] private AudioSource ambientTickingSource;
    [SerializeField] private AudioSource newHourTickSource;

    private float elapsedInCurrentHour;
    private Quaternion handStartingRotation;

    public int CurrentHour { get; private set; }
    public int TotalElapsedHours { get; private set; }
    public float RealSecondsPerGameHour => realSecondsPerGameHour;
    public float HourProgress => elapsedInCurrentHour / realSecondsPerGameHour;
    public float SecondsIntoCurrentHour => elapsedInCurrentHour;
    public int CurrentDialHour => CurrentHour % 12;
    public Transform HourHand => hourHand;
    public bool IsPaused { get; private set; }

    public event Action<int, int> HourAdvanced;
    public event Action<int> MonsterCheckReached;

    private void Awake()
    {
        ResolveHourHand();
        ResolveAudioSources();
        CurrentHour = startingHour;
        if (hourHand != null)
        {
            handStartingRotation = hourHand.localRotation;
            UpdateHourHand();
        }
    }

    private void Start()
    {
        if (timeRunning && !IsPaused && ambientTickingSource != null
            && ambientTickingLoop != null && !ambientTickingSource.isPlaying)
            ambientTickingSource.Play();
    }

    private void Update()
    {
        if (!timeRunning || IsPaused)
            return;

        elapsedInCurrentHour += Time.deltaTime;
        while (elapsedInCurrentHour >= realSecondsPerGameHour)
        {
            elapsedInCurrentHour -= realSecondsPerGameHour;
            AdvanceOneHour();
        }

        if (!moveInDiscreteHourSteps)
            UpdateHourHand();
    }

    public void SetPaused(bool paused)
    {
        IsPaused = paused;
        RefreshAmbientAudioState();
    }

    public void SetTimeRunning(bool running)
    {
        timeRunning = running;
        RefreshAmbientAudioState();
    }

    public void SetHourHandVisible(bool visible)
    {
        if (hourHand != null)
            hourHand.gameObject.SetActive(visible);
    }

    /// <summary>
    /// Time from now to the next hour boundary, plus one complete game hour.
    /// Example with five-second hours: starting at t=23 finishes at t=30 (7s).
    /// </summary>
    public float GetCookingDurationFromCurrentPhase()
    {
        float untilNearestBoundary = elapsedInCurrentHour <= 0.001f
            ? 0f
            : realSecondsPerGameHour - elapsedInCurrentHour;
        return untilNearestBoundary + realSecondsPerGameHour;
    }

    public int GetCookingCompletionHourFromCurrentPhase()
    {
        int hoursAdvanced = elapsedInCurrentHour <= 0.001f ? 1 : 2;
        return (CurrentHour + hoursAdvanced) % 24;
    }

    /// <summary>Useful for debug controls and future clock-anchor mechanics.</summary>
    public void AdvanceOneHour()
    {
        TotalElapsedHours++;
        CurrentHour = (startingHour + TotalElapsedHours) % 24;
        UpdateHourHand();
        if (newHourTickSource != null && newHourTick != null)
            newHourTickSource.PlayOneShot(newHourTick, newHourTickVolume);
        HourAdvanced?.Invoke(CurrentHour, TotalElapsedHours);

        if (monsterCheckIntervalHours > 0 && TotalElapsedHours % monsterCheckIntervalHours == 0)
            MonsterCheckReached?.Invoke(TotalElapsedHours);
    }

    private void ResolveHourHand()
    {
        if (hourHand != null)
            return;

        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.name == hourHandChildName)
            {
                hourHand = child;
                return;
            }
        }
        Debug.LogWarning($"ClockController could not find hour hand '{hourHandChildName}'. Assign it in the Inspector.", this);
    }

    private void ResolveAudioSources()
    {
        if (ambientTickingLoop != null && ambientTickingSource == null)
            ambientTickingSource = gameObject.AddComponent<AudioSource>();
        if (newHourTick != null && newHourTickSource == null)
            newHourTickSource = gameObject.AddComponent<AudioSource>();

        ConfigureAudioSource(ambientTickingSource, true, ambientTickingVolume);
        ConfigureAudioSource(newHourTickSource, false, 1f);
        if (ambientTickingSource != null)
            ambientTickingSource.clip = ambientTickingLoop;
    }

    private void ConfigureAudioSource(AudioSource source, bool loop, float volume)
    {
        if (source == null)
            return;
        source.playOnAwake = false;
        source.loop = loop;
        source.volume = volume;
        source.spatialBlend = audioSpatialBlend;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = audioMinDistance;
        source.maxDistance = audioMaxDistance;
        source.dopplerLevel = 0f;
    }

    private void RefreshAmbientAudioState()
    {
        if (ambientTickingSource == null || ambientTickingLoop == null)
            return;

        if (timeRunning && !IsPaused)
        {
            ambientTickingSource.UnPause();
            if (!ambientTickingSource.isPlaying)
                ambientTickingSource.Play();
        }
        else if (ambientTickingSource.isPlaying)
        {
            ambientTickingSource.Pause();
        }
    }

    private void UpdateHourHand()
    {
        if (hourHand == null)
            return;

        float displayedHour = CurrentHour % 12;
        if (!moveInDiscreteHourSteps)
            displayedHour += HourProgress;

        float direction = clockwise ? -1f : 1f;
        hourHand.localRotation = handStartingRotation
            * Quaternion.AngleAxis(direction * displayedHour * 30f, localRotationAxis.normalized);
    }

    private void OnValidate()
    {
        realSecondsPerGameHour = Mathf.Max(0.01f, realSecondsPerGameHour);
        monsterCheckIntervalHours = Mathf.Max(1, monsterCheckIntervalHours);
        audioMinDistance = Mathf.Max(0.01f, audioMinDistance);
        audioMaxDistance = Mathf.Max(audioMinDistance, audioMaxDistance);
        if (localRotationAxis.sqrMagnitude < 0.001f)
            localRotationAxis = Vector3.forward;
    }
}
