using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
            NameText.text = creature.SpeciesDef.LabelCap;
            LevelText.text = creature.Level.ToString();
            DescriptionText.text = creature.SpeciesDef.Description;
            HealthBar.SetValue(creature.HP, creature.MaxHP, showText: true);

            HelperFunctions.DestroyAllChildredImmediately(StatContainer);
            foreach(CreatureStat stat in creature.Stats.Values)
            {
                if (!stat.Def.ShowInCreatureInfo) continue;

                UI_CreatureInfoStatRow statRow = Instantiate(StatRowPrefab, StatContainer.transform);
                statRow.Init(stat);
            }
        }
    }
}
