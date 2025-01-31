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
            gameObject.layer = chunk.World.Layer_WallMesh;
        }

        public override void SetVisibility(Actor activeVisionActor)
        {
            // Define visibility array, for each world coordinate (including all world coordinates of chunk):
            // 0 = not rendered
            // 1 = fog of war (tinted / sometimes transparent)
            // 2 = visible
            List<float> visibilityArray = new List<float>();
            for (int x = -1; x <= Chunk.Size; x++)
            {
                for (int y = -1; y <= Chunk.Size; y++)
                {
                    int visibility = 0; // 0 = not rendered

                    Vector2Int localCoordinates = new Vector2Int(x, y);
                    Vector2Int worldCoordiantes = Chunk.GetWorldCoordinates(localCoordinates);
                    if(!World.IsInWorld(worldCoordiantes)) // Outside world
                    {
                        visibilityArray.Add(0);
                        continue;
                    }
                    Vector3Int globalCellCoordinates = new Vector3Int(worldCoordiantes.x, Altitude, worldCoordiantes.y);

                    List<Wall> walls = World.GetWalls(globalCellCoordinates);

                    if(walls == null) // No walls in this cell
                    {
                        visibilityArray.Add(0); // 0 = not rendered
                        continue;
                    }

                    if (walls.Any(x => x.GetVisibility(activeVisionActor) == VisibilityType.Visible)) visibility = 2; // 2 = visible
                    else if (walls.Any(x => x.GetVisibility(activeVisionActor) == VisibilityType.FogOfWar)) visibility = 1; // 1 = fog of war

                    visibilityArray.Add(visibility);
                }
            }


            // Pass to shader
            SetShaderVisibilityData(visibilityArray);
        }
    }
}
