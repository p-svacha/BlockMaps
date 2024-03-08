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
            OnInit(chunk);

            gameObject.layer = chunk.World.Layer_SurfaceNode;
        }

        public override void OnDraw()
        {
            // Create surface material submesh so it always has index 0
            MeshBuilder.GetSubmesh(ResourceManager.Singleton.SurfaceMaterial);

            // Draw each node on the mesh builder
            foreach (SurfaceNode node in Chunk.GetAllSurfaceNodes())
            {
                // Generate mesh
                node.Draw(MeshBuilder);
                node.SetMesh(this);
            }
        }

        public override void OnMeshApplied()
        {
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
                // Base surface
                int surfaceId = (int)node.Surface.Id;
                surfaceArray.Add(surfaceId);

                // Blend west
                SurfaceNode westNode = World.GetAdjacentSurfaceNode(node, Direction.W);
                if (DoBlend(node, westNode, Direction.W)) surfaceBlend_W.Add((int)westNode.Surface.Id);
                else surfaceBlend_W.Add(surfaceId);
                // Blend north
                SurfaceNode northNode = World.GetAdjacentSurfaceNode(node, Direction.N);
                if (DoBlend(node, northNode, Direction.N)) surfaceBlend_N.Add((int)northNode.Surface.Id);
                else surfaceBlend_N.Add(surfaceId);
                // Blend south
                SurfaceNode southNode = World.GetAdjacentSurfaceNode(node, Direction.S);
                if (DoBlend(node, southNode, Direction.S)) surfaceBlend_S.Add((int)southNode.Surface.Id);
                else surfaceBlend_S.Add(surfaceId);
                // Blend east
                SurfaceNode eastNode = World.GetAdjacentSurfaceNode(node, Direction.E);
                if (DoBlend(node, eastNode, Direction.E)) surfaceBlend_E.Add((int)eastNode.Surface.Id);
                else surfaceBlend_E.Add(surfaceId);

                // Blend nw
                SurfaceNode nwNode = World.GetAdjacentSurfaceNode(node, Direction.NW);
                if (DoBlend(node, nwNode, Direction.NW)) surfaceBlend_NW.Add((int)nwNode.Surface.Id);
                else surfaceBlend_NW.Add(surfaceId);
                // Blend ne
                SurfaceNode neNode = World.GetAdjacentSurfaceNode(node, Direction.NE);
                if (DoBlend(node, neNode, Direction.NE)) surfaceBlend_NE.Add((int)neNode.Surface.Id);
                else surfaceBlend_NE.Add(surfaceId);
                // Blend se
                SurfaceNode seNode = World.GetAdjacentSurfaceNode(node, Direction.SE);
                if (DoBlend(node, seNode, Direction.SE)) surfaceBlend_SE.Add((int)seNode.Surface.Id);
                else surfaceBlend_SE.Add(surfaceId);
                // Blend sw
                SurfaceNode swNode = World.GetAdjacentSurfaceNode(node, Direction.SW);
                if (DoBlend(node, swNode, Direction.SW)) surfaceBlend_SW.Add((int)swNode.Surface.Id);
                else surfaceBlend_SW.Add(surfaceId);
            }

            // Set blend values for surface material only
            Material surfaceMaterial = Renderer.materials[0];
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

        /// <summary>
        /// Returns if a source node should have the adjacent node in a given direction blended into itself.
        /// </summary>
        private bool DoBlend(SurfaceNode sourceNode, SurfaceNode adjacentNode, Direction dir)
        {
            if (adjacentNode == null) return false; // no adajcent node
            if (!World.DoAdjacentHeightsMatch(sourceNode, adjacentNode, dir)) return false; // adjacent node isn't connected seamlessly
            if (!sourceNode.Surface.DoBlend) return false; // source node surface doesn't blend
            if (!adjacentNode.Surface.DoBlend) return false; // adjacent node surface doesn't blend
            return true;
        }

        public override void SetVisibility(Actor player)
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
            for (int i = 0; i < Renderer.materials.Length; i++)
            {
                Renderer.materials[i].SetFloat("_FullVisibility", 0);
                Renderer.materials[i].SetFloatArray("_TileVisibility", surfaceVisibilityArray);
            }
        }
    }
}
