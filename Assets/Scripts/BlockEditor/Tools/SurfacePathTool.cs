using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WorldEditor
{
    public class SurfacePathTool : EditorTool
    {
        public override EditorToolId Id => EditorToolId.SurfacePath;
        public override string Name => "Build Paths";
        public override Sprite Icon => ResourceManager.Singleton.PathToolSprite;

        #region Update

        public override void UpdateTool()
        {
            if (World.HoveredSurfaceNode != null)
            {
                Color c = World.CanBuildSurfacePath(World.HoveredSurfaceNode) ? Color.white : Color.red;
                World.HoveredSurfaceNode.ShowOverlay(ResourceManager.Singleton.TileSelector, c);
            }
        }

        #endregion

        #region Hooks

        public override void HandleLeftClick()
        {
            if (World.HoveredSurfaceNode != null && World.CanBuildSurfacePath(World.HoveredSurfaceNode))
                World.BuildSurfacePath(World.HoveredSurfaceNode, SurfaceManager.Instance.GetSurface(SurfaceId.Tarmac));
        }

        public override void HandleRightClick()
        {
            if (World.HoveredSurfaceNode != null && World.HoveredSurfaceNode.HasPath) World.RemoveSurfacePath(World.HoveredSurfaceNode);
        }

        public override void OnHoveredSurfaceNodeChanged(SurfaceNode oldNode, SurfaceNode newNode)
        {
            if (oldNode != null) oldNode.ShowOverlay(false);
            if (newNode != null) newNode.ShowOverlay(true);
        }

        public override void OnDeselect()
        {
            if(World.HoveredSurfaceNode != null) World.HoveredSurfaceNode.ShowOverlay(false);
        }

        #endregion
    }
}
