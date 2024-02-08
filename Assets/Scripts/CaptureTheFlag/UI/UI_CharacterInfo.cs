using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CaptureTheFlag
{
    public class UI_CharacterInfo : MonoBehaviour
    {
        private Character Character;

        [Header("Elements")]
        public TextMeshProUGUI TitleText;
        public TextMeshProUGUI MovementText;
        public TextMeshProUGUI StaminaRegenText;
        public TextMeshProUGUI VisionText;
        public UI_ProgressBar ActionBar;
        public UI_ProgressBar StaminaBar;

        public void Init(Character c)
        {
            gameObject.SetActive(true);
            Character = c;

            TitleText.text = c.Name;
            MovementText.text = c.MovementSkill.ToString();
            StaminaRegenText.text = c.StaminaRegeneration.ToString();
            VisionText.text = c.Entity.VisionRange.ToString();
            ActionBar.SetValue(c.ActionPoints, c.MaxActionPoints, showText: true, "0.#");
            StaminaBar.SetValue(c.Stamina, c.MaxStamina, showText: true, "0.#");
        }

        public void ShowActionPreview(float cost)
        {
            ActionBar.SetPendingValue(Character.ActionPoints, Character.ActionPoints - cost, Character.MaxActionPoints, valueFormat: "0.#", ActionBar.ProgressBar.GetComponent<Image>().color, Color.black);
            StaminaBar.SetPendingValue(Character.Stamina, Character.Stamina - cost, Character.MaxStamina, valueFormat: "0.#", StaminaBar.ProgressBar.GetComponent<Image>().color, Color.black);
        }
    }
}
