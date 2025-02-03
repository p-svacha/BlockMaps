using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ExodusOutposAlpha
{
    public class UI_EoaGame : MonoBehaviour
    {
        private EoaGame Game;

        public GameObject LoadingScreenOverlay;

        public TextMeshProUGUI TimeText;

        public void OnGameStarting(EoaGame game)
        {
            Game = game;

            RefreshTimeText();
        }

        public void RefreshTimeText()
        {
            TimeText.text = Game.GlobalSimulationTime.GetAbsoluteTimeString();
        }
    }
}
