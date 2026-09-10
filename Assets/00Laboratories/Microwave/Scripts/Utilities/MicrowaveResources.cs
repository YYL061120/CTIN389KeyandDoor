using UnityEngine;

namespace OOLaboratories.Microwave
{
    [CreateAssetMenu(fileName = "MicrowaveResources", menuName = "ScriptableObjects/MicrowaveResources", order = 1)]
    public class MicrowaveResources : ScriptableObject
    {
        private static MicrowaveResources s_Instance;

        /// <summary>Gets the singleton microwave resources instance or creates it.</summary>
        public static MicrowaveResources Instance
        {
            get
            {
                // if known, immediately return the instance.
                if (s_Instance) return s_Instance;

                // load the microwave resources from the resources directory.
                LoadResources();

                return s_Instance;
            }
        }

        /// <summary>
        /// Before the first scene loads, we access the instance property to load all resources.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void BeforeSceneLoad()
        {
            // load the microwave resources from the resources directory.
            LoadResources();
        }

        /// <summary>Loads the shape editor resources from the resources directory.</summary>
        private static void LoadResources()
        {
            s_Instance = (MicrowaveResources)Resources.Load("MicrowaveResources");
        }

        public Texture2D character0;
        public Texture2D character1;
        public Texture2D character2;
        public Texture2D character3;
        public Texture2D character4;
        public Texture2D character5;
        public Texture2D character6;
        public Texture2D character7;
        public Texture2D character8;
        public Texture2D character9;
        public Texture2D characterColumn;

        public AudioClip beepCommand01;
        public AudioClip beepCommand02;
        public AudioClip cookingBegin;
        public AudioClip cookingLoop;
        public AudioClip cookingPause;
        public AudioClip cookingFinish;
        public AudioClip cookingOpen;
        public AudioClip doorClose01;
        public AudioClip doorClose02;
        public AudioClip doorClose03;
        public AudioClip doorOpen;

        public Material microwaveGuiMaterial;

        private static Material _temporaryGuiMaterial;

        public static Material temporaryGuiMaterial
        {
            get
            {
                if (!_temporaryGuiMaterial)
                    _temporaryGuiMaterial = new Material(Instance.microwaveGuiMaterial);
                return _temporaryGuiMaterial;
            }
        }

        public static Texture2D GetCharacterTexture(char character)
        {
            var resources = Instance;

            switch (character)
            {
                case '0': return resources.character0;
                case '1': return resources.character1;
                case '2': return resources.character2;
                case '3': return resources.character3;
                case '4': return resources.character4;
                case '5': return resources.character5;
                case '6': return resources.character6;
                case '7': return resources.character7;
                case '8': return resources.character8;
                case '9': return resources.character9;
                case ':': return resources.characterColumn;
            }

            return resources.character0;
        }
    }
}