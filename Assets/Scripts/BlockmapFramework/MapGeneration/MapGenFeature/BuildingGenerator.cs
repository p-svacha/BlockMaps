using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework.WorldGeneration
{
    /// <summary>
    /// Class containing logic for procedurally generated buildings that can be placed on the map, consisting of various world objects.
    /// </summary>
    public static class BuildingGenerator
    {
        private const int MinSize = 5;


        public static void GenerateBuilding(Parcel parcel, BlockmapNode node)
        {
            if (parcel.Dimensions.x < MinSize) return;
            if (parcel.Dimensions.y < MinSize) return;

            World world = parcel.World;


            WallMaterialDef wallMaterial = WallMaterialDefOf.Brick;
            SurfaceDef floor = SurfaceDefOf.WoodParquet;

            int floorAltitude = node.BaseAltitude;
            int buildingSizeX = Random.Range(MinSize, parcel.Dimensions.x + 1);
            int buildingSizeY = Random.Range(MinSize, parcel.Dimensions.y + 1);
            int baseBuildingHeight = 5;

            Vector2Int buildingDimensions = new Vector2Int(buildingSizeX, buildingSizeY);
            Vector2Int buildingPosInParcel = new Vector2Int(parcel.Dimensions.x / 2 - buildingSizeX / 2, parcel.Dimensions.y / 2 - buildingSizeY / 2);

            // Generate footprint (inside/outside)
            Dictionary<Vector2Int, int> buildingHeight = new Dictionary<Vector2Int, int>();
            for (int x = 0; x < buildingSizeX; x++)
            {
                for (int z = 0; z < buildingSizeY; z++)
                {
                    buildingHeight.Add(new Vector2Int(x, z), baseBuildingHeight);
                }
            }

            int numCutCorners = Random.Range(0, 2 + 1);
            for(int i = 0; i < numCutCorners; i++) OffsetCorner(buildingDimensions, buildingHeight);

            // Remove entities
            for (int x = 0; x < buildingSizeX; x++)
            {
                for (int z = 0; z < buildingSizeY; z++)
                {
                    Vector2Int localCoord = new Vector2Int(x, z);
                    int localHeight = buildingHeight[localCoord];
                    if (localHeight == 0) continue;

                    Vector2Int worldCoord = node.WorldCoordinates + buildingPosInParcel + localCoord;

                    GroundNode ground = world.GetGroundNode(worldCoord);
                    List<Entity> entitiesToRemove = ground.Entities.ToList();
                    foreach (Entity e in entitiesToRemove) world.RemoveEntity(e, updateWorld: false);
                }
            }

            // Floor (lower ground or create air nodes for floor)
            for (int x = 0; x < buildingSizeX; x++)
            {
                for (int z = 0; z < buildingSizeY; z++)
                {
                    Vector2Int localCoord = new Vector2Int(x, z);
                    int localHeight = buildingHeight[localCoord];
                    if (localHeight == 0) continue;

                    Vector2Int worldCoord = node.WorldCoordinates + buildingPosInParcel + localCoord;

                    GroundNode ground = world.GetGroundNode(worldCoord);

                    if (ground.BaseAltitude >= floorAltitude) // adjust ground
                    {
                        ground.SetAltitude(floorAltitude);
                        ground.SetSurface(floor);
                    }
                    else // Create air node
                    {
                        if(ground.MaxAltitude == floorAltitude) ground.SetAltitude(floorAltitude - 1); // lower ground 1 if it would intersect new air node
                        world.BuildAirNode(worldCoord, floorAltitude, floor, updateWorld: false);
                    }
                }
            }

            // Place walls and roof
            for (int x = 0; x < buildingSizeX; x++)
            {
                for (int z = 0; z < buildingSizeY; z++)
                {
                    Vector2Int localCoord = new Vector2Int(x, z);
                    int localHeight = buildingHeight[localCoord];
                    if (localHeight == 0) continue; // not inside building -> no walls needed

                    // Check if wall is needed in any direction
                    Vector2Int worldCoord = node.WorldCoordinates + buildingPosInParcel + localCoord;

                    foreach (Direction side in HelperFunctions.GetSides())
                    {
                        if (!buildingHeight.TryGetValue(HelperFunctions.GetWorldCoordinatesInDirection(localCoord, side), out int adjHeight) || adjHeight != localHeight)
                        {
                            // Wall above floor
                            for (int y = adjHeight; y < localHeight; y++)
                            {
                                world.BuildWall(new Vector3Int(worldCoord.x, node.BaseAltitude + y, worldCoord.y), side, WallShapeDefOf.Solid, wallMaterial, updateWorld: false);
                            }

                            // Wall below floor (to ground)
                            if(adjHeight == 0)
                            {
                                GroundNode groundNode = world.GetGroundNode(worldCoord);
                                for (int y = groundNode.BaseAltitude; y < floorAltitude; y++)
                                {
                                    world.BuildWall(new Vector3Int(worldCoord.x, y, worldCoord.y), side, WallShapeDefOf.Solid, wallMaterial, updateWorld: false);
                                }
                            }
                        }
                    }

                    // Roof
                    world.BuildAirNode(worldCoord, node.BaseAltitude + buildingHeight[localCoord], SurfaceDefOf.CorrugatedSteel, updateWorld: false);
                }
            }

            parcel.UpdateWorld();
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
