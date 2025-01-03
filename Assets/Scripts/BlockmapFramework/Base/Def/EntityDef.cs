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
        /// How the entity behaves around water - if and how it can exist on water.
        /// </summary>
        public WaterBehaviour WaterBehaviour { get; init; } = WaterBehaviour.Forbidden;

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
            WaterBehaviour = orig.WaterBehaviour;
        }


        /// <summary>
        /// Retrieve specific CompProperties of this def. Throws an error if it doesn't have it.
        /// </summary>
        public T GetCompProperties<T>() where T : CompProperties
        {
            if (Components != null)
            {
                int i = 0;
                for (int count = Components.Count; i < count; i++)
                {
                    if (Components[i] is T result)
                    {
                        return result;
                    }
                }
            }
            throw new System.Exception($"CompProperties {typeof(T)} not found on EntityDef {Label}. EntityDef has {Components.Count} CompProperties.");
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

    public enum WaterBehaviour
    {
        /// <summary>
        /// The entity can be placed on water and will be fully above the water surface.
        /// </summary>
        AboveWater,

        /// <summary>
        /// The entity can be placed on water and will be sink halway below the water surface.
        /// <br/>Useful to simulate swimming characters.
        /// </summary>
        HalfBelowWaterSurface,

        /// <summary>
        /// The entity cannot be placed on water.
        /// </summary>
        Forbidden
    }
}
