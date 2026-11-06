using System.Collections;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.Networking;

namespace ValheimRadioMod
{
    internal class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        public Dictionary<string, List<AudioClip>> Stations { get; private set; } = new Dictionary<string, List<AudioClip>>();

        // Tracks the total length of each station's playlist
        private Dictionary<string, float> stationDurations = new Dictionary<string, float>();

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        

        public void LoadStations(string modPath)
        {
            string audioPath = Path.Combine(modPath, "Audio");

            if (!Directory.Exists(audioPath))
            {
                Jotunn.Logger.LogWarning("Audio folder not found: " + audioPath);
                return;
            }

            foreach (string stationDir in Directory.GetDirectories(audioPath))
            {
                string stationName = Path.GetFileName(stationDir);
                List<AudioClip> clips = new List<AudioClip>();

                foreach (string file in Directory.GetFiles(stationDir))
                {
                    string ext = Path.GetExtension(file).ToLower();
                    if (ext == ".mp3" || ext == ".ogg" || ext == ".wav")
                    {
                        StartCoroutine(LoadClip(file, stationName, clips));
                    }
                }

                Stations[stationName] = clips;
                Jotunn.Logger.LogInfo($"Found station: {stationName}");
            }
        }

        private IEnumerator LoadClip(string path, string stationName, List<AudioClip> clipList)
        {
            string url = "file://" + path.Replace("\\", "/");
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.UNKNOWN))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    clip.name = Path.GetFileNameWithoutExtension(path);
                    clipList.Add(clip);

                    // Update total duration
                    if (!stationDurations.ContainsKey(stationName))
                        stationDurations[stationName] = 0f;
                    stationDurations[stationName] += clip.length;

                    Jotunn.Logger.LogInfo($"Loaded clip: {clip.name} for station {stationName}");
                }
                else
                {
                    Jotunn.Logger.LogError($"Failed to load audio: {path} - {www.error}");
                }
            }
        }

        /// <summary>
        /// Returns the current clip and the playback position (in seconds) for a station,
        /// based on the shared server time.
        /// </summary>
        public (AudioClip clip, float position) GetCurrentPlayback(string stationName)
        {
            if (!Stations.ContainsKey(stationName) || Stations[stationName].Count == 0)
                return (null, 0f);

            if (!stationDurations.ContainsKey(stationName) || stationDurations[stationName] <= 0f)
                return (null, 0f);

            // Use Valheim's shared time (ZNet)
            double serverTime = ZNet.instance != null ? ZNet.instance.GetTimeSeconds() : Time.time;

            float totalDuration = stationDurations[stationName];
            float cyclePosition = (float)(serverTime % totalDuration);

            float accumulated = 0f;
            foreach (AudioClip clip in Stations[stationName])
            {
                if (cyclePosition < accumulated + clip.length)
                {
                    float clipPosition = cyclePosition - accumulated;
                    return (clip, clipPosition);
                }
                accumulated += clip.length;
            }

            // Fallback
            return (Stations[stationName][0], 0f);
        }
    }
}