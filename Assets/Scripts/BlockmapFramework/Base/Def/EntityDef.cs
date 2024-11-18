using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// The blueprint of an entity.
    /// </summary>
    public class EntityDef : Def
    {
        /// <summary>
        /// The class that will be instantiated when making entity.
        /// </summary>
        public Type EntityClass { get; init; } = null;

        /// <summary>
        /// Definitions of how this entity is rendered in the world.
        /// </summary>
        public EntityRenderProperties RenderProperties { get; init; } = null;

        /// <summary>
        /// Definitions of how this entity affects the vision of other entities.
        /// </summary>
        public EntityVisionImpactProperties VisionImpact { get; init; } = new EntityVisionImpactProperties();

        /// <summary>
        /// Components that add custom behaviour to this entity.
        /// </summary>
        public List<CompProperties> Components { get; init; } = new();

        /// <summary>
        /// How far this entity can see.
        /// </summary>
        public float VisionRange { get; init; } = 0f;

        /// <summary>
        /// The maximum bounds of this entity in all 3 dimensions.
        /// </summary>
        public Vector3Int Dimensions { get; init; } = new Vector3Int(1, 1, 1);

        /// <summary>
        /// Flag if characters can move through this entity.
        /// </summary>
        public bool IsPassable { get; init; } = false;

        /// <summary>
        /// Flag if entity can only be placed when the whole footprint is flat.
        /// </summary>
        public bool RequiresFlatTerrain { get; init; } = false;

        /// <summary>
        /// The index of the material in the MeshRenderer that is colored based on the owner's player color.
        /// <br/> -1 means there is no material.
        /// </summary>
        public int PlayerColorMaterialIndex { get; init; } = -1;

        /// <summary>
        /// Flag if the height of this entity is not fixed and can have different values.
        /// </summary>
        public bool VariableHeight { get; init; } = false;

        public override bool Validate()
        {
            if (RenderProperties.RenderType == EntityRenderType.Batch && (Dimensions.x > 1 || Dimensions.z > 1)) throw new Exception(LoadingErrorPrefix + "x and z dimensions must be 1 for batch-rendered entities.");

            return base.Validate();
        }
    }
}
