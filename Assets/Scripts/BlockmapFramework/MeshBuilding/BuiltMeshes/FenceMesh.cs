using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    public class FenceMesh : ChunkMesh
    {
        public int Altitude { get; private set; }

        public void Init(Chunk chunk, int altitude)
        {
            OnInit(chunk);

            Altitude = altitude;
            gameObject.layer = chunk.World.Layer_FenceMesh;
        }

        public override void SetVisibility(Actor activeVisionActor)
        {
            // Define visibility array
            List<float> visibilityArray = new List<float>();
            for (int x = -1; x <= Chunk.Size; x++)
            {
                for (int y = -1; y <= Chunk.Size; y++)
                {
                    int visibility = 0; // 0 = unexplored

                    Vector2Int localCoordinates = new Vector2Int(x, y);
                    Vector2Int worldCoordiantes = Chunk.GetWorldCoordinates(localCoordinates);

                    List<BlockmapNode> nodes = Chunk.World.GetNodes(worldCoordiantes, Altitude).ToList();

                    if (nodes.Any(x => x.GetVisibility(activeVisionActor) == VisibilityType.Visible)) visibility = 2; // 2 = visible
                    else if (nodes.Any(x => x.GetVisibility(activeVisionActor) == VisibilityType.FogOfWar)) visibility = 1; // 1 = fog of war

                    visibilityArray.Add(visibility);
                }
            }


            // Pass to shader
            SetShaderVisibilityData(visibilityArray);
        }
    }
}
