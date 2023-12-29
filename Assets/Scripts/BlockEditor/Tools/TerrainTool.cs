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
        public override int HotkeyNumber => 2;

        public override void UpdateTool()
        {
            if (World.HoveredSurfaceNode != null)
            {
                Texture2D overlayTexture = GetTextureForHoverMode(World.NodeHoverMode);
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

        private Texture2D GetTextureForHoverMode(Direction mode)
        {
            if (mode == Direction.None) return ResourceManager.Singleton.TileSelector;
            if (mode == Direction.N) return ResourceManager.Singleton.TileSelectorN;
            if (mode == Direction.E) return ResourceManager.Singleton.TileSelectorE;
            if (mode == Direction.S) return ResourceManager.Singleton.TileSelectorS;
            if (mode == Direction.W) return ResourceManager.Singleton.TileSelectorW;
            if (mode == Direction.NE) return ResourceManager.Singleton.TileSelectorNE;
            if (mode == Direction.NW) return ResourceManager.Singleton.TileSelectorNW;
            if (mode == Direction.SW) return ResourceManager.Singleton.TileSelectorSW;
            if (mode == Direction.SE) return ResourceManager.Singleton.TileSelectorSE;
            return null;
        }
    }
}
