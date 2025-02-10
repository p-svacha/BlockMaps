using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Tries spawning the entity on or as close to the given node as possible.
    /// </summary>
    public class EntitySpawnPositionProperties_AsCloseToNodeAsPossible : EntitySpawnPositionProperties
    {
        private BlockmapNode Node;
        private int Counter;
        private List<List<Direction>> DirectionLayers; // Randomized direction orders

        public EntitySpawnPositionProperties_AsCloseToNodeAsPossible(BlockmapNode node, int maxRadius = 10)
        {
            Node = node;
            Counter = -1;
            DirectionLayers = new List<List<Direction>>();

            for (int i = 0; i < maxRadius; i++)
            {
                List<Direction> dirLayer = HelperFunctions.GetSides().GetShuffledList();
                DirectionLayers.Add(dirLayer);
            }
        }

        public override BlockmapNode GetNewTargetNode(EntitySpawnProperties spawnProps)
        {
            Counter++;

            if (Counter == 0) return Node;

            int layerIndex = 0;
            int directionIndex = 0;
            int steps = Counter;

            // Determine which layer and direction to use
            while (steps >= 0)
            {
                if (layerIndex >= DirectionLayers.Count) return null;

                int currentLayerSize = DirectionLayers[layerIndex].Count;
                if (steps < currentLayerSize)
                {
                    directionIndex = steps;
                    break;
                }

                steps -= currentLayerSize;
                layerIndex++;
            }

            // Compute target coordinates
            Vector2Int targetCoordinates = Node.WorldCoordinates;
            for (int i = 0; i <= layerIndex; i++)
            {
                targetCoordinates += HelperFunctions.GetDirectionVectorInt(DirectionLayers[i][directionIndex % DirectionLayers[i].Count]);
            }

            List<BlockmapNode> nodes = spawnProps.World.GetNodes(targetCoordinates);
            return (nodes == null || nodes.Count == 0) ? null : nodes.RandomElement();
        }
    }
}

