using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using BlockmapFramework;

namespace CaptureTheFlag.UI
{
    public class UI_Lobby : MonoBehaviour
    {
        public CtfGame Game;

        [Header("Elements")]
        public GameObject PlayerListContainer;
        public TMP_Dropdown MapDropdown;
        public TMP_Dropdown MapSizeDropdown;
        public Image MapPreviewImage;
        public Button StartGameButton;

        [Header("Prefabs")]
        public UI_PlayerRow PlayerRowPrefab;

        // Players
        private List<ClientInfo> Players = new List<ClientInfo>();

        // Game settings
        public List<WorldGenerator> Generators = new List<WorldGenerator>()
        {
                new CTFMapGenerator_Forest(),
        };
        public Dictionary<string, int> MapSizes = new Dictionary<string, int>()
        {
            { "Tiny", 3 },
            { "Small", 4 },
            { "Medium", 5 },
            { "Big", 6 },
            { "Large", 7 },
        };


        public void Init(CtfGame game)
        {
            Game = game;

            // Game settings dropdowns

            // Map
            MapDropdown.ClearOptions();
            List<string> mapOptions = new List<string>() { "Random" };
            foreach (WorldGenerator gen in Generators) mapOptions.Add(gen.Label);
            MapDropdown.AddOptions(mapOptions);
            MapDropdown.value = 0;

            // Map size
            MapSizeDropdown.ClearOptions();
            List<string> mapSizes = new List<string>() { "Random" };
            foreach (var size in MapSizes)
            {
                int numNodes = size.Value * World.ChunkSize;
                string label = $"{size.Key} ({numNodes}x{numNodes})";
                mapSizes.Add(label);
            }
            MapSizeDropdown.AddOptions(mapSizes);
            MapSizeDropdown.value = 0;

            // Buttons
            StartGameButton.onClick.AddListener(StartBtn_OnClick);
        }


        public void SetPlayerList(List<ClientInfo> clients)
        {
            Players = clients;

            HelperFunctions.DestroyAllChildredImmediately(PlayerListContainer, skipElements: 1);
            foreach(ClientInfo playerInfo in Players)
            {
                UI_PlayerRow row = Instantiate(PlayerRowPrefab, PlayerListContainer.transform);
                row.Init(playerInfo);
            }
        }

        private void StartBtn_OnClick()
        {

        }
    }
}
