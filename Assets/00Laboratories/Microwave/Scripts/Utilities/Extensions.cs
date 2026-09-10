using UnityEngine;

namespace OOLaboratories.Microwave
{
    public static class Extensions
    {
        /// <summary>
        /// Assigns an AudioClip to the AudioSource, plays it and scales the AudioSource volume by volumeScale.
        /// </summary>
        public static void Play(this AudioSource audioSource, AudioClip clip, float volumeScale)
        {
            audioSource.volume = volumeScale;
            audioSource.clip = clip;
            audioSource.Play();
        }

        /// <summary>Returns a random number between min and max (exclusive) with an exception.</summary>
        public static int RandomExcept(int min, int max, int except)
        {
            if (min == max) return min;
            if (min == max - 1) return min;
            int result = Random.Range(min, max - 1);
            if (result >= except) result += 1;
            return result;
        }

        /// <summary>Returns an item from the given array with an exception.</summary>
        public static T RandomExcept<T>(this T[] array, T except)
        {
            if (array.Length == 0) return default(T);
            int index = System.Array.IndexOf(array, except);
            if (index == -1) return array[Random.Range(0, array.Length)];
            return array[RandomExcept(0, array.Length, index)];
        }
    }
}