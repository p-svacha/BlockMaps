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

        private CtfMatch ActiveMatch;

        [Header("UIs")]
        public UI_MainMenu MainMenuUI;
        public UI_Lobby LobbyUI;
        public CtfMatchUi MatchUI;
        public GameObject LoadingScreenOverlay;
        public UI_EndGameScreen EndGameScreen;

        [Header("Misc")]
        public LineRenderer PathPreviewRenderer;

        // Multiplayer
        private List<ClientInfo> ClientInfos = new List<ClientInfo>();
        private bool ClientUpdateReceived;
        private bool WeJustJoinedTheServer;
        private bool IsMultiplayerMatchReady;
        private int MultiplayerMapSize;
        private int MultiplayerMapSeed;
        private bool MultiplayerPlayAsBlue;
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
                if (WeJustJoinedTheServer)
                {
                    WeJustJoinedTheServer = false;
                    GoToLobby();
                }

                // Another client joined the server we are in
                else
                {
                    LobbyUI.SetPlayerList(ClientInfos);
                }
            }

            /*
            else if (NetworkClient.Instance.ClientId != null && !NetworkClient.Instance.IsInGameOrLobby) // We just connected to server => go to lobby
            {
                CreateMultiplayerLobby();

                if(NetworkServer.Instance.ConnectedClients.Count == 2)
                {
                    IsWaitingForOtherPlayerAsHost = false;
                    int mapSeed = CTFMapGenerator.GetRandomSeed();
                    int mapSize = GetRandomMapSize();
                    NetworkClient.Instance.SendMessage(new NetworkMessage_InitializeMultiplayerMatch(mapSize, mapSeed, NetworkServer.Instance.ConnectedClients[0].Client.RemoteEndPoint.ToString(), NetworkServer.Instance.ConnectedClients[1].Client.RemoteEndPoint.ToString()));
                }

            }
            */

            if(IsMultiplayerMatchReady)
            {
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

        public void ConnectToServer(bool isHost)
        {
            NetworkClient.Instance.Game = this;
            NetworkClient.Instance.ServerIP = MainMenuUI.MultiplayerIpInput.text;
            NetworkClient.Instance.ConnectToServer(callback: OnConnectionToServerEstablished);
            if (isHost) NetworkClient.Instance.SetAsHost();
        }


        /// <summary>
        /// Gets called on seperate thread when a new client has connected to the server.
        /// <br/>Contains information about all currently connected clients and if the newly connected client was ourselves.
        /// </summary>
        public void OnNewClientConnected(List<ClientInfo> infos, bool isSelf)
        {
            ClientInfos = infos;
            ClientUpdateReceived = true;
            if (isSelf) WeJustJoinedTheServer = true;
        }

        private void GoToLobby()
        {
            MainMenuUI.gameObject.SetActive(false);
            LobbyUI.gameObject.SetActive(true);
            LobbyUI.SetPlayerList(ClientInfos);
        }

        private void StartMultiplayerMatch()
        {
            ActiveMatch = new CtfMatch(this);
            NetworkClient.Instance.Match = ActiveMatch;
            ActiveMatch.InitializeGame(CtfMatchType.Multiplayer, MultiplayerMapSize, MultiplayerMapSeed, MultiplayerPlayAsBlue, MultiplayerP1ClientId, MultiplayerP2ClientId);
            MatchUI.Init(ActiveMatch);
            MainMenuUI.gameObject.SetActive(false);
        }


        private void OnConnectionToServerEstablished()
        {
            // When connection to server has been established, inform all clients that a new client connected
            NetworkClient.Instance.SendMessage(new NetworkMessage_ClientConnected(MainMenuUI.MultiplayerNameInput.text));
        }

        public void SetMultiplayerMatchAsReady(int mapSize, int mapSeed, bool playAsBlue, string player1ClientId, string player2ClientId)
        {
            MultiplayerMapSize = mapSize;
            MultiplayerMapSeed = mapSeed;
            MultiplayerPlayAsBlue = playAsBlue;
            IsMultiplayerMatchReady = true;
            MultiplayerP1ClientId = player1ClientId;
            MultiplayerP2ClientId = player2ClientId;
        }

        #endregion
    }
}
