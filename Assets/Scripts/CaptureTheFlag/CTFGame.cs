using BlockmapFramework;
using CaptureTheFlag.Network;
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
        public static string VERSION = "0.0.1";

        [Header("UIs")]
        public UI_MainMenu MainMenuUI;
        public UI_Lobby LobbyUI;
        public CtfMatchUi MatchUI;
        public GameObject LoadingScreenOverlay;
        public UI_EndGameScreen EndGameScreen;

        [Header("Misc")]
        public LineRenderer PathPreviewRenderer;

        // Current Match
        private CtfMatchLobby ActiveLobby;
        private CtfMatch ActiveMatch;

        // Multiplayer
        public List<ClientInfo> ServerClientInfos = new List<ClientInfo>(); // Infos of all clients connected to the CaptureTheFlag server.
        private bool ClientUpdateReceived;
        private bool WeJustConnectedToServer;
        private CtfMatchLobby UpdatedLobbyInfo;
        private bool IsMultiplayerMatchReady;

        private int MultiplayerWorldGenIndex;
        private int MultiplayerMapSize;
        private int MultiplayerMapSeed;
        private bool MultiplayerPlayAsP1;
        private string MultiplayerP1ClientId;
        private string MultiplayerP2ClientId;

        // Ticks
        private float TickAccumulator = 0f;
        private const float TICKS_PER_SECOND = 60f;
        private const float TICK_INTERVAL = 1f / TICKS_PER_SECOND;

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
            LobbyUI.Init(this);

            MatchUI.gameObject.SetActive(false);
            MainMenuUI.gameObject.SetActive(true);
            LobbyUI.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (ActiveMatch != null)
            {
                ActiveMatch.Update();

                // Ticks
                float dt = Time.deltaTime;
                TickAccumulator += dt;

                // Process as many ticks as fit into this frame
                while (TickAccumulator >= TICK_INTERVAL)
                {
                    TickAccumulator -= TICK_INTERVAL;
                    ActiveMatch.Tick();
                }
            }

            // The list of connected clients (ClientInfos) changed since last update
            if(ClientUpdateReceived)
            {
                Debug.Log("Client update received");
                ClientUpdateReceived = false;

                // We ourselves joined a server
                if (WeJustConnectedToServer)
                {
                    WeJustConnectedToServer = false;
                    NetworkClient.Instance.SendMessage(new NetworkMessage("RequestJoinLobby")); // Remove this once a lobby browser is implemented and instead show that browser
                }

                // Another client joined the server
                else { }
            }

            else if(UpdatedLobbyInfo != null)
            {
                if (ActiveLobby == null) GoToLobby();
                SetLobbyData(UpdatedLobbyInfo);

                UpdatedLobbyInfo = null;
            }

            if(IsMultiplayerMatchReady)
            {
                IsMultiplayerMatchReady = false;
                StartMultiplayerMatch();
            }
            
        }

        public void StartSingleplayerMatch()
        {
            MainMenuUI.gameObject.SetActive(false);
            LobbyUI.gameObject.SetActive(false);
            MatchUI.gameObject.SetActive(true);

            ActiveMatch = new CtfMatch(this);
            ActiveMatch.InitializeGame(CtfMatchType.Singleplayer, mapGeneratorIndex: 0, GetRandomMapSize(), playAsP1: true);
            MatchUI.Init(ActiveMatch);
        }

        public void ShowEndGameScreen(string text)
        {
            EndGameScreen.gameObject.SetActive(true);
            EndGameScreen.Text.text = text;
        }

        public void GoToMainMenu()
        {
            ActiveMatch = null;
            MainMenuUI.gameObject.SetActive(true);
        }

        private int GetRandomMapSize() => Random.Range(4, 8 + 1);

        #region Multiplayer

        public void HostAndConnectToServer()
        {
            NetworkServer.Instance.StartServer();
            ConnectToServer(isHost: true);
        }

        /// <summary>
        /// ----- Process of connecting to server and joining a lobby -----
        /// 1. ConnectToServer
        /// 2. OnConnectionToServerEstablished: Send NetworkMessage_ClientConnected
        /// 3. Receive NetworkMessage_ConnectedClientsInfo: Set WeJustConnectedToServer = true
        /// 4. Send NetworkMessage with Type ("RequestJoinLobby")
        /// 5. Receive NetworkMessage_LobbyInfo: Go to Lobby
        /// </summary>
        public void ConnectToServer(bool isHost)
        {
            NetworkClient.Instance.Game = this;
            NetworkClient.Instance.ServerIP = MainMenuUI.MultiplayerIpInput.text;
            NetworkClient.Instance.ConnectToServer(callback: OnConnectionToServerEstablished);
            if (isHost) NetworkClient.Instance.SetAsHost();
        }

        private void OnConnectionToServerEstablished()
        {
            // When connection to server has been established, inform all clients that a new client connected
            NetworkClient.Instance.SendMessage(new NetworkMessage_ClientConnected(MainMenuUI.MultiplayerNameInput.text));
        }


        /// <summary>
        /// Gets called on seperate thread when a new client has connected to the server.
        /// <br/>Contains information about all currently connected clients and if the newly connected client was ourselves.
        /// </summary>
        public void OnNewClientConnected(List<ClientInfo> infos, bool isSelf)
        {
            ServerClientInfos = infos;
            ClientUpdateReceived = true;
            if (isSelf) WeJustConnectedToServer = true;
        }

        /// <summary>
        /// Gets called on seperate thread when receiving the information about the lobby we want to join / we are in.
        /// </summary>
        public void SetUpdatedLobbyInfo(CtfMatchLobby lobbyInfo)
        {
            UpdatedLobbyInfo = lobbyInfo;
        }

        private void GoToLobby()
        {
            MainMenuUI.gameObject.SetActive(false);
            LobbyUI.gameObject.SetActive(true);
        }

        private void SetLobbyData(CtfMatchLobby lobby)
        {
            ActiveLobby = lobby;
            LobbyUI.SetData(ActiveLobby);
        }

        public void SetMultiplayerMatchAsReady(int generatorIndex, int worldSize, int seed, string p1ClientId, string p2ClientId)
        {
            MultiplayerWorldGenIndex = generatorIndex;
            MultiplayerMapSize = worldSize;
            MultiplayerMapSeed = seed;
            MultiplayerP1ClientId = p1ClientId;
            MultiplayerP2ClientId = p2ClientId;

            MultiplayerPlayAsP1 = p1ClientId == NetworkClient.Instance.ClientId;

            IsMultiplayerMatchReady = true;
        }

        private void StartMultiplayerMatch()
        {
            MainMenuUI.gameObject.SetActive(false);
            LobbyUI.gameObject.SetActive(false);
            MatchUI.gameObject.SetActive(true);

            ActiveMatch = new CtfMatch(this);
            NetworkClient.Instance.Match = ActiveMatch;
            ActiveMatch.InitializeGame(CtfMatchType.Multiplayer, MultiplayerWorldGenIndex, MultiplayerMapSize, MultiplayerMapSeed, MultiplayerPlayAsP1, MultiplayerP1ClientId, MultiplayerP2ClientId);
            MatchUI.Init(ActiveMatch);
        }

        #endregion
    }
}
