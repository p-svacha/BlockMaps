using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TheThoriumChallenge
{
    public class UI_ActionTimelineElement : MonoBehaviour
    {
        public GameObject SelectionFrame;
        public Button SelectionButton;
        public Image Background;
        public Image PreviewImage;
        public TextMeshProUGUI NatHoursMinutesText;

        private TooltipTarget TooltipTarget;

        public void Init(Creature c)
        {
            TooltipTarget = GetComponent<TooltipTarget>();
            TooltipTarget.Text = c.LabelCap;

            PreviewImage.sprite = c.UiSprite;
            NatHoursMinutesText.text = $"in {(c.NextActionTime - Game.Instance.CurrentStage.GlobalSimulationTime).ValueInSeconds}s";

            Background.color = c.IsPlayerControlled ? GameUI.Instance.FriendlyBackgroundColor : GameUI.Instance.HostileBackgroundColor;
        }
    }
}
