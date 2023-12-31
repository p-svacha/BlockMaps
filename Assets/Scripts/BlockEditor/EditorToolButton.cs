using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace WorldEditor
{
    public class EditorToolButton : MonoBehaviour
    {
        public EditorTool Tool { get; private set; }

        [Header("Elements")]
        public GameObject SelectedFrame;
        public Image Icon;
        public TextMeshProUGUI TitleText;

        public void Init(BlockEditor editor, EditorTool tool)
        {
            TitleText.text = tool.Name;
            Tool = tool;
            Icon.sprite = tool.Icon;
            GetComponent<Button>().onClick.AddListener(() => editor.SelectTool(Tool.Id));
        }

        public void SetSelected(bool value)
        {
            SelectedFrame.gameObject.SetActive(value);
        }
    }
}
