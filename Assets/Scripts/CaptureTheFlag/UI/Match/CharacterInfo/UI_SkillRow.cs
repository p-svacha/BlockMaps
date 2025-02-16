using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlockmapFramework;

namespace BlockmapFramework.UI
{
    public class UI_SkillRow : MonoBehaviour
    {
        [Header("Elements")]
        public TextMeshProUGUI LabelText;
        public GameObject ValueContainer;
        public GameObject ValueBar;
        public TextMeshProUGUI ValueText;

        public void Init(Skill skill)
        {
            LabelText.text = skill.Def.LabelCap;
            int value = skill.GetSkillLevel();
            ValueText.text = value.ToString("0.#");

            if (value == 0)
            {
                HideValueBar();
            }

            else
            {
                float maxValue = skill.Def.MaxLevel;
                float ratio = value / maxValue;
                ValueBar.GetComponent<RectTransform>().anchorMax = new Vector2(ratio, 1f);
            }

            TooltipTarget tooltip = GetComponent<TooltipTarget>();
            tooltip.Title = skill.Def.LabelCap;
            tooltip.Text = $"{skill.Def.Description}";
            tooltip.Text += $"\n\nBase Level: {skill.BaseLevel}";
            tooltip.Text += $"\nFinal Value: {skill.GetSkillLevel()}";
        }

        public void Init(string label, string value)
        {
            LabelText.text = label;
            ValueText.text = value;
            HideValueBar();
            Destroy(GetComponent<TooltipTarget>());
        }

        private void HideValueBar()
        {
            ValueBar.GetComponent<RectTransform>().anchorMax = new Vector2(0f, 1f);
        }
    }
}
