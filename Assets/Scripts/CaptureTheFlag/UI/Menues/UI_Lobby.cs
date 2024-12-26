using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using BlockmapFramework;
using CaptureTheFlag.Network;
using System.Linq;

namespace CaptureTheFlag.UI
{
    public class UI_Lobby : MonoBehaviour
    {
        private CtfMatchLobby LobbyData;
        public CtfGame Game;

        [Header("Elements")]
        public GameObject PlayerListContainer;
        public TMP_Dropdown MapDropdown;
        public TMP_Dropdown MapSizeDropdown;
        public Image MapPreviewImage;
        public Button StartGameButton;

        [Header("Prefabs")]
        public UI_PlayerRow PlayerRowPrefab;

        public void Init(CtfGame game)
        {
            Game = game;

            // Game settings dropdowns

            // Map
            MapDropdown.ClearOptions();
            List<string> mapOptions = new List<string>() { "Random" };
            foreach (WorldGenerator gen in CtfMatch.WorldGenerators) mapOptions.Add(gen.Label);
            MapDropdown.AddOptions(mapOptions);
            MapDropdown.value = 0;

            // Map size
            MapSizeDropdown.ClearOptions();
            List<string> mapSizes = new List<string>() { "Random" };
            foreach (var size in CtfMatch.MapSizes)
            {
                int numNodes = size.Value * World.ChunkSize;
                string label = $"{size.Key} ({numNodes}x{numNodes})";
                mapSizes.Add(label);
            }
            MapSizeDropdown.AddOptions(mapSizes);
            MapSizeDropdown.value = 0;

            // Hooks
            StartGameButton.onClick.AddListener(StartBtn_OnClick);
            MapDropdown.onValueChanged.AddListener(MapDropdown_OnValueChanged);
            MapSizeDropdown.onValueChanged.AddListener(MapSizeDropdown_OnValueChanged);
        }


        public void SetData(CtfMatchLobby data)
        {
            LobbyData = data;

            // Player list
            HelperFunctions.DestroyAllChildredImmediately(PlayerListContainer, skipElements: 1);
            int index = 0;
            foreach(ClientInfo playerInfo in data.Clients)
            {
                UI_PlayerRow row = Instantiate(PlayerRowPrefab, PlayerListContainer.transform);
                row.Init(playerInfo, CtfMatch.PlayerColors[index]);
                index++;
            }

            // Game settings
            MapDropdown.value = data.Settings.ChosenWorldGeneratorIndex;
            data.Settings.ChosenWorldGeneratorOption = MapDropdown.options[MapDropdown.value].text;

            MapSizeDropdown.value = data.Settings.ChosenMapSizeIndex;
            data.Settings.ChosenMapSizeOption = MapSizeDropdown.options[MapSizeDropdown.value].text;

            // Map preview
            string mapPreviewBasePath = "CaptureTheFlag/MapPreviewImages/";
            if (data.Settings.ChosenWorldGeneratorIndex == 0) MapPreviewImage.sprite = Resources.Load<Sprite>(mapPreviewBasePath + "Random");
            if (data.Settings.ChosenWorldGeneratorIndex == 1) MapPreviewImage.sprite = Resources.Load<Sprite>(mapPreviewBasePath + "Forest");
        }

        private void MapDropdown_OnValueChanged(int value)
        {
            LobbyData.Settings.ChosenWorldGeneratorIndex = value;
            NetworkClient.Instance.SendMessage(LobbyData.ToNetworkMessage());
        }
        private void MapSizeDropdown_OnValueChanged(int value)
        {
            LobbyData.Settings.ChosenMapSizeIndex = value;
            NetworkClient.Instance.SendMessage(LobbyData.ToNetworkMessage());
        }

        private void StartBtn_OnClick()
        {
            if (LobbyData.Clients.Count != 2) return;

            int mapSeed = CTFMapGenerator.GetRandomSeed();
            int worldGeneratorIndex = LobbyData.Settings.ChosenWorldGeneratorIndex == 0 ? Random.Range(0, CtfMatch.WorldGenerators.Count) : LobbyData.Settings.ChosenWorldGeneratorIndex - 1;
            int worldSize = LobbyData.Settings.ChosenMapSizeIndex == 0 ? Random.Range(0, CtfMatch.MapSizes.Count) : CtfMatch.MapSizes.Values.ToList()[LobbyData.Settings.ChosenMapSizeIndex - 1];

            NetworkClient.Instance.SendMessage(new NetworkMessage_InitializeMultiplayerMatch(worldGeneratorIndex, worldSize, mapSeed, LobbyData.Clients[0].ClientId, LobbyData.Clients[1].ClientId));
        }
    }
}
