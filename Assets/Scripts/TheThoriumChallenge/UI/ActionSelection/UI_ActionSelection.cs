using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlockmapFramework;

namespace TheThoriumChallenge
{
    public class UI_ActionSelection : MonoBehaviour
    {
        [Header("Elements")]
        public Button DoNothingButton;

        public GameObject DescriptionPanel;
        public TextMeshProUGUI AbilityDescriptionText;
        public GameObject CostPanel;
        public TextMeshProUGUI AbilityCostText;

        public GameObject AbilitySelectionRowContainer;

        public GameObject NotificationTextPanel;
        public TextMeshProUGUI NotificationText;

        [Header("Prefabs")]
        public GameObject AbilitySelectionRowPrefab;
        public UI_AbilitySelectionElement AbilitySelectionElementPrefab;

        private List<Ability> Abilities;
        private Dictionary<int, UI_AbilitySelectionElement> AbilitySelectionButtons;
        private UI_AbilitySelectionElement HighlightedAbility;
        private UI_AbilitySelectionElement SelectedAbility;

        private void Awake()
        {
            DoNothingButton.onClick.AddListener(() => Game.Instance.CurrentStage.DoNothing());
        }

        public void Init(Creature creature)
        {
            HighlightedAbility = null;
            int numAbilitiesPerRow = 3;
            AbilitySelectionButtons = new Dictionary<int, UI_AbilitySelectionElement>();

            HelperFunctions.DestroyAllChildredImmediately(AbilitySelectionRowContainer);
            Abilities = creature.Abilities;
            int numAbilities = Abilities.Count;
            int numRows = ((numAbilities - 1) / numAbilitiesPerRow) + 1;

            for(int i = 0; i < numRows; i++)
            {
                GameObject row = Instantiate(AbilitySelectionRowPrefab, AbilitySelectionRowContainer.transform);
                for(int j = 0; j < numAbilitiesPerRow; j++)
                {
                    int abilityIndex = i * numAbilitiesPerRow + j;
                    UI_AbilitySelectionElement abilityElem = Instantiate(AbilitySelectionElementPrefab, row.transform);
                    if (abilityIndex >= numAbilities) abilityElem.Init(this, null, abilityIndex); // Dummy button
                    else abilityElem.Init(this, Abilities[abilityIndex], abilityIndex);

                    AbilitySelectionButtons.Add(abilityIndex, abilityElem);
                }
            }

            SetHighlightedAbility(0);
        }

        public void SetHighlightedAbility(int index)
        {
            if (SelectedAbility != null) return;

            if (HighlightedAbility != null) HighlightedAbility.Background.color = GameUI.Instance.UiButtonColor_Default;
            HighlightedAbility = AbilitySelectionButtons[index];
            HighlightedAbility.Background.color = GameUI.Instance.UiButtonColor_Highlighted;
            if (index >= Abilities.Count) // Dummy button - no ability
            {
                DescriptionPanel.gameObject.SetActive(false);
                CostPanel.gameObject.SetActive(false);
            }
            else
            {
                Ability ability = Abilities[index];
                DescriptionPanel.gameObject.SetActive(true);
                CostPanel.gameObject.SetActive(true);
                AbilityDescriptionText.text = ability.Description;
                AbilityCostText.text = ability.BaseCost.ToString();

                if(ability.GetPossibleTargets().Count == 0)
                {
                    NotificationTextPanel.SetActive(true);
                    NotificationText.text = "No valid targets";
                }
                else NotificationTextPanel.SetActive(false);
            }
        }
        public void SetSelectedAbility(Ability ability)
        {
            if (SelectedAbility != null) SelectedAbility.Background.color = GameUI.Instance.UiButtonColor_Default;
            if (HighlightedAbility != null)
            {
                HighlightedAbility.Background.color = GameUI.Instance.UiButtonColor_Default;
                HighlightedAbility = null;
            }

            SelectedAbility = AbilitySelectionButtons[Abilities.IndexOf(ability)];
            SelectedAbility.Background.color = GameUI.Instance.UiButtonColor_Selected;
        }
        public void UnsetSelectedAbility()
        {
            if (SelectedAbility != null)
            {
                UI_AbilitySelectionElement selectedAbility = SelectedAbility;
                SelectedAbility = null;
                SetHighlightedAbility(selectedAbility.AbilityIndex);
            }
        }

        public void SetHighlightedTarget(BlockmapNode node)
        {
            AbilityCostText.text = SelectedAbility.Ability.GetCost(node).ToString();
        }
        public void UnsetHighlightedTarget()
        {
            AbilityCostText.text = SelectedAbility.Ability.BaseCost.ToString();
        }
    }
}
