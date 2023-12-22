using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CaptureTheFlag
{
    public class CTFUi : MonoBehaviour
    {
        [Header("Elements")]
        public TextMeshProUGUI TileInfoText;

        float deltaTime; // for fps

        private void Update()
        {
            string text = "";

            // Add FPS
            deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
            float fps = 1.0f / deltaTime;
            text += "\n" + Mathf.Ceil(fps).ToString() + " FPS";

            TileInfoText.text = text;
        }
    }
}
