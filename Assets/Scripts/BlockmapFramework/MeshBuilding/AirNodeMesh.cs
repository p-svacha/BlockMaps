using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// A mesh that contains all geometry for all nodes within one height level in a chunk.
    /// </summary>
    public class AirNodeMesh : MonoBehaviour
    {
        private Chunk Chunk;
        public int HeightLevel { get; private set; }

        public void Init(Chunk chunk, int level)
        {
            Chunk = chunk;
            HeightLevel = level;

            gameObject.layer = chunk.World.Layer_AirNode;
            transform.SetParent(chunk.transform);
        }

        /// <summary>
        /// Builds the mesh of this height level
        /// </summary>
        public void Draw()
        {
            List<BlockmapNode> nodesToDraw = Chunk.GetAllAirNodes().Where(x => x.BaseHeight == HeightLevel).ToList();

            if (nodesToDraw.Count == 0) // Remove existing mesh if no nodes anymore
            {
                MeshFilter mesh = gameObject.GetComponent<MeshFilter>();
                if (mesh != null) mesh.mesh.Clear();
                return;
            }

            MeshBuilder meshBuilder = new MeshBuilder(gameObject);
            int airSubmesh = meshBuilder.AddNewSubmesh(ResourceManager.Singleton.CliffMaterial); // todo: change material according to path type
            foreach (BlockmapNode airNode in nodesToDraw) airNode.Draw(meshBuilder);
            meshBuilder.ApplyMesh();
        }

    }
}
