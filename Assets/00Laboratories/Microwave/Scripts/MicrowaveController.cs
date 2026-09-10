using UnityEngine;

namespace OOLaboratories.Microwave
{
    public class MicrowaveController : MonoBehaviour
    {
        #region Microwave Plate

        /// <summary>The microwave plate transform.</summary>
        private Transform microwavePlate;

        /// <summary>The plate needs time to spin to full speed and stop.</summary>
        private float plateVelocity = 0.0f;

        /// <summary>Updates the microwave plate animation.</summary>
        private void UpdatePlate()
        {
            // the plate will not turn when the door is open or when paused.
            bool isPlateTurning = isCooking && !isPaused && !isDoorOpen;

            plateVelocity = Mathf.Clamp01(plateVelocity + (isPlateTurning ? 8f : -3f) * Time.deltaTime);
            if (plateVelocity <= 0f) return;

            // the widely used speed in domestic microwave ovens is 5 rpm. this is a good choice for
            // improving the temperature uniformity with high energy efficiency.
            microwavePlate.Rotate(new Vector3(0f, (360f / 12f) * plateVelocity * Time.deltaTime, 0f));
        }

        #endregion Microwave Plate

        #region Microwave Fan

        /// <summary>The microwave fan transform.</summary>
        private Transform microwaveFan;

        /// <summary>The fan needs time to spin to full speed and stop.</summary>
        private float fanVelocity = 0.0f;

        /// <summary>Updates the microwave fan animation.</summary>
        private void UpdateFan()
        {
            fanVelocity = Mathf.Clamp01(fanVelocity + (isCooking ? 2f : -1f) * Time.deltaTime);
            if (fanVelocity <= 0f) return;

            // the actual fan would be around 1800 rpm but that won't work due to the shutter speed
            // of the frame rate, here we just use an artistic design decision.
            microwaveFan.Rotate(new Vector3(0f, 0f, -(360f * 2f) * fanVelocity * Time.deltaTime));
        }

        #endregion Microwave Fan

        #region Microwave Light

        /// <summary>The light needs time to be fully on and off.</summary>
        private float lightVelocity = 0.0f;

        /// <summary>Updates the microwave light.</summary>
        private void UpdateLight()
        {
            float lightTarget;

            if (isCooking)
            {
                if (isDoorOpen)
                {
                    lightTarget = 0.1f;
                }
                else
                {
                    lightTarget = 1.0f;
                }
            }
            else
            {
                if (isDoorOpen)
                {
                    lightTarget = 0.1f;
                }
                else
                {
                    lightTarget = 0.0f;
                }
            }

            lightVelocity = Mathf.Lerp(lightVelocity, lightTarget, Time.fixedDeltaTime * 20.0f);

            var lightColor = materialInstance.GetColor("_LightColor");
            lightColor.a = lightVelocity;
            materialInstance.SetColor("_LightColor", lightColor);
        }

        #endregion Microwave Light

        #region Microwave Door

        /// <summary>Whether the door was open a moment ago.</summary>
        private bool wasDoorOpen;

        /// <summary>The instantiated microwave material.</summary>
        private Material materialInstance;

        /// <summary>The microwave door transform.</summary>
        private Transform microwaveDoor;

        private void UpdateDoor()
        {
            // keep the door closed without self intersecting.
            if (doorAngle > 230f)
                doorAngle = 0f;

            // the door has just been opened:
            if (!wasDoorOpen && isDoorOpen)
            {
                wasDoorOpen = true;
                OnDoorOpen();
            }

            // the door has just been closed:
            if (wasDoorOpen && !isDoorOpen)
            {
                wasDoorOpen = false;
                OnDoorClose();
            }
        }

        /// <summary>Called when the microwave door has been opened.</summary>
        private void OnDoorOpen()
        {
            PlayDoorOpenSound();
            isPaused = true;
        }

        /// <summary>Called when the microwave door has been closed.</summary>
        private void OnDoorClose()
        {
            PlayDoorCloseSound();
        }

        #endregion Microwave Door

        #region Microwave Display

        private RenderTexture renderTexture;
        private float renderTextureWidth;
        private float renderTextureHeight;
        private string displayLastTime = "";
        private string displayCurrentTime = "";

        private static readonly Color colorDisplayLeds = new Color(0.149f, 0.192f, 0.533f);

        /// <summary>Updates the microwave display.</summary>
        private void UpdateDisplay()
        {
            if (!renderTexture) return;

            if (isCooking)
            {
                var minutes = secondsRemaining / 60;
                var seconds = secondsRemaining % 60;
                displayCurrentTime = minutes.ToString("D2") + ":" + seconds.ToString("D2");
            }
            else
            {
                displayCurrentTime = System.DateTime.Now.Hour.ToString("D2") + ":" + System.DateTime.Now.Minute.ToString("D2");
            }

            // only redraw the screen when required.
            if (displayCurrentTime != displayLastTime)
            {
                displayLastTime = displayCurrentTime;

                renderTextureWidth = renderTexture.width;
                renderTextureHeight = renderTexture.height;
                Graphics.SetRenderTarget(renderTexture);

                // draw everything to the render texture.
                GL.Clear(true, true, Color.black);
                GL.PushMatrix();
                GL.LoadPixelMatrix(renderTextureWidth, 0f, 0f, renderTextureHeight);

                DisplayDrawTime();

                GL.PopMatrix();
                Graphics.SetRenderTarget(null);
            }
        }

