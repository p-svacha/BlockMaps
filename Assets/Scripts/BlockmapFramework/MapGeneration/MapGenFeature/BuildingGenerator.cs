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

            // Generate dictionaries that define the building blueprint
            Dictionary<Vector2Int, int> buildingHeight = new Dictionary<Vector2Int, int>(); // for walls to divide inside from outside (walls are created between different heights to close the gap between them)
            Dictionary<Vector2Int, int> roofAltitude = new Dictionary<Vector2Int, int>(); // altitude at which roof nodes get built
            Dictionary<Vector2Int, SurfaceDef> groundSurfaces = new Dictionary<Vector2Int, SurfaceDef>(); // If values are set here, these ground nodes will be flattened and converted to that surface
            Dictionary<Vector2Int, Dictionary<Direction, bool>> cornerPillars = new Dictionary<Vector2Int, Dictionary<Direction, bool>>(); // corner pillars from ground to roof
            for (int x = 0; x < buildingSizeX; x++)
            {
                for (int z = 0; z < buildingSizeY; z++)
                {
                    buildingHeight.Add(new Vector2Int(x, z), baseBuildingHeight);
                    roofAltitude.Add(new Vector2Int(x, z), baseBuildingHeight);
                    groundSurfaces.Add(new Vector2Int(x, z), null);
                    cornerPillars.Add(new Vector2Int(x, z), new Dictionary<Direction, bool>() { { Direction.SW, false }, { Direction.SE, false }, { Direction.NE, false }, { Direction.NW, false } });
                }
            }

            int numCutCorners = Random.Range(0, 2 + 1);
            for(int i = 0; i < numCutCorners; i++) OffsetCorner(buildingDimensions, baseBuildingHeight, buildingHeight, roofAltitude, groundSurfaces, cornerPillars);

            // Remove existing entities that would be in the way
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
                    Vector2Int worldCoord = node.WorldCoordinates + buildingPosInParcel + localCoord;
                    GroundNode ground = world.GetGroundNode(worldCoord);

                    // Inside floor
                    int localHeight = buildingHeight[localCoord];
                    if (localHeight > 0)
                    {
                        if (ground.BaseAltitude >= floorAltitude) // adjust ground
                        {
                            ground.SetAltitude(floorAltitude);
                            ground.SetSurface(floor);
                        }
                        else // Create air node
                        {
                            if (ground.MaxAltitude == floorAltitude) ground.SetAltitude(floorAltitude - 1); // lower ground 1 if it would intersect new air node
                            world.BuildAirNode(worldCoord, floorAltitude, floor, updateWorld: false);
                        }
                    }

                    // Outside (terrace) floor
                    if(groundSurfaces[localCoord] != null)
                    {
                        ground.SetAltitude(floorAltitude);
                        ground.SetSurface(groundSurfaces[localCoord]);
                        TerrainFunctions.SmoothOutside(ground);
                    }
                }
            }

            // Place walls
            for (int x = 0; x < buildingSizeX; x++)
            {
                for (int z = 0; z < buildingSizeY; z++)
                {
                    Vector2Int localCoord = new Vector2Int(x, z);
                    Vector2Int worldCoord = node.WorldCoordinates + buildingPosInParcel + localCoord;
                    int localHeight = buildingHeight[localCoord];

                    // Check if wall is needed in any direction
                    if (localHeight > 0)
                    {
                        foreach (Direction side in HelperFunctions.GetSides())
                        {
                            if (!buildingHeight.TryGetValue(HelperFunctions.GetWorldCoordinatesInDirection(localCoord, side), out int adjHeight) || adjHeight != localHeight)
                            {
                                // Wall above floor
                                for (int y = adjHeight; y < localHeight; y++)
                                {
                                    world.BuildWall(new Vector3Int(worldCoord.x, floorAltitude + y, worldCoord.y), side, WallShapeDefOf.Solid, wallMaterial, updateWorld: false);
                                }

                                // Wall below floor (to ground)
                                if (adjHeight == 0)
                                {
                                    GroundNode groundNode = world.GetGroundNode(worldCoord);
                                    for (int y = groundNode.BaseAltitude; y < floorAltitude; y++)
                                    {
                                        world.BuildWall(new Vector3Int(worldCoord.x, y, worldCoord.y), side, WallShapeDefOf.Solid, wallMaterial, updateWorld: false);
                                    }
                                }
                            }
                        }
                    }

                    // Roof
                    if(roofAltitude[localCoord] > 0) world.BuildAirNode(worldCoord, floorAltitude + roofAltitude[localCoord], SurfaceDefOf.CorrugatedSteel, updateWorld: false);

                    // Pillar
                    foreach (Direction corner in cornerPillars[localCoord].Keys)
                    {
                        if (cornerPillars[localCoord][corner])
                        {
                            GroundNode groundNode = world.GetGroundNode(worldCoord);
                            Debug.Log($"haspilar: {groundNode.Altitude[corner]} - {roofAltitude[localCoord]}");

                            for (int y = groundNode.Altitude[corner]; y < floorAltitude + roofAltitude[localCoord]; y++)
                            {
                                world.BuildWall(new Vector3Int(worldCoord.x, y, worldCoord.y), corner, WallShapeDefOf.Corner, wallMaterial, updateWorld: false);
                            }
                        }
                    }
                }
            }

            parcel.UpdateWorld();
        }

        /// <summary>
        /// Cuts or raises a random corner out of the footprint
        /// </summary>
        private static void OffsetCorner(Vector2Int buildingSize, int baseBuildingHeight, Dictionary<Vector2Int, int> buildingHeight, Dictionary<Vector2Int, int> roofAltitude, Dictionary<Vector2Int, SurfaceDef> groundSurfaces, Dictionary<Vector2Int, Dictionary<Direction, bool>> cornerPillars)
        {
            int cutLengthX = Random.Range(2, ((int)(buildingSize.x * 0.75f)) + 1);
            int cutLengthY = Random.Range(2, ((int)(buildingSize.y * 0.75f)) + 1);

            bool cutCorner = Random.value < 0.5f;

            Direction corner = HelperFunctions.GetRandomCorner();

            int startX = HelperFunctions.GetAffectedDirections(corner).Contains(Direction.W) ? 0 : buildingSize.x - cutLengthX;
            int endX = HelperFunctions.GetAffectedDirections(corner).Contains(Direction.W) ? cutLengthX : buildingSize.x;
            int startY = HelperFunctions.GetAffectedDirections(corner).Contains(Direction.S) ? 0 : buildingSize.y - cutLengthY;
            int endY = HelperFunctions.GetAffectedDirections(corner).Contains(Direction.S) ? cutLengthY : buildingSize.y;

            if (cutCorner)
            {
                bool keepRoof = Random.value < 0.5f;

                for (int x = startX; x < endX; x++)
                {
                    for (int y = startY; y < endY; y++)
                    {
                        Vector2Int localCoord = new Vector2Int(x, y);
                        buildingHeight[localCoord] = 0;

                        // Roof
                        if (keepRoof)
                        {
                            roofAltitude[localCoord] = baseBuildingHeight;
                        }
                        else // Remove roof
                        {
                            roofAltitude[localCoord] = 0;
                        }
                    }
                }

                if (keepRoof)
                {
                    // Chance to add pillar
                    Dictionary<PillarType, float> pillarTypeWeights = new Dictionary<PillarType, float>()
                    {
                        { PillarType.None, 1f },
                        { PillarType.Small, 1f },
                        { PillarType.Big, 1f },
                    };

                    PillarType pillar = pillarTypeWeights.GetWeightedRandomElement();
                    
                    Debug.Log($"Adding pillar in corner {corner}");

                    if (pillar == PillarType.Big)
                    {
                        buildingHeight[HelperFunctions.GetCornerCoordinates(buildingSize, corner)] = baseBuildingHeight;
                    }
                    else if (pillar == PillarType.Small)
                    {
                        cornerPillars[HelperFunctions.GetCornerCoordinates(buildingSize, corner)][corner] = true;
                    }
                }

                bool makeTerrace = Random.value < 0.5f; // if true, flattens the ground and turns it into concrete

                if(makeTerrace)
                {
                    for (int x = startX; x < endX; x++)
                    {
                        for (int y = startY; y < endY; y++)
                        {
                            Vector2Int localCoord = new Vector2Int(x, y);
                            groundSurfaces[localCoord] = SurfaceDefOf.Concrete;
                        }
                    }
                }
            }

            else // Raise corner 
            {
                int newHeight = Random.Range(6, 7 + 1);
                for (int x = startX; x < endX; x++)
                {
                    for (int y = startY; y < endY; y++)
                    {
                        Vector2Int localCoord = new Vector2Int(x, y);
                        buildingHeight[localCoord] = newHeight;
                        roofAltitude[localCoord] = newHeight;
                    }
                }
            }
        }

        private static WallMaterialDef GetRandomMaterial()
        {
            return DefDatabase<WallMaterialDef>.AllDefs.RandomElement();
        }
    }

    #region Enums

    public enum PillarType
    {
        None,
        Big,
        Small
    }

    #endregion
}
