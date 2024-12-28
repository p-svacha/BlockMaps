using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CaptureTheFlag.UI
{
    public class UI_GameOverPanel : MonoBehaviour
    {
        public CtfMatch Match;

        [Header("Elements")]
        public TextMeshProUGUI Text;
        public Button MainMenuButton;

        public void Init(CtfMatch match)
        {
            Match = match;
            MainMenuButton.onClick.AddListener(() => Match.Game.GoToMainMenu());
        }
    }
}
