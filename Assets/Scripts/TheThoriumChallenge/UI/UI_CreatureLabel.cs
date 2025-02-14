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
        public TextMeshProUGUI NameText;
        public UI_ProgressBar HealthBar;

        public void Init(Creature creature)
        {
            Creature = creature;

            WorldOffset = new Vector3(0f, (creature.Height / 2f) + 0.1f, 0f);

            NameText.text = $"{creature.Def.LabelCap} [{creature.Level}]";
            Color color = creature.IsPlayerControlled ? GameUI.Instance.FriendlyTextColor : GameUI.Instance.HostileTextColor;
            NameText.color = color;
            HealthBar.SetValue(creature.HP, creature.MaxHP, showText: false);
            HealthBar.ProgressBar.GetComponent<Image>().color = color;
        }

        private void Update()
        {
            if (Creature == null) return;

            bool showLabel = Creature.IsVisible || Creature.IsExploredBy(Game.Instance.CurrentLevel.LocalPlayer);

            if (showLabel)
            {
                NameText.gameObject.SetActive(true);
                HealthBar.gameObject.SetActive(true);

                Vector3 targetWorldPosition = Creature.IsVisible ? Creature.MeshObject.transform.position + WorldOffset : (Vector3)Creature.LastKnownPosition[Game.Instance.LocalPlayer] + WorldOffset;
                Vector3 screenPosition = Creature.World.Camera.Camera.WorldToScreenPoint(targetWorldPosition);
                transform.position = screenPosition;

                // Transparent when not currently visible
                if (Creature.IsVisible && NameText.color.a != 1f) NameText.color = new Color(NameText.color.r, NameText.color.g, NameText.color.b, 1f);
                else if (!Creature.IsVisible && NameText.color.a != 0.5f) NameText.color = new Color(NameText.color.r, NameText.color.g, NameText.color.b, 0.3f);
            }

            else
            {
                NameText.gameObject.SetActive(false);
                HealthBar.gameObject.SetActive(false);
            }
        }
    }
}
