using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace TheThoriumChallenge
{
    public class UI_AbilitySelectionElement : MonoBehaviour, IPointerEnterHandler
    {
        private UI_ActionSelection ActionSelection;
        private int AbilityIndex;

        [Header("Elements")]
        public Image Background;
        public Button Button;
        public TextMeshProUGUI AbilityNameText;

        public void Init(UI_ActionSelection actionSelection, Ability ability, int abilityIndex)
        {
            ActionSelection = actionSelection;
            AbilityIndex = abilityIndex;

            if (ability == null)
            {
                AbilityNameText.text = "- - - - -";
            }
            else
            {
                Button.onClick.AddListener(() => Game.Instance.CurrentStage.GoToChooseTargetMode(ability));
                AbilityNameText.text = ability.Label;
            }

            Background.color = GameUI.Instance.UiButtonColor_Default;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ActionSelection.SetHighlightedAbility(AbilityIndex);
        }
    }
}
