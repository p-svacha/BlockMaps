using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    public class WallMesh : ChunkMesh
    {
        public int Altitude { get; private set; }

        public void Init(Chunk chunk, int level)
        {
            OnInit(chunk);

            Altitude = level;
            gameObject.layer = chunk.World.Layer_Wall;
        }

        public override void SetVisibility(Actor player)
        {
            // Set renderer
            if (Renderer == null) Renderer = GetComponent<MeshRenderer>();

            // Define visibility array
            List<float> visibilityArray = new List<float>();
            for (int x = -1; x <= Chunk.Size; x++)
            {
                for (int y = -1; y <= Chunk.Size; y++)
                {
                    int visibility = 0; // 0 = unexplored

                    Vector2Int localCoordinates = new Vector2Int(x, y);
                    Vector2Int worldCoordiantes = Chunk.GetWorldCoordinates(localCoordinates);
                    Vector3Int globalCellCoordinates = new Vector3Int(worldCoordiantes.x, Altitude, worldCoordiantes.y);

                    List<Wall> walls = World.GetWalls(globalCellCoordinates);

                    visibility = 2; // 2 = visible
                    //if (walls.Any(x => x.IsVisibleBy(player))) visibility = 2; // 2 = visible
                    //else if (walls.Any(x => x.IsExploredBy(player))) visibility = 1; // 1 = fog of war

                    visibilityArray.Add(visibility);
                }
            }


            // Pass to shader
            SetShaderVisibilityData(visibilityArray);
        }
    }
}
