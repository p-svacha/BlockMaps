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

        public GameObject ClassesContainer;

        public TextMeshProUGUI DescriptionText;
        public UI_ProgressBar HealthBar;
        public UI_SkillList SkillList;
        public Button InfoButton;

        [Header("Prefabs")]
        public UI_CreatureClassFlag ClassFlagPrefab;

        private void Awake()
        {
            gameObject.SetActive(false);
            InfoButton.onClick.AddListener(() => GameUI.Instance.EntityInfoWindow.Show(Creature));
        }

        public void Show(Creature creature)
        {
            Creature = creature;
            Color color = creature.IsPlayerControlled ? GameUI.Instance.FriendlyTextColor : GameUI.Instance.HostileTextColor;

            NameText.text = creature.Def.LabelCap;
            NameText.color = color;
            LevelText.text = $"Level {creature.Level}";

            HelperFunctions.DestroyAllChildredImmediately(ClassesContainer);
            foreach(CreatureClassDef classDef in creature.Classes)
            {
                UI_CreatureClassFlag classFlag = Instantiate(ClassFlagPrefab, ClassesContainer.transform);
                classFlag.Init(classDef);
            }

            DescriptionText.text = creature.Def.Description;
            HealthBar.SetValue(creature.HP, creature.MaxHP, showText: creature.IsPlayerControlled);
            HealthBar.SetBarColor(color);

            SkillList.Init(creature.SkillsComp);
        }
    }
}
