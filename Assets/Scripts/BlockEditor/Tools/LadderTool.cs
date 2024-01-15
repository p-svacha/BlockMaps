using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WorldEditor
{
    public class LadderTool : EditorTool
    {
        public override EditorToolId Id => EditorToolId.Ladder;
        public override string Name => "Build Ladder";
        public override Sprite Icon => ResourceManager.Singleton.LadderToolSprite;

        public override void UpdateTool()
        {
            if (World.HoveredNode != null)
            {
                Texture2D overlayTexture = ResourceManager.Singleton.GetTileSelector(World.NodeHoverModeSides);

                Color c = Color.white;
                if (!World.CanBuildLadder(World.HoveredNode, World.NodeHoverModeSides)) c = Color.red;

                World.HoveredNode.ShowOverlay(overlayTexture, c);
            }
        }

        public override void HandleLeftClick()
        {
            if (World.HoveredNode == null) return;
            if (!World.CanBuildLadder(World.HoveredNode, World.NodeHoverModeSides)) return;

            World.BuildLadder(World.HoveredNode, World.NodeHoverModeSides);
        }

        public override void HandleRightClick()
        {
            if (World.HoveredEntity == null) return;
            if (!(World.HoveredEntity is LadderEntity)) return;

            World.RemoveLadder((World.HoveredEntity as LadderEntity).Ladder);
        }


        public override void OnHoveredNodeChanged(BlockmapNode oldNode, BlockmapNode newNode)
        {
            if (oldNode != null) oldNode.ShowOverlay(false);
            if (newNode != null) newNode.ShowOverlay(true);
        }

        public override void OnDeselect()
        {
            if (World.HoveredNode != null) World.HoveredNode.ShowOverlay(false);
        }
    }
}
