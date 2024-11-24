using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Class containing logic for procedurally generated buildings that can be placed on the map, consisting of various world objects.
    /// </summary>
    public static class BuildingGenerator
    {

        public static void GenerateBuilding(BlockmapNode node)
        {
            World world = node.World;

            WallMaterialDef mat = WallMaterialDefOf.Brick;

            int floorAltitude = node.BaseAltitude;
            int parcelLengthX = Random.Range(6, 18 + 1);
            int parcelLengthZ = Random.Range(6, 18 + 1);
            int baseBuildingHeight = 5;
            Vector2Int parcelSize = new Vector2Int(parcelLengthX, parcelLengthZ);

            // Generate footprint (inside/outside)
            Dictionary<Vector2Int, int> buildingHeight = new Dictionary<Vector2Int, int>();
            for (int x = 0; x < parcelLengthX; x++)
            {
                for (int z = 0; z < parcelLengthZ; z++)
                {
                    buildingHeight.Add(new Vector2Int(x, z), baseBuildingHeight);
                }
            }

            int numCutCorners = Random.Range(0, 2 + 1);
            for(int i = 0; i < numCutCorners; i++) OffsetCorner(parcelSize, buildingHeight);

            // Remove entities
            for (int x = 0; x < parcelLengthX; x++)
            {
                for (int z = 0; z < parcelLengthZ; z++)
                {
                    Vector2Int localCoord = new Vector2Int(x, z);
                    int localHeight = buildingHeight[localCoord];
                    if (localHeight == 0) continue;

                    Vector2Int worldCoord = node.WorldCoordinates + localCoord;

                    GroundNode ground = world.GetGroundNode(worldCoord);
                    List<Entity> entitiesToRemove = ground.Entities.ToList();
                    foreach (Entity e in entitiesToRemove) world.RemoveEntity(e, updateWorld: false);
                }
            }

            // Flatten surface
            for (int x = 0; x < parcelLengthX; x++)
            {
                for (int z = 0; z < parcelLengthZ; z++)
                {
                    Vector2Int localCoord = new Vector2Int(x, z);
                    int localHeight = buildingHeight[localCoord];
                    if (localHeight == 0) continue;

                    Vector2Int worldCoord = node.WorldCoordinates + localCoord;

                    GroundNode ground = world.GetGroundNode(worldCoord);
                    ground.SetAltitude(floorAltitude);
                }
            }

            // Place walls and roof
            for (int x = 0; x < parcelLengthX; x++)
            {
                for (int z = 0; z < parcelLengthZ; z++)
                {
                    Vector2Int localCoord = new Vector2Int(x, z);
                    int localHeight = buildingHeight[localCoord];
                    if (localHeight == 0) continue; // not inside building -> no walls needed

                    // Check if wall is needed in any direction
                    Vector2Int worldCoord = node.WorldCoordinates + localCoord;

                    foreach (Direction side in HelperFunctions.GetSides())
                    {
                        if (!buildingHeight.TryGetValue(HelperFunctions.GetWorldCoordinatesInDirection(localCoord, side), out int adjHeight) || adjHeight != localHeight)
                        {
                            for (int y = adjHeight; y < localHeight; y++)
                            {
                                world.BuildWall(new Vector3Int(worldCoord.x, node.BaseAltitude + y, worldCoord.y), side, WallShapeDefOf.Solid, mat, updateWorld: false);
                            }
                        }
                    }

                    // Roof
                    world.BuildAirNode(worldCoord, node.BaseAltitude + buildingHeight[localCoord], SurfaceDefOf.CorrugatedSteel, updateWorld: false);
                }
            }

            MapGenFeatureFunctions.UpdateWorld(world, node.WorldCoordinates, parcelLengthX, parcelLengthZ);
        }

        /// <summary>
        /// Cuts or raises a random corner out of the footprint
        /// </summary>
        private static void OffsetCorner(Vector2Int parcelSize, Dictionary<Vector2Int, int> buildingHeight)
        {
            int cutLengthX = Random.Range(2, ((int)(parcelSize.x * 0.75f)) + 1);
            int cutLengthY = Random.Range(2, ((int)(parcelSize.y * 0.75f)) + 1);

            int newHeight = Random.value < 0.5f ? 0 : Random.Range(6, 7 + 1);

            Direction corner = HelperFunctions.GetRandomCorner();

            int startX = HelperFunctions.GetAffectedDirections(corner).Contains(Direction.E) ? 0 : parcelSize.x - cutLengthX;
            int endX = HelperFunctions.GetAffectedDirections(corner).Contains(Direction.E) ? cutLengthX : parcelSize.x;
            int startY = HelperFunctions.GetAffectedDirections(corner).Contains(Direction.S) ? 0 : parcelSize.y - cutLengthY;
            int endY = HelperFunctions.GetAffectedDirections(corner).Contains(Direction.S) ? cutLengthY : parcelSize.y;

            Debug.Log($"Cutting corner {corner} with x={cutLengthX} and y={cutLengthY}.");

            for (int x = startX; x < endX; x++)
            {
                for(int y = startY; y < endY; y++)
                {
                    buildingHeight[new Vector2Int(x, y)] = newHeight;
                }
            }
        }

        private static WallMaterialDef GetRandomMaterial()
        {
            return DefDatabase<WallMaterialDef>.AllDefs.RandomElement();
        }
    }
}
