using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// A mesh that contains all geometry for all nodes within one height level in a chunk.
    /// </summary>
    public class AirNodeMesh : NodeMesh
    {
        public int Altitude { get; private set; }

        public void Init(Chunk chunk, int level)
        {
            OnInit(chunk);

            Altitude = level;
            gameObject.layer = chunk.World.Layer_AirNode;
        }

        protected override BlockmapNode GetNode(Vector2Int localCoordinates)
        {
            return World.GetAirNodes(Chunk.GetWorldCoordinates(localCoordinates), Altitude).OrderBy(x => x.MaxAltitude).FirstOrDefault();
        }

        public override void SetVisibility(Actor player)
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

                    List<AirNode> nodes = Chunk.World.GetAirNodes(worldCoordiantes, Altitude);

                    if (nodes.Any(x => x.IsVisibleBy(player))) visibility = 2; // 2 = visible
                    else if (nodes.Any(x => x.IsExploredBy(player))) visibility = 1; // 1 = fog of war

                    visibilityArray.Add(visibility);
                }
            }

            // Pass to shader
            SetShaderVisibilityData(visibilityArray);
        }

        #region Generator

        /// <summary>
        /// Generates all AirNodeMeshes for a single chunk. (one per height level)
        /// </summary>
        public static Dictionary<int, AirNodeMesh> GenerateAirNodeMeshes(Chunk chunk)
        {
            Dictionary<int, AirNodeMesh> meshes = new Dictionary<int, AirNodeMesh>();

            for (int altitude = 0; altitude < World.MAX_ALTITUDE; altitude++)
            {
                List<AirNode> nodesToDraw = chunk.GetAirNodes(altitude);
                if (nodesToDraw.Count == 0) continue;

                // Generate mesh
                GameObject meshObject = new GameObject("AirNodeMesh_" + altitude);
                AirNodeMesh mesh = meshObject.AddComponent<AirNodeMesh>();
                mesh.Init(chunk, altitude);

                MeshBuilder meshBuilder = new MeshBuilder(meshObject);
                foreach (AirNode airNode in nodesToDraw)
                {
                    airNode.DrawSurface(meshBuilder);
                    airNode.SetMesh(mesh);
                }
                meshBuilder.ApplyMesh();
                mesh.OnMeshApplied();

                meshes.Add(altitude, mesh);
            }

            return meshes;
        }

        #endregion
    }
}
