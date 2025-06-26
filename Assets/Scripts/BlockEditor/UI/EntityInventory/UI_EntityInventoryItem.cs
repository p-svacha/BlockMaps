using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlockmapFramework;

namespace WorldEditor {
    public class UI_EntityInventoryItem : MonoBehaviour
    {
        private SelectAndMoveTool Tool;
        private Entity InventoryEntity;

        [Header("Elements")]
        public Image Icon;
        public TextMeshProUGUI Label;
        public Button DropButton;
        public Button RemoveButton;

        public void Init(SelectAndMoveTool tool, Entity e)
        {
            if (!e.IsInInventory) throw new System.Exception("Entity is not in inventory.");
            InventoryEntity = e;
            Tool = tool;

            Icon.sprite = e.UiSprite;
            Label.text = e.LabelCap;
            DropButton.onClick.AddListener(DropButton_OnClick);
            RemoveButton.onClick.AddListener(RemoveButton_OnClick);
        }

        private void DropButton_OnClick()
        {
            InventoryEntity.World.DropFromInventory(InventoryEntity, InventoryEntity.Holder.OriginNode, updateWorld: true);
            Tool.RefreshSelectedEntityPanel();
        }

        private void RemoveButton_OnClick()
        {
            InventoryEntity.World.RemoveEntity(InventoryEntity, updateWorld: true);
            Tool.RefreshSelectedEntityPanel();
        }
    }
}
