using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WorldEditor
{
    public class NodeShapingTool : EditorTool
    {
        public override EditorToolId Id => EditorToolId.NodeShaping;
        public override string Name => "Edit Node Shapes";
        public override Sprite Icon => ResourceManager.LoadSprite(IconBasePath + "NodeShaping");

        public override void UpdateTool()
        {
            // Update tile overlay
            if (World.HoveredDynamicNode != null)
            {
                Texture2D overlayTexture = ResourceManager.GetTileSelector(World.NodeHoverMode9);
                bool canIncrease = World.CanChangeShape(World.HoveredDynamicNode, World.NodeHoverMode9, isIncrease: true);
                bool canDecrease = World.CanChangeShape(World.HoveredDynamicNode, World.NodeHoverMode9, isIncrease: false);

                Color c;
                if (canIncrease && canDecrease) c = Color.white;
                else if (canIncrease || canDecrease) c = Color.yellow;
                else c = Color.red;

                World.HoveredDynamicNode.ShowOverlay(overlayTexture, c);
            }
        }

        public override void HandleLeftClick()
        {
            if (World.HoveredDynamicNode != null && World.CanChangeShape(World.HoveredDynamicNode, World.NodeHoverMode9, isIncrease: true))
            {
                World.ChangeShape(World.HoveredDynamicNode, World.NodeHoverMode9, isIncrease: true, updateWorld: true);
            }
        }

        public override void HandleRightClick()
        {
            if (World.HoveredDynamicNode != null && World.CanChangeShape(World.HoveredDynamicNode, World.NodeHoverMode9, isIncrease: false))
            {
                World.ChangeShape(World.HoveredDynamicNode, World.NodeHoverMode9, isIncrease: false, updateWorld: true);
            }
        }

        public override void OnHoveredNodeChanged(BlockmapNode oldNode, BlockmapNode newNode)
        {
            // Hide previous node overlay
            if (oldNode != null) oldNode.ShowOverlay(false);
        }
        public override void OnDeselect()
        {
            // Hide node overlay
            if (World.HoveredNode != null) World.HoveredNode.ShowOverlay(false);
        }
    }
}