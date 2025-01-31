using BlockmapFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        private const int DOOR_HEIGHT = 4;

        Room Hallway;
        Room Quarters;
        Room StorageRoom;

        protected override List<Action> GetGenerationSteps()
        {
            return new List<Action>()
            {
                CreateOutpost,
                SpawnCharacters
            };
        }

        private void CreateOutpost()
        {
            // Hallway
            int hallwayLength = 28;
            int hallwayStartX = World.NumNodesPerSide / 2 - hallwayLength / 2;

            int hallwayWidth = 4;
            int hallwayStartY = World.NumNodesPerSide / 2 - hallwayWidth / 2;
            Parcel hallwayParcel = new Parcel(World, new Vector2Int(hallwayStartX, hallwayStartY), new Vector2Int(hallwayLength, hallwayWidth));
            Hallway = CreateRoom("hallway", level: 0, hallwayParcel);

            // Quarters
            int quartersSize = 8;
            int quartersStartX = hallwayStartX + 5;
            int quartersStartY = hallwayStartY + hallwayWidth;
            Parcel quartersParcel = new Parcel(World, new Vector2Int(quartersStartX, quartersStartY), new Vector2Int(quartersSize, quartersSize));
            Quarters = CreateRoom("quarters", level: 0, quartersParcel);

            // Storage
            int storageSize = 12;
            int storageStartX = hallwayStartX - 1;
            int storageStartY = hallwayParcel.MinY - storageSize;
            Parcel storageParcel = new Parcel(World, new Vector2Int(storageStartX, storageStartY), new Vector2Int(storageSize, storageSize));
            StorageRoom = CreateRoom("storage", level: 0, storageParcel);

            // Connect rooms
            ConnectRooms(Quarters, Hallway, level: 0);
            ConnectRooms(StorageRoom, Hallway, level: 0);
        }

        private Room CreateRoom(string label, int level, Parcel parcel)
        {
            List<BlockmapNode> floorNodes = new List<BlockmapNode>();
            List<Wall> interiorWalls = new List<Wall>();

            // Floor
            for (int x = parcel.MinX; x < parcel.MaxX; x++)
            {
                for (int y = parcel.MinY; y < parcel.MaxY; y++)
                {
                    Vector2Int coords = new Vector2Int(x, y);

                    // Floor
                    GroundNode node = World.GetGroundNode(coords);
                    World.SetSurface(node, SurfaceDefOf.DiamondPlate, updateWorld: false);
                    floorNodes.Add(node);

                    // Wall
                    if(x == parcel.MinX)
                    {
                        BuildRoomInteriorWalls(coords, Direction.W, GROUND_FLOOR_ALTITUDE, FLOOR_HEIGHT, out List<Wall> builtWalls);
                        interiorWalls.AddRange(builtWalls);
                    }
                    if (x == parcel.MaxX - 1)
                    {
                        BuildRoomInteriorWalls(coords, Direction.E, GROUND_FLOOR_ALTITUDE, FLOOR_HEIGHT, out List<Wall> builtWalls);
                        interiorWalls.AddRange(builtWalls);
                    }
                    if (y == parcel.MinY)
                    {
                        BuildRoomInteriorWalls(coords, Direction.S, GROUND_FLOOR_ALTITUDE, FLOOR_HEIGHT, out List<Wall> builtWalls);
                        interiorWalls.AddRange(builtWalls);
                    }
                    if (y == parcel.MaxY - 1)
                    {
                        BuildRoomInteriorWalls(coords, Direction.N, GROUND_FLOOR_ALTITUDE, FLOOR_HEIGHT, out List<Wall> builtWalls);
                        interiorWalls.AddRange(builtWalls);
                    }

                    // Ceiling
                    World.BuildAirNode(coords, GROUND_FLOOR_ALTITUDE + FLOOR_HEIGHT, SurfaceDefOf.DiamondPlate, updateWorld: false);
                }
            }

            // Room
            return World.AddRoom(label, floorNodes, interiorWalls);
        }
        private void BuildRoomInteriorWalls(Vector2Int coordinates, Direction side, int startAltitude, int height, out List<Wall> builtWalls)
        {
            builtWalls = new List<Wall>();
            for(int i = startAltitude; i < startAltitude + height; i++)
            {
                Wall builtWall = World.BuildWall(new Vector3Int(coordinates.x, i, coordinates.y), side, WallShapeDefOf.Solid, WallMaterialDefOf.CorrugatedSteel, updateWorld: false);
                builtWalls.Add(builtWall);
            }
        }
        private void ConnectRooms(Room r1, Room r2, int level)
        {
            // Get all wall pieces in r1 that are connected to r2
            List<Wall> r1Candidates = r1.InteriorWalls.Where(w => w.ExteriorRoom == r2).ToList();

            // Take a wall that acts as a reference point for the connection
            Wall chosenR1Wall = r1Candidates.RandomElement();

            // Calculate altitudes at which we need to destroy walls in both rooms
            int startAltitude = GetLevelAltitude(level);
            int endAltitude = startAltitude + DOOR_HEIGHT;

            // Remove walls in r1
            Vector2Int r1Coordinates = chosenR1Wall.WorldCoordinates;
            for (int i = startAltitude; i < endAltitude; i++)
            {
                Wall wallToRemove = World.GetWall(new Vector3Int(r1Coordinates.x, i, r1Coordinates.y), chosenR1Wall.Side);
                World.RemoveWall(wallToRemove, updateWorld: false);
            }

            // Remove walls in r2
            Vector2Int r2Coordinates = HelperFunctions.GetCoordinatesInDirection(r1Coordinates, chosenR1Wall.Side);
            for (int i = startAltitude; i < endAltitude; i++)
            {
                Wall wallToRemove = World.GetWall(new Vector3Int(r2Coordinates.x, i, r2Coordinates.y), chosenR1Wall.OppositeSide);
                World.RemoveWall(wallToRemove, updateWorld: false);
            }

        }


        private void SpawnCharacters()
        {
            BlockmapNode spawnNode = Quarters.FloorNodes.RandomElement();
            World.SpawnEntity(DefDatabase<EntityDef>.GetNamed("Human4"), spawnNode, Direction.N, isMirrored: false, World.GetActor(1), updateWorld: false);
        }

        private int GetLevelAltitude(int level) => GROUND_FLOOR_ALTITUDE + level * FLOOR_HEIGHT;
    }
}
