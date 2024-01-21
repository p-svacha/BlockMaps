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

        private WallType SelectedWallType;

        private GameObject BuildPreview;

        [Header("Elements")]
        public UI_SelectionPanel WallSelection;
        public TMP_InputField HeightInput;
        public TMP_InputField ClimbabilityText;

        public override void Init(BlockEditor editor)
        {
            base.Init(editor);

            WallSelection.Clear();
            foreach (WallType wall in WallTypeManager.Instance.GetAllWallTypes())
                WallSelection.AddElement(wall.PreviewSprite, Color.white, wall.Name, () => SelectWallType(wall.Id));

            WallSelection.SelectFirstElement();
        }

        private void SelectWallType(WallTypeId wall)
        {
            WallType type = WallTypeManager.Instance.GetWallType(wall);
            SelectedWallType = type;
            ClimbabilityText.text = type.ClimbSkillRequirement.ToString();
        }

        public override void UpdateTool()
        {
            // Ctrl + mouse wheel: change height
            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (Input.mouseScrollDelta.y < 0 && HeightInput.text != "")
                {
                    int height = int.Parse(HeightInput.text);
                    if (height > 1) height--;
                    HeightInput.text = height.ToString();
                }
                if (Input.mouseScrollDelta.y > 0 && HeightInput.text != "")
                {
                    int height = int.Parse(HeightInput.text);
                    height++;
                    HeightInput.text = height.ToString();
                }
            }

            if (World.HoveredNode != null)
            {
                if (HeightInput.text == "") return;
                int height = int.Parse(HeightInput.text);

                Texture2D overlayTexture = ResourceManager.Singleton.GetTileSelector(World.NodeHoverMode8);

                Color c = Color.white;
                if (!World.CanBuildWall(SelectedWallType, World.HoveredNode, World.NodeHoverMode8, height)) c = Color.red;

                World.HoveredNode.ShowOverlay(overlayTexture, c);

                // Build Preview
                BuildPreview.SetActive(true);
                BuildPreview.transform.position = World.HoveredNode.Chunk.WorldPosition;
                MeshBuilder previewMeshBuilder = new MeshBuilder(BuildPreview);
                WallMeshGenerator.DrawWall(previewMeshBuilder, SelectedWallType, World.HoveredNode, World.NodeHoverMode8, height, isPreview: true);
                previewMeshBuilder.ApplyMesh(addCollider: false, castShadows: false);
                BuildPreview.GetComponent<MeshRenderer>().material.color = c;
            }
            else BuildPreview.SetActive(false);
        }

        public override void HandleLeftClick()
        {
            if (World.HoveredNode == null) return;
            if (HeightInput.text == "") return;
            int height = int.Parse(HeightInput.text);
            if (!World.CanBuildWall(SelectedWallType, World.HoveredNode, World.NodeHoverMode8, height)) return;

            World.PlaceWall(SelectedWallType, World.HoveredNode, World.NodeHoverMode8, height);
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

        public override void OnSelect()
        {
            BuildPreview = new GameObject("WallPreview");
        }
        public override void OnDeselect()
        {
            GameObject.Destroy(BuildPreview);
            if (World.HoveredNode != null) World.HoveredNode.ShowOverlay(false);
        }
    }
}
