using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// A kind of entity whose mesh is generated at runtime and which is placed and rendered on a node-to-node basis.
    /// </summary>
    public abstract class ProceduralEntity : Entity
    {
        protected abstract string BaseTypeId { get; }
        public override Sprite GetThumbnail() => Resources.Load<Sprite>("Editor/Thumbnails/ProceduralEntities/" + Name);

        public ProceduralEntity GetInstance(int height)
        {
            GameObject obj = new GameObject(Name);
            ProceduralEntity procEntity = obj.AddComponent<PE001_Hedge>();
            procEntity.InitProceduralEntity(height);
            return procEntity;
        }

        public void InitProceduralEntity(int height)
        {
            TypeId = GetTypeId(height);
            Dimensions = new Vector3Int(1, height, 1);
        }

        protected string GetTypeId(int height) => BaseTypeId + "_" + height.ToString();
        public void BuildMesh(MeshBuilder meshBuilder) => BuildMesh(meshBuilder, OriginNode, Height, isPreview: false);
        public abstract void BuildMesh(MeshBuilder meshBuilder, BlockmapNode node, int height, bool isPreview = false);

        public override void UpdateVisibility(Actor player) { } // Visibility is handled through ProceduralEntityChunkMesh
    }
}
