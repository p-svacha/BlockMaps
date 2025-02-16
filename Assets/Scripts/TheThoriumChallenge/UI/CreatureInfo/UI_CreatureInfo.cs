using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlockmapFramework;
using BlockmapFramework.UI;

namespace TheThoriumChallenge
{
    public class UI_CreatureInfo : MonoBehaviour
    {
        private Creature Creature;

        [Header("Elements")]
        public TextMeshProUGUI NameText;
        public TextMeshProUGUI LevelText;
        public TextMeshProUGUI DescriptionText;
        public UI_ProgressBar HealthBar;
        public UI_SkillList SkillList;
        public Button InfoButton;

        private void Awake()
        {
            gameObject.SetActive(false);
            InfoButton.onClick.AddListener(() => GameUI.Instance.EntityInfoWindow.Show(Creature));
        }

        public void Show(Creature creature)
        {
            Creature = creature;
            NameText.text = creature.Def.LabelCap;
            LevelText.text = $"Level {creature.Level}";
            DescriptionText.text = creature.Def.Description;
            HealthBar.SetValue(creature.HP, creature.MaxHP, showText: true);

            SkillList.Init(creature.SkillsComp);
        }
    }
}
