using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CaptureTheFlag
{
    public class UI_CharacterAction : MonoBehaviour
    {
        private SpecialCharacterAction Action;

        [Header("Elements")]
        public Button Button;
        public TextMeshProUGUI TitleText;
        public Image Icon;

        public void Init(SpecialCharacterAction action)
        {
            Action = action;
            Button.onClick.AddListener(OnClick);
            TitleText.text = action.Name;
            Icon.sprite = action.Icon;
        }

        private void OnClick()
        {
            if (!Action.CanPerformNow()) return;

            if (Action.Match.MatchType == CtfMatchType.Singleplayer) Action.Perform();
            if (Action.Match.MatchType == CtfMatchType.Multiplayer) Action.Match.PerformMultiplayerAction(Action);

        }
    }
}
