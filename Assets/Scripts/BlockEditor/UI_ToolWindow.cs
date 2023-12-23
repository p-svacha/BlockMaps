using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace WorldEditor
{
    public class UI_ToolWindow : MonoBehaviour
    {
        [Header("Elements")]
        public TextMeshProUGUI TitleText;

        public void SelectTool(EditorTool tool)
        {
            TitleText.text = tool.Name;
            for (int i = 1; i < transform.childCount; i++) transform.GetChild(i).gameObject.SetActive(false);
            tool.gameObject.SetActive(true);
        }
    }
}
