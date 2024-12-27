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

        public void Init(CtfMatchType type, ClientInfo playerInfo, Color c)
        {
            LabelText.text = $"{playerInfo.DisplayName}";
            if (type == CtfMatchType.Multiplayer) LabelText.text += $" ({playerInfo.ClientId})";
            ColorIcon.color = c;
        }
    }
}
