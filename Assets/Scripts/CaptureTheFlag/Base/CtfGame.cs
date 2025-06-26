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
    public class CtfGame : GameLoop
    {
        public static string VERSION = "0.0.7-dev";

        [Header("UIs")]
        public UI_MainMenu MainMenuUI;
        public UI_Lobby LobbyUI;
        public CtfMatchUi MatchUI;
        public GameObject LoadingScreenOverlay;

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

        private void Start()
        {
            DefDatabaseRegistry.ClearAllDatabases();

            // Load defs
            DefDatabase<SkillDef>.AddDefs(SkillDefs.GetDefs());
            DefDatabase<StatDef>.AddDefs(StatDefs.GetDefs());
            DefDatabaseRegistry.AddAllGlobalDefs();
            DefDatabase<EntityDef>.AddDefs(EntityDefs.GetObjectDefs());
            DefDatabase<EntityDef>.AddDefs(EntityDefs.GetCharacterDefs());
            DefDatabase<EntityDef>.AddDefs(ItemDefs.GetDefs());
            DefDatabaseRegistry.ResolveAllReferences();
            DefDatabaseRegistry.OnLoadingDone();

            // Init materials
            MaterialManager.InitializeBlendableSurfaceMaterial();

            // Menu UIs
            MainMenuUI.Init(this);
            LobbyUI.Init(this);

            MatchUI.gameObject.SetActive(false);
            MainMenuUI.gameObject.SetActive(true);
            LobbyUI.gameObject.SetActive(false);
        }

        protected override void HandleInputs()
        {
            ActiveMatch?.HandleInputs();
        }
        protected override void Tick()
        {
            ActiveMatch?.Tick();
        }
        protected override void OnFrame()
        {
            // The list of connected clients (ClientInfos) changed since last update
            if (ClientUpdateReceived)
            {
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

            else if (UpdatedLobbyInfo != null)
            {
                if (ActiveLobby == null) GoToLobby();
                SetLobbyData(UpdatedLobbyInfo);

                UpdatedLobbyInfo = null;
            }

            if (IsMultiplayerMatchReady)
            {
                IsMultiplayerMatchReady = false;
                StartMatch();
            }
        }
        protected override void Render(float alpha)
        {
            ActiveMatch?.Render(alpha);
        }
        protected override void OnFixedUpdate()
        {
            ActiveMatch?.FixedUpdate();
        }

        public void StartSingleplayerLobby()
        {
            ActiveLobby = new CtfMatchLobby(MainMenuUI.DisplayNameInput.text);
            LobbyUI.SetData(ActiveLobby);

            GoToLobby();
        }
        public void StartMatch()
        {
            ActiveMatch = new CtfMatch(this);
            ActiveMatch.InitializeGame(ActiveLobby);
            MatchUI.Init(ActiveMatch);

            SwitchWindow(MatchUI.gameObject);
        }

        #region Window switch

        private void GoToLobby()
        {
            SwitchWindow(LobbyUI.gameObject);
        }

        public void GoToMainMenu()
        {
            ActiveMatch = null;
            SwitchWindow(MainMenuUI.gameObject);
        }

        private void SwitchWindow(GameObject window)
        {
            MainMenuUI.gameObject.SetActive(window == MainMenuUI.gameObject);
            LobbyUI.gameObject.SetActive(window == LobbyUI.gameObject);
            MatchUI.gameObject.SetActive(window == MatchUI.gameObject);
        }

        #endregion

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
            NetworkClient.Instance.SendMessage(new NetworkMessage_ClientConnected(MainMenuUI.DisplayNameInput.text));
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

        private void SetLobbyData(CtfMatchLobby lobby)
        {
            ActiveLobby = lobby;
            LobbyUI.SetData(ActiveLobby);
        }

        public void SetMultiplayerMatchAsReady()
        {
            IsMultiplayerMatchReady = true;
        }

        #endregion
    }
}
