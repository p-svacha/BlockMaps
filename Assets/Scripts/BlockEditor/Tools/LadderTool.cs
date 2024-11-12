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

        private int TargetIndex;
        private BlockmapNode TargetNode;

        private GameObject BuildPreview;

        public override void UpdateTool()
        {
            if (World.HoveredNode != null)
            {
                Texture2D overlayTexture = ResourceManager.Singleton.GetTileSelector(World.NodeHoverModeSides);

                List<BlockmapNode> possibleTargets = World.GetPossibleLadderTargetNodes(World.HoveredNode, World.NodeHoverModeSides);
                bool canBuild = possibleTargets.Count > 0;

                Color c = canBuild ? Color.white : Color.red;
                World.HoveredNode.ShowOverlay(overlayTexture, c);

                if (canBuild)
                {
                    // Ctrl + scroll to change target
                    if (Input.GetKey(KeyCode.LeftControl))
                    {
                        if (Input.mouseScrollDelta.y < 0 && TargetIndex > 0) TargetIndex--;
                        if (Input.mouseScrollDelta.y > 0 && TargetIndex < possibleTargets.Count - 1) TargetIndex++;
                    }
                    TargetNode = possibleTargets[TargetIndex];

                    BlockmapNode bottom = World.HoveredNode;
                    BlockmapNode top = TargetNode;
                    Direction side = World.NodeHoverModeSides;
                    int height = top.GetMaxAltitude(HelperFunctions.GetOppositeDirection(side)) - bottom.GetMinAltitude(side);

                    // Preview
                    BuildPreview.SetActive(true);
                    MeshBuilder previewMeshBuilder = new MeshBuilder(BuildPreview);
                    BuildPreview.transform.position = new Vector3(bottom.CenterWorldPosition.x, World.TILE_HEIGHT * bottom.GetMinAltitude(side), bottom.CenterWorldPosition.z);
                    BuildPreview.transform.rotation = HelperFunctions.Get2dRotationByDirection(side);
                    LadderMeshGenerator.GenerateLadderMesh(previewMeshBuilder, height, isPreview: true);
                    previewMeshBuilder.ApplyMesh();
                    BuildPreview.GetComponent<MeshRenderer>().material.color = c;
                }
                else
                {
                    TargetNode = null;
                    BuildPreview.SetActive(false);
                }
            }
            else
            {
                TargetNode = null;
                BuildPreview.SetActive(false);
            }
        }

        public override void HandleLeftClick()
        {
            if (World.HoveredNode == null) return;
            if (TargetNode == null) return;

            World.BuildLadder(World.HoveredNode, TargetNode, World.NodeHoverModeSides);
        }

        public override void HandleRightClick()
        {
            if (World.HoveredEntity == null) return;
            if (!(World.HoveredEntity is Ladder)) return;

            World.RemoveEntity(World.HoveredEntity);
        }


        public override void OnHoveredNodeChanged(BlockmapNode oldNode, BlockmapNode newNode)
        {
            if (oldNode != null) oldNode.ShowOverlay(false);
            if (newNode != null) newNode.ShowOverlay(true);

            TargetIndex = 0;
        }

        public override void OnSelect()
        {
            BuildPreview = new GameObject("LadderPreview");
        }
        public override void OnDeselect()
        {
            GameObject.Destroy(BuildPreview);
            if (World.HoveredNode != null) World.HoveredNode.ShowOverlay(false);
        }
    }
}
