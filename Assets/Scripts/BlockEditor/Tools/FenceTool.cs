using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace WorldEditor
{
    public class FenceTool : EditorTool
    {
        public override EditorToolId Id => EditorToolId.Fence;
        public override string Name => "Build Fences";
        public override Sprite Icon => ResourceManager.LoadSprite(IconBasePath + "Fence");

        private FenceDef SelectedFenceDef;

        private GameObject BuildPreview;

        [Header("Elements")]
        public UI_SelectionPanel FenceSelection;
        public TMP_InputField HeightInput;
        public TMP_InputField ClimbabilityText;

        public override void Init(BlockEditor editor)
        {
            base.Init(editor);

            FenceSelection.Clear();
            foreach (FenceDef def in DefDatabase<FenceDef>.AllDefs)
            {
                FenceSelection.AddElement(def.UiSprite, Color.white, def.LabelCap, () => SelectFence(def));
            }
            FenceSelection.SelectFirstElement();
        }

        private void SelectFence(FenceDef def)
        {
            SelectedFenceDef = def;
            ClimbabilityText.text = def.ClimbSkillRequirement.ToString();
        }

        public override void UpdateTool()
        {
            // Update build preview
            if (World.HoveredNode != null)
            {
                if (HeightInput.text == "") return;
                int height = int.Parse(HeightInput.text);

                Texture2D overlayTexture = ResourceManager.GetTileSelector(World.NodeHoverMode8);

                Color c = Color.white;
                if (!World.CanBuildFence(SelectedFenceDef, World.HoveredNode, World.NodeHoverMode8, height)) c = Color.red;

                World.HoveredNode.ShowOverlay(overlayTexture, c);

                // Build Preview
                BuildPreview.SetActive(true);
                BuildPreview.transform.position = World.HoveredNode.Chunk.WorldPosition;
                MeshBuilder previewMeshBuilder = new MeshBuilder(BuildPreview);
                FenceMeshGenerator.DrawFence(previewMeshBuilder, SelectedFenceDef, World.HoveredNode, World.NodeHoverMode8, height, isPreview: true);
                previewMeshBuilder.ApplyMesh(addCollider: false, castShadows: false);
                BuildPreview.GetComponent<MeshRenderer>().material.color = c;
            }
            else BuildPreview.SetActive(false);
        }

        public override void HandleKeyboardInputs()
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
        }

        public override void HandleLeftClick()
        {
            if (World.HoveredNode == null) return;
            if (HeightInput.text == "") return;
            int height = int.Parse(HeightInput.text);
            if (!World.CanBuildFence(SelectedFenceDef, World.HoveredNode, World.NodeHoverMode8, height)) return;

            World.BuildFence(SelectedFenceDef, World.HoveredNode, World.NodeHoverMode8, height, updateWorld: true);
        }

        public override void HandleRightClick()
        {
            if (World.HoveredFence == null) return;

            World.RemoveFence(World.HoveredFence, updateWorld: true);
        }

        public override void OnHoveredNodeChanged(BlockmapNode oldNode, BlockmapNode newNode)
        {
            if (oldNode != null) oldNode.ShowOverlay(false);
            if (newNode != null) newNode.ShowOverlay(true);
        }

        public override void OnSelect()
        {
            BuildPreview = new GameObject("FencePreview");
        }
        public override void OnDeselect()
        {
            GameObject.Destroy(BuildPreview);
            if (World.HoveredNode != null) World.HoveredNode.ShowOverlay(false);
        }
    }
}
