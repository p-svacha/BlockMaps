using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CaptureTheFlag.UI
{
    public class UI_EndGameScreen : MonoBehaviour
    {
        public CtfGame Game;

        [Header("Elements")]
        public TextMeshProUGUI Text;
        public Button MainMenuButton;

        public void Init(CtfGame game)
        {
            Game = game;
            MainMenuButton.onClick.AddListener(() => Game.GoToMainMenu());
        }
    }
}
