using BlockmapFramework;
using BlockmapFramework.WorldGeneration;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag
{
    public class CTFMapGenerator_Forest : CTFMapGenerator
    {
        public override string Label => "CTF - Forest";

        private int[,] HeightMap;

        protected override void OnGenerationStart()
        {
            base.OnGenerationStart();

            GenerationSteps = new List<System.Action>()
            {
                GenerateNoise,
                ApplyHeightmap,
                CreateMapZones,
                AddWaterBodies,
                AddHedges,
                AddForests,
                AddPaths,
                AddShacks,
                AddFencesAroundPaths,
                CreatePlayerBases,
            };
        }

        #region HeightMap

        private void GenerateNoise()
        {
            PerlinNoise noise = new PerlinNoise();
            noise.Scale = 0.02f;
            LayerOperation op = new LayerOperation(4, 2, 0.5f);

            float heightModifier = 20f;
            HeightMap = new int[WorldSize + 1, WorldSize + 1];
            for (int x = 0; x < WorldSize + 1; x++)
            {
                for (int y = 0; y < WorldSize + 1; y++)
                {
                    float value = op.DoOperation(new GradientNoise[] { noise }, x, y);
                    value *= heightModifier;
                    HeightMap[x, y] = (int)(value);
                }
            }
        }

        private void ApplyHeightmap()
        {

            foreach (GroundNode n in World.GetAllGroundNodes())
            {
                Dictionary<Direction, int> nodeHeights = new Dictionary<Direction, int>()
                    {
                        { Direction.SW, HeightMap[n.WorldCoordinates.x, n.WorldCoordinates.y] },
                        { Direction.SE, HeightMap[n.WorldCoordinates.x + 1, n.WorldCoordinates.y] },
                        { Direction.NE, HeightMap[n.WorldCoordinates.x + 1, n.WorldCoordinates.y + 1] },
                        { Direction.NW, HeightMap[n.WorldCoordinates.x, n.WorldCoordinates.y + 1] },
                    };
                n.SetAltitude(nodeHeights);
            }
        }

        #endregion

        #region Water

        private void AddWaterBodies()
        {
            int numWaterBodies = 0;
            int attempts = 0;

            int targetAttempts = WorldSize / 5;

            while (attempts < targetAttempts)
            {
                attempts++;
                GroundNode n = World.GetRandomGroundNode();
                WaterBody b = World.CanAddWater(n, 3);
                if (b != null)
                {
                    World.AddWaterBody(b, updateWorld: false);
                    numWaterBodies++;
                }
            }

            // End
            Log("Generated " + numWaterBodies + " water bodies after " + attempts + " attempts.");
        }

        #endregion

        #region Hedges

        private int MIN_HEDGE_LENGTH = 3;
        private int MAX_HEDGE_LENGTH = 12;

        private int MIN_HEDGE_HEIGHT = 2;
        private int MAX_HEDGE_HEIGHT = 4;

        private void AddHedges()
        {
            int numHedges = TotalNumChunks / 2;
            for(int i = 0; i < numHedges; i++)
            {
                int hedgeLength = Random.Range(MIN_HEDGE_LENGTH, MAX_HEDGE_LENGTH + 1);
                int hedgeHeight = Random.Range(MIN_HEDGE_HEIGHT, MAX_HEDGE_HEIGHT + 1);

                Vector2Int position = GetRandomWorldCoordinates();
                List<Vector2Int> previousPositions = new List<Vector2Int>();

                for(int j = 0; j < hedgeLength; j++)
                {
                    previousPositions.Add(position);
                    if (!World.IsInWorld(position)) break;

                    GroundNode node = World.GetGroundNode(position);
                    if (World.CanSpawnEntity(EntityDefOf.ProcHedge, node, Direction.N,hedgeHeight, forceHeadspaceRecalc: true))
                    {
                        World.SpawnEntity(EntityDefOf.ProcHedge, node, Direction.N, World.Gaia, updateWorld: false, hedgeHeight);
                    }

                    // Next position
                    int numAttempts = 0;
                    Vector2Int nextPosition = position;
                    while (previousPositions.Contains(nextPosition) && numAttempts++ < 10)
                    {
                        nextPosition = HelperFunctions.GetCoordinatesInDirection(position, HelperFunctions.GetRandomSide());
                    }
                    position = nextPosition;
                }
            }
        }

        #endregion

        #region Trees

        private void AddForests()
        {
            PerlinNoise forestDensityMap = new PerlinNoise();
            forestDensityMap.Scale = 0.1f;
            float densityModifier = 0.15f; // max tree chance per tile

            for (int x = 0; x < WorldSize; x++)
            {
                for (int y = 0; y < WorldSize; y++)
                {
                    float densityMapValue = forestDensityMap.GetValue(x, y);
                    if (densityMapValue > 0.3f && Random.value < densityMapValue * densityModifier)
                    {
                        SpawnRandomTree(x, y);
                    }
                }
            }
        }
        private void SpawnRandomTree(int x, int y)
        {
            BlockmapNode targetNode = World.GetGroundNode(new Vector2Int(x, y));
            Direction rotation = HelperFunctions.GetRandomSide();

            EntityDef def = GetRandomTreeDef();

            if (World.CanSpawnEntity(def, targetNode, rotation, forceHeadspaceRecalc: true))
            {
                World.SpawnEntity(def, targetNode, rotation, World.Gaia, updateWorld: false);
            }
        }
        private EntityDef GetRandomTreeDef()
        {
            Dictionary<string, float> candidateDefNames = new Dictionary<string, float>()
            {
                { "PineSmall", 1f },
                { "PineMedium", 3f },
                { "PineBig", 1f },
                { "LogSmall", 0.3f },
            };
            string chosenDef = HelperFunctions.GetWeightedRandomElement(candidateDefNames);
            return DefDatabase<EntityDef>.GetNamed(chosenDef);
        }

        #endregion

        #region Paths

        private const float MIN_PATH_SEGMENT_LENGTH = 3f;
        private const float MAX_PATH_SEGMENT_LENGTH = 10f;
        private const float MAX_PATH_ANGLE_CHANGE = 30f;
        private const int MIN_PATH_THICKNESS = 1;
        private const int MAX_PATH_THICKNESS = 3;
        private const float FENCE_PROBABILITY = 0.65f;

        /// <summary>
        /// Adds some dirt paths to the map with a random walker algorithm.
        /// </summary>
        private void AddPaths()
        {
            int numStartPoints = NumChunksPerSide / 2;

            for(int i = 0; i < numStartPoints; i++)
            {
                Vector2 startPosition = GetRandomWorldPosition2d();
                float startAngle = Random.Range(0, 360);

                // Create a path going to both directions
                int thickness = Random.Range(MIN_PATH_THICKNESS, MAX_PATH_THICKNESS + 1);
                List<GroundNode> path = DrawPath(startPosition, startAngle, thickness);
                DrawPath(startPosition, startAngle + 180, thickness, existingPath: path);
            }
        }
        /// <summary>
        /// Draws a path until reaching another path or the map edge
        /// </summary>
        private List<GroundNode> DrawPath(Vector2 startPosition, float startAngle, int thickness, List<GroundNode> existingPath = null, List<SurfaceDef> ignoreSurfaces = null)
        {
            // Log($"Drawing path from {startPosition} with angle {startAngle}");
            Vector2 currentPosition = startPosition;
            float currentAngle = startAngle;

            List<GroundNode> allPathNodes = new List<GroundNode>();
            if (existingPath != null) allPathNodes.AddRange(existingPath);

            bool continuePath = true;
            int length = 0;
            while (continuePath && length < 500)
            {
                length++;
                Vector2 segmentStartPosition = currentPosition;

                float angleChange = Random.Range(-MAX_PATH_ANGLE_CHANGE, MAX_PATH_ANGLE_CHANGE);
                currentAngle += angleChange;

                float segmentLength = Random.Range(MIN_PATH_SEGMENT_LENGTH, MAX_PATH_SEGMENT_LENGTH);

                Vector2 segmentEndPosition = segmentStartPosition + new Vector2(
                    Mathf.Sin(Mathf.Deg2Rad * currentAngle) * segmentLength,
                    Mathf.Cos(Mathf.Deg2Rad * currentAngle) * segmentLength);

                List<Vector2Int> pathCoordinates = HelperFunctions.RasterizeLine(segmentStartPosition, segmentEndPosition, thickness);

                foreach (Vector2Int pc in pathCoordinates)
                {
                    GroundNode node = World.GetGroundNode(pc);
                    if (node == null) continue;
                    if (!allPathNodes.Contains(node))
                    {
                        if (node.SurfaceDef == SurfaceDefOf.DirtPath)
                        {
                            continuePath = false; // reached other path
                            break;
                        }
                        else if (ignoreSurfaces == null || !ignoreSurfaces.Contains(node.SurfaceDef))
                        {
                            World.SetSurface(node, SurfaceDefOf.DirtPath, updateWorld: false);
                            allPathNodes.Add(node);
                            World.RemoveEntities(node, updateWorld: false);
                        }
                    }
                }

                currentPosition = segmentEndPosition;

                if (currentPosition.x < 0 || currentPosition.x > WorldSize || currentPosition.y < 0 || currentPosition.y > WorldSize) continuePath = false; // Reached world edge
            }

            return allPathNodes;
        }

        private void AddFencesAroundPaths()
        {
            // Create mask of where to create fences
            PerlinNoise fenceMask = new PerlinNoise();
            CutoffOperation cutoff = new CutoffOperation(FENCE_PROBABILITY, FENCE_PROBABILITY);

            for (int x = 0; x < WorldSize; x++)
            {
                for (int y = 0; y < WorldSize; y++)
                {
                    Vector2Int worldCoordinates = new Vector2Int(x, y);
                    GroundNode node = World.GetGroundNode(worldCoordinates);

                    // Check surface
                    if (node.SurfaceDef != SurfaceDefOf.DirtPath) continue;
                    if (node.WaterNode != null) continue;

                    // Check mask
                    float maskValue = cutoff.DoOperation(new GradientNoise[] { fenceMask }, x, y);
                    if (maskValue != 1) continue;

                    // Try to build fence on all side
                    foreach(Direction side in HelperFunctions.GetSides())
                    {
                        // Don't build if any adjacent node is path too
                        GroundNode adjNode = World.GetAdjacentGroundNode(worldCoordinates, side);
                        if (adjNode == null || adjNode.SurfaceDef == SurfaceDefOf.DirtPath) continue;

                        

                        GroundNode cornerNodePre = World.GetAdjacentGroundNode(worldCoordinates, HelperFunctions.GetPreviousDirection8(side));
                        if (cornerNodePre == null || cornerNodePre.SurfaceDef == SurfaceDefOf.DirtPath)
                        {
                            GroundNode adjNodePre = World.GetAdjacentGroundNode(worldCoordinates, HelperFunctions.GetPreviousSideDirection(side));
                            if (adjNodePre == null || adjNodePre.SurfaceDef != SurfaceDefOf.DirtPath) continue;
                        }

                        GroundNode cornerNodePost = World.GetAdjacentGroundNode(worldCoordinates, HelperFunctions.GetNextDirection8(side));
                        if (cornerNodePost == null || cornerNodePost.SurfaceDef == SurfaceDefOf.DirtPath)
                        {
                            GroundNode adjNodePost = World.GetAdjacentGroundNode(worldCoordinates, HelperFunctions.GetNextSideDirection(side));
                            if (adjNodePost == null || adjNodePost.SurfaceDef != SurfaceDefOf.DirtPath) continue;
                        } 
                        

                        // Build
                        World.BuildFence(FenceDefOf.WoodenFence, node, side, 1, updateWorld: false);
                    }
                }
            }
        }

        #endregion

        #region Shacks

        private const int MIN_SHACK_PARCEL_SIZE = 5;
        private const int MAX_SHACK_PARCEL_SIZE = 15;

        private void AddShacks()
        {
            float minShackDensity = 0.25f; // Min amount of shacks per num chunks per side
            float maxShackDensity = 1f; // Max amount of shacks per num chunks per side

            float shackDensity = Random.Range(minShackDensity, maxShackDensity);
            int targetShacks = Mathf.RoundToInt(NumChunksPerSide * shackDensity);

            int numShacks = 0;
            int attempts = 0;
            int maxAttempts = targetShacks * 20;
            while(numShacks < targetShacks && attempts < maxAttempts)
            {
                attempts++;

                if (TryPlaceRandomShack()) numShacks++;
            }

            Log($"Generated {numShacks}/{targetShacks} shacks after {attempts} attempts. Target density = {shackDensity}.");
        }

        private bool TryPlaceRandomShack()
        {
            int parcelSizeX = Random.Range(MIN_SHACK_PARCEL_SIZE, MAX_SHACK_PARCEL_SIZE + 1);
            int parcelSizeY = Random.Range(MIN_SHACK_PARCEL_SIZE, MAX_SHACK_PARCEL_SIZE + 1);
            Vector2Int parcelCenter = GetRandomWorldCoordinates();
            Vector2Int parcelPosition = new Vector2Int(parcelCenter.x - parcelSizeX / 2, parcelCenter.y - parcelSizeY / 2);

            Parcel parcel = new Parcel(World, parcelPosition, new Vector2Int(parcelSizeX, parcelSizeY));
            if(CanPlaceShack(parcel))
            {
                // Shack
                ShackGenerator.GenerateShack(parcel, World.GetGroundNode(parcelCenter), updateWorld: false);

                // Path leading to shack
                foreach(Door door in ShackGenerator.GeneratedShackInfo.Doors)
                {
                    int doorAltitude = door.MinAltitude;
                    GroundNode outsideGroundNode = World.GetAdjacentGroundNode(door.OriginNode, door.Rotation);

                    if (outsideGroundNode != null)
                    {
                        if (Mathf.Abs(outsideGroundNode.BaseAltitude - doorAltitude) <= 1) // Path directly on ground to door
                        {
                            outsideGroundNode.SetAltitude(doorAltitude);
                            TerrainFunctions.SmoothOutside(outsideGroundNode, 1, new() { ShackGenerator.GeneratedShackInfo.FloorSurface });
                            int thickness = Random.Range(MIN_PATH_THICKNESS, MAX_PATH_THICKNESS + 1);
                            float pathOffsetX = (Random.value * 0.5f) - 0.25f;
                            float pathOffsetY = (Random.value * 0.5f) - 0.25f;
                            DrawPath(outsideGroundNode.WorldCenter2D + new Vector2(pathOffsetX, pathOffsetY), HelperFunctions.GetDirectionAngle(door.Rotation) + 180, thickness, ignoreSurfaces: new() { ShackGenerator.GeneratedShackInfo.FloorSurface });
                        }
                        else // Elevated patio in front of door
                        {
                            // todo
                        }
                    }
                }

                return true;
            }
            return false;
        }

        private bool CanPlaceShack(Parcel parcel)
        {
            if (!parcel.IsInWorld()) return false;
            if (parcel.HasAnyWater()) return false;
            if (parcel.HasAnyNodesWithSurface(SurfaceDefOf.CorrugatedSteel)) return false;
            if (parcel.HasAnyNodesWithSurface(SurfaceDefOf.DirtPath)) return false;
            return true;
        }



        #endregion
    }
}
