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

        [Header("Elements")]
        public TextMeshProUGUI TitleText;
        public TextMeshProUGUI MovementText;
        public TextMeshProUGUI StaminaRegenText;
        public TextMeshProUGUI VisionText;
        public UI_ProgressBar ActionBar;
        public UI_ProgressBar StaminaBar;

        public UI_ToggleButton VisionCutoffButton;

        public void Init(CtfMatch match)
        {
            Match = match;
            VisionCutoffButton.Button.onClick.AddListener(() => { Match.ToggleVisionCutoff(); VisionCutoffButton.SetToggle(Match.IsVisionCutoffEnabled); });
        }

        public void ShowCharacter(CtfCharacter c, CharacterAction hoveredAction = null)
        {
            gameObject.SetActive(true);
            Character = c;

            TitleText.text = c.LabelCap;
            MovementText.text = c.MovementSpeed.ToString();
            StaminaRegenText.text = c.StaminaRegeneration.ToString();
            VisionText.text = c.VisionRange.ToString();
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

        private void ShowActionPreview(float cost)
        {
            ActionBar.SetPendingValue(Character.ActionPoints, Character.ActionPoints - cost, Character.MaxActionPoints, valueFormat: "0.#", ActionBar.ProgressBar.GetComponent<Image>().color, Color.black);
            StaminaBar.SetPendingValue(Character.Stamina, Character.Stamina - cost, Character.MaxStamina, valueFormat: "0.#", StaminaBar.ProgressBar.GetComponent<Image>().color, Color.black);
        }
    }
}
