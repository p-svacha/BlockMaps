using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// A kind of entity whose mesh is generated at runtime and which is rendered on a node-to-node basis (like walls).
    /// </summary>
    public abstract class ProceduralEntity : Entity
    {
        public ProceduralEntity GetInstance()
        {
            GameObject obj = new GameObject(Name);
            ProceduralEntity procEntity = obj.AddComponent<PE001_Hedge>();
            return procEntity;
        }
        public abstract void BuildMesh(MeshBuilder meshBuilder, BlockmapNode node, bool isPreview = false);

        public override void UpdateVisiblity(Actor player) { } // Visibility is handled through ProceduralEntityChunkMesh
    }
}
