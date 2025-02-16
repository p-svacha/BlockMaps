using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlockmapFramework;
using BlockmapFramework.UI;

namespace CaptureTheFlag.UI
{
    public class UI_CharacterInfo : MonoBehaviour
    {
        private CtfMatch Match;

        [Header("Elements")]
        public TextMeshProUGUI TitleText;
        public UI_ToggleButton VisionCutoffButton;
        public Button InfoButton;

        public TextMeshProUGUI DescriptionText;
        public UI_ToggleButton StatButton;
        public GameObject SkillPanel;
        public GameObject SkillListContainer;

        public TextMeshProUGUI ActionPointsPerTurnText;
        public UI_ProgressBar ActionBar;
        public TextMeshProUGUI StaminaPerTurnText;
        public UI_ProgressBar StaminaBar;

        public Image ItemIcon;
        public Button ItemButton;

        [Header("Prefabs")]
        public UI_SkillRow SkillRowPrefab;

        // internal
        private bool IsStatWindowActive;
        private CtfCharacter displayedCharacter;

        public void Init(CtfMatch match)
        {
            Match = match;
            VisionCutoffButton.Button.onClick.AddListener(() => { Match.ToggleVisionCutoff(); VisionCutoffButton.SetToggle(Match.IsVisionCutoffEnabled); });
            StatButton.Button.onClick.AddListener(StatButton_OnClick);
            InfoButton.onClick.AddListener(() => UI_EntityInfoWindow.Instance.Show(Match.SelectedCharacter));
            SkillPanel.SetActive(false);
            ItemButton.onClick.AddListener(Item_OnClick);
        }

        /// <summary>
        /// Called every frame.
        /// </summary>
        public void UpdateCharacterInfo()
        {
            if(Match.SelectedCharacter == null)
            {
                if (gameObject.activeSelf) gameObject.SetActive(false);
                return;
            }

            if(Match.SelectedCharacter != null)
            {
                CtfCharacter c = Match.SelectedCharacter;
                if (!gameObject.activeSelf) gameObject.SetActive(true);

                // Some elements only need to be redrawn when selected character changes
                if (c != displayedCharacter)
                {
                    TitleText.text = c.LabelCap;
                    DescriptionText.text = c.Description.ToString();
                    RefreshSkillPanel(c);
                    ActionPointsPerTurnText.text = $"+{c.CtfComp.MaxActionPoints} / turn";
                    StaminaPerTurnText.text = $"+{c.StaminaRegeneration.ToString("0.#")} / turn";

                    displayedCharacter = c;

                    // Item
                    RefreshItemDisplay();
                }

                // Some elements need to be redrawn every frame
                ActionBar.SetValue(c.ActionPoints, c.MaxActionPoints, showText: true, "0.#");
                StaminaBar.SetValue(c.Stamina, c.MaxStamina, showText: true, "0.#");
                VisionCutoffButton.SetToggle(Match.IsVisionCutoffEnabled);

                if (Match.HoveredAction != null)
                {
                    if (Match.HoveredAction.CanPerformNow() || c.CurrentAction == Match.HoveredAction)
                    {
                        ShowActionPreview(c, Match.HoveredAction.Cost);
                    }
                }
            }
        }

        public void RefreshItemDisplay()
        {
            if (Match.SelectedCharacter == null) return;

            if (Match.SelectedCharacter.HeldItem != null)
            {
                ItemIcon.gameObject.SetActive(true);
                ItemIcon.sprite = Match.SelectedCharacter.HeldItem.UiSprite;
            }
            else
            {
                ItemIcon.gameObject.SetActive(false);
            }
        }

        private void Item_OnClick()
        {
            if (Match.SelectedCharacter == null) return;
            if (Match.SelectedCharacter.HeldItem == null) return;

            List<ContextMenuOption> options = new List<ContextMenuOption>()
            {
                new ContextMenuOption("Consume", () => { Debug.Log("Consume"); }),
                new ContextMenuOption("Drop", () => { Debug.Log("Drop"); }),
            };
            ContextMenu.Instance.Show(options);
        }

        private void StatButton_OnClick()
        {
            IsStatWindowActive = !IsStatWindowActive;
            StatButton.SetToggle(IsStatWindowActive);

            if (IsStatWindowActive)
            {
                RefreshSkillPanel(Match.SelectedCharacter);
                SkillPanel.SetActive(true);
            }
            else
            {
                SkillPanel.SetActive(false);
            }
        }

        private void RefreshSkillPanel(CtfCharacter c)
        {
            HelperFunctions.DestroyAllChildredImmediately(SkillListContainer);

            // Skills
            foreach (Skill skill in c.GetAllSkills())
            {
                UI_SkillRow skillRow = Instantiate(SkillRowPrefab, SkillListContainer.transform);
                skillRow.Init(skill);
            }

            // Additional info
            UI_SkillRow canUseDoorsRow = Instantiate(SkillRowPrefab, SkillListContainer.transform);
            canUseDoorsRow.Init("Can use doors", c.CanInteractWithDoors ? "Yes" : "No");
        }

        private void ShowActionPreview(CtfCharacter c, float cost)
        {
            ActionBar.SetPendingValue(c.ActionPoints, c.ActionPoints - cost, c.MaxActionPoints, valueFormat: "0.#", ActionBar.ProgressBar.GetComponent<Image>().color, Color.black);
            StaminaBar.SetPendingValue(c.Stamina, c.Stamina - cost, c.MaxStamina, valueFormat: "0.#", StaminaBar.ProgressBar.GetComponent<Image>().color, Color.black);
        }
    }
}
