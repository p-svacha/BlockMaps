using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BlockmapFramework
{
    public class UI_EntityInfoWindow_StatRow : MonoBehaviour
    {
        [Header("Colors")]
        public Color SelectedColor;

        [Header("Elements")]
        public TextMeshProUGUI LabelText;
        public TextMeshProUGUI ValueText;
        public Button Button;

        public void Init(UI_EntityInfoWindow window, Stat stat)
        {
            LabelText.text = stat.Def.LabelCap;
            ValueText.text = stat.GetValueText(stat.GetValue());
            Button.onClick.AddListener(() => window.ShowStatDetails(stat));
        }
    }
}
