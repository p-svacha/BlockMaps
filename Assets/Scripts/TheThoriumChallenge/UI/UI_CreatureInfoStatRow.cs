using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace TheThoriumChallenge
{
    public class UI_CreatureInfoStatRow : MonoBehaviour
    {
        public TextMeshProUGUI LabelText;
        public TextMeshProUGUI ValueText;

        public void Init(CreatureStat stat)
        {
            LabelText.text = stat.Def.LabelShort;
            ValueText.text = stat.GetValueText();

            GetComponent<TooltipTarget>().Title = stat.Def.LabelCap;
            GetComponent<TooltipTarget>().Text = stat.GetBreakdownString();
        }
    }
}
