using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace WorldEditor
{
    public class DoorTool : EditorTool
    {
        public override EditorToolId Id => EditorToolId.Door;
        public override string Name => "Doors";
        public override Sprite Icon => ResourceManager.Singleton.DoorToolSprite;

        private GameObject BuildPreview;
        private int Height => int.Parse(HeightInput.text);

        [Header("Elements")]
        public TMP_InputField HeightInput;

        public override void UpdateTool()
        {
            HandleInputs();
            UpdatePreview();
        }

        private void HandleInputs()
        {
            // Ctrl + mouse wheel: change height
            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (Input.mouseScrollDelta.y < 0) SetHeight(Height - 1);
                if (Input.mouseScrollDelta.y > 0) SetHeight(Height + 1);
            }
        }
        private void SetHeight(int height)
        {
            if (height < 0) height = 0;
            if (height > World.MAX_ALTITUDE) height = World.MAX_ALTITUDE;
            HeightInput.text = height.ToString();
        }

        private void UpdatePreview()
        {
            if (World.HoveredNode != null)
            {
                BlockmapNode node = World.HoveredNode;
                Direction side = World.NodeHoverModeSides;
                Texture2D overlayTexture = ResourceManager.Singleton.GetTileSelector(side);

                bool canBuild = World.CanBuildDoor(node, side, Height);

                Color c = canBuild ? Color.white : Color.red;
                node.ShowOverlay(overlayTexture, c);

                // Preview
                MeshBuilder previewMeshBuilder = new MeshBuilder(BuildPreview);
                BuildPreview.transform.position = new Vector3(node.GetCenterWorldPosition().x, World.TILE_HEIGHT * node.GetMinAltitude(side), node.GetCenterWorldPosition().z) + Door.GetWorldPositionOffset(side);
                BuildPreview.transform.rotation = HelperFunctions.Get2dRotationByDirection(side);
                Door.GenerateDoorMesh(previewMeshBuilder, Height, isPreview: true);
                previewMeshBuilder.ApplyMesh();
                BuildPreview.GetComponent<MeshRenderer>().material.color = c;
            }
            else
            {
                if (BuildPreview.TryGetComponent(out MeshRenderer r)) r.enabled = false;
            }
        }

        public override void HandleLeftClick()
        {
            if (World.HoveredNode == null) return;

            World.BuildDoor(World.HoveredNode, World.NodeHoverModeSides, Height);
        }

        public override void HandleRightClick()
        {
            if (World.HoveredEntity == null) return;
            if (!(World.HoveredEntity is Door)) return;

            World.RemoveDoor((World.HoveredEntity as Door));
        }

        public override void HandleMiddleClick()
        {
            if (World.HoveredEntity == null) return;
            if (!(World.HoveredEntity is Door)) return;

            (World.HoveredEntity as Door).Toggle();
        }


        public override void OnHoveredNodeChanged(BlockmapNode oldNode, BlockmapNode newNode)
        {
            if (oldNode != null) oldNode.ShowOverlay(false);
            if (newNode != null) newNode.ShowOverlay(true);
        }

        public override void OnSelect()
        {
            BuildPreview = new GameObject("DoorPreview");
        }
        public override void OnDeselect()
        {
            GameObject.Destroy(BuildPreview);
            if (World.HoveredNode != null) World.HoveredNode.ShowOverlay(false);
        }
    }
}
