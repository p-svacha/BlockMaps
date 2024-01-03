using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// A mesh that contains all geometry for all nodes within one height level in a chunk.
    /// </summary>
    public class AirNodeMesh : ChunkMesh
    {
        public int HeightLevel { get; private set; }

        public void Init(Chunk chunk, int level)
        {
            OnInit(chunk);

            HeightLevel = level;
            gameObject.layer = chunk.World.Layer_AirNode;
        }

        /// <summary>
        /// Builds the mesh of this height level
        /// </summary>
        public override void Draw()
        {
            List<AirPathNode> nodesToDraw = Chunk.GetAllAirNodes(HeightLevel);

            if (nodesToDraw.Count == 0) // Deactivate self if nothing there to draw
            {
                gameObject.SetActive(false);
                return;
            }

            // Generate mesh
            gameObject.SetActive(true);
            MeshBuilder meshBuilder = new MeshBuilder(gameObject);
            meshBuilder.AddNewSubmesh(ResourceManager.Singleton.PathMaterial); // Submesh 0: path
            meshBuilder.AddNewSubmesh(ResourceManager.Singleton.PathCurbMaterial); // Submesh 1: path curb
            foreach (BlockmapNode airNode in nodesToDraw)
            {
                airNode.Draw(meshBuilder);
                airNode.SetMesh(this);
            }

            meshBuilder.ApplyMesh();

            // Set chunk values for all materials
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            for (int i = 0; i < renderer.materials.Length; i++)
            {
                renderer.materials[i].SetFloat("_ChunkSize", Chunk.Size);
                renderer.materials[i].SetFloat("_ChunkCoordinatesX", Chunk.Coordinates.x);
                renderer.materials[i].SetFloat("_ChunkCoordinatesY", Chunk.Coordinates.y);
            }
        }

        public override void SetVisibility(Player player)
        {
            if (!gameObject.activeSelf) return;

            // Define visibility array
            List<float> visibilityArray = Enumerable.Repeat(0f, (Chunk.Size + 2) * (Chunk.Size + 2)).ToList(); // Init with 0 everywhere
            for (int x = -1; x <= Chunk.Size; x++)
            {
                for (int y = -1; y <= Chunk.Size; y++)
                {
                    List<AirPathNode> nodes = Chunk.World.GetAirNodes(Chunk.GetWorldCoordinates(new Vector2Int(x, y))).Where(x => x.BaseHeight == HeightLevel).ToList();
                    foreach (AirPathNode node in nodes)
                    {
                        int visibility;
                        if (node.IsVisibleBy(player)) visibility = 2; // 2 = visible
                        else if (node.IsExploredBy(player)) visibility = 1; // 1 = fog of war
                        else visibility = 0; // 0 = unexplored

                        int arrayIndex = (node.LocalCoordinates.y + 1) + (Chunk.Size + 2) * (node.LocalCoordinates.x + 1);
                        visibilityArray[arrayIndex] = visibility;
                    }
                }
            }

            // Pass visibility array to shader
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            for (int i = 0; i < renderer.materials.Length; i++)
            {
                renderer.materials[i].SetFloatArray("_TileVisibility", visibilityArray);
            }
        }
    }
}
