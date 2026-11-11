using BepInEx;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using SimpleJson;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ValheimRadioMod
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    internal class ValheimRadioMod : BaseUnityPlugin
    {
        public const string PluginGUID = "com.schpipo.ValheimRadioMod";
        public const string PluginName = "ValheimRadioMod";
        public const string PluginVersion = "0.1.2";
        private bool firstTracksStarted = false;

        private void Awake()
        {
            Jotunn.Logger.LogInfo("ValheimRadioMod has landed");

            var harmony = new HarmonyLib.Harmony("com.schpipo.ValheimRadioMod");
            MouseScrollPatch.ApplyPatch(harmony);
            harmony.PatchAll();

            LoadLocalization();

            // Create AudioManager
            GameObject audioManagerObj = new GameObject("RadioAudioManager");
            audioManagerObj.AddComponent<AudioManager>();
            DontDestroyOnLoad(audioManagerObj);

            string modPath = Path.GetDirectoryName(Info.Location);
            AudioManager.Instance.LoadStations(modPath);

            AddRadioPiece();
            // Main screen radio station loading UI
            var go = new GameObject("RadioLoadingUI");
            go.AddComponent<RadioLoadingUI>();
            DontDestroyOnLoad(go);
        }
        [HarmonyPatch(typeof(Player), "StartEmote")]
        private static class BlockEmoteOnRadio
        {
            static bool Prefix()
            {
                return !RadioBehaviour.IsAnyHovered;
            }
        }
        private void LoadLocalization()
        {
            string modPath = Path.GetDirectoryName(Info.Location);
            string locPath = Path.Combine(modPath, "Localization.json");

            if (!File.Exists(locPath))
            {
                Jotunn.Logger.LogWarning("Localization.json not found");
                return;
            }

            try
            {
                string json = File.ReadAllText(locPath);
                var data = SimpleJson.SimpleJson.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(json);

                var localization = LocalizationManager.Instance.GetLocalization();

                foreach (var language in data)
                {
                    localization.AddTranslation(language.Key, language.Value);
                    Jotunn.Logger.LogInfo($"Loaded localization for {language.Key}");
                }
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError($"Failed to load Localization.json: {e.Message}");
            }
        }

        private void AddRadioPiece()
        {
            string modPath = Path.GetDirectoryName(Info.Location);
            AssetBundle bundle = AssetUtils.LoadAssetBundle(Path.Combine(modPath, "radio"));
            GameObject radioPrefab = bundle.LoadAsset<GameObject>("radio"); // Prefab name check
            // Add ZNetview
            if (radioPrefab.GetComponent<ZNetView>() == null)
            {
                var znet = radioPrefab.AddComponent<ZNetView>();
                znet.m_persistent = true;
            }
            radioPrefab.AddComponent<RadioBehaviour>();
            Piece piece = radioPrefab.AddComponent<Piece>();
            piece.m_name = "Radio";
            piece.m_description = "A radio that plays music.";
            piece.m_canBeRemoved = true;
            piece.m_enabled = true;

            // Configure the piece
            // string modPath = Path.GetDirectoryName(Info.Location);
            Sprite icon = AssetUtils.LoadSpriteFromFile(Path.Combine(modPath, "icon_build.png"));

            PieceConfig config = new PieceConfig
            {
                Name = "Radio",
                Description = "A radio that plays music.",
                PieceTable = PieceTables.Hammer,
                Category = "Misc",
                Usage = new[] { PieceUsages.Furniture },
                Icon = icon
            };

            var req = new RequirementConfig("Wood", 5);
            req.Recover = true;
            config.AddRequirement(req);

            // Register the piece
            PieceManager.Instance.AddPiece(new CustomPiece(radioPrefab, false, config));
        }
        private void Update()
        {
            if (!firstTracksStarted && ZNet.instance != null && Player.m_localPlayer != null)
            {
                firstTracksStarted = true;
            }
        }
    }
}