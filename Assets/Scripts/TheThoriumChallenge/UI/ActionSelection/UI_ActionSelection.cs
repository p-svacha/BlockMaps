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

        [Header("Prefabs")]
        public GameObject AbilitySelectionRowPrefab;
        public UI_AbilitySelectionElement AbilitySelectionElementPrefab;

        private List<Ability> Abilities;
        private Dictionary<int, UI_AbilitySelectionElement> AbilitySelectionButtons;
        private UI_AbilitySelectionElement HighlightedAbility;

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
            Abilities = creature.Abilities.GetAllAbilities();
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
            if (HighlightedAbility != null) HighlightedAbility.Background.color = GameUI.Instance.UiButtonColor_Default;
            HighlightedAbility = AbilitySelectionButtons[index];
            HighlightedAbility.Background.color = GameUI.Instance.UiButtonColor_Highlighted;
            if (index >= Abilities.Count)
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
            }
        }
        public void SetSelectedAbility(Ability ability)
        {

        }
        public void SetHighlightedTarget(BlockmapNode node)
        {

        }
    }
}
