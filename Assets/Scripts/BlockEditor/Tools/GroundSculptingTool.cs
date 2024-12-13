using BlockmapFramework;
using BlockmapFramework.WorldGeneration;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

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
        public TMP_InputField SmoothStepInput;

        public override void UpdateTool()
        {
            // Update tile overlay
            if (World.HoveredGroundNode != null)
            {
                if (AreaSize == 1) // Single tile height change
                {

                    Texture2D overlayTexture = ResourceManager.Singleton.GetTileSelector(World.NodeHoverMode9);
                    bool canIncrease = World.CanChangeShape(World.HoveredGroundNode, World.NodeHoverMode9, isIncrease: true);
                    bool canDecrease = World.CanChangeShape(World.HoveredGroundNode, World.NodeHoverMode9, isIncrease: false);

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

        public override void HandleKeyboardInputs()
        {
            // Ctrl + mouse wheel: change area size
            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (Input.mouseScrollDelta.y < 0 && AreaSize > 1) AreaSize--;
                if (Input.mouseScrollDelta.y > 0 && AreaSize < World.ChunkSize) AreaSize++;
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

        public override void HandleMiddleClick()
        {
            if (World.HoveredGroundNode == null) return;

            Parcel modifiedArea = TerrainFunctions.SmoothOutside(World, new Parcel(World, World.HoveredGroundNode.WorldCoordinates, new Vector2Int(AreaSize, AreaSize)), int.Parse(SmoothStepInput.text));
            modifiedArea.UpdateWorld();
        }

        private void ApplyHeightChange(bool isIncrease)
        {
            if (AreaSize == 1 && World.NodeHoverMode9 != Direction.None) // Partial height change of single tile
            {
                if (World.HoveredGroundNode != null && World.CanChangeShape(World.HoveredGroundNode, World.NodeHoverMode9, isIncrease))
                {
                    World.ChangeShape(World.HoveredGroundNode, World.NodeHoverMode9, isIncrease);
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
                        int minHeight = nodesInArea.Min(x => x.BaseAltitude);
                        affectedNodes = nodesInArea.Where(x => x.BaseAltitude == minHeight && x.CanChangeShape(Direction.None, isIncrease)).ToList();
                    }
                    else
                    {
                        int maxHeight = nodesInArea.Max(x => x.MaxAltitude);
                        affectedNodes = nodesInArea.Where(x => x.MaxAltitude == maxHeight && x.CanChangeShape(Direction.None, isIncrease: false)).ToList();
                    }

                    foreach (GroundNode node in affectedNodes)
                    {
                        node.ChangeShape(Direction.None, isIncrease);
                    }

                    // Smooth outside edges of affected nodes
                    if (SmoothEdgeToggle.isOn)
                    {
                        Parcel modifiedArea = TerrainFunctions.SmoothOutside(World, new Parcel(World, coordinates, new Vector2Int(AreaSize, AreaSize)), int.Parse(SmoothStepInput.text));
                        modifiedArea.UpdateWorld();
                    }

                    else
                    {
                        World.UpdateNavmeshAround(World.HoveredGroundNode.WorldCoordinates, AreaSize, AreaSize);
                        World.RedrawNodesAround(World.HoveredGroundNode.WorldCoordinates, AreaSize, AreaSize);
                        World.UpdateVisionOfNearbyEntitiesDelayed(World.HoveredGroundNode.CenterWorldPosition, AreaSize, AreaSize);
                    }
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
