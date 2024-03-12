using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace WorldEditor
{
    public class UI_EditorToolButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private BlockEditor Editor;
        public EditorTool Tool { get; private set; }

        [Header("Elements")]
        public GameObject SelectedFrame;
        public Image Background;
        public Image Icon;
        public TextMeshProUGUI TitleText;

        public void Init(BlockEditor editor, EditorTool tool)
        {
            Editor = editor;
            TitleText.text = tool.Name;
            Tool = tool;
            Icon.sprite = tool.Icon;
            GetComponent<Button>().onClick.AddListener(() => editor.SelectTool(Tool.Id));
        }

        public void SetSelected(bool value)
        {
            //SelectedFrame.gameObject.SetActive(value);
            Background.color = value ? ResourceManager.Singleton.UI_ButtonSelectedColor : ResourceManager.Singleton.UI_ButtonDefaultColor;
        }

        // Called when the mouse enters the button
        public void OnPointerEnter(PointerEventData eventData)
        {
            Editor.ToolNamePanel.SetActive(true);
            Editor.ToolNameText.text = Tool.Name;
        }
        // Called when the mouse exits the button
        public void OnPointerExit(PointerEventData eventData)
        {
            Editor.ToolNamePanel.SetActive(false);
        }
    }
}
