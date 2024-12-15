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

        private static List<WallMaterialDef> WallMaterials = new List<WallMaterialDef>()
        {
            WallMaterialDefOf.Brick,
            WallMaterialDefOf.WoodPlanks,
        };

        public static void GenerateBuilding(Parcel parcel, BlockmapNode node)
        {
            if (parcel.Dimensions.x < MinSize) return;
            if (parcel.Dimensions.y < MinSize) return;

            World world = parcel.World;


            WallMaterialDef mainWallMaterial = GetRandomMaterial();
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

            // Generate noises used when placing the building
            Dictionary<Direction, PerlinNoise> windowNoises = new Dictionary<Direction, PerlinNoise>();
            foreach (Direction side in HelperFunctions.GetSides()) windowNoises.Add(side, new PerlinNoise() { Scale = 0.2f });

            // Declare dictionaries that are filled when placing the building
            Dictionary<Vector2Int, BlockmapNode> buildingFloorNodes = new Dictionary<Vector2Int, BlockmapNode>(); // the erdgeschoss ground/air node for each coordinate

            bool useDarkMetalFoundation = Random.value < 0.5f;

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
                            buildingFloorNodes.Add(localCoord, ground);
                        }
                        else // Create air node
                        {
                            if (ground.MaxAltitude == floorAltitude) ground.SetAltitude(floorAltitude - 1); // lower ground 1 if it would intersect new air node
                            AirNode newNode = world.BuildAirNode(worldCoord, floorAltitude, floor, updateWorld: false);
                            buildingFloorNodes.Add(localCoord, newNode);
                        }
                    }

                    // Outside (terrace) floor
                    if(groundSurfaces[localCoord] != null)
                    {
                        ground.SetAltitude(floorAltitude);
                        ground.SetSurface(groundSurfaces[localCoord]);
                        TerrainFunctions.SmoothOutside(ground, smoothStep: 1);
                    }
                }
            }

            // Place walls / doors
            for (int x = 0; x < buildingSizeX; x++)
            {
                for (int z = 0; z < buildingSizeY; z++)
                {
                    Vector2Int localCoord = new Vector2Int(x, z);
                    Vector2Int worldCoord = node.WorldCoordinates + buildingPosInParcel + localCoord;
                    int localHeight = buildingHeight[localCoord];
                    int ceilingAltidude = floorAltitude + localHeight;
                    GroundNode groundNode = world.GetGroundNode(worldCoord);

                    AirNode ceilingNode = null;

                    // Roof
                    if (roofAltitude[localCoord] > 0) ceilingNode = world.BuildAirNode(worldCoord, floorAltitude + roofAltitude[localCoord], SurfaceDefOf.CorrugatedSteel, updateWorld: false);

                    // Check if wall is needed in any direction
                    if (localHeight > 0) // This node is inside building
                    {
                        foreach (Direction side in HelperFunctions.GetSides())
                        {
                            // Check if it is a wall leading to the outside of the building
                            bool isWallToOutsideBuilding = (!buildingHeight.TryGetValue(HelperFunctions.GetCoordinatesInDirection(localCoord, side), out int adjHeight) || adjHeight == 0);
                            
                            // Check if it is a wall due to different ceiling height
                            bool isWallWithinBuilding = (adjHeight > 0 && adjHeight < localHeight);

                            if (isWallToOutsideBuilding)
                            {
                                // Check if we want a door here
                                bool placeDoor = Random.value < 0.04f;
                                int doorHeight = 4;
                                if(placeDoor) world.BuildDoor(buildingFloorNodes[localCoord], side, doorHeight, isMirrored: Random.value < 0.5f, updateWorld: false);

                                // Check if we want a window here
                                bool placeWindow = !placeDoor && windowNoises[side].GetValue(worldCoord.x, worldCoord.y) > 0.65f;
                                int windowLowerMargin = 2;
                                int windowHeight = 2;

                                // Wall from ground to ceiling
                                for (int y = groundNode.BaseAltitude; y < ceilingAltidude; y++)
                                {
                                    if (!placeDoor || (y < floorAltitude || y >= floorAltitude + doorHeight))
                                    {
                                        WallMaterialDef wallMat = mainWallMaterial;
                                        if (useDarkMetalFoundation && y <= floorAltitude) wallMat = WallMaterialDefOf.MetalDark;

                                        WallShapeDef wallShape = WallShapeDefOf.Solid;
                                        if (placeWindow && (y >= floorAltitude + windowLowerMargin && y < floorAltitude + windowLowerMargin + windowHeight)) wallShape = WallShapeDefOf.Window;

                                        world.BuildWall(new Vector3Int(worldCoord.x, y, worldCoord.y), side, wallShape, wallMat, updateWorld: false);
                                    }
                                }

                                // Check if we want a ladder outside
                                bool placeLadder = Random.value < 0.04f;
                                if (placeLadder)
                                {
                                    GroundNode outsideGroundNode = world.GetAdjacentGroundNode(worldCoord, side);
                                    world.BuildLadder(outsideGroundNode, ceilingNode, HelperFunctions.GetOppositeDirection(side), updateWorld: false);
                                }
                            }
                            else if (isWallWithinBuilding)
                            {
                                // Wall from lower adjacent ceiling height to this ceiling height
                                int adjCeilAltitude = floorAltitude + adjHeight;
                                for (int y = adjCeilAltitude; y < ceilingAltidude; y++)
                                {
                                    world.BuildWall(new Vector3Int(worldCoord.x, y, worldCoord.y), side, WallShapeDefOf.Solid, mainWallMaterial, updateWorld: false);
                                }
                            }
                        }
                    }

                    // Pillar
                    foreach (Direction corner in cornerPillars[localCoord].Keys)
                    {
                        if (cornerPillars[localCoord][corner])
                        {
                            Debug.Log($"haspilar: {groundNode.Altitude[corner]} - {roofAltitude[localCoord]}");

                            for (int y = groundNode.Altitude[corner]; y < floorAltitude + roofAltitude[localCoord]; y++)
                            {
                                world.BuildWall(new Vector3Int(worldCoord.x, y, worldCoord.y), corner, WallShapeDefOf.Corner, mainWallMaterial, updateWorld: false);
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
            return WallMaterials.RandomElement();
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
