using BepInEx;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

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

            AddRadioPiece();
        }

        private void AddRadioPiece()
        {
            // Create a simple empty cube prefab
            GameObject radioPrefab = PrefabManager.Instance.CreateEmptyPrefab("piece_radio_cube");

            // Make the cube red
            MeshRenderer renderer = radioPrefab.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.red;
            }

            // Add the required Piece component
            Piece piece = radioPrefab.AddComponent<Piece>();
            piece.m_name = "Radio";
            piece.m_description = "A radio that plays music.";

            // Configure the piece
            PieceConfig config = new PieceConfig
            {
                Name = "Radio",
                Description = "A radio that plays music.",
                PieceTable = PieceTables.Hammer,
                Category = "Misc",
                Usage = new[] { PieceUsages.Furniture }
            };

            config.AddRequirement("Wood", 5);

            // Register the piece
            PieceManager.Instance.AddPiece(new CustomPiece(radioPrefab, false, config));
        }
    }
}