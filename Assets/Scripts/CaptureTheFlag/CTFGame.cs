using BlockmapFramework;
using CaptureTheFlag.Networking;
using CaptureTheFlag.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CaptureTheFlag
{
    /// <summary>
    /// The god object of capture the flag handling all overarching systems like the main menu, loading screen, starting and stopping games, networking etc.
    /// </summary>
    public class CtfGame : MonoBehaviour
    {
        private CtfMatch ActiveMatch;

        [Header("UIs")]
        public UI_MainMenu MainMenuUI;
        public CtfMatchUi MatchUI;
        public GameObject LoadingScreenOverlay;

        public GameObject EndGameScreen;
        public TextMeshProUGUI EndGameText;
        public Button EndGameMenuButton;

        [Header("Misc")]
        public LineRenderer PathPreviewRenderer;

        // Multiplayer
        private bool IsWaitingForOtherPlayerAsHost;
        private bool IsMultiplayerMatchReady;
        private int MultiplayerMapSize;
        private int MultiplayerMapSeed;
        private bool MultiplayerPlayAsBlue;

        private void Start()
        {
            // Load defs
            DefDatabaseRegistry.AddAllGlobalDefs();
            DefDatabase<EntityDef>.AddDefs(EntityDefs.Defs);
            DefDatabaseRegistry.ResolveAllReferences();
            DefDatabaseRegistry.OnLoadingDone();
            DefDatabaseRegistry.BindAllDefOfs();

            // Init materials
            MaterialManager.InitializeBlendableSurfaceMaterial();

            // Menu UIs
            MainMenuUI.Init(this);
        }

        private void Update()
        {
            ActiveMatch?.Tick();

            if(IsWaitingForOtherPlayerAsHost)
            {
                if(NetworkManager.Instance.ConnectedClients.Count == 2)
                {
                    IsWaitingForOtherPlayerAsHost = false;
                    int mapSeed = CTFMapGenerator.GetRandomSeed();
                    int mapSize = GetRandomMapSize();
                    NetworkClient.Instance.SendAction(new NetworkAction_StartMatch(mapSize, mapSeed));
                }
            }

            if(IsMultiplayerMatchReady)
            {
                Debug.Log("MP match START");
                IsMultiplayerMatchReady = false;
                StartMultiplayerMatch();
            }
        }

        public void StartSingleplayerMatch()
        {
            ActiveMatch = new CtfMatch(this);
            ActiveMatch.InitializeGame(CtfMatchType.Singleplayer, GetRandomMapSize(), playAsBlue: true);
            MatchUI.Init(ActiveMatch);
            MainMenuUI.gameObject.SetActive(false);
        }

        public void ShowEndGameScreen(string text)
        {
            EndGameScreen.SetActive(true);
            EndGameText.text = text;
        }

        private int GetRandomMapSize() => Random.Range(4, 8 + 1);

        #region Multiplayer

        private void StartMultiplayerMatch()
        {
            ActiveMatch = new CtfMatch(this);
            NetworkClient.Instance.Match = ActiveMatch;
            ActiveMatch.InitializeGame(CtfMatchType.Multiplayer, MultiplayerMapSize, MultiplayerMapSeed, MultiplayerPlayAsBlue);
            MatchUI.Init(ActiveMatch);
            MainMenuUI.gameObject.SetActive(false);
        }

        public void HostServer()
        {
            NetworkClient.Instance.Game = this;
            NetworkManager.Instance.StartServerAndConnectAsHost();
            IsWaitingForOtherPlayerAsHost = true;
        }

        public void ConnectToServer()
        {
            NetworkClient.Instance.Game = this;
            NetworkClient.Instance.ServerIP = MainMenuUI.MultiplayerIpInput.text;
            NetworkClient.Instance.ConnectToServer();
        }

        public void SetMultiplayerMatchAsReady(int mapSize, int mapSeed, bool playAsBlue)
        {
            IsWaitingForOtherPlayerAsHost = false;
            MultiplayerMapSize = mapSize;
            MultiplayerMapSeed = mapSeed;
            MultiplayerPlayAsBlue = playAsBlue;
            IsMultiplayerMatchReady = true;
            Debug.Log("MP match is ready");
        }

        #endregion
    }
}