        private void DisplayDrawTime()
        {
            var xpos = 10;
            for (int i = 0; i < displayCurrentTime.Length; i++)
            {
                GLUtilities.DrawGuiTextured(MicrowaveResources.GetCharacterTexture(displayCurrentTime[i]), () =>
                {
                    GLUtilities.DrawFlippedUvRectangle(xpos, 20, 38, 60, colorDisplayLeds);
                    xpos += 49;
                });
            }
        }

        #endregion Microwave Display

        #region Microwave Logic

        private float logicTimer = 0f;

        private void UpdateLogic()
        {
            // having the door open will also pause the program.
            if (!isPaused)
            {
                // decrease the seconds remaining every second.
                logicTimer += Time.deltaTime;
                if (logicTimer > 1f)
                {
                    logicTimer -= 1f;
                    if (secondsRemaining == 1)
                        OnMicrowaveFinished();

                    if (--secondsRemaining < 0)
                        secondsRemaining = 0;
                }
            }
        }

        private void AddTime(int seconds)
        {
            secondsRemaining += seconds;

            // cannot exceed 99:59.
            var ninetynineMinutes = 60 * 99 + 59;
            if (secondsRemaining > ninetynineMinutes)
                secondsRemaining = ninetynineMinutes;
        }

        private void OnMicrowaveFinished()
        {
            PlayCookingFinishedSound();
        }

        #endregion Microwave Logic

        #region Microwave Sound

        private AudioSource oneShotAudioSource;

        private AudioSource cookingSingleAudioSource;
        private AudioSource cookingLoopAudioSource;

        private AudioClip[] beepClips;
        private AudioClip lastBeepClip;

        private AudioClip[] doorCloseClips;
        private AudioClip lastDoorCloseClip;

        private int cookingSoundState = 0;
        private bool cookingLoopSoundEnabled = false;

        private void UpdateSound()
        {
            // default idle state.
            if (cookingSoundState == 0)
            {
                // the microwave is cooking and not paused with the door closed.
                if (isCooking && !isPaused && !isDoorOpen)
                {
                    // play the cooking begin sound.
                    PlayCookingBeginSound();

                    // go to next state.
                    cookingSoundState = 1;
                }
            }

            // waiting for cooking begin sound to finish.
            if (cookingSoundState == 1)
            {
                // if the cooking is interrupted:
                if (!isCooking || isPaused || isDoorOpen)
                {
                    // stop the cooking begin sound (but not finish).
                    if (cookingSingleAudioSource.clip != MicrowaveResources.Instance.cookingFinish)
                        cookingSingleAudioSource.Stop();

                    // go to idle state.
                    cookingSoundState = 0;
                }

                // after 3 seconds we enable the loop.
                if (cookingSingleAudioSource.time > 3f)
                {
                    // fade in the cooking loop sound.
                    cookingLoopSoundEnabled = true;

                    // go to next state.
                    cookingSoundState = 2;
                }
            }

            // the cooking loop is currently playing.
            if (cookingSoundState == 2)
            {
                // if the cooking is interrupted:
                if (!isCooking || isPaused || isDoorOpen)
                {
                    // stop the cooking loop sound.
                    cookingLoopSoundEnabled = false;

                    // go to idle state.
                    cookingSoundState = 0;
                }
            }

            // update the cooking loop sound volume.
            cookingLoopAudioSource.volume = Mathf.Lerp(cookingLoopAudioSource.volume, cookingLoopSoundEnabled ? 0.5f : 0.0f, Time.deltaTime * 10f);
        }

        private void PlayCookingBeginSound()
        {
            cookingSingleAudioSource.Play(MicrowaveResources.Instance.cookingBegin, 0.5f);
        }

        private void PlayCookingPauseSound()
        {
            oneShotAudioSource.Play(MicrowaveResources.Instance.cookingPause, 0.5f);
        }

        private void PlayCookingFinishedSound()
        {
            cookingSingleAudioSource.Play(MicrowaveResources.Instance.cookingFinish, 0.5f);
        }

        private void PlayCommandBeepSound()
        {
            lastBeepClip = beepClips.RandomExcept(lastBeepClip);
            oneShotAudioSource.PlayOneShot(lastBeepClip, 0.5f);
        }

        private void PlayDoorOpenSound()
        {
            // while cooking and not paused we have a special recording.
            if (isCooking && !isPaused)
            {
                oneShotAudioSource.Play(MicrowaveResources.Instance.cookingOpen, 0.45f);
            }
            else
            {
                oneShotAudioSource.PlayOneShot(MicrowaveResources.Instance.doorOpen, 0.8f);
            }
        }

        private void PlayDoorCloseSound()
        {
            lastDoorCloseClip = doorCloseClips.RandomExcept(lastDoorCloseClip);
            oneShotAudioSource.PlayOneShot(lastDoorCloseClip, 0.8f);
        }

