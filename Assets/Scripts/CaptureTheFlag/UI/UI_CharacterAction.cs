using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CaptureTheFlag
{
    public class UI_CharacterAction : MonoBehaviour
    {
        [Header("Elements")]
        public Button Button;
        public TextMeshProUGUI TitleText;
        public Image Icon;

        public void Init(SpecialAction action)
        {
            Button.onClick.AddListener(action.Perform);
            TitleText.text = action.Name;
            Icon.sprite = action.Icon;
        }
    }
}
