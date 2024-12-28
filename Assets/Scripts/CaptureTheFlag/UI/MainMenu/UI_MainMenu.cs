using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace CaptureTheFlag.UI
{
    public class UI_MainMenu : MonoBehaviour
    {
        public CtfGame Game;

        [Header("Elements")]
        public TextMeshProUGUI VersionText;
        public Button SingleplayerButton;
        public Button MultiplayerHostButton;
        public Button MultiplayerConnectButton;
        public Button QuitButton;
        public TMP_InputField MultiplayerIpInput;
        public TMP_InputField DisplayNameInput;

        public void Init(CtfGame game)
        {
            Game = game;
            VersionText.text = $"version {CtfGame.VERSION}";

            SingleplayerButton.onClick.AddListener(SingleplayerBtn_OnClick);
            MultiplayerHostButton.onClick.AddListener(MpHostBtn_OnClick);
            MultiplayerConnectButton.onClick.AddListener(MpConnectBtn_OnClick);
            QuitButton.onClick.AddListener(QuitBtn_OnClick);

            MultiplayerIpInput.text = HelperFunctions.GetLocalIPv4();
        }

        private void SingleplayerBtn_OnClick()
        {
            Game.StartSingleplayerLobby();
        }

        private void MpHostBtn_OnClick()
        {
            Game.HostAndConnectToServer();
        }

        private void MpConnectBtn_OnClick()
        {
            Game.ConnectToServer(isHost: false);
        }

        private void QuitBtn_OnClick()
        {
            Application.Quit();
        }
    }
}
