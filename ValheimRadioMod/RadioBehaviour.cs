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
        public float Volume = 0.5f;
        public string Group = "";

        private AudioSource audioSource;
        private bool forceClipRefresh = false;
        private ZNetView znetView;
        private float ePressTime = -1f;
        private const float LongPressDuration = 0.6f;

        private bool wasOn = false;
        private bool initialized = false;

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
            wasOn = IsOn;
            initialized = true;

            if (string.IsNullOrEmpty(CurrentStation) && AudioManager.Instance != null && AudioManager.Instance.StationNames.Count > 0)
            {
                foreach (var station in AudioManager.Instance.StationNames)
                {
                    CurrentStation = station;
                    break;
                }
                SaveToZDO();
            }

            if (IsOn)
                forceClipRefresh = true;
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

            audioSource.mute = Time.timeScale == 0f;

            if (Time.time < hoverEndTime && !TextInput.IsVisible())
            {
                if (ZInput.GetKeyDown(KeyCode.E))
                    ePressTime = Time.time;

                if (ZInput.GetKey(KeyCode.E) && ePressTime > 0f)
                {
                    if (Time.time - ePressTime >= LongPressDuration)
                    {
                        CycleStation();
                        ePressTime = -1f;
                    }
                }

                if (ZInput.GetKeyUp(KeyCode.E))
                {
                    if (ePressTime > 0f && Time.time - ePressTime < LongPressDuration)
                        SetOn(!IsOn);
                    ePressTime = -1f;
                }

                if (ZInput.GetKeyDown(KeyCode.E) && (ZInput.GetKey(KeyCode.LeftShift) || ZInput.GetKey(KeyCode.RightShift)))
                {
                    OpenGroupRename();
                    ePressTime = -1f;
                }
            }

            if (znetView != null && znetView.IsValid())
                LoadFromZDO();

            if (initialized && IsOn != wasOn)
            {
                forceClipRefresh = true;
                wasOn = IsOn;
            }

            if (!IsOn || AudioManager.Instance == null)
            {
                if (audioSource.isPlaying)
                    audioSource.Stop();
                return;
            }

            var (clip, position) = AudioManager.Instance.GetCurrentPlayback(CurrentStation);
            if (clip == null) return;

            if (audioSource.clip != clip || forceClipRefresh || !audioSource.isPlaying)
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
                    SetVolume(Volume + Mathf.Sign(scroll) * 0.05f);
                    MouseScrollPatch.LastScrollDelta = 0f;
                }
            }
        }

        private void OpenGroupRename()
        {
            TextInput.instance.RequestText(this, "$radio_group", 20);
        }

        public string GetText() => Group ?? "";

        public void SetText(string text) => SetGroup(text.Trim());

        public string GetHoverText()
        {
            hoverEndTime = Time.time + 0.2f;
            HoveredRadios.Add(this);

            string status = IsOn ? Localization.instance.Localize("$radio_on") : Localization.instance.Localize("$radio_off");
            string groupDisplay = string.IsNullOrEmpty(Group)
                ? Localization.instance.Localize("$radio_no_group")
                : Group;

            string trackInfo = "";
            if (IsOn && AudioManager.Instance != null && !string.IsNullOrEmpty(CurrentStation))
            {
                var (clip, _) = AudioManager.Instance.GetCurrentPlayback(CurrentStation);
                if (clip != null)
                {
                    string cleanName = CleanTrackName(clip.name);
                    trackInfo = $"\n<color=#FFD700>{CurrentStation}</color>\n<color=#AAAAAA>{cleanName}</color>";
                }
            }

            return $"<size=16><b>{Localization.instance.Localize("$radio_name")}</b></size> ({status}){trackInfo}\n" +
                   $"<color=#888888>────────────────</color>\n" +
                   $"[<color=yellow><b>E</b></color>] {Localization.instance.Localize("$radio_toggle")}\n" +
                   $"[<color=yellow><b>{Localization.instance.Localize("$radio_e_hold")}</b></color>] {Localization.instance.Localize("$radio_next_station")}\n" +
                   $"[<color=yellow><b>Shift + E</b></color>] {Localization.instance.Localize("$radio_group")}: {groupDisplay}\n" +
                   $"[<color=yellow><b>{Localization.instance.Localize("$radio_mouse_wheel")}</b></color>] {Localization.instance.Localize("$radio_volume")} ({Volume:P0})";
        }

        public string GetHoverName() => "Radio";
        public float GetHoverOffset() => 1.5f;

        public bool Interact(Humanoid user, bool hold, bool alt) => false;
        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

        private void CycleStation()
        {
            if (AudioManager.Instance == null || AudioManager.Instance.StationNames.Count == 0) return;

            var stations = new List<string>(AudioManager.Instance.StationNames);
            int index = stations.IndexOf(CurrentStation);
            string newStation = stations[(index + 1) % stations.Count];

            SetStation(newStation);
        }

        public void SetOn(bool value)
        {
            if (znetView != null && znetView.IsValid() && !znetView.IsOwner())
                znetView.ClaimOwnership();

            IsOn = value;
            forceClipRefresh = true;
            SaveToZDO();
            SyncGroup();
        }

        public void SetVolume(float value)
        {
            if (znetView != null && znetView.IsValid() && !znetView.IsOwner())
                znetView.ClaimOwnership();

            Volume = Mathf.Clamp01(value);
            SaveToZDO();
            SyncGroup();
        }

        public void SetStation(string station)
        {
            if (znetView != null && znetView.IsValid() && !znetView.IsOwner())
                znetView.ClaimOwnership();

            CurrentStation = station;
            forceClipRefresh = true;
            SaveToZDO();
            SyncGroup();
        }

        public void SetGroup(string group)
        {
            if (znetView != null && znetView.IsValid() && !znetView.IsOwner())
                znetView.ClaimOwnership();

            Group = group ?? "";
            SaveToZDO();

            if (string.IsNullOrEmpty(Group)) return;

            foreach (var radio in AllRadios)
            {
                if (radio == this || radio.Group != Group) continue;

                IsOn = radio.IsOn;
                Volume = radio.Volume;
                CurrentStation = radio.CurrentStation;
                forceClipRefresh = true;
                SaveToZDO();
                break;
            }

            SyncGroup();
        }

        private void SyncGroup()
        {
            if (string.IsNullOrEmpty(Group)) return;

            foreach (var radio in AllRadios)
            {
                if (radio == this || radio.Group != Group) continue;

                if (radio.znetView != null && radio.znetView.IsValid() && !radio.znetView.IsOwner())
                    radio.znetView.ClaimOwnership();

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