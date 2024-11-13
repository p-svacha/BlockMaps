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
        protected abstract ProceduralEntityId ProceduralId { get; }
        protected abstract bool PE_BlocksVision { get; }
        protected abstract string PE_Name { get; }
        public override Sprite GetThumbnail() => Resources.Load<Sprite>("Editor/Thumbnails/ProceduralEntities/" + Name);

        /// <summary>
        /// The local coordinates of the node within the chunk that this entity is on.
        /// </summary>
        public Vector2Int LocalCoordinates => OriginNode.LocalCoordinates;

        /// <summary>
        /// The coordinates in 3d space of the base of this procedural entity.
        /// </summary>
        public Vector3Int LocalCellCoordinates => new Vector3Int(OriginNode.LocalCoordinates.x, MinAltitude, OriginNode.LocalCoordinates.y);

        public ProceduralEntity GetCopy(int height)
        {
            GameObject obj = new GameObject(Name);
            ProceduralEntity procEntity = obj.AddComponent<PE001_Hedge>();
            procEntity.InitProceduralEntity(height);
            return procEntity;
        }

        public void InitProceduralEntity(int height)
        {
            TypeId = GetTypeId(height);
            BlocksVision = PE_BlocksVision;
            Name = PE_Name;
            Dimensions = new Vector3Int(1, height, 1);
        }

        public override void OnRegister()
        {
            OriginNode.Chunk.RegisterProcEntity(this);
        }
        public override void OnDeregister()
        {
            OriginNode.Chunk.DeregisterProcEntity(this);
        }

        protected string GetTypeId(int height) => ProceduralId.ToString() + "_" + height.ToString();
        public void BuildMesh(MeshBuilder meshBuilder) => BuildMesh(meshBuilder, OriginNode, Height, isPreview: false);
        public abstract void BuildMesh(MeshBuilder meshBuilder, BlockmapNode node, int height, bool isPreview = false);

        public override void UpdateVisibility(Actor player) { } // Visibility is handled through ProceduralEntityChunkMesh
    }
}
