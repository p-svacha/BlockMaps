using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace WorldEditor
{
    public class TerrainTool : EditorTool
    {
        public override EditorToolId Id => EditorToolId.Terrain;
        public override string Name => "Edit Terrain";
        public override Sprite Icon => ResourceManager.Singleton.TerrainToolSprite;

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
            if (World.HoveredSurfaceNode != null)
            {
                if (AreaSize == 1) // Single tile height change
                {

                    Texture2D overlayTexture = ResourceManager.Singleton.GetTileSelector(World.NodeHoverMode9);
                    bool canIncrease = World.CanChangeHeight(World.HoveredSurfaceNode, World.NodeHoverMode9, isIncrease: true);
                    bool canDecrease = World.CanChangeHeight(World.HoveredSurfaceNode, World.NodeHoverMode9, isIncrease: false);

                    Color c;
                    if (canIncrease && canDecrease) c = Color.white;
                    else if (canIncrease || canDecrease) c = Color.yellow;
                    else c = Color.red;

                    World.HoveredSurfaceNode.ShowOverlay(overlayTexture, c);
                }

                else // Multitile height change
                {
                    Texture2D overlayTexture = ResourceManager.Singleton.GetTileSelector(Direction.None);
                    Color c = Color.white;
                    World.HoveredSurfaceNode.ShowOverlay(overlayTexture, c, AreaSize);
                }
                
            }
        }

        public override void HandleLeftClick()
        {
            if (AreaSize == 1) // Single tile height change
            {
                if (World.HoveredSurfaceNode != null && World.CanChangeHeight(World.HoveredSurfaceNode, World.NodeHoverMode9, isIncrease: true))
                {
                    World.ChangeHeight(World.HoveredSurfaceNode, World.NodeHoverMode9, isIncrease: true);
                }
            }
            else // Multitile height change
            {
                if (World.HoveredSurfaceNode != null)
                {
                    List<SurfaceNode> nodesInArea = World.GetSurfaceNodes(World.HoveredSurfaceNode.WorldCoordinates, AreaSize, AreaSize);

                    int minHeight = nodesInArea.Min(x => x.BaseHeight);

                    List<SurfaceNode> affectedNodes = nodesInArea.Where(x => x.BaseHeight == minHeight && x.CanChangeHeight(Direction.None, isIncrease: true)).ToList();
                    HashSet<Chunk> affectedChunks = new HashSet<Chunk>();

                    foreach (SurfaceNode node in affectedNodes)
                    {
                        node.ChangeHeight(Direction.None, isIncrease: true);
                        affectedChunks.Add(node.Chunk);
                    }

                    // Manually update world stuff in one step instead of after each node to increase performance
                    World.UpdateNavmeshAround(World.HoveredSurfaceNode.WorldCoordinates, AreaSize, AreaSize);
                    foreach (Chunk c in affectedChunks) World.RedrawChunk(c);
                    World.UpdateVisionOfNearbyEntitiesDelayed(World.HoveredSurfaceNode.GetCenterWorldPosition(), AreaSize, AreaSize);
                }
            }
        }

        public override void HandleRightClick()
        {
            if (AreaSize == 1) // Single tile height change
            {
                if (World.HoveredSurfaceNode != null && World.CanChangeHeight(World.HoveredSurfaceNode, World.NodeHoverMode9, isIncrease: false))
                {
                    World.ChangeHeight(World.HoveredSurfaceNode, World.NodeHoverMode9, isIncrease: false);
                }
            }
            else // Multitile height change
            {
                if (World.HoveredSurfaceNode != null)
                {
                    List<SurfaceNode> nodesInArea = World.GetSurfaceNodes(World.HoveredSurfaceNode.WorldCoordinates, AreaSize, AreaSize);

                    int maxHeight = nodesInArea.Max(x => x.MaxHeight);

                    List<SurfaceNode> affectedNodes = nodesInArea.Where(x => x.MaxHeight == maxHeight && x.CanChangeHeight(Direction.None, isIncrease: false)).ToList();
                    HashSet<Chunk> affectedChunks = new HashSet<Chunk>();

                    foreach (SurfaceNode node in affectedNodes)
                    {
                        node.ChangeHeight(Direction.None, isIncrease: false);
                        affectedChunks.Add(node.Chunk);
                    }

                    // Manually update world stuff in one step instead of after each node to increase performance
                    World.UpdateNavmeshAround(World.HoveredSurfaceNode.WorldCoordinates, AreaSize, AreaSize);
                    foreach (Chunk c in affectedChunks) World.RedrawChunk(c);
                    World.UpdateVisionOfNearbyEntitiesDelayed(World.HoveredSurfaceNode.GetCenterWorldPosition(), AreaSize, AreaSize);
                }
            }
        }

        public override void OnHoveredSurfaceNodeChanged(SurfaceNode oldNode, SurfaceNode newNode)
        {
            // Hide overlay from all chunks around previously hovered node
            if(oldNode != null)
                foreach (Chunk chunk in World.GetChunks(oldNode.Chunk.Coordinates, 2, 2)) 
                    chunk.SurfaceMesh.ShowOverlay(false);
        }

        public override void OnDeselect()
        {
            if (World.HoveredSurfaceNode != null) World.HoveredSurfaceNode.ShowOverlay(false);
        }
    }
}
