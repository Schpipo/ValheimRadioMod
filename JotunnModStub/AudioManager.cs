using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.Networking;

namespace ValheimRadioMod
{
    internal class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private Dictionary<string, List<string>> stationFiles = new();
        private Dictionary<string, List<AudioClip>> stationClips = new();
        private Dictionary<string, List<float>> stationDurations = new();
        private Dictionary<string, float> stationTotalDuration = new();

        public IReadOnlyCollection<string> StationNames => stationFiles.Keys;

        private void Awake()
        {
            if (Instance != null) { Destroy(this); return; }
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

                var files = Directory.GetFiles(stationDir)
                    .Where(f => {
                        string ext = Path.GetExtension(f).ToLowerInvariant();
                        return ext is ".mp3" or ".ogg" or ".wav" or ".m4a";
                    })
                    .OrderBy(f => ExtractOrder(Path.GetFileNameWithoutExtension(f)))
                    .ThenBy(f => Path.GetFileName(f), System.StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (files.Count == 0) continue;

                stationFiles[stationName] = files;
                stationClips[stationName] = new List<AudioClip>();
                stationDurations[stationName] = new List<float>();
            }

            StartCoroutine(LoadAllTracksDelayed());
        }

        private int ExtractOrder(string name)
        {
            var match = Regex.Match(name, @"^(\d+)");
            return match.Success ? int.Parse(match.Value) : int.MaxValue;
        }

        private IEnumerator LoadAllTracksDelayed()
        {
            // Warten bis das Spiel weitgehend geladen ist
            yield return new WaitForSeconds(10f);

            Jotunn.Logger.LogInfo("Starting full station load...");

            foreach (var station in stationFiles.Keys)
            {
                yield return StartCoroutine(LoadEntireStation(station));
                yield return new WaitForSeconds(0.3f);
            }

            Jotunn.Logger.LogInfo("All stations loaded.");
        }

        private IEnumerator LoadEntireStation(string station)
        {
            var files = stationFiles[station];
            float total = 0f;

            for (int i = 0; i < files.Count; i++)
            {
                string path = files[i];
                string url = "file://" + path.Replace("\\", "/");

                AudioType audioType = AudioType.UNKNOWN;
                string ext = Path.GetExtension(path).ToLowerInvariant();

                if (ext == ".mp3")
                    audioType = AudioType.MPEG;
                else if (ext == ".ogg")
                    audioType = AudioType.OGGVORBIS;
                else if (ext == ".wav")
                    audioType = AudioType.WAV;

                using var www = UnityWebRequestMultimedia.GetAudioClip(url, audioType);
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    clip.name = Path.GetFileNameWithoutExtension(path);

                    stationClips[station].Add(clip);
                    stationDurations[station].Add(clip.length);
                    total += clip.length;
                }
                else
                {
                    Jotunn.Logger.LogWarning($"Failed to load {path}");
                    stationClips[station].Add(null);
                    stationDurations[station].Add(0f);
                }

                if (i % 2 == 0)
                    yield return null;
            }

            stationTotalDuration[station] = total;
            Jotunn.Logger.LogInfo($"Station '{station}' loaded: {files.Count} tracks, total {total:F1}s");
        }

        public (AudioClip clip, float position) GetCurrentPlayback(string station)
        {
            if (!stationClips.TryGetValue(station, out var clips) || clips.Count == 0)
                return (null, 0f);

            if (!stationTotalDuration.TryGetValue(station, out float totalDuration) || totalDuration <= 0f)
                return (null, 0f);

            double serverTime = ZNet.instance != null ? ZNet.instance.GetTimeSeconds() : Time.timeAsDouble;
            float cyclePos = (float)(serverTime % totalDuration);

            float accumulated = 0f;
            int targetIndex = 0;
            float posInClip = 0f;

            var durations = stationDurations[station];
            for (int i = 0; i < durations.Count; i++)
            {
                float len = durations[i];
                if (len <= 0f) continue;

                if (cyclePos < accumulated + len)
                {
                    targetIndex = i;
                    posInClip = cyclePos - accumulated;
                    break;
                }
                accumulated += len;
            }

            AudioClip clip = clips[targetIndex];
            if (clip == null)
                return (null, 0f);

            return (clip, Mathf.Clamp(posInClip, 0f, clip.length - 0.05f));
        }
    }
}