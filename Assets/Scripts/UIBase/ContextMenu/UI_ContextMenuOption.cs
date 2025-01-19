using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_ContextMenuOption : MonoBehaviour
{
    public ContextMenu ContextMenu { get; private set; }
    public ContextMenuOption Option;
    

    [Header("Elements")]
    public Button Button;
    public TextMeshProUGUI Text;

    public void Init(ContextMenu contextMenu, ContextMenuOption option)
    {
        ContextMenu = contextMenu;
        Option = option;

        Text.text = option.Label;
        Button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        ContextMenu.Hide();
        Option.Action.Invoke();
    }
}
