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
        public Button SingleplayerButton;
        public Button MultiplayerHostButton;
        public Button MultiplayerConnectButton;
        public TMP_InputField MultiplayerIpInput;

        public void Init(CtfGame game)
        {
            Game = game;
            SingleplayerButton.onClick.AddListener(SingleplayerBtn_OnClick);
            MultiplayerHostButton.onClick.AddListener(MpHostBtn_OnClick);
            MultiplayerConnectButton.onClick.AddListener(MpConnectBtn_OnClick);
        }

        private void SingleplayerBtn_OnClick()
        {
            Game.StartSingleplayerMatch();
        }

        private void MpHostBtn_OnClick()
        {
            Game.HostServer();
        }

        private void MpConnectBtn_OnClick()
        {
            Game.ConnectToServer();
        }
    }
}
