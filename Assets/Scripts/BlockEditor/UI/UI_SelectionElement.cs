using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlockmapFramework;

namespace WorldEditor
{
    public class UI_SelectionElement : MonoBehaviour
    {
        [Header("Elements")]
        public Image Icon;
        public Button Button;
        public GameObject SelectionFrame;
        public TextMeshProUGUI Text;

        public void Init(Sprite iconSprite, Color iconColor, string text)
        {
            Icon.sprite = iconSprite;
            Icon.color = iconColor;
            SelectionFrame.SetActive(false);
            Text.text = text;
        }

        public void SetSelected(bool value)
        {
            SelectionFrame.SetActive(value);
        }
    }
}