        #endregion Microwave Sound

        private void Awake()
        {
            // find the required components.
            var meshRenderer = GetComponent<MeshRenderer>();

            // get the microwave material.
            var microwaveMaterial = meshRenderer.sharedMaterial;

            // create an instance of the material so we don't change the original.
            materialInstance = new Material(microwaveMaterial);

            // assign the instance of all children using the microwave material.
            foreach (var childMeshRenderer in GetComponentsInChildren<MeshRenderer>())
                if (childMeshRenderer.sharedMaterial == microwaveMaterial)
                    childMeshRenderer.sharedMaterial = materialInstance;

            // assign the copy to the mesh renderer.
            meshRenderer.sharedMaterial = materialInstance;

            // create a render texture for the display.
            renderTexture = new RenderTexture(256, 99, 24, RenderTextureFormat.ARGB32);
            renderTexture.useMipMap = true;
            materialInstance.SetTexture("_DisplayTex", renderTexture);

            // find the microwave children by name.
            microwavePlate = transform.Find("Plate");
            microwaveFan = transform.Find("Fan");
            microwaveDoor = transform.Find("Door");

            // create audio sources.
            var resources = MicrowaveResources.Instance;
            beepClips = new AudioClip[] { resources.beepCommand01, resources.beepCommand02 };
            doorCloseClips = new AudioClip[] { resources.doorClose01, resources.doorClose02, resources.doorClose03 };

            oneShotAudioSource = MicrowaveComponents.AddAudioSource(gameObject);
            cookingSingleAudioSource = MicrowaveComponents.AddAudioSource(gameObject);
            cookingSingleAudioSource.volume = 0.6f;
            cookingLoopAudioSource = MicrowaveComponents.AddAudioSource(gameObject);
            cookingLoopAudioSource.clip = resources.cookingLoop;
            cookingLoopAudioSource.loop = true;
            cookingLoopAudioSource.volume = 0f;
            cookingLoopAudioSource.Play();

            // begin monitoring the door.
            wasDoorOpen = isDoorOpen;
            isPaused = wasDoorOpen;
        }

        private void Update()
        {
            UpdateDoor();
            UpdateLogic();

            UpdatePlate();
            UpdateFan();
            UpdateLight();

            UpdateDisplay();

            UpdateSound();
        }

        /// <summary>Gets or sets the angle of the door in degrees.</summary>
        public float doorAngle
        {
            get
            {
                return microwaveDoor.localRotation.eulerAngles.y;
            }
            set
            {
                microwaveDoor.localRotation = Quaternion.Euler(0f, value, 0f);
            }
        }

        /// <summary>Gets whether the door is currently open.</summary>
        public bool isDoorOpen
        {
            get
            {
                var angle = doorAngle; return angle > 2.0f && angle < 230f;
            }
        }

        /// <summary>Gets whether the microwave has been paused by the user.</summary>
        public bool isPaused { get; private set; }

        /// <summary>Gets the amount of seconds remaining while cooking.</summary>
        public int secondsRemaining { get; private set; }

        /// <summary>Gets whether the microwave is currently cooking.</summary>
        public bool isCooking
        {
            get
            {
                return secondsRemaining > 0;
            }
        }

        /// <summary>Starts the microwave or adds a minute to the cooking program.</summary>
        /// <param name="seconds">
        /// The time increments to be used, defaults to a minute. When the microwave is not
        /// currently running, this can also be used to immediately set a long timer like 3 minutes.
        /// </param>
        public void StartMicrowave(int seconds = 60)
        {
            // if the microwave is not cooking:
            if (!isCooking)
            {
                // and the door is open:
                if (isDoorOpen)
                {
                    // add 60 seconds:
                    PlayCommandBeepSound();
                    AddTime(seconds);
                }
                // and the door is closed:
                else
                {
                    // add 60 seconds:
                    AddTime(seconds);
                    // resume cooking:
                    isPaused = false;
                }
            }
            // if the microwave is cooking:
            else
            {
                // and the door is open:
                if (isDoorOpen)
                {
                    // add 60 seconds:
                    PlayCommandBeepSound();
                    AddTime(seconds);
                }
                // and the door is closed:
                else
                {
                    if (!isPaused)
                    {
                        // add 60 seconds:
                        PlayCommandBeepSound();
                        AddTime(seconds);
                    }

                    // resume cooking:
                    isPaused = false;
                }
            }
        }

        /// <summary>Pauses the microwave or stops it when pressed again.</summary>
        public void StopMicrowave()
        {
            // if the microwave is cooking:
            if (isCooking)
            {
                // and the door is open:
                if (isDoorOpen)
                {
                    // stop cooking.
                    PlayCommandBeepSound();
                    secondsRemaining = 0;
                }
                // and the door is closed:
                else
                {
                    // if not paused:
                    if (!isPaused)
                    {
                        // pause the cooking.
                        isPaused = true;
                        PlayCookingPauseSound();
                    }
                    // if already paused:
                    else
                    {
                        // stop cooking.
                        PlayCommandBeepSound();
                        secondsRemaining = 0;
                    }
                }
            }
        }
    }
}