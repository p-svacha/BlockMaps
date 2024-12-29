using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CaptureTheFlag.UI
{
    public class UI_CharacterInfo : MonoBehaviour
    {
        private CtfMatch Match;
        private CtfCharacter Character;
        private CharacterAction HoveredAction;

        [Header("Elements")]
        public TextMeshProUGUI TitleText;
        public UI_ToggleButton VisionCutoffButton;

        public TextMeshProUGUI DescriptionText;
        public UI_ToggleButton StatButton;
        public GameObject StatPanel;
        public GameObject StatListContainer;

        public UI_ProgressBar ActionBar;
        public UI_ProgressBar StaminaBar;

        [Header("Prefabs")]
        public UI_StatRow StatRowPrefab;

        // internal
        private bool IsStatWindowActive;

        public void Init(CtfMatch match)
        {
            Match = match;
            VisionCutoffButton.Button.onClick.AddListener(() => { Match.ToggleVisionCutoff(); VisionCutoffButton.SetToggle(Match.IsVisionCutoffEnabled); });
            StatButton.Button.onClick.AddListener(StatButton_OnClick);
            StatPanel.SetActive(false);
        }

        public void ShowCharacter(CtfCharacter c, CharacterAction hoveredAction = null)
        {
            if (Character == c && HoveredAction == hoveredAction) return;

            gameObject.SetActive(true);
            Character = c;
            HoveredAction = hoveredAction;

            TitleText.text = c.LabelCap;
            DescriptionText.text = c.Description.ToString();
            RefreshStatPanel();
            ActionBar.SetValue(c.ActionPoints, c.MaxActionPoints, showText: true, "0.#");
            StaminaBar.SetValue(c.Stamina, c.MaxStamina, showText: true, "0.#");

            VisionCutoffButton.SetToggle(Match.IsVisionCutoffEnabled);

            if (hoveredAction != null)
            {
                if (hoveredAction.CanPerformNow() || Character.CurrentAction == hoveredAction)
                {
                    ShowActionPreview(hoveredAction.Cost);
                }
            }
        }

        private void StatButton_OnClick()
        {
            IsStatWindowActive = !IsStatWindowActive;
            StatButton.SetToggle(IsStatWindowActive);

            if(IsStatWindowActive)
            {
                RefreshStatPanel();
                StatPanel.SetActive(true);
            }
            else
            {
                StatPanel.SetActive(false);
            }
        }

        private void RefreshStatPanel()
        {
            HelperFunctions.DestroyAllChildredImmediately(StatListContainer);
            foreach (Stat stat in Character.GetAllStats())
            {
                UI_StatRow statRow = Instantiate(StatRowPrefab, StatListContainer.transform);
                statRow.Init(stat);
            }
        }

        private void ShowActionPreview(float cost)
        {
            ActionBar.SetPendingValue(Character.ActionPoints, Character.ActionPoints - cost, Character.MaxActionPoints, valueFormat: "0.#", ActionBar.ProgressBar.GetComponent<Image>().color, Color.black);
            StaminaBar.SetPendingValue(Character.Stamina, Character.Stamina - cost, Character.MaxStamina, valueFormat: "0.#", StaminaBar.ProgressBar.GetComponent<Image>().color, Color.black);
        }
    }
}
