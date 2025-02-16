using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using BlockmapFramework;

namespace TheThoriumChallenge
{
    public class UI_CreatureInfoStatRow : MonoBehaviour
    {
        public TextMeshProUGUI LabelText;
        public TextMeshProUGUI ValueText;

        public void Init(Stat stat)
        {
            LabelText.text = stat.Def.Label;
            ValueText.text = stat.GetValueText();

            GetComponent<TooltipTarget>().Title = stat.Def.LabelCap;
            GetComponent<TooltipTarget>().Text = stat.GetBreakdownString();
        }
    }
}
