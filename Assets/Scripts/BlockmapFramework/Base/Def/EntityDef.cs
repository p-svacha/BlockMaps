using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        public Type EntityClass { get; init; } = typeof(Entity);

        /// <summary>
        /// Definitions of how this entity is rendered in the world.
        /// </summary>
        public EntityRenderProperties RenderProperties { get; init; } = null;

        /// <summary>
        /// Definitions of how this entity affects the vision of other entities.
        /// </summary>
        public EntityVisionImpactProperties VisionImpactProperties { get; init; } = new EntityVisionImpactProperties();

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
        /// If true, characters can never move on nodes that the entity occupies.
        /// </summary>
        public bool Impassable { get; init; } = true;

        /// <summary>
        /// Flag if entity can only be placed when the whole footprint is flat.
        /// </summary>
        public bool RequiresFlatTerrain { get; init; } = false;

        /// <summary>
        /// Flag if the height of this entity is not fixed and can have different values.
        /// </summary>
        public bool VariableHeight { get; init; } = false;

        /// <summary>
        /// Creates a new EntityDef.
        /// </summary>
        public EntityDef() { }

        /// <summary>
        /// Creates a deep copy of another EntityDef.
        /// </summary>
        public EntityDef(EntityDef orig)
        {
            // Base Def
            DefName = orig.DefName;
            Label = orig.Label;
            Description = orig.Description;
            UiPreviewSprite = orig.UiPreviewSprite;

            // EntityDef
            EntityClass = orig.EntityClass;
            RenderProperties = new EntityRenderProperties(orig.RenderProperties);
            VisionImpactProperties = new EntityVisionImpactProperties(orig.VisionImpactProperties);
            Components = orig.Components.Select(c => c.Clone()).ToList();
            VisionRange = orig.VisionRange;
            Dimensions = new Vector3Int(orig.Dimensions.x, orig.Dimensions.y, orig.Dimensions.z);
            Impassable = orig.Impassable;
            RequiresFlatTerrain = orig.RequiresFlatTerrain;
            VariableHeight = orig.VariableHeight;
        }

        public override bool Validate()
        {
            if (RenderProperties.RenderType == EntityRenderType.StandaloneModel && RenderProperties.Model == null) ThrowValidationError("Model cannot be null in an EntityDef with RenderType = StandaloneModel.");
            if (RenderProperties.RenderType == EntityRenderType.Batch && (Dimensions.x > 1 || Dimensions.z > 1)) ThrowValidationError("x and z dimensions must be 1 for batch-rendered entities.");
            if (VisionImpactProperties.VisionColliderType == VisionColliderType.BlockPerNode && VisionImpactProperties.VisionBlockHeights.Any(x => x.Value > Dimensions.y)) ThrowValidationError("The height of a vision collider cannot be higher than the height of the entity.");

            foreach (CompProperties props in Components)
                if(!props.Validate(this))
                    return false;

            return base.Validate();
        }
    }
}
