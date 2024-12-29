using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CaptureTheFlag.UI
{
    public class UI_StatRow : MonoBehaviour
    {
        [Header("Elements")]
        public TextMeshProUGUI LabelText;
        public GameObject ValueContainer;
        public GameObject ValueBar;
        public TextMeshProUGUI ValueText;

        public void Init(Stat stat)
        {
            LabelText.text = stat.Def.LabelCap;
            float value = stat.GetValue();

            if (stat.Def.Type == StatType.Binary)
            {
                ValueText.text = value == 1f ? "Yes" : "No";
                HideValueBar();
            }

            else
            {
                if (value == 0)
                {
                    ValueText.text = "Unable";
                    HideValueBar();
                }

                else
                {
                    ValueText.text = value.ToString("0.#");

                    if (stat.Def.HigherIsBetter)
                    {
                        float maxValue = stat.Def.MaxValue;
                        float ratio = value / maxValue;
                        ValueBar.GetComponent<RectTransform>().anchorMax = new Vector2(ratio, 1f);
                    }
                    else HideValueBar();
                }
            }

            TooltipTarget tooltip = GetComponent<TooltipTarget>();
            tooltip.Title = stat.Def.LabelCap;
            tooltip.Text = $"{stat.Def.Description}";
            if(stat.Def.Type != StatType.Binary && stat.Def.HigherIsBetter)
                tooltip.Text += $"\n\nMaximun: {stat.Def.MaxValue}";
        }

        private void HideValueBar()
        {
            ValueBar.GetComponent<RectTransform>().anchorMax = new Vector2(0f, 1f);
        }
    }
}
