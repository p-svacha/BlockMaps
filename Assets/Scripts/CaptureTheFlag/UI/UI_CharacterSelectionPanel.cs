using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CaptureTheFlag
{
    public class UI_CharacterSelectionPanel : MonoBehaviour
    {
        private Character Character;
        private CTFGame Game;

        [Header("Elements")]
        public GameObject SelectionFrame;
        public Button SelectionButton;
        public TextMeshProUGUI TitleText;
        public Image PreviewImage;
        public UI_ProgressBar ActionBar;
        public UI_ProgressBar StaminaBar;

        // Double click
        private float DoubleClickTimeThreshold = 0.5f;
        private float LastClickTime = 0f;

        public void Init(CTFGame game, Character character)
        {
            Game = game;
            Character = character;

            TitleText.text = character.Entity.Name;
            PreviewImage.sprite = character.Avatar;
            SelectionButton.onClick.AddListener(SelectionButton_OnClick);

            SetSelected(false);
        }

        private void SelectionButton_OnClick()
        {
            if (Time.time - LastClickTime < DoubleClickTimeThreshold) // Double click detected
            {
                Game.World.CameraJumpToFocusEntity(Character.Entity);
            }
            else // Single click
            {
                Game.SelectCharacter(Character);
            }
            

            LastClickTime = Time.time;
        }

        public void SetSelected(bool value)
        {
            SelectionFrame.SetActive(value);
        }
    }
}