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
        public TextMeshProUGUI NatSecondsText;

        private TooltipTarget TooltipTarget;

        public void Init(Creature e)
        {
            TooltipTarget = GetComponent<TooltipTarget>();
            TooltipTarget.Text = e.LabelCap;

            PreviewImage.sprite = e.UiSprite;
            NatHoursMinutesText.text = e.NextActionTime.GetAbsoluteTimeString(includeDays: false);
            NatSecondsText.text = ":" + e.NextActionTime.GetAbsoluteTimeString(includeDays: false, includeHours: false, includeMinutes: false, includeSeconds: true);
        }
    }
}
