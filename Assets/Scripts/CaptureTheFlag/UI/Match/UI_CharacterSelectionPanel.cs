using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CaptureTheFlag
{
    public class UI_CharacterSelectionPanel : MonoBehaviour
    {
        private CtfCharacter Character;
        private CtfMatch Match;

        [Header("Elements")]
        public GameObject SelectionFrame;
        public Button SelectionButton;
        public TextMeshProUGUI TitleText;
        public TextMeshProUGUI JailTimeText;
        public Image PreviewImage;
        public UI_ProgressBar ActionBar;
        public UI_ProgressBar StaminaBar;

        // Double click
        private float DoubleClickTimeThreshold = 0.5f;
        private float LastClickTime = 0f;

        public void Init(CtfMatch match, CtfCharacter character)
        {
            Match = match;
            Character = character;

            TitleText.text = character.LabelCap;
            PreviewImage.sprite = character.Avatar;
            SelectionButton.onClick.AddListener(SelectionButton_OnClick);
            Refresh();

            SetSelected(false);
        }

        public void Refresh()
        {
            JailTimeText.gameObject.SetActive(Character.JailTime > 0);
            JailTimeText.text = Character.JailTime.ToString();
            ActionBar.SetValue(Character.ActionPoints, Character.MaxActionPoints);
            StaminaBar.SetValue(Character.Stamina, Character.MaxStamina);
        }

        private void SelectionButton_OnClick()
        {
            if (Time.time - LastClickTime < DoubleClickTimeThreshold) // Double click detected
            {
                Match.World.CameraPanToFocusEntity(Character, duration: 0.5f, false);
            }
            else // Single click
            {
                Match.SelectCharacter(Character);
            }
            
            LastClickTime = Time.time;
        }

        public void SetSelected(bool value)
        {
            SelectionFrame.SetActive(value);
        }
    }
}
