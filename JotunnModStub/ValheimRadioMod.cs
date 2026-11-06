using BepInEx;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using SimpleJson; // falls vorhanden, sonst manuelles Parsen

namespace ValheimRadioMod
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    internal class ValheimRadioMod : BaseUnityPlugin
    {
        public const string PluginGUID = "com.schpipo.ValheimRadioMod";
        public const string PluginName = "ValheimRadioMod";
        public const string PluginVersion = "0.0.1";

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
            // Create a simple empty cube prefab
            GameObject radioPrefab = PrefabManager.Instance.CreateEmptyPrefab("piece_radio_cube");

            // Make the cube red
            MeshRenderer renderer = radioPrefab.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.blue;
            }
            // Add RadioBehaviour component
            radioPrefab.AddComponent<RadioBehaviour>();
            // Add the required Piece component
            Piece piece = radioPrefab.AddComponent<Piece>();
            piece.m_name = "Radio";
            piece.m_description = "A radio that plays music.";

            // Configure the piece
            string modPath = Path.GetDirectoryName(Info.Location);
            Sprite icon = AssetUtils.LoadSpriteFromFile(Path.Combine(modPath, "icon.png"));

            PieceConfig config = new PieceConfig
            {
                Name = "Radio",
                Description = "A radio that plays music.",
                PieceTable = PieceTables.Hammer,
                Category = "Misc",
                Usage = new[] { PieceUsages.Furniture },
                Icon = icon
            };

            config.AddRequirement("Wood", 5);

            // Register the piece
            PieceManager.Instance.AddPiece(new CustomPiece(radioPrefab, false, config));
        }
    }
}