using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TheThoriumChallenge
{
    public class UI_Game : MonoBehaviour
    {
        private Game Game;

        public GameObject LoadingScreenOverlay;

        public TextMeshProUGUI TimeText;
        public UI_ActionTimeline ActionTimeline;

        public void OnGameStarting(Game game)
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
