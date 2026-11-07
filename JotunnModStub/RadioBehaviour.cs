using System.Collections.Generic;
using UnityEngine;

namespace ValheimRadioMod
{
    public class RadioBehaviour : MonoBehaviour, Hoverable, Interactable, TextReceiver
    {
        private static readonly HashSet<RadioBehaviour> AllRadios = new HashSet<RadioBehaviour>();
        private static readonly HashSet<RadioBehaviour> HoveredRadios = new HashSet<RadioBehaviour>();

        public static bool IsAnyHovered => HoveredRadios.Count > 0;

        private float hoverEndTime = 0f;

        public string CurrentStation = "";
        public bool IsOn = true;
        public float Volume = 0.8f;
        public string Group = "";          // neu

        private AudioSource audioSource;
        private bool forceClipRefresh = false;
        private ZNetView znetView;

        private void Awake()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.maxDistance = 30f;
            audioSource.loop = false;
            audioSource.volume = Volume;

            znetView = GetComponent<ZNetView>();
            AllRadios.Add(this);

            // Shader-Fix (wie bisher)
            Shader pieceShader = Shader.Find("Custom/Piece");
            if (pieceShader != null)
            {
                foreach (var renderer in GetComponentsInChildren<Renderer>(true))
                {
                    Material[] mats = renderer.materials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        if (mats[i] != null)
                            mats[i].shader = pieceShader;
                    }
                    renderer.materials = mats;
                }
            }
        }

        private void Start()
        {
            LoadFromZDO();

            if (string.IsNullOrEmpty(CurrentStation) && AudioManager.Instance != null && AudioManager.Instance.StationNames.Count > 0)
            {
                foreach (var station in AudioManager.Instance.StationNames)
                {
                    CurrentStation = station;
                    break;
                }
                SaveToZDO();
            }
        }

        private void OnDestroy()
        {
            AllRadios.Remove(this);
            HoveredRadios.Remove(this);
        }

        private void LoadFromZDO()
        {
            if (znetView == null || !znetView.IsValid()) return;

            CurrentStation = znetView.GetZDO().GetString("station", CurrentStation);
            IsOn = znetView.GetZDO().GetBool("on", IsOn);
            Volume = znetView.GetZDO().GetFloat("volume", Volume);
            Group = znetView.GetZDO().GetString("group", Group);
        }

        private void SaveToZDO()
        {
            if (znetView == null || !znetView.IsValid() || !znetView.IsOwner()) return;

            znetView.GetZDO().Set("station", CurrentStation);
            znetView.GetZDO().Set("on", IsOn);
            znetView.GetZDO().Set("volume", Volume);
            znetView.GetZDO().Set("group", Group);
        }

        private void Update()
        {
            if (Time.time >= hoverEndTime)
                HoveredRadios.Remove(this);

            if (Time.time < hoverEndTime && ZInput.GetKeyDown(KeyCode.G))
            {
                OpenGroupRename();
                return; // verhindert, dass G weitergeht
            }
            // ZDO-Änderungen von anderen Clients übernehmen
            if (znetView != null && znetView.IsValid())
                LoadFromZDO();

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
            else if (Mathf.Abs(audioSource.time - position) > 0.4f)
            {
                audioSource.time = position;
            }

            audioSource.volume = Volume;

            if (Time.time < hoverEndTime)
            {
                float scroll = MouseScrollPatch.LastScrollDelta;
                if (scroll != 0f)
                {
                    SetVolume(Volume + scroll * 0.05f);
                    MouseScrollPatch.LastScrollDelta = 0f;
                }
            }
            
        }
        private void OpenGroupRename()
        {
            TextInput.instance.RequestText(this, "$piece_portal_tag", 20); // 20 Zeichen Limit, Titel wie bei Portalen
        }

        public string GetText()
        {
            return Group ?? "";
        }

        public void SetText(string text)
        {
            SetGroup(text.Trim());
        }
        public string GetHoverText()
        {
            hoverEndTime = Time.time + 0.2f;
            HoveredRadios.Add(this);

            string status = IsOn ? "An" : "Aus";
            string trackInfo = "";
            string groupInfo = string.IsNullOrEmpty(Group) ? "" : $"\nGruppe: {Group}";

            if (IsOn && AudioManager.Instance != null && !string.IsNullOrEmpty(CurrentStation))
            {
                var (clip, _) = AudioManager.Instance.GetCurrentPlayback(CurrentStation);
                if (clip != null)
                {
                    string cleanName = CleanTrackName(clip.name);
                    trackInfo = $"\nSender: {CurrentStation}\nTrack: {cleanName}";
                }
            }

            return $"Radio ({status}){groupInfo}{trackInfo}\n" +
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
            }
            else
            {
                SetOn(!IsOn);
            }
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

        private void CycleStation()
        {
            if (AudioManager.Instance == null || AudioManager.Instance.StationNames.Count == 0) return;

            var stations = new List<string>(AudioManager.Instance.StationNames);
            int index = stations.IndexOf(CurrentStation);
            string newStation = stations[(index + 1) % stations.Count];

            SetStation(newStation);
        }

        // ---------- Gruppen-Sync ----------

        public void SetOn(bool value)
        {
            IsOn = value;
            forceClipRefresh = true;
            SaveToZDO();
            SyncGroup();
        }

        public void SetVolume(float value)
        {
            Volume = Mathf.Clamp01(value);
            SaveToZDO();
            SyncGroup();
        }

        public void SetStation(string station)
        {
            CurrentStation = station;
            forceClipRefresh = true;
            SaveToZDO();
            SyncGroup();
        }

        public void SetGroup(string group)
        {
            Group = group ?? "";
            SaveToZDO();
        }

        private void SyncGroup()
        {
            if (string.IsNullOrEmpty(Group)) return;

            foreach (var radio in AllRadios)
            {
                if (radio == this || radio.Group != Group) continue;

                radio.IsOn = IsOn;
                radio.Volume = Volume;
                radio.CurrentStation = CurrentStation;
                radio.forceClipRefresh = true;
                radio.SaveToZDO();
            }
        }

        private string CleanTrackName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            var match = System.Text.RegularExpressions.Regex.Match(name, @"^\d+[\s\.\-_]+");
            return match.Success ? name.Substring(match.Length).Trim() : name;
        }
    }
}