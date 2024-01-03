using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class WallMesh : ChunkMesh
    {
        public void Init(Chunk chunk)
        {
            base.OnInit(chunk);

            gameObject.layer = chunk.World.Layer_Wall;
        }

        public override void Draw()
        {
            MeshBuilder meshBuilder = new MeshBuilder(gameObject);


            // todo



            meshBuilder.ApplyMesh(castShadows: false);

            MeshRenderer renderer = GetComponent<MeshRenderer>();

            // Set chunk values for all materials
            for (int i = 0; i < renderer.materials.Length; i++)
            {
                renderer.materials[i].SetFloat("_ChunkSize", Chunk.Size);
                renderer.materials[i].SetFloat("_ChunkCoordinatesX", Chunk.Coordinates.x);
                renderer.materials[i].SetFloat("_ChunkCoordinatesY", Chunk.Coordinates.y);
            }
        }


        public override void SetVisibility(Player player) // same as SurfaceNode
        {
            // Define surface visibility array based on node visibility
            List<float> surfaceVisibilityArray = new List<float>();
            for (int x = -1; x <= Chunk.Size; x++)
            {
                for (int y = -1; y <= Chunk.Size; y++)
                {
                    SurfaceNode targetNode = Chunk.World.GetSurfaceNode(Chunk.GetWorldCoordinates(new Vector2Int(x, y)));

                    int visibility;
                    if (targetNode != null && targetNode.IsVisibleBy(player)) visibility = 2; // 2 = visible
                    else if (targetNode != null && targetNode.IsExploredBy(player)) visibility = 1; // 1 = fog of war
                    else visibility = 0; // 0 = unexplored
                    surfaceVisibilityArray.Add(visibility);
                }
            }

            // Set visibility in all surface mesh materials
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            for (int i = 0; i < renderer.materials.Length; i++)
            {
                renderer.materials[i].SetFloatArray("_TileVisibility", surfaceVisibilityArray);
            }
        }
    }
}
