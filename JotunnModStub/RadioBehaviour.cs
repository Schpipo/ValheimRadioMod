using System.Collections.Generic;
using UnityEngine;

namespace ValheimRadioMod
{
    public class RadioBehaviour : MonoBehaviour, Hoverable, Interactable
    {
        private float hoverEndTime = 0f;

        public string CurrentStation = "";
        public bool IsOn = true;
        public float Volume = 0.8f;

        private AudioSource audioSource;
        private bool forceClipRefresh = false;

        private void Awake()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.maxDistance = 30f;
            audioSource.loop = false;
            audioSource.volume = Volume;
        }

        private void Start()
        {
            if (string.IsNullOrEmpty(CurrentStation) && AudioManager.Instance != null && AudioManager.Instance.Stations.Count > 0)
            {
                foreach (var station in AudioManager.Instance.Stations.Keys)
                {
                    CurrentStation = station;
                    break;
                }
            }
        }

        private void Update()
        {
            if (!IsOn || AudioManager.Instance == null)
            {
                if (audioSource.isPlaying)
                    audioSource.Stop();
                return;
            }

            var (clip, position) = AudioManager.Instance.GetCurrentPlayback(CurrentStation);

            if (clip == null) return;

            if (audioSource.clip != clip || forceClipRefresh)
            {
                audioSource.clip = clip;
                audioSource.time = position;
                audioSource.Play();
                forceClipRefresh = false;
            }
            else if (Mathf.Abs(audioSource.time - position) > 1.5f)
            {
                audioSource.time = position;
            }

            audioSource.volume = Volume;

            // Apply scroll only to this radio if it was recently hovered
            if (Time.time < hoverEndTime)
            {
                float scroll = MouseScrollPatch.LastScrollDelta;
                if (scroll != 0f)
                {
                    Volume = Mathf.Clamp01(Volume + scroll * 0.5f);
                    MouseScrollPatch.LastScrollDelta = 0f;
                }
            }
        }

        public string GetHoverText()
        {
            hoverEndTime = Time.time + 0.2f;

            string status = IsOn ? "An" : "Aus";
            return $"Radio ({status})\n" +
                   $"[<color=yellow><b>E</b></color>] An/Aus\n" +
                   $"[<color=yellow><b>Shift + E</b></color>] Sender wechseln\n" +
                   $"[<color=yellow><b>Mausrad</b></color>] Lautstärke ({Volume:P0})";
        }

        public string GetHoverName() => "Radio";
        public float GetHoverOffset() => 1.5f;

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold) return false;

            if (alt)
            {
                CycleStation();
                forceClipRefresh = true;
            }
            else
            {
                IsOn = !IsOn;
                if (IsOn) forceClipRefresh = true;
            }
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

        private void CycleStation()
        {
            if (AudioManager.Instance == null || AudioManager.Instance.Stations.Count == 0) return;

            var stations = new List<string>(AudioManager.Instance.Stations.Keys);
            int index = stations.IndexOf(CurrentStation);
            CurrentStation = stations[(index + 1) % stations.Count];
        }
    }
}