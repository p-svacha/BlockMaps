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
        /// If the height of some nodes of the entity differs from the default height (Dimensions.y), it can be overwritten here.
        /// <br/>If a node within the dimensions of an shouldn't be affected at all by it, overwrite that coordinate with a height of 0.
        /// <br/>The key refers to the local coordinate within the entity, and the value to the overwritten height.
        /// </summary>
        public Dictionary<Vector2Int, int> OverrideHeights { get; init; } = new Dictionary<Vector2Int, int>();

        /// <summary>
        /// If true, characters can never move on nodes that the entity occupies.
        /// </summary>
        public bool Impassable { get; init; } = true;

        /// <summary>
        /// The amount the entity increases the cost when traversing a node that it is on.
        /// </summary>
        public float MovementSlowdown { get; init; } = 0f;

        /// <summary>
        /// If true, the entity will block incoming vision rays and stuff behind will not be visible.
        /// <br/>If false, the entity will be see-through.
        /// </summary>
        public bool BlocksVision { get; init; } = true;

        /// <summary>
        /// How this entity affects the vision of other entities.
        /// </summary>
        public VisionColliderType VisionColliderType { get; init; } = VisionColliderType.EntityShape;

        /// <summary>
        /// Flag if entity can only be placed when the whole footprint is flat.
        /// </summary>
        public bool RequiresFlatTerrain { get; init; } = false;

        /// <summary>
        /// Flag if the height of this entity is not fixed and can have different values.
        /// </summary>
        public bool VariableHeight { get; init; } = false;

        /// <summary>
        /// How the entity gets rendered when explored, but not currently visible
        /// </summary>
        public ExploredBehaviour ExploredBehaviour { get; init; } = ExploredBehaviour.ExploredForever;

        /// <summary>
        /// How the entity behaves around water - if and how it can exist on water.
        /// </summary>
        public WaterBehaviour WaterBehaviour { get; init; } = WaterBehaviour.Forbidden;

        /// <summary>
        /// Flag, if this entity can exist in the inventory of other entities.
        /// </summary>
        public bool CanBeHeldByOtherEntities { get; init; } = false;

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
            UiSprite = orig.UiSprite;

            // EntityDef
            EntityClass = orig.EntityClass;
            RenderProperties = new EntityRenderProperties(orig.RenderProperties);
            BlocksVision = orig.BlocksVision;
            VisionColliderType = orig.VisionColliderType;
            VisionRange = orig.VisionRange;
            Dimensions = new Vector3Int(orig.Dimensions.x, orig.Dimensions.y, orig.Dimensions.z);
            OverrideHeights = orig.OverrideHeights.ToDictionary(x => new Vector2Int(x.Key.x, x.Key.y), x => x.Value);
            Impassable = orig.Impassable;
            MovementSlowdown = orig.MovementSlowdown;
            RequiresFlatTerrain = orig.RequiresFlatTerrain;
            VariableHeight = orig.VariableHeight;
            ExploredBehaviour = orig.ExploredBehaviour;
            WaterBehaviour = orig.WaterBehaviour;
            CanBeHeldByOtherEntities = orig.CanBeHeldByOtherEntities;

            Components = orig.Components.Select(c => c.Clone()).ToList();
        }


        public bool HasCompProperties<T>()
        {
            return Components != null && Components.Any(c => c is T);
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
            if (RenderProperties.RenderType == EntityRenderType.Batch && (Dimensions.x > 1 || Dimensions.z > 1)) ThrowValidationError("x and z dimensions must be 1 for batch-rendered entities.");
            if (VisionColliderType == VisionColliderType.EntityShape && OverrideHeights.Any(x => x.Value > Dimensions.y)) ThrowValidationError("The height of a vision collider cannot be higher than the height of the entity.");
            if (Impassable && MovementSlowdown > 0f) ThrowValidationError("An EntityDef can not have a MovementSlowdown when it is impassable. It's either or.");
            if (VisionColliderType == VisionColliderType.NodeBased && BlocksVision) ThrowValidationError("Can not block vision when the visibility is node-based.");

            if (RenderProperties.PositionType == PositionType.Custom && RenderProperties.GetWorldPositionFunction == EntityManager.GetWorldPosition) ThrowValidationError("GetWorldPositionFunction needs to be set to a custom function if PositionType is set to Custom.");
            if (RenderProperties.PositionType != PositionType.Custom && RenderProperties.GetWorldPositionFunction != EntityManager.GetWorldPosition) ThrowValidationError("GetWorldPositionFunction cannot be changed if PositionType is not set to Custom.");
            if (RenderProperties.PositionType == PositionType.CenterPoint && (Dimensions.x != 1 || Dimensions.z != 1)) ThrowValidationError("CenterPoint positioning is only implemented for 1x1 entities atm.");

            foreach (CompProperties props in Components)
                if(!props.Validate(this))
                    return false;

            return base.Validate();
        }

        public override void ResolveReferences()
        {
            foreach (CompProperties props in Components) props.ResolveReferences();
        }
    }

    /// <summary>
    /// How the explored state of the entity behaves.
    /// </summary>
    public enum ExploredBehaviour
    {
        /// <summary>
        /// Once explored, the entity stays in that explored state until it is seen again.
        /// <br/>Default behaviour that works well for static entities that never move and also for characters where you want to know where you've last seen them.
        /// </summary>
        ExploredForever,

        /// <summary>
        /// Once explored, the entity stays in that explored state until we go back to the node where we have last seen it (LastKnownPosition).
        /// <br/>If it is still there, it is visible and therefore explored. If it is no longer there, it gets marked as unexplored again (LastKnownPosition gets removed).
        /// </summary>
        ExploredUntilNotSeenOnLastKnownPosition,

        /// <summary>
        /// The entity has no explored state. It is either visible or unexplored.
        /// </summary>
        None
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
