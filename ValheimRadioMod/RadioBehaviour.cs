using System;
using System.Collections.Generic;
using UnityEngine;

namespace ValheimRadioMod
{
    public class RadioBehaviour : MonoBehaviour, Hoverable, Interactable, TextReceiver
    {
        public static readonly HashSet<RadioBehaviour> AllRadios = new HashSet<RadioBehaviour>();
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
        private static bool lastInputWasController = false;
        private float lastInputSwitchTime = 0f;
        private const float InputSwitchDelay = 0.3f;

        public float HoverEndTime => hoverEndTime;
        private float lastVolumeTick = 0f;
        private const float VolumeHoldDelay = 0.6f;
        private const float VolumeTickInterval = 0.6f;
        private float aPressTime = -1f;
        private float yPressTime = -1f;
        private float lastStationChangeTime = 0f;

        public static RadioBehaviour GetFirstHovered()
        {
            foreach (var r in AllRadios)
            {
                if (r != null && Time.time < r.hoverEndTime)
                    return r;
            }
            return null;
        }

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
            bool controller = IsAnyControllerInput();
            bool keyboardMouse = Input.anyKeyDown ||
                                 Mathf.Abs(Input.GetAxis("Mouse X")) > 1f ||
                                 Mathf.Abs(Input.GetAxis("Mouse Y")) > 1f ||
                                 Mathf.Abs(Input.GetAxis("Mouse ScrollWheel")) > 0.01f;

            if (Time.time - lastInputSwitchTime > InputSwitchDelay)
            {
                if (controller && !keyboardMouse && !lastInputWasController)
                {
                    lastInputWasController = true;
                    lastInputSwitchTime = Time.time;
                }
                else if (keyboardMouse && !controller && lastInputWasController)
                {
                    lastInputWasController = false;
                    lastInputSwitchTime = Time.time;
                }
            }

            if (Time.time >= hoverEndTime)
                HoveredRadios.Remove(this);

            audioSource.mute = Time.timeScale == 0f;

            if (Time.time < hoverEndTime && !TextInput.IsVisible())
            {
                if (GroupSelectionUI.LastConfirmTime > 0f && Time.time - GroupSelectionUI.LastConfirmTime < 0.3f)
                    return; // frisch bestätigt → nichts auswerten
                // --- A (kurz = An/Aus, lang = leiser) ---
                if ((Input.GetKeyDown(KeyCode.JoystickButton0) || ZInput.GetButtonDown("JoyButtonA")) && Time.time - GroupSelectionUI.LastConfirmTime > 0.3f)
                    aPressTime = Time.time;

                if ((Input.GetKey(KeyCode.JoystickButton0) || ZInput.GetButton("JoyButtonA")) && aPressTime > 0f)
                {
                    if (Time.time - aPressTime >= VolumeHoldDelay && Time.time - lastVolumeTick >= VolumeTickInterval)
                    {
                        SetVolume(Volume - 0.05f);
                        lastVolumeTick = Time.time;
                    }
                }

                if (Input.GetKeyUp(KeyCode.JoystickButton0) || ZInput.GetButtonUp("JoyButtonA"))
                {
                    if (aPressTime > 0f && Time.time - aPressTime < VolumeHoldDelay)
                        SetOn(!IsOn);
                    aPressTime = -1f;
                }

                // --- X (Sender) mit Cooldown ---
                if ((ZInput.GetButtonDown("JoyButtonX") || Input.GetKeyDown(KeyCode.JoystickButton2)) && Time.time - lastStationChangeTime > 0.5f)
                {
                    CycleStation();
                    lastStationChangeTime = Time.time;
                }

                // --- Y (kurz = Gruppe, lang = lauter) ---
                if (Input.GetKeyDown(KeyCode.JoystickButton3))
                    yPressTime = Time.time;

                if (Input.GetKey(KeyCode.JoystickButton3) && yPressTime > 0f)
                {
                    if (Time.time - yPressTime >= VolumeHoldDelay && Time.time - lastVolumeTick >= VolumeTickInterval)
                    {
                        SetVolume(Volume + 0.05f);
                        lastVolumeTick = Time.time;
                    }
                }

                if (Input.GetKeyUp(KeyCode.JoystickButton3))
                {
                    if (yPressTime > 0f && Time.time - yPressTime < VolumeHoldDelay)
                        OpenGroupRename();
                    yPressTime = -1f;
                }
                // --- Tastatur nur wenn zuletzt Tastatur benutzt wurde ---
                if (!lastInputWasController)
                {
                    if (ZInput.GetKeyDown(KeyCode.E))
                        ePressTime = Time.time;

                    if (ZInput.GetKey(KeyCode.E) && ePressTime > 0f && Time.time - ePressTime >= LongPressDuration)
                    {
                        CycleStation();
                        ePressTime = -1f;
                    }

                    if (ZInput.GetKeyUp(KeyCode.E) && ePressTime > 0f && Time.time - ePressTime < LongPressDuration)
                    {
                        SetOn(!IsOn);
                        ePressTime = -1f;
                    }

                    if (ZInput.GetKeyDown(KeyCode.E) && (ZInput.GetKey(KeyCode.LeftShift) || ZInput.GetKey(KeyCode.RightShift)))
                    {
                        OpenGroupRename();
                        ePressTime = -1f;
                    }
                }
            }
            else
            {
                aPressTime = -1f;
                yPressTime = -1f;
                ePressTime = -1f;
            }

