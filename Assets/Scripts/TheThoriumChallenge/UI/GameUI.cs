using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlockmapFramework;

namespace TheThoriumChallenge
{
    public class GameUI : MonoBehaviour
    {
        public static GameUI Instance;
        private Game Game;

        [Header("Elements")]
        public GameObject LoadingScreenOverlay;
        public UI_EntityInfoWindow EntityInfoWindow;

        public TextMeshProUGUI TimeText;
        public UI_ActionTimeline ActionTimeline;
        public GameObject CreatureLabelContainer;
        public UI_CreatureInfo CreatureInfo;
        public UI_ActionSelection ActionSelection;

        [Header("Prefabs")]
        public UI_CreatureLabel CreatureLabelPrefab;

        [Header("UI Base Colors")]
        public Color UiBackgroundColor;
        public Color UiButtonColor_Default;
        public Color UiButtonColor_Highlighted;
        public Color UiButtonColor_Selected;

        [Header("Special Colors")]
        public Color FriendlyTextColor;
        public Color FriendlyBackgroundColor;
        public Color HostileTextColor;
        public Color HostileBackgroundColor;

        private Creature CreatureInfoCreature;

        private void Awake()
        {
            Instance = this;
            EntityInfoWindow.gameObject.SetActive(false);
        }

        public void OnGameStarting(Game game)
        {
            Game = game;
        }

        private void Update()
        {
            if (Game == null) return;

            UpdateCreatureInfo();
        }

        public void RefreshTimeText()
        {
            TimeText.text = Game.CurrentStage.GlobalSimulationTime.GetAsString();
        }

        public void UpdateCreatureInfo()
        {
            Stage stage = Game.CurrentStage;

            Creature prevInfoCreature = CreatureInfoCreature;
            if (stage.HoveredCreature != null) CreatureInfoCreature = stage.HoveredCreature;
            else if (stage.ActiveTurnCreature != null && stage.ActiveTurnCreature.IsVisible) CreatureInfoCreature = stage.ActiveTurnCreature;
            else CreatureInfoCreature = null;

            if(CreatureInfoCreature == null) CreatureInfo.gameObject.SetActive(false);
            else
            {
                if (prevInfoCreature == null) CreatureInfo.gameObject.SetActive(true);
                if (CreatureInfoCreature != prevInfoCreature)
                {
                    CreatureInfo.Show(CreatureInfoCreature);
                }
            }
        }

        public void ShowCreatureActionSelection(Creature creature)
        {
            ActionSelection.gameObject.SetActive(true);
            ActionSelection.Init(creature);
        }
        public void HidereatureActionSelection()
        {
            ActionSelection.gameObject.SetActive(false);
        }
    }
}
