using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorldEditor
{
    public class DoorTool : EditorTool
    {
        public override EditorToolId Id => EditorToolId.Door;
        public override string Name => "Doors";
        public override Sprite Icon => ResourceManager.Singleton.DoorToolSprite;

        private GameObject BuildPreview;
        private int Height => int.Parse(HeightInput.text);
        private EntityDef SelectedDoor;

        [Header("Elements")]
        public TMP_InputField HeightInput;
        public Toggle MirrorToggle;

        public override void Init(BlockEditor editor)
        {
            base.Init(editor);
            SelectedDoor = BlockmapFramework.EntityDefOf.Door;
        }

        public override void UpdateTool()
        {
            UpdatePreview();
        }

        public override void HandleKeyboardInputs()
        {
            // Ctrl + Scroll - Change height
            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (Input.mouseScrollDelta.y < 0) SetHeight(Height - 1);
                if (Input.mouseScrollDelta.y > 0) SetHeight(Height + 1);
            }

            // M - Toggle mirrored
            if (Input.GetKeyDown(KeyCode.M)) SetMirrored(!MirrorToggle.isOn);
        }
        private void SetHeight(int height)
        {
            if (height < 0) height = 0;
            if (height > World.MAX_ALTITUDE) height = World.MAX_ALTITUDE;
            HeightInput.text = height.ToString();
        }
        private void SetMirrored(bool show)
        {
            MirrorToggle.isOn = show;
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
                BuildPreview.transform.position = SelectedDoor.RenderProperties.GetWorldPositionFunction(SelectedDoor, World, node, side, MirrorToggle.isOn);
                BuildPreview.transform.rotation = HelperFunctions.Get2dRotationByDirection(side);
                Door.GenerateDoorMesh(previewMeshBuilder, Height, MirrorToggle.isOn, isPreview: true);
                previewMeshBuilder.ApplyMesh();
                BuildPreview.GetComponent<MeshRenderer>().enabled = true;
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
            if (!World.CanBuildDoor(World.HoveredNode, World.NodeHoverModeSides, Height)) return;

            World.BuildDoor(World.HoveredNode, World.NodeHoverModeSides, Height, MirrorToggle.isOn, updateWorld: true);
        }

        public override void HandleRightClick()
        {
            if (World.HoveredEntity == null) return;
            if (!(World.HoveredEntity is Door)) return;

            World.RemoveEntity(World.HoveredEntity, updateWorld: true);
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
