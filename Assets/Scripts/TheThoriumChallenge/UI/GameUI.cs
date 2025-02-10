using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TheThoriumChallenge
{
    public class GameUI : MonoBehaviour
    {
        public static GameUI Instance;
        private Game Game;

        [Header("Elements")]
        public GameObject LoadingScreenOverlay;

        public TextMeshProUGUI TimeText;
        public UI_ActionTimeline ActionTimeline;
        public GameObject CreatureLabelContainer;
        public UI_CreatureInfo CreatureInfo;

        [Header("Prefabs")]
        public UI_CreatureLabel CreatureLabelPrefab;

        [Header("Colors")]
        public Color FriendlyTextColor;
        public Color FriendlyBackgroundColor;
        public Color HostileTextColor;
        public Color HostileBackgroundColor;

        private void Awake()
        {
            Instance = this;
        }

        public void OnGameStarting(Game game)
        {
            Game = game;
        }

        public void RefreshTimeText()
        {
            TimeText.text = Game.CurrentLevel.GlobalSimulationTime.GetAsString();
        }

        public void ShowCreatureInfo(Creature creature)
        {
            CreatureInfo.gameObject.SetActive(true);
            CreatureInfo.Show(creature);
        }
        public void HideCreatureInfo()
        {
            CreatureInfo.gameObject.SetActive(false);
        }
    }
}
