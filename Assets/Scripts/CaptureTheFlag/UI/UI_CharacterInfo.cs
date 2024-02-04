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
        public UI_ProgressBar ActionBar;
        public UI_ProgressBar StaminaBar;

        public void Init(Character c)
        {
            gameObject.SetActive(true);
            Character = c;

            TitleText.text = c.Name;
            MovementText.text = c.MovementSkill.ToString();
            StaminaRegenText.text = c.StaminaRegeneration.ToString();
            ActionBar.UpdateValues(c.ActionPoints, c.MaxActionPoints, c.ActionPoints.ToString() + "/" + c.MaxActionPoints.ToString());
            StaminaBar.UpdateValues(c.Stamina, c.MaxStamina, c.Stamina.ToString() + "/" + c.MaxStamina.ToString());
        }
    }
}
