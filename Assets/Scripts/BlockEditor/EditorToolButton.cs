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
        public TextMeshProUGUI HotkeyText;

        public void Init(BlockEditor editor, EditorTool tool)
        {
            Tool = tool;
            Icon.sprite = tool.Icon;
            HotkeyText.text = tool.HotkeyNumber.ToString();
            GetComponent<Button>().onClick.AddListener(() => editor.SelectTool(Tool.Id));
        }

        public void SetSelected(bool value)
        {
            SelectedFrame.gameObject.SetActive(value);
        }
    }
}
