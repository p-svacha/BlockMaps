using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Represents the surface mesh of a single chunk.
    /// </summary>
    public class SurfaceMesh : ChunkMesh
    {
        public void Init(Chunk chunk)
        {
            base.OnInit(chunk);

            gameObject.layer = chunk.World.Layer_SurfaceNode;
        }

        public override void Draw()
        {
            MeshBuilder meshBuilder = new MeshBuilder(gameObject);
            meshBuilder.AddNewSubmesh(ResourceManager.Singleton.SurfaceMaterial); // Submesh 0: surface
            meshBuilder.AddNewSubmesh(ResourceManager.Singleton.CliffMaterial); // Submesh 1: cliffs
            meshBuilder.AddNewSubmesh(ResourceManager.Singleton.PathMaterial); // Submesh 2: path
            meshBuilder.AddNewSubmesh(ResourceManager.Singleton.PathCurbMaterial); // Submesh 3: pathCurb

            // Shader values
            List<float> surfaceArray = new List<float>();
            List<float> surfaceBlend_W = new List<float>();
            List<float> surfaceBlend_N = new List<float>();
            List<float> surfaceBlend_S = new List<float>();
            List<float> surfaceBlend_E = new List<float>();
            List<float> surfaceBlend_NW = new List<float>();
            List<float> surfaceBlend_NE = new List<float>();
            List<float> surfaceBlend_SW = new List<float>();
            List<float> surfaceBlend_SE = new List<float>();

            foreach (SurfaceNode node in Chunk.GetAllSurfaceNodes())
            {
                // Generate mesh
                node.Draw(meshBuilder);
                node.SetMesh(this);

                // Base surface
                int surfaceId = (int)node.Surface.Id;
                surfaceArray.Add(surfaceId);

                // Blend west
                if (node.ConnectedNodes.TryGetValue(Direction.W, out BlockmapNode westNode)) surfaceBlend_W.Add((int)westNode.Surface.Id);
                else surfaceBlend_W.Add(surfaceId);
                // Blend north
                if (node.ConnectedNodes.TryGetValue(Direction.N, out BlockmapNode northNode)) surfaceBlend_N.Add((int)northNode.Surface.Id);
                else surfaceBlend_N.Add(surfaceId);
                // Blend south
                if (node.ConnectedNodes.TryGetValue(Direction.S, out BlockmapNode southNode)) surfaceBlend_S.Add((int)southNode.Surface.Id);
                else surfaceBlend_S.Add(surfaceId);
                // Blend east
                if (node.ConnectedNodes.TryGetValue(Direction.E, out BlockmapNode eastNode)) surfaceBlend_E.Add((int)eastNode.Surface.Id);
                else surfaceBlend_E.Add(surfaceId);

                // Blend nw
                if (node.ConnectedNodes.TryGetValue(Direction.NW, out BlockmapNode nwNode)) surfaceBlend_NW.Add((int)nwNode.Surface.Id);
                else surfaceBlend_NW.Add(surfaceId);
                // Blend ne
                if (node.ConnectedNodes.TryGetValue(Direction.NE, out BlockmapNode neNode)) surfaceBlend_NE.Add((int)neNode.Surface.Id);
                else surfaceBlend_NE.Add(surfaceId);
                // Blend se
                if (node.ConnectedNodes.TryGetValue(Direction.SE, out BlockmapNode seNode)) surfaceBlend_SE.Add((int)seNode.Surface.Id);
                else surfaceBlend_SE.Add(surfaceId);
                // Blend sw
                if (node.ConnectedNodes.TryGetValue(Direction.SW, out BlockmapNode swNode)) surfaceBlend_SW.Add((int)swNode.Surface.Id);
                else surfaceBlend_SW.Add(surfaceId);
            }
            meshBuilder.ApplyMesh(castShadows: false);

            MeshRenderer renderer = GetComponent<MeshRenderer>();

            // Set chunk values for all materials
            for (int i = 0; i < renderer.materials.Length; i++)
            {
                renderer.materials[i].SetFloat("_ChunkSize", Chunk.Size);
                renderer.materials[i].SetFloat("_ChunkCoordinatesX", Chunk.Coordinates.x);
                renderer.materials[i].SetFloat("_ChunkCoordinatesY", Chunk.Coordinates.y);
            }

            // Set blend values for surface material only
            Material surfaceMaterial = renderer.materials[0];
            surfaceMaterial.SetFloatArray("_TileSurfaces", surfaceArray);
            surfaceMaterial.SetFloatArray("_TileBlend_W", surfaceBlend_W);
            surfaceMaterial.SetFloatArray("_TileBlend_E", surfaceBlend_E);
            surfaceMaterial.SetFloatArray("_TileBlend_N", surfaceBlend_N);
            surfaceMaterial.SetFloatArray("_TileBlend_S", surfaceBlend_S);
            surfaceMaterial.SetFloatArray("_TileBlend_NW", surfaceBlend_NW);
            surfaceMaterial.SetFloatArray("_TileBlend_NE", surfaceBlend_NE);
            surfaceMaterial.SetFloatArray("_TileBlend_SE", surfaceBlend_SE);
            surfaceMaterial.SetFloatArray("_TileBlend_SW", surfaceBlend_SW);
        }
        public override void SetVisibility(Player player)
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
