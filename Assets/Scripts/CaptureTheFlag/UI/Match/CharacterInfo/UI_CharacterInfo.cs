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

                TitleText.text = c.LabelCap;
                DescriptionText.text = c.Description.ToString();
                RefreshStatPanel(c);
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

        private void StatButton_OnClick()
        {
            IsStatWindowActive = !IsStatWindowActive;
            StatButton.SetToggle(IsStatWindowActive);

            if (IsStatWindowActive)
            {
                RefreshStatPanel(Match.SelectedCharacter);
                StatPanel.SetActive(true);
            }
            else
            {
                StatPanel.SetActive(false);
            }
        }

        private void RefreshStatPanel(CtfCharacter c)
        {
            HelperFunctions.DestroyAllChildredImmediately(StatListContainer);
            foreach (Stat stat in c.GetAllStats())
            {
                UI_StatRow statRow = Instantiate(StatRowPrefab, StatListContainer.transform);
                statRow.Init(stat);
            }
        }

        private void ShowActionPreview(CtfCharacter c, float cost)
        {
            ActionBar.SetPendingValue(c.ActionPoints, c.ActionPoints - cost, c.MaxActionPoints, valueFormat: "0.#", ActionBar.ProgressBar.GetComponent<Image>().color, Color.black);
            StaminaBar.SetPendingValue(c.Stamina, c.Stamina - cost, c.MaxStamina, valueFormat: "0.#", StaminaBar.ProgressBar.GetComponent<Image>().color, Color.black);
        }
    }
}
