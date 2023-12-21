using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class Entity : MonoBehaviour
    {
        public World World;
        //public List<Block> OccupiedBlocks;  // Block that the entity is on at this frame
        public BlockmapNode OriginNode;       // Tile that the entity is currently on (the origin of the entity (southwest corner) is on)
        public List<SurfaceNode> OccupiedTerrainNodes = new List<SurfaceNode>();    // Tile that the entity is on at this frame

        public bool[,,] Shape;   // Size and shape of the entity, starting from southwest bottom corner

        public virtual void Init(World world, BlockmapNode position, bool[,,] shape)
        {
            World = world;
            Shape = shape;
            OriginNode = position;
            transform.position = position.GetCenterWorldPosition();
            UpdateOccupiedTerrainTiles();
        }

        // Update is called once per frame
        void Update()
        {

        }

        /// <summary>
        /// Sets OccupiedNodes according to the current OriginNode of the entity. 
        /// </summary>
        protected void UpdateOccupiedTerrainTiles()
        {
            // Remove entity from all currently occupied tiles
            foreach (SurfaceNode t in OccupiedTerrainNodes) t.RemoveEntity(this);
            OccupiedTerrainNodes.Clear();

            if (OriginNode.Type == NodeType.Surface)
            {
                // Set new occupied tiles and add entity to them
                for (int x = 0; x < Shape.GetLength(0); x++)
                {
                    for (int y = 0; y < Shape.GetLength(1); y++)
                    {
                        for (int z = 0; z < Shape.GetLength(2); z++)
                        {
                            if (Shape[x, y, z])
                            {
                                SurfaceNode occupiedTile = World.GetSurfaceNode(OriginNode.WorldCoordinates + new Vector2Int(x, y));
                                OccupiedTerrainNodes.Add(occupiedTile);
                                occupiedTile.AddEntity(this);
                            }
                        }
                    }
                }
            }
        }


        public static Quaternion Get2dRotationByDirection(Direction dir)
        {
            if (dir == Direction.N) return Quaternion.Euler(0f, 90f, 0f);
            if (dir == Direction.NE) return Quaternion.Euler(0f, 135f, 0f);
            if (dir == Direction.E) return Quaternion.Euler(0f, 180f, 0f);
            if (dir == Direction.SE) return Quaternion.Euler(0f, 225f, 0f);
            if (dir == Direction.S) return Quaternion.Euler(0f, 270f, 0f);
            if (dir == Direction.SW) return Quaternion.Euler(0f, 315f, 0f);
            if (dir == Direction.W) return Quaternion.Euler(0f, 0f, 0f);
            if (dir == Direction.NW) return Quaternion.Euler(0f, 45f, 0f);
            return Quaternion.Euler(0f, 0f, 0f);
        }

    }
}
