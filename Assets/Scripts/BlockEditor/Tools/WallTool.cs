using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WorldEditor
{
    public class WallTool : EditorTool
    {
        public override EditorToolId Id => EditorToolId.Wall;
        public override string Name => "Build Walls/Fences";
        public override Sprite Icon => ResourceManager.Singleton.WallToolSprite;

        private WallType SelectedWall;

        [Header("Elements")]
        public UI_SelectionPanel WallSelection;

        public override void Init(BlockEditor editor)
        {
            base.Init(editor);

            WallSelection.Clear();
            foreach (WallType wall in editor.WallTypes)
                WallSelection.AddElement(wall.PreviewSprite, Color.white, wall.Name, () => SelectWallType(wall));
        }

        private void SelectWallType(WallType wall) => SelectedWall = wall;

        public override void UpdateTool()
        {
            if (World.HoveredNode != null)
            {
                Texture2D overlayTexture = ResourceManager.Singleton.GetTileSelector(World.NodeSideHoverMode);

                Color c = Color.white;
                if (!World.CanBuildWall(World.HoveredNode, World.NodeSideHoverMode)) c = Color.red;

                World.HoveredSurfaceNode.ShowOverlay(overlayTexture, c);
            }
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
