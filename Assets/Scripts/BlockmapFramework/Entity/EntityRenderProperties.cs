using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Property within an EntityDef that contains all rules of how entities should be rendered in the world.
    /// </summary>
    public class EntityRenderProperties
    {
        /// <summary>
        /// The fundamental way of how the mesh for a node of that surface is generated.
        /// </summary>
        public EntityRenderType RenderType { get; init; } = EntityRenderType.NoRender;

        /// <summary>
        /// If RenderType is set to StandaloneModel, this model will be used.
        /// </summary>
        public GameObject Model { get; init; } = null;

        /// <summary>
        /// If RenderType is set to StandaloneModel, this scaling value will be used for the model.
        /// </summary>
        public float ModelScale { get; init; } = 1f;

        /// <summary>
        /// The index of the material in the MeshRenderer that is colored based on the owner's player color.
        /// <br/> -1 means there is no material.
        /// </summary>
        public int PlayerColorMaterialIndex { get; init; } = -1;

        /// <summary>
        /// If Type is set to Batch, this function will render the entity.
        /// </summary>
        public System.Action<MeshBuilder, BlockmapNode, int, bool> BatchRenderFunction { get; init; } = (meshBuilder, node, height, isPreview) => throw new System.Exception("BatchRenderFunction not defined");

        /// <summary>
        /// If Type is set to StandaloneGenerated, this function will be used to create the mesh for the entity.
        /// </summary>
        public System.Action<MeshBuilder, int, bool, bool> StandaloneRenderFunction { get; init; } = (meshBuilder, height, isMirrored, isPreview) => throw new System.Exception("StandaloneRenderFunction not defined");

        /// <summary>
        /// The function to retrieve the exact world position of the entity if placed on the given node with the given properties.
        /// </summary>
        public System.Func<EntityDef, World, BlockmapNode, Direction, bool, Vector3> GetWorldPositionFunction { get; init; } = EntityManager.GetWorldPosition;
    }
}