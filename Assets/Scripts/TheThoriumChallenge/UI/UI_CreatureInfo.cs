using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlockmapFramework;

namespace TheThoriumChallenge
{
    public class UI_CreatureInfo : MonoBehaviour
    {
        [Header("Elements")]
        public TextMeshProUGUI NameText;
        public TextMeshProUGUI LevelText;
        public TextMeshProUGUI DescriptionText;
        public UI_ProgressBar HealthBar;

        public GameObject StatContainer;

        [Header("Prefabs")]
        public UI_CreatureInfoStatRow StatRowPrefab;

        private void Awake()
        {
            gameObject.SetActive(false);
        }

        public void Show(Creature creature)
        {
            NameText.text = creature.Def.LabelCap;
            LevelText.text = creature.Level.ToString();
            DescriptionText.text = creature.Def.Description;
            HealthBar.SetValue(creature.HP, creature.MaxHP, showText: true);

            HelperFunctions.DestroyAllChildredImmediately(StatContainer);
            foreach(Stat stat in creature.Stats.GetAllStats())
            {
                UI_CreatureInfoStatRow statRow = Instantiate(StatRowPrefab, StatContainer.transform);
                statRow.Init(stat);
            }
        }
    }
}
