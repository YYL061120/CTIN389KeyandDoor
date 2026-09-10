using UnityEngine;

namespace OOLaboratories.Microwave
{
    public static class MicrowaveComponents
    {
        public static AudioSource AddAudioSource(GameObject gameObject)
        {
            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.maxDistance = 40.0f;
            audioSource.dopplerLevel = 0.0f;
            audioSource.playOnAwake = false;
            return audioSource;
        }
    }
}