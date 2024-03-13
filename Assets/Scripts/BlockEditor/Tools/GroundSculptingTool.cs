using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WorldEditor
{
    public class GroundSculptingTool : EditorTool
    {
        public override EditorToolId Id => EditorToolId.GroundSculpting;
        public override string Name => "Edit Terrain";
        public override Sprite Icon => ResourceManager.Singleton.GroundSculptingSprite;

        private int AreaSize = 1;

        [Header("Elements")]
        public Toggle SmoothEdgeToggle;

        public override void UpdateTool()
        {
            // Ctrl + mouse wheel: change area size
            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (Input.mouseScrollDelta.y < 0 && AreaSize > 1) AreaSize--;
                if (Input.mouseScrollDelta.y > 0 && AreaSize < World.ChunkSize) AreaSize++;
            }

            // Update tile overlay
            if (World.HoveredGroundNode != null)
            {
                if (AreaSize == 1) // Single tile height change
                {

                    Texture2D overlayTexture = ResourceManager.Singleton.GetTileSelector(World.NodeHoverMode9);
                    bool canIncrease = World.CanChangeHeight(World.HoveredGroundNode, World.NodeHoverMode9, isIncrease: true);
                    bool canDecrease = World.CanChangeHeight(World.HoveredGroundNode, World.NodeHoverMode9, isIncrease: false);

                    Color c;
                    if (canIncrease && canDecrease) c = Color.white;
                    else if (canIncrease || canDecrease) c = Color.yellow;
                    else c = Color.red;

                    World.HoveredGroundNode.ShowOverlay(overlayTexture, c);
                }

                else // Multitile height change
                {
                    Texture2D overlayTexture = ResourceManager.Singleton.GetTileSelector(Direction.None);
                    Color c = Color.white;
                    World.HoveredGroundNode.ShowOverlay(overlayTexture, c, AreaSize);
                }
                
            }
        }

        public override void HandleLeftClick()
        {
            ApplyHeightChange(isIncrease: true);
        }

        public override void HandleRightClick()
        {
            ApplyHeightChange(isIncrease: false);
        }
        
        private void ApplyHeightChange(bool isIncrease)
        {
            if (AreaSize == 1 && World.NodeHoverMode9 != Direction.None) // Partial height change of single tile
            {
                if (World.HoveredGroundNode != null && World.CanChangeHeight(World.HoveredGroundNode, World.NodeHoverMode9, isIncrease))
                {
                    World.ChangeHeight(World.HoveredGroundNode, World.NodeHoverMode9, isIncrease);
                }
            }
            else // Height change of full tile(s)
            {
                if (World.HoveredGroundNode != null)
                {
                    Vector2Int coordinates = World.HoveredGroundNode.WorldCoordinates;
                    List<GroundNode> nodesInArea = World.GetGroundNodes(coordinates, AreaSize, AreaSize);

                    List<GroundNode> affectedNodes = new List<GroundNode>();

                    if(isIncrease)
                    {
                        int minHeight = nodesInArea.Min(x => x.BaseHeight);
                        affectedNodes = nodesInArea.Where(x => x.BaseHeight == minHeight && x.CanChangeHeight(Direction.None, isIncrease)).ToList();
                    }
                    else
                    {
                        int maxHeight = nodesInArea.Max(x => x.MaxHeight);
                        affectedNodes = nodesInArea.Where(x => x.MaxHeight == maxHeight && x.CanChangeHeight(Direction.None, isIncrease: false)).ToList();
                    }

                    foreach (GroundNode node in affectedNodes)
                    {
                        node.ChangeHeight(Direction.None, isIncrease);
                    }

                    // Smooth outside edges
                    if (SmoothEdgeToggle.isOn)
                    {
                        // North side
                        for (int x = 0; x < AreaSize; x++)
                        {
                            GroundNode node = World.GetGroundNode(new Vector2Int(coordinates.x + x, coordinates.y + AreaSize - 1));
                            if (!affectedNodes.Contains(node)) continue;
                            SmoothNodeEdge(node, Direction.N, isIncrease);
                        }
                        // East side
                        for (int y = 0; y < AreaSize; y++)
                        {
                            GroundNode node = World.GetGroundNode(new Vector2Int(coordinates.x + AreaSize - 1, coordinates.y + y));
                            if (!affectedNodes.Contains(node)) continue;
                            SmoothNodeEdge(node, Direction.E, isIncrease);
                        }
                        // South side
                        for (int x = 0; x < AreaSize; x++)
                        {
                            GroundNode node = World.GetGroundNode(new Vector2Int(coordinates.x + x, coordinates.y));
                            if (!affectedNodes.Contains(node)) continue;
                            SmoothNodeEdge(node, Direction.S, isIncrease);
                        }
                        // West side
                        for (int y = 0; y < AreaSize; y++)
                        {
                            GroundNode node = World.GetGroundNode(new Vector2Int(coordinates.x, coordinates.y + y));
                            if (!affectedNodes.Contains(node)) continue;
                            SmoothNodeEdge(node, Direction.W, isIncrease);
                        }
                    }

                    // Manually update world stuff in one step instead of after each node to increase performance
                    World.UpdateNavmeshAround(World.HoveredGroundNode.WorldCoordinates, AreaSize, AreaSize);
                    World.RedrawNodesAround(World.HoveredGroundNode.WorldCoordinates, AreaSize, AreaSize);
                    World.UpdateVisionOfNearbyEntitiesDelayed(World.HoveredGroundNode.GetCenterWorldPosition(), AreaSize, AreaSize);
                }
            }
        }

        /// <summary>
        /// Smooths out the edge outside one side of a node
        /// </summary>
        private void SmoothNodeEdge(GroundNode node, Direction dir, bool isIncrease)
        {
            Direction preDir = HelperFunctions.GetPreviousDirection8(dir);
            Direction preDir_Opp = HelperFunctions.GetOppositeDirection(preDir);
            Direction postDir = HelperFunctions.GetNextDirection8(dir);
            Direction postDir_Opp = HelperFunctions.GetOppositeDirection(postDir);

            GroundNode adjNodePre = World.GetAdjacentGroundNode(node, preDir);
            GroundNode adjNodeFull = World.GetAdjacentGroundNode(node, dir);
            GroundNode adjNodePost = World.GetAdjacentGroundNode(node, postDir);

            if (adjNodePre != null && adjNodePre.Height[preDir_Opp] != node.Height[preDir]) adjNodePre.ChangeHeight(preDir_Opp, isIncrease);
            if (adjNodeFull != null && adjNodeFull.Height[postDir_Opp] != node.Height[postDir]) adjNodeFull.ChangeHeight(postDir_Opp, isIncrease);
            if (adjNodeFull != null && adjNodeFull.Height[preDir_Opp] != node.Height[preDir]) adjNodeFull.ChangeHeight(preDir_Opp, isIncrease);
            if (adjNodePost != null && adjNodePost.Height[postDir_Opp] != node.Height[postDir]) adjNodePost.ChangeHeight(postDir_Opp, isIncrease);
        }

        public override void OnHoveredGroundNodeChanged(GroundNode oldNode, GroundNode newNode)
        {
            // Hide overlay from all chunks around previously hovered node
            if(oldNode != null)
                foreach (Chunk chunk in World.GetChunks(oldNode.Chunk.Coordinates, 2, 2)) 
                    chunk.GroundMesh.ShowOverlay(false);
        }

        public override void OnDeselect()
        {
            // Hide overlay from all chunks around previously hovered node
            if (World.HoveredGroundNode != null)
                foreach (Chunk chunk in World.GetChunks(World.HoveredGroundNode.Chunk.Coordinates, 2, 2))
                    chunk.GroundMesh.ShowOverlay(false);
        }
    }
}
