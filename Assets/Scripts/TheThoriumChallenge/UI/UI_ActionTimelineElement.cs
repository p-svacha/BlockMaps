using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace TheThoriumChallenge
{
    public class UI_ActionTimelineElement : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Creature Creature { get; private set; }
        public bool IsActingNow { get; private set; }

        public GameObject SelectionFrame;
        public Button SelectionButton;
        public Image Background;
        public Image PreviewImage;
        public TextMeshProUGUI NatHoursMinutesText;

        private TooltipTarget TooltipTarget;

        // Double click
        private float DoubleClickTimeThreshold = 0.5f;
        private float LastClickTime = 0f;

        public void Init(Creature c, bool actingNow)
        {
            Creature = c;
            IsActingNow = actingNow;

            TooltipTarget = GetComponent<TooltipTarget>();
            TooltipTarget.Text = c.LabelCap;

            SelectionButton.onClick.AddListener(SelectionButton_OnClick);

            PreviewImage.sprite = c.UiSprite;

            if (actingNow) NatHoursMinutesText.text = "Now!";
            else NatHoursMinutesText.text = $"in {(c.NextActionTime - Game.Instance.CurrentStage.GlobalSimulationTime).ValueInSeconds}s";

            Background.color = c.IsPlayerControlled ? GameUI.Instance.FriendlyBackgroundColor : GameUI.Instance.HostileBackgroundColor;

            if (IsActingNow) Creature.OverheadLabel.ShowSelectedFrame(true);
        }

        private void SelectionButton_OnClick()
        {
            if (Time.time - LastClickTime < DoubleClickTimeThreshold) // Double click
            {
                Creature.World.CameraPanToFocusEntity(Creature, duration: 0.5f, false);
            }
            else // Single click
            {
                
            }

            LastClickTime = Time.time;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Creature.OverheadLabel.ShowSelectedFrame(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if(!IsActingNow) Creature.OverheadLabel.ShowSelectedFrame(false);
        }

        public void ShowSelectedFrame(bool show)
        {
            SelectionFrame.SetActive(show);
        }
    }
}
