using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace CaptureTheFlag.UI
{
    public class UI_PlayerRow : MonoBehaviour
    {
        [Header("Elements")]
        public TextMeshProUGUI LabelText;
        public Image ColorIcon;

        public void Init(ClientInfo playerInfo)
        {
            LabelText.text = $"{playerInfo.DisplayName} ({playerInfo.ClientId})";
        }
    }
}
