using BlockmapFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ExodusOutposAlpha.WorldGeneration
{
    public class WorldGenerator_Exodus : WorldGenerator
    {
        public override string Label => "Exodus: Outpost Alpha";
        public override string Description => "Creates a random map.";
        public override bool StartAsVoid => false;

        private const int GROUND_FLOOR_ALTITUDE = 5;
        private const int FLOOR_HEIGHT = 5;

        protected override List<Action> GetGenerationSteps()
        {
            return new List<Action>()
            {
                CreateOutpost,
            };
        }

        private void CreateOutpost()
        {
            // Hallway
            int hallwayLength = 28;
            int hallwayStartX = World.NumNodesPerSide / 2 - hallwayLength / 2;

            int hallwayWidth = 4;
            int hallwayStartY = World.NumNodesPerSide / 2 - hallwayWidth / 2;
            CreateRoom(new Parcel(World, new Vector2Int(hallwayStartX, hallwayStartY), new Vector2Int(hallwayLength, hallwayWidth)));
        }

        private void CreateRoom(Parcel parcel)
        {
            // Floor
            for (int x = parcel.MinX; x < parcel.MaxX; x++)
            {
                for (int y = parcel.MinY; y < parcel.MaxY; y++)
                {
                    Vector2Int coords = new Vector2Int(x, y);

                    // Floor
                    GroundNode node = World.GetGroundNode(coords);
                    World.SetSurface(node, SurfaceDefOf.DiamondPlate, updateWorld: false);

                    // Wall
                    if(x == parcel.MinX)
                    {
                        BuildWalls(coords, Direction.W, GROUND_FLOOR_ALTITUDE, FLOOR_HEIGHT);
                    }
                    if (x == parcel.MaxX - 1)
                    {
                        BuildWalls(coords, Direction.E, GROUND_FLOOR_ALTITUDE, FLOOR_HEIGHT);
                    }
                    if (y == parcel.MinY)
                    {
                        BuildWalls(coords, Direction.S, GROUND_FLOOR_ALTITUDE, FLOOR_HEIGHT);
                    }
                    if (y == parcel.MaxY - 1)
                    {
                        BuildWalls(coords, Direction.N, GROUND_FLOOR_ALTITUDE, FLOOR_HEIGHT);
                    }

                    // Ceiling
                    World.BuildAirNode(coords, GROUND_FLOOR_ALTITUDE + FLOOR_HEIGHT, SurfaceDefOf.DiamondPlate, updateWorld: false);
                }
            }
        }

        private void BuildWalls(Vector2Int coordinates, Direction side, int startAltitude, int height)
        {
            for(int i = startAltitude; i < startAltitude + height; i++)
            {
                World.BuildWall(new Vector3Int(coordinates.x, i, coordinates.y), side, WallShapeDefOf.Solid, WallMaterialDefOf.CorrugatedSteel, updateWorld: false);
            }
        }
    }
}
