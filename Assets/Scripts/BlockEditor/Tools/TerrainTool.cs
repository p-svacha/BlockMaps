using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace WorldEditor
{
    public class TerrainTool : EditorTool
    {
        public override EditorToolId Id => EditorToolId.Terrain;
        public override string Name => "Edit Terrain";
        public override Sprite Icon => ResourceManager.Singleton.TerrainToolSprite;

        public override void UpdateTool()
        {
            if (World.HoveredSurfaceNode != null)
            {
                Texture2D overlayTexture = ResourceManager.Singleton.GetTileSelector(World.NodeHoverMode);
                bool canIncrease = World.CanChangeHeight(World.HoveredSurfaceNode, World.NodeHoverMode, isIncrease: true);
                bool canDecrease = World.CanChangeHeight(World.HoveredSurfaceNode, World.NodeHoverMode, isIncrease: false);
                Color c = Color.white;
                if (canIncrease && canDecrease) c = Color.white;
                else if (canIncrease) c = Color.green;
                else if (canDecrease) c = Color.yellow;
                else c = Color.red;
                World.HoveredSurfaceNode.ShowOverlay(overlayTexture, c);
            }
        }

        public override void HandleLeftClick()
        {
            if (World.HoveredSurfaceNode != null && World.CanChangeHeight(World.HoveredSurfaceNode, World.NodeHoverMode, isIncrease: true))
            {
                World.ChangeHeight(World.HoveredSurfaceNode, World.NodeHoverMode, isIncrease: true);
            }
        }

        public override void HandleRightClick()
        {
            if (World.HoveredSurfaceNode != null && World.CanChangeHeight(World.HoveredSurfaceNode, World.NodeHoverMode, isIncrease: false))
            {
                World.ChangeHeight(World.HoveredSurfaceNode, World.NodeHoverMode, isIncrease: false);
            }
        }

        public override void OnHoveredSurfaceNodeChanged(SurfaceNode oldNode, SurfaceNode newNode)
        {
            if (oldNode != null) oldNode.ShowOverlay(false);
            if (newNode != null) newNode.ShowOverlay(true);
        }

        public override void OnDeselect()
        {
            if (World.HoveredSurfaceNode != null) World.HoveredSurfaceNode.ShowOverlay(false);
        }
    }
}
