using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WorldEditor
{
    public class UI_CollapseableSection : MonoBehaviour
    {
        private bool IsCollapsed;

        [Header("Elements")]
        public Button CollapseButton;
        public GameObject ContentContainer;

        public void Start()
        {
            CollapseButton.onClick.AddListener(CollapseButton_OnClick);
            IsCollapsed = false;
            CollapseButton.GetComponentInChildren<TextMeshProUGUI>().text = "-";
        }

        private void CollapseButton_OnClick()
        {
            IsCollapsed = !IsCollapsed;
            ContentContainer.SetActive(!IsCollapsed);

            if (IsCollapsed) CollapseButton.GetComponentInChildren<TextMeshProUGUI>().text = "+";
            else CollapseButton.GetComponentInChildren<TextMeshProUGUI>().text = "-";
        }
    }
}
