using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BlockmapFramework
{
    public class UI_EntityInfoWindow : MonoBehaviour
    {
        // Singleton
        public static UI_EntityInfoWindow Instance;
        private void Awake()
        {
            Instance = this;
            gameObject.SetActive(false);
            CloseButton.onClick.AddListener(() => gameObject.SetActive(false));
        }

        [Header("Elements")]
        public TextMeshProUGUI TitleText;
        public TextMeshProUGUI DescriptionText;
        public Button CloseButton;

        public GameObject StatListContainer;
        public TextMeshProUGUI StatDetailsText;

        [Header("Prefabs")]
        public UI_EntityInfoWindow_StatRow StatRowPrefab;

        public void Show(Entity e)
        {
            gameObject.SetActive(true);
            TitleText.text = e.LabelCap;
            DescriptionText.text = e.Description;

            HelperFunctions.DestroyAllChildredImmediately(StatListContainer);
            foreach(Stat stat in e.GetAllStats())
            {
                UI_EntityInfoWindow_StatRow statRow = Instantiate(StatRowPrefab, StatListContainer.transform);
                statRow.Init(this, stat);
            }

            StatDetailsText.text = "";
        }

        public void ShowStatDetails(Stat stat)
        {
            // Label
            string text = stat.Def.LabelCap;

            // Description
            if(stat.Def.Description != "")
            {
                text += "\n\n" + stat.Def.Description;
            }

            // Base value
            text += $"\n\nBase Value: {stat.GetValueText(stat.Def.BaseValue)}";

            // Skill offsets
            foreach(SkillImpact skillOffset in stat.Def.SkillOffsets)
            {
                float offsetValue = skillOffset.GetValueFor(stat.Entity);
                string sign = offsetValue > 0 ? "+" : "";
                text += $"\n\t{skillOffset.SkillDef.LabelCap} Skill: {sign}{stat.GetValueText(offsetValue)}";
            }

            // Final value
            text += $"\n\nFinal Value: {stat.GetValueText(stat.GetValue())}";

            StatDetailsText.text = text;
        }
    }
}
