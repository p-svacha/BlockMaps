using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CaptureTheFlag.UI
{
    public class UI_ToggleButton : MonoBehaviour
    {
        public Button Button;
        public Image InnerFrame;
        public Color OnColor;
        public Color OffColor;

        public void SetToggle(bool value)
        {
            if (value) InnerFrame.color = OnColor;
            else InnerFrame.color = OffColor;
        }
    }
}
