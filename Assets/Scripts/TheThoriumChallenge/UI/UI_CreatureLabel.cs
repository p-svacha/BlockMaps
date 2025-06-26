using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TheThoriumChallenge
{
    public class UI_CreatureLabel : MonoBehaviour
    {
        private Creature Creature;
        private Vector3 WorldOffset;

        [Header("Elements")]
        public GameObject Content;

        public GameObject SelectionFrame;
        public TextMeshProUGUI NameText;
        public Image LevelBackground;
        public TextMeshProUGUI LevelText;
        public UI_ProgressBar HealthBar;

        public void Init(Creature creature)
        {
            Creature = creature;

            WorldOffset = new Vector3(0f, (creature.Height / 2f) + 0.1f, 0f);

            NameText.text = $"{creature.Def.LabelCap}";
            LevelText.text = $"{creature.Level}";

            Color color = creature.IsPlayerControlled ? GameUI.Instance.FriendlyTextColor : GameUI.Instance.HostileTextColor;
            NameText.color = color;
            LevelBackground.color = color;
            SelectionFrame.GetComponent<Image>().color = color;

            HealthBar.SetValue(creature.HP, creature.MaxHP, showText: false);
            HealthBar.ProgressBar.GetComponent<Image>().color = color;
        }

        private void Update()
        {
            if (Creature == null) return;

            bool showLabel = Creature.IsVisible || Creature.IsExploredBy(Game.Instance.CurrentStage.LocalPlayer);

            if (showLabel)
            {
                Content.SetActive(true);

                Vector3 targetWorldPosition = Creature.GetLastSeenInfo(Game.Instance.LocalPlayer).Position + WorldOffset;
                Vector3 screenPosition = Creature.World.Camera.Camera.WorldToScreenPoint(targetWorldPosition);
                transform.position = screenPosition;

                // Transparent when not currently visible
                if (Creature.IsVisible && NameText.color.a != 1f) NameText.color = new Color(NameText.color.r, NameText.color.g, NameText.color.b, 1f);
                else if (!Creature.IsVisible && NameText.color.a != 0.5f) NameText.color = new Color(NameText.color.r, NameText.color.g, NameText.color.b, 0.3f);

                // Healthbar
                if (Creature.IsVisible)
                {
                    HealthBar.SetValue(Creature.HP, Creature.MaxHP, showText: false);
                }
            }

            else
            {
                Content.SetActive(false);
            }
        }

        public void ShowSelectedFrame(bool show)
        {
            SelectionFrame.SetActive(show);
        }
    }
}
