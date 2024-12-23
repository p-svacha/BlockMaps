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
        public CtfMatchUi MatchUI;
        public GameObject LoadingScreenOverlay;
        public UI_EndGameScreen EndGameScreen;

        [Header("Misc")]
        public LineRenderer PathPreviewRenderer;

        // Multiplayer
        private bool IsWaitingForOtherPlayerAsHost;
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

            if (IsWaitingForOtherPlayerAsHost)
            {
                if(NetworkServer.Instance.ConnectedClients.Count == 2)
                {
                    IsWaitingForOtherPlayerAsHost = false;
                    int mapSeed = CTFMapGenerator.GetRandomSeed();
                    int mapSize = GetRandomMapSize();
                    NetworkClient.Instance.SendMessage(new NetworkMessage_InitializeMultiplayerMatch(mapSize, mapSeed, NetworkServer.Instance.ConnectedClients[0].Client.RemoteEndPoint.ToString(), NetworkServer.Instance.ConnectedClients[1].Client.RemoteEndPoint.ToString()));
                }
            }

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

        private void StartMultiplayerMatch()
        {
            ActiveMatch = new CtfMatch(this);
            NetworkClient.Instance.Match = ActiveMatch;
            ActiveMatch.InitializeGame(CtfMatchType.Multiplayer, MultiplayerMapSize, MultiplayerMapSeed, MultiplayerPlayAsBlue, MultiplayerP1ClientId, MultiplayerP2ClientId);
            MatchUI.Init(ActiveMatch);
            MainMenuUI.gameObject.SetActive(false);
        }

        public void HostServer()
        {
            NetworkClient.Instance.Game = this;
            NetworkServer.Instance.StartServerAndConnectAsHost();
            IsWaitingForOtherPlayerAsHost = true;
        }

        public void ConnectToServer()
        {
            NetworkClient.Instance.Game = this;
            NetworkClient.Instance.ServerIP = MainMenuUI.MultiplayerIpInput.text;
            NetworkClient.Instance.ConnectToServer();
        }

        public void SetMultiplayerMatchAsReady(int mapSize, int mapSeed, bool playAsBlue, string player1ClientId, string player2ClientId)
        {
            IsWaitingForOtherPlayerAsHost = false;
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