            if (znetView != null && znetView.IsValid())
                LoadFromZDO();

            if (initialized && IsOn != wasOn)
            {
                forceClipRefresh = true;
                wasOn = IsOn;
            }

            if (!IsOn || AudioManager.Instance == null || !isActiveAndEnabled)
            {
                if (audioSource != null && audioSource.isPlaying)
                    audioSource.Stop();
                return;
            }
            // AudioSource erst jetzt aktivieren
            if (audioSource != null && !audioSource.enabled)
                audioSource.enabled = true;

            var (clip, position) = AudioManager.Instance.GetCurrentPlayback(CurrentStation);
            if (clip == null) return;

            if (audioSource.clip != clip || forceClipRefresh || !audioSource.isPlaying)
            {
                audioSource.clip = clip;
                audioSource.time = position;
                audioSource.Play();
                forceClipRefresh = false;
            }
            else if (Mathf.Abs(audioSource.time - position) > 0.2f)
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

        private bool IsAnyControllerInput()
        {
            for (int i = 0; i < 20; i++)
            {
                if (Input.GetKey((KeyCode)((int)KeyCode.JoystickButton0 + i)))
                    return true;
            }

            try
            {
                if (Mathf.Abs(ZInput.GetJoyLeftStickX()) > 0.2f ||
                    Mathf.Abs(ZInput.GetJoyLeftStickY()) > 0.2f ||
                    Mathf.Abs(ZInput.GetJoyRightStickX()) > 0.2f ||
                    Mathf.Abs(ZInput.GetJoyRightStickY()) > 0.2f)
                    return true;
            }
            catch { }

            if (ZInput.GetButton("JoyButtonA") || ZInput.GetButton("JoyButtonB") ||
                ZInput.GetButton("JoyButtonX") || ZInput.GetButton("JoyButtonY") ||
                ZInput.GetButton("JoyDPadUp") || ZInput.GetButton("JoyDPadDown") ||
                ZInput.GetButton("JoyDPadLeft") || ZInput.GetButton("JoyDPadRight") ||
                ZInput.GetButton("JoyUse") || ZInput.GetButton("JoyAltUse"))
                return true;

            return false;
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

            string toggleKey = lastInputWasController ? "A" : "E";
            string stationKey = lastInputWasController ? "X" : Localization.instance.Localize("$radio_e_hold");
            string groupKey = lastInputWasController ? "Y" : "Shift + E";
            string volumeKey = lastInputWasController ? "A/Y halten" : Localization.instance.Localize("$radio_mouse_wheel");

            return $"<size=16><b>{Localization.instance.Localize("$radio_name")}</b></size> ({status}){trackInfo}\n" +
                   $"<color=#888888>────────────────</color>\n" +
                   $"[<color=yellow><b>{toggleKey}</b></color>] {Localization.instance.Localize("$radio_toggle")}\n" +
                   $"[<color=yellow><b>{stationKey}</b></color>] {Localization.instance.Localize("$radio_next_station")}\n" +
                   $"[<color=yellow><b>{groupKey}</b></color>] {Localization.instance.Localize("$radio_group")}: {groupDisplay}\n" +
                   $"[<color=yellow><b>{volumeKey}</b></color>] {Localization.instance.Localize("$radio_volume")} ({Volume:P0})";
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