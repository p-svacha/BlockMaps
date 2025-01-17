using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        public Vector3 ModelScale { get; set; } = Vector3.one;

        /// <summary>
        /// The index of the material in the MeshRenderer that is colored based on the owner's player color.
        /// <br/> -1 means there is no material.
        /// </summary>
        public int PlayerColorMaterialIndex { get; init; } = -1;

        /// <summary>
        /// If an entity has multiple variants, this list contains them. If an entity has only a single variant, this list is empty and the materials defined in the Model are used.
        /// <br/>Each variant can define specific materials that overwritten with other materials.
        /// </summary>
        public List<EntityVariant> Variants { get; init; } = new();

        /// <summary>
        /// If Type is set to Batch, this function will render the entity.
        /// </summary>
        public System.Action<MeshBuilder, BlockmapNode, int, bool> BatchRenderFunction { get; init; } = (meshBuilder, node, height, isPreview) => throw new System.Exception("BatchRenderFunction not defined");

        /// <summary>
        /// If Type is set to StandaloneGenerated, this function will be used to create the mesh for the entity.
        /// </summary>
        public System.Action<MeshBuilder, int, bool, bool> StandaloneRenderFunction { get; init; } = (meshBuilder, height, isMirrored, isPreview) => throw new System.Exception("StandaloneRenderFunction not defined");

        /// <summary>
        /// Defines where exactly the entity transform is located when placed somewhere.
        /// </summary>
        public PositionType PositionType { get; init; } = PositionType.LowestPoint;

        /// <summary>
        /// The function to retrieve the exact world position of the entity if placed on the given node with the given properties.
        /// <br/>Parameters are: EntityDef, World, TargetNode, TargetRotation, EntityHeight, IsMirrored.
        /// <br/>Only needs to be changed if PositionType is set to custom.
        /// </summary>
        public System.Func<EntityDef, World, BlockmapNode, Direction, int, bool, Vector3> GetWorldPositionFunction { get; init; } = EntityManager.GetWorldPosition;

        /// <summary>
        /// Creates new EntityRenderPropertes
        /// </summary>
        public EntityRenderProperties() { }

        /// <summary>
        /// Creates a deep copy of existing EntityRenderPropertes.
        /// </summary>
        public EntityRenderProperties(EntityRenderProperties orig)
        {
            RenderType = orig.RenderType;
            Model = orig.Model;
            ModelScale = new Vector3(orig.ModelScale.x, orig.ModelScale.y, orig.ModelScale.z);
            PlayerColorMaterialIndex = orig.PlayerColorMaterialIndex;
            Variants = orig.Variants.Select(v => new EntityVariant(v)).ToList();
            BatchRenderFunction = orig.BatchRenderFunction;
            StandaloneRenderFunction = orig.StandaloneRenderFunction;
            PositionType = orig.PositionType;
            GetWorldPositionFunction = orig.GetWorldPositionFunction;
        }
    }

    public enum PositionType
    {
        /// <summary>
        /// The entity gets placed in the x/z center of all occupied nodes.
        /// <br/>Its y position will be set according to the lowest base world altitude of all occupied nodes.
        /// <br/>Commonly used for natural static objects (like rocks, trees) where clipping into terrain is fine.
        /// </summary>
        LowestPoint,

        /// <summary>
        /// The entity gets placed in the x/z center of all occupied nodes.
        /// <br/>Its y position will be set according to the center mesh world position point of the center-most occupied node.
        /// <br/>Commonly used for moving entities / characters so they are exactly at the same position when placed as they would be when walking there.
        /// </summary>
        CenterPoint,

        /// <summary>
        /// The entity gets placed in the x/z center of all occupied nodes.
        /// <br/>Its y position will be set according to the highest max world altitude of all occupied nodes.
        /// <br/>Commonly used for entities that should never clip the ground and are okay with floating above some of their occupied nodes.
        /// </summary>
        HighestPoint,

        /// <summary>
        /// The entity world position gets calculated by a custom function, referenced in WorldPositionFunction.
        /// </summary>
        Custom
    }
}