using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CaptureTheFlag
{
    public class UI_CharacterAction : MonoBehaviour
    {
        private SpecialAction Action;

        [Header("Elements")]
        public Button Button;
        public TextMeshProUGUI TitleText;
        public Image Icon;

        public void Init(SpecialAction action)
        {
            Action = action;
            Button.onClick.AddListener(OnClick);
            TitleText.text = action.Name;
            Icon.sprite = action.Icon;
        }

        private void OnClick()
        {
            Action.Perform();
        }
    }
}
