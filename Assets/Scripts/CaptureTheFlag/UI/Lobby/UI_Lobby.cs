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
        public TMP_Dropdown SpawnTypeDropdown;
        public Image MapPreviewImage;
        public TextMeshProUGUI MapPreviewMapName;
        public TextMeshProUGUI MapPreviewMapDescription;
        public Button StartGameButton;

        [Header("Prefabs")]
        public UI_PlayerRow PlayerRowPrefab;

        private Dictionary<int, Sprite> MapPreviewSprites;
        private string RandomMapDescription = "One of the available map generators will be picked at random.";

        public void Init(CtfGame game)
        {
            Game = game;

            // Map
            MapDropdown.ClearOptions();
            List<string> mapOptions = new List<string>() { "Random" };
            foreach (WorldGenerator gen in CtfMatch.WorldGenerators) mapOptions.Add(gen.Label);
            MapDropdown.AddOptions(mapOptions);
            MapDropdown.value = 0;

            // Preview sprites
            string mapPreviewBasePath = "CaptureTheFlag/MapPreviewImages/";
            MapPreviewSprites = new Dictionary<int, Sprite>();
            for(int i = 0; i < MapDropdown.options.Count; i++) MapPreviewSprites.Add(i, Resources.Load<Sprite>(mapPreviewBasePath + MapDropdown.options[i].text));

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

            // Character spawn type
            SpawnTypeDropdown.ClearOptions();
            List<string> spawnTypeOptions = new List<string>() { "Random" };
            foreach(var value in System.Enum.GetValues(typeof(CharacterSpawnType)))
            {
                spawnTypeOptions.Add(HelperFunctions.GetEnumDescription((CharacterSpawnType)value));
            }
            SpawnTypeDropdown.AddOptions(spawnTypeOptions);
            SpawnTypeDropdown.value = 0;

            // Listeners
            StartGameButton.onClick.AddListener(StartBtn_OnClick);
            MapDropdown.onValueChanged.AddListener(MapDropdown_OnValueChanged);
            MapSizeDropdown.onValueChanged.AddListener(MapSizeDropdown_OnValueChanged);
            SpawnTypeDropdown.onValueChanged.AddListener(SpawnTypeDropdown_OnValueChanged);
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
                row.Init(data.MatchType, playerInfo, CtfMatch.PlayerColors[index]);
                index++;
            }

            // Game settings
            MapDropdown.value = data.Settings.WorldGeneratorDropdownIndex;
            MapSizeDropdown.value = data.Settings.WorldSizeDropdownIndex;
            SpawnTypeDropdown.value = data.Settings.SpawnTypeDropdownIndex;

            // Map preview
            MapPreviewImage.sprite = MapPreviewSprites[data.Settings.WorldGeneratorDropdownIndex];
            MapPreviewMapName.text = data.Settings.WorldGeneratorDropdownOption;
            MapPreviewMapDescription.text = data.Settings.WorldGeneratorDropdownIndex == 0 ? RandomMapDescription : CtfMatch.WorldGenerators[data.Settings.WorldGeneratorIndex].Description;
        }

        private void MapDropdown_OnValueChanged(int value)
        {
            LobbyData.Settings.SetWorldGeneratorDropdownIndex(value);
            OnMatchSettingChanged();
        }
        private void MapSizeDropdown_OnValueChanged(int value)
        {
            LobbyData.Settings.SetMapSizeDropdownIndex(value);
            OnMatchSettingChanged();
        }
        private void SpawnTypeDropdown_OnValueChanged(int value)
        {
            LobbyData.Settings.SetSpawnTypeDropdownIndex(value);
            OnMatchSettingChanged();
        }

        private void OnMatchSettingChanged()
        {
            if (LobbyData.MatchType == CtfMatchType.Singleplayer) SetData(LobbyData);
            if (LobbyData.MatchType == CtfMatchType.Multiplayer) NetworkClient.Instance.SendMessage(LobbyData.ToNetworkMessage());
        }

        private void StartBtn_OnClick()
        {
            if (LobbyData.Clients.Count != 2) return;

            if (LobbyData.MatchType == CtfMatchType.Singleplayer) Game.StartMatch();
            if (LobbyData.MatchType == CtfMatchType.Multiplayer) NetworkClient.Instance.SendMessage(new NetworkMessage("InitializeMultiplayerMatch"));
        }
    }
}
