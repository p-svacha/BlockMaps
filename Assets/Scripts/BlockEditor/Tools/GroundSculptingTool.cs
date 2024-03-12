using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace WorldEditor
{
    public class GroundSculptingTool : EditorTool
    {
        public override EditorToolId Id => EditorToolId.GroundSculpting;
        public override string Name => "Edit Terrain";
        public override Sprite Icon => ResourceManager.Singleton.GroundSculptingSprite;

        private int AreaSize = 1;

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
            if (AreaSize == 1) // Single tile height change
            {
                if (World.HoveredGroundNode != null && World.CanChangeHeight(World.HoveredGroundNode, World.NodeHoverMode9, isIncrease: true))
                {
                    World.ChangeHeight(World.HoveredGroundNode, World.NodeHoverMode9, isIncrease: true);
                }
            }
            else // Multitile height change
            {
                if (World.HoveredGroundNode != null)
                {
                    List<GroundNode> nodesInArea = World.GetGroundNodes(World.HoveredGroundNode.WorldCoordinates, AreaSize, AreaSize);

                    int minHeight = nodesInArea.Min(x => x.BaseHeight);

                    List<GroundNode> affectedNodes = nodesInArea.Where(x => x.BaseHeight == minHeight && x.CanChangeHeight(Direction.None, isIncrease: true)).ToList();

                    foreach (GroundNode node in affectedNodes)
                    {
                        node.ChangeHeight(Direction.None, isIncrease: true);
                    }

                    // Manually update world stuff in one step instead of after each node to increase performance
                    World.UpdateNavmeshAround(World.HoveredGroundNode.WorldCoordinates, AreaSize, AreaSize);
                    World.RedrawNodesAround(World.HoveredGroundNode.WorldCoordinates, AreaSize, AreaSize);
                    World.UpdateVisionOfNearbyEntitiesDelayed(World.HoveredGroundNode.GetCenterWorldPosition(), AreaSize, AreaSize);
                }
            }
        }

        public override void HandleRightClick()
        {
            if (AreaSize == 1) // Single tile height change
            {
                if (World.HoveredGroundNode != null && World.CanChangeHeight(World.HoveredGroundNode, World.NodeHoverMode9, isIncrease: false))
                {
                    World.ChangeHeight(World.HoveredGroundNode, World.NodeHoverMode9, isIncrease: false);
                }
            }
            else // Multitile height change
            {
                if (World.HoveredGroundNode != null)
                {
                    List<GroundNode> nodesInArea = World.GetGroundNodes(World.HoveredGroundNode.WorldCoordinates, AreaSize, AreaSize);

                    int maxHeight = nodesInArea.Max(x => x.MaxHeight);

                    List<GroundNode> affectedNodes = nodesInArea.Where(x => x.MaxHeight == maxHeight && x.CanChangeHeight(Direction.None, isIncrease: false)).ToList();

                    foreach (GroundNode node in affectedNodes)
                    {
                        node.ChangeHeight(Direction.None, isIncrease: false);
                    }

                    // Manually update world stuff in one step instead of after each node to increase performance
                    World.UpdateNavmeshAround(World.HoveredGroundNode.WorldCoordinates, AreaSize, AreaSize);
                    World.RedrawNodesAround(World.HoveredGroundNode.WorldCoordinates, AreaSize, AreaSize);
                    World.UpdateVisionOfNearbyEntitiesDelayed(World.HoveredGroundNode.GetCenterWorldPosition(), AreaSize, AreaSize);
                }
            }
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
