using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.WorldGeneration
{
    public static class TerrainFunctions
    {

        public static void SmoothOutside(BlockmapNode node, int smoothStep) => SmoothOutside(node.World, new Parcel(node.World, node.WorldCoordinates, Vector2Int.one), smoothStep);

        /// <summary>
        /// Smooths the ground area outside the given parcel so there are no hard edges around the parcel.
        /// <br/>Starts with the ring outside the parcel and then goes more and more outside until there are no gaps left.
        /// <br/>Smooth step defines the targeted steepness along the smoothed area.
        /// <br/>Returns the modified area as a new parcel.
        /// </summary>
        public static Parcel SmoothOutside(World world, Parcel parcel, int smoothStep = 1)
        {
            bool anotherLayerRequired;

            // Start with edges that are the inside boundary ring of the parcel
            int startX = parcel.Position.x;
            int endX = parcel.Position.x + parcel.Dimensions.x - 1;
            int startY = parcel.Position.y;
            int endY = parcel.Position.y + parcel.Dimensions.y - 1;

            do
            {
                anotherLayerRequired = false;

                // Sides
                for (int x = startX - 1; x <= endX + 1; x++)
                {
                    for (int y = startY - 1; y <= endY + 1; y++)
                    {
                        // Skip the interior of the parcel
                        if (x >= startX && x <= endX && y >= startY && y <= endY) continue;

                        bool isWestEdge = x == startX - 1;
                        bool isEastEdge = x == endX + 1;
                        bool isSouthEdge = y == startY - 1;
                        bool isNorthEdge = y == endY + 1;

                        Vector2Int worldCoords = new Vector2Int(x, y);
                        GroundNode groundNode = world.GetGroundNode(worldCoords);
                        if (groundNode == null) continue;

                        // East
                        if (isEastEdge && !isSouthEdge && !isNorthEdge)
                        {
                            GroundNode westGroundNode = world.GetGroundNode(HelperFunctions.GetWorldCoordinatesInDirection(worldCoords, Direction.W));

                            // Match west side to western adjacent nodes east side
                            groundNode.Altitude[Direction.NW] = westGroundNode.Altitude[Direction.NE];
                            groundNode.Altitude[Direction.SW] = westGroundNode.Altitude[Direction.SE];

                            // Check if we have to adjust east side too so it's not too steep
                            if (groundNode.Altitude[Direction.NE] - groundNode.Altitude[Direction.NW] > smoothStep)
                            {
                                groundNode.Altitude[Direction.NE] = groundNode.Altitude[Direction.NW] + smoothStep;
                                anotherLayerRequired = true;
                            }
                            else if (groundNode.Altitude[Direction.NE] - groundNode.Altitude[Direction.NW] < -smoothStep)
                            {
                                groundNode.Altitude[Direction.NE] = groundNode.Altitude[Direction.NW] - smoothStep;
                                anotherLayerRequired = true;
                            }

                            if (groundNode.Altitude[Direction.SE] - groundNode.Altitude[Direction.SW] > smoothStep)
                            {
                                groundNode.Altitude[Direction.SE] = groundNode.Altitude[Direction.SW] + smoothStep;
                                anotherLayerRequired = true;
                            }
                            else if (groundNode.Altitude[Direction.SE] - groundNode.Altitude[Direction.SW] < -smoothStep)
                            {
                                groundNode.Altitude[Direction.SE] = groundNode.Altitude[Direction.SW] - smoothStep;
                                anotherLayerRequired = true;
                            }
                        }

                        // West
                        if (isWestEdge && !isSouthEdge && !isNorthEdge)
                        {
                            GroundNode eastGroundNode = world.GetGroundNode(HelperFunctions.GetWorldCoordinatesInDirection(worldCoords, Direction.E));

                            // Match east side to eastern adjacent nodes west side
                            groundNode.Altitude[Direction.NE] = eastGroundNode.Altitude[Direction.NW];
                            groundNode.Altitude[Direction.SE] = eastGroundNode.Altitude[Direction.SW];

                            // Check if we have to adjust west side too so it's not too steep
                            if(groundNode.Altitude[Direction.NW] - groundNode.Altitude[Direction.NE] > smoothStep)
                            {
                                groundNode.Altitude[Direction.NW] = groundNode.Altitude[Direction.NE] + smoothStep;
                                anotherLayerRequired = true;
                            }
                            else if (groundNode.Altitude[Direction.NW] - groundNode.Altitude[Direction.NE] < -smoothStep)
                            {
                                groundNode.Altitude[Direction.NW] = groundNode.Altitude[Direction.NE] - smoothStep;
                                anotherLayerRequired = true;
                            }

                            if (groundNode.Altitude[Direction.SW] - groundNode.Altitude[Direction.SE] > smoothStep)
                            {
                                groundNode.Altitude[Direction.SW] = groundNode.Altitude[Direction.SE] + smoothStep;
                                anotherLayerRequired = true;
                            }
                            else if (groundNode.Altitude[Direction.SW] - groundNode.Altitude[Direction.SE] < -smoothStep)
                            {
                                groundNode.Altitude[Direction.SW] = groundNode.Altitude[Direction.SE] - smoothStep;
                                anotherLayerRequired = true;
                            }
                        }

                        // North
                        if (isNorthEdge && !isEastEdge && !isWestEdge)
                        {
                            GroundNode southGroundNode = world.GetGroundNode(HelperFunctions.GetWorldCoordinatesInDirection(worldCoords, Direction.S));

                            // Match south side to southern adjacent nodes north side
                            groundNode.Altitude[Direction.SE] = southGroundNode.Altitude[Direction.NE];
                            groundNode.Altitude[Direction.SW] = southGroundNode.Altitude[Direction.NW];

                            // Check if we have to adjust south side too so it's not too steep
                            if (groundNode.Altitude[Direction.NE] - groundNode.Altitude[Direction.SE] > smoothStep)
                            {
                                groundNode.Altitude[Direction.NE] = groundNode.Altitude[Direction.SE] + smoothStep;
                                anotherLayerRequired = true;
                            }
                            else if (groundNode.Altitude[Direction.NE] - groundNode.Altitude[Direction.SE] < -smoothStep)
                            {
                                groundNode.Altitude[Direction.NE] = groundNode.Altitude[Direction.SE] - smoothStep;
                                anotherLayerRequired = true;
                            }

                            if (groundNode.Altitude[Direction.NW] - groundNode.Altitude[Direction.SW] > smoothStep)
                            {
                                groundNode.Altitude[Direction.NW] = groundNode.Altitude[Direction.SW] + smoothStep;
                                anotherLayerRequired = true;
                            }
                            else if (groundNode.Altitude[Direction.NW] - groundNode.Altitude[Direction.SW] < -smoothStep)
                            {
                                groundNode.Altitude[Direction.NW] = groundNode.Altitude[Direction.SW] - smoothStep;
                                anotherLayerRequired = true;
                            }
                        }

                        // South
                        if (isSouthEdge && !isEastEdge && !isWestEdge)
                        {
                            GroundNode northGroundNode = world.GetGroundNode(HelperFunctions.GetWorldCoordinatesInDirection(worldCoords, Direction.N));

                            // Match north side to northern adjacent nodes south side
                            groundNode.Altitude[Direction.NE] = northGroundNode.Altitude[Direction.SE];
                            groundNode.Altitude[Direction.NW] = northGroundNode.Altitude[Direction.SW];

                            // Check if we have to adjust south side too so it's not too steep
                            if (groundNode.Altitude[Direction.SE] - groundNode.Altitude[Direction.NE] > smoothStep)
                            {
                                groundNode.Altitude[Direction.SE] = groundNode.Altitude[Direction.NE] + smoothStep;
                                anotherLayerRequired = true;
                            }
                            else if (groundNode.Altitude[Direction.SE] - groundNode.Altitude[Direction.NE] < -smoothStep)
                            {
                                groundNode.Altitude[Direction.SE] = groundNode.Altitude[Direction.NE] - smoothStep;
                                anotherLayerRequired = true;
                            }

                            if (groundNode.Altitude[Direction.SW] - groundNode.Altitude[Direction.NW] > smoothStep)
                            {
                                groundNode.Altitude[Direction.SW] = groundNode.Altitude[Direction.NW] + smoothStep;
                                anotherLayerRequired = true;
                            }
                            else if (groundNode.Altitude[Direction.SW] - groundNode.Altitude[Direction.NW] < -smoothStep)
                            {
                                groundNode.Altitude[Direction.SW] = groundNode.Altitude[Direction.NW] - smoothStep;
                                anotherLayerRequired = true;
                            }
                        }
                    }
                }

                // Corners
                for (int x = startX - 1; x <= endX + 1; x++)
                {
                    for (int y = startY - 1; y <= endY + 1; y++)
                    {
                        // Skip the interior of the parcel
                        if (x >= startX && x <= endX && y >= startY && y <= endY) continue;

                        bool isWestEdge = x == startX - 1;
                        bool isEastEdge = x == endX + 1;
                        bool isSouthEdge = y == startY - 1;
                        bool isNorthEdge = y == endY + 1;

                        Vector2Int worldCoords = new Vector2Int(x, y);
                        GroundNode groundNode = world.GetGroundNode(worldCoords);
                        if (groundNode == null) continue;

                        // SW
                        if (isWestEdge && isSouthEdge)
                        {
                            GroundNode neGroundNode = world.GetGroundNode(HelperFunctions.GetWorldCoordinatesInDirection(worldCoords, Direction.NE));
                            groundNode.Altitude[Direction.NE] = neGroundNode.Altitude[Direction.SW];

                            GroundNode nGroundNode = world.GetGroundNode(HelperFunctions.GetWorldCoordinatesInDirection(worldCoords, Direction.N));
                            groundNode.Altitude[Direction.NW] = nGroundNode.Altitude[Direction.SW];

                            GroundNode eGroundNode = world.GetGroundNode(HelperFunctions.GetWorldCoordinatesInDirection(worldCoords, Direction.E));
                            groundNode.Altitude[Direction.SE] = eGroundNode.Altitude[Direction.SW];

                            if (groundNode.Altitude[Direction.SW] - groundNode.Altitude[Direction.SE] > smoothStep)
                            {
                                groundNode.Altitude[Direction.SW] = groundNode.Altitude[Direction.SE] + smoothStep;
                                anotherLayerRequired = true;
                            }
                            else if (groundNode.Altitude[Direction.SW] - groundNode.Altitude[Direction.SE] < -smoothStep)
                            {
                                groundNode.Altitude[Direction.SW] = groundNode.Altitude[Direction.SE] - smoothStep;
                                anotherLayerRequired = true;
                            }
                        }

                        // SE
                        if (isEastEdge && isSouthEdge)
                        {
                            GroundNode nwGroundNode = world.GetGroundNode(HelperFunctions.GetWorldCoordinatesInDirection(worldCoords, Direction.NW));
                            groundNode.Altitude[Direction.NW] = nwGroundNode.Altitude[Direction.SE];

                            GroundNode nGroundNode = world.GetGroundNode(HelperFunctions.GetWorldCoordinatesInDirection(worldCoords, Direction.N));
                            groundNode.Altitude[Direction.NE] = nGroundNode.Altitude[Direction.SE];

                            GroundNode wGroundNode = world.GetGroundNode(HelperFunctions.GetWorldCoordinatesInDirection(worldCoords, Direction.W));
                            groundNode.Altitude[Direction.SW] = wGroundNode.Altitude[Direction.SE];

                            if (groundNode.Altitude[Direction.SE] - groundNode.Altitude[Direction.SW] > smoothStep)
                            {
                                groundNode.Altitude[Direction.SE] = groundNode.Altitude[Direction.SW] + smoothStep;
                                anotherLayerRequired = true;
                            }
                            else if (groundNode.Altitude[Direction.SE] - groundNode.Altitude[Direction.SW] < -smoothStep)
                            {
                                groundNode.Altitude[Direction.SE] = groundNode.Altitude[Direction.SW] - smoothStep;
                                anotherLayerRequired = true;
                            }
                        }

                        // NE
                        if (isEastEdge && isNorthEdge)
                        {
                            GroundNode swGroundNode = world.GetGroundNode(HelperFunctions.GetWorldCoordinatesInDirection(worldCoords, Direction.SW));
                            groundNode.Altitude[Direction.SW] = swGroundNode.Altitude[Direction.NE];

                            GroundNode sGroundNode = world.GetGroundNode(HelperFunctions.GetWorldCoordinatesInDirection(worldCoords, Direction.S));
                            groundNode.Altitude[Direction.SE] = sGroundNode.Altitude[Direction.NE];

                            GroundNode wGroundNode = world.GetGroundNode(HelperFunctions.GetWorldCoordinatesInDirection(worldCoords, Direction.W));
                            groundNode.Altitude[Direction.NW] = wGroundNode.Altitude[Direction.NE];

                            if (groundNode.Altitude[Direction.NE] - groundNode.Altitude[Direction.NW] > smoothStep)
                            {
                                groundNode.Altitude[Direction.NE] = groundNode.Altitude[Direction.NW] + smoothStep;
                                anotherLayerRequired = true;
                            }
                            else if (groundNode.Altitude[Direction.NE] - groundNode.Altitude[Direction.NW] < -smoothStep)
                            {
                                groundNode.Altitude[Direction.NE] = groundNode.Altitude[Direction.NW] - smoothStep;
                                anotherLayerRequired = true;
                            }
                        }

                        // NW
                        if (isWestEdge && isNorthEdge)
                        {
                            GroundNode seGroundNode = world.GetGroundNode(HelperFunctions.GetWorldCoordinatesInDirection(worldCoords, Direction.SE));
                            groundNode.Altitude[Direction.SE] = seGroundNode.Altitude[Direction.NW];

                            GroundNode sGroundNode = world.GetGroundNode(HelperFunctions.GetWorldCoordinatesInDirection(worldCoords, Direction.S));
                            groundNode.Altitude[Direction.SW] = sGroundNode.Altitude[Direction.NW];

                            GroundNode eGroundNode = world.GetGroundNode(HelperFunctions.GetWorldCoordinatesInDirection(worldCoords, Direction.E));
                            groundNode.Altitude[Direction.NE] = eGroundNode.Altitude[Direction.NW];

                            if (groundNode.Altitude[Direction.NW] - groundNode.Altitude[Direction.NE] > smoothStep)
                            {
                                groundNode.Altitude[Direction.NW] = groundNode.Altitude[Direction.NE] + smoothStep;
                                anotherLayerRequired = true;
                            }
                            else if (groundNode.Altitude[Direction.NW] - groundNode.Altitude[Direction.NE] < -smoothStep)
                            {
                                groundNode.Altitude[Direction.NW] = groundNode.Altitude[Direction.NE] - smoothStep;
                                anotherLayerRequired = true;
                            }
                        }
                    }
                }

                // Recalc shape
                for (int x = startX - 1; x <= endX + 1; x++)
                {
                    for (int y = startY - 1; y <= endY + 1; y++)
                    {
                        // Skip the interior of the parcel
                        if (x >= startX && x <= endX && y >= startY && y <= endY) continue;

                        Vector2Int worldCoords = new Vector2Int(x, y);
                        GroundNode groundNode = world.GetGroundNode(worldCoords);
                        if (groundNode == null) continue;

                        groundNode.RecalculateShape();
                    }
                }

                if (anotherLayerRequired)
                {
                    startX--;
                    endX++;
                    startY--;
                    endY++;
                }

            } while (anotherLayerRequired);

            return new Parcel(world, new Vector2Int(startX - 1, startY - 1), new Vector2Int((endX - startX) + 2, (endY - startY) + 2));
        }
    }
}
