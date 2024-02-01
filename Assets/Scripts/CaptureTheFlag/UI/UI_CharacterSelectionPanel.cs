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

        public void Init(CTFGame game, Character character)
        {
            Game = game;
            Character = character;

            TitleText.text = character.Entity.Name;

            SetSelected(false);
        }

        public void SetSelected(bool value)
        {
            SelectionFrame.SetActive(value);
        }
    }
}
