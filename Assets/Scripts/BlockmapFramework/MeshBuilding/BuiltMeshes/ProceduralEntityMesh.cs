using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    public class ProceduralEntityMesh : ChunkMesh
    {
        public int HeightLevel { get; private set; }

        public void Init(Chunk chunk, int level)
        {
            OnInit(chunk);

            HeightLevel = level;
            gameObject.layer = chunk.World.Layer_ProceduralEntityMesh;
        }

        public override void SetVisibility(Actor player) // Same as WallMesh
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

                    List<BlockmapNode> nodes = Chunk.World.GetNodes(worldCoordiantes, HeightLevel).ToList();

                    if (nodes.Any(x => x.IsVisibleBy(player))) visibility = 2; // 2 = visible
                    else if (nodes.Any(x => x.IsExploredBy(player))) visibility = 1; // 1 = fog of war

                    visibilityArray.Add(visibility);
                }
            }


            // Pass to shader
            SetShaderVisibilityData(visibilityArray);
        }
    }
}
