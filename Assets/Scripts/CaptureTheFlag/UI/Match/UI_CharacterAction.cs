using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace CaptureTheFlag
{
    public class UI_CharacterAction : MonoBehaviour, IPointerEnterHandler
    {
        private SpecialCharacterAction Action;

        [Header("Elements")]
        public Button Button;
        public TextMeshProUGUI TitleText;
        public Image Icon;
        public TextMeshProUGUI CostText;

        public void Init(SpecialCharacterAction action)
        {
            Action = action;
            Button.onClick.AddListener(OnClick);
            TitleText.text = action.Name;
            Icon.sprite = action.Icon;
            CostText.text = $"Cost: {action.Cost.ToString("0.#")}";

            GetComponent<TooltipTarget>().Text = action.Name;
        }

        private void OnClick()
        {
            if (!Action.CanPerformNow()) return;

            GetComponent<TooltipTarget>().HideTooltip();
            if (Action.Match.MatchType == CtfMatchType.Singleplayer) Action.Perform();
            if (Action.Match.MatchType == CtfMatchType.Multiplayer) Action.Match.PerformMultiplayerAction(Action);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Action.Match.HoveredAction = Action;
        }
    }
}
