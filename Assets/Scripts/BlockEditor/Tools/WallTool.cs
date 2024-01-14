using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace WorldEditor
{
    public class WallTool : EditorTool
    {
        public override EditorToolId Id => EditorToolId.Wall;
        public override string Name => "Build Walls/Fences";
        public override Sprite Icon => ResourceManager.Singleton.WallToolSprite;

        private WallTypeId SelectedWallType;

        [Header("Elements")]
        public UI_SelectionPanel WallSelection;
        public TMP_InputField HeightInput;

        public override void Init(BlockEditor editor)
        {
            base.Init(editor);

            WallSelection.Clear();
            foreach (WallType wall in WallTypeManager.Instance.GetAllWallTypes())
                WallSelection.AddElement(wall.PreviewSprite, Color.white, wall.Name, () => SelectWallType(wall.Id));

            WallSelection.SelectFirstElement();
        }

        private void SelectWallType(WallTypeId wall) => SelectedWallType = wall;

        public override void UpdateTool()
        {
            if (World.HoveredNode != null)
            {
                if (HeightInput.text == "") return;
                int height = int.Parse(HeightInput.text);

                Texture2D overlayTexture = ResourceManager.Singleton.GetTileSelector(World.NodeHoverMode8);

                Color c = Color.white;
                if (!World.CanBuildWall(WallTypeManager.Instance.GetWallType(SelectedWallType), World.HoveredNode, World.NodeHoverMode8, height)) c = Color.red;

                World.HoveredNode.ShowOverlay(overlayTexture, c);
            }
        }

        public override void HandleLeftClick()
        {
            if (World.HoveredNode == null) return;
            if (HeightInput.text == "") return;
            int height = int.Parse(HeightInput.text);
            WallType type = WallTypeManager.Instance.GetWallType(SelectedWallType);
            if (!World.CanBuildWall(type, World.HoveredNode, World.NodeHoverMode8, height)) return;

            World.PlaceWall(type, World.HoveredNode, World.NodeHoverMode8, height);
        }

        public override void HandleRightClick()
        {
            if (World.HoveredWall == null) return;

            World.RemoveWall(World.HoveredWall);
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
