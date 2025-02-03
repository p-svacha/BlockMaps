using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Contains information about restrictions and requirement when spawning an entity.
    /// </summary>
    public class EntitySpawnProperties
    {
        /// <summary>
        /// The world the entity would be spawned in.
        /// </summary>
        public World World { get; private set; }

        /// <summary>
        /// The EntityDef of the entity that would be spawned.
        /// <br/>Either Def or DefProbabilities needs to be set, but not both.
        /// </summary>
        public EntityDef Def { get; init; } = null;

        /// <summary>
        /// The set of EntityDefs with their probabilities that could be spawned.
        /// <br/>Either Def or DefProbabilities needs to be set, but not both.
        /// </summary>
        public Dictionary<EntityDef, float> DefProbabilities { get; init; } = null;

        /// <summary>
        /// The final, resolved rotation EntityDef of that should be spawned. 
        /// </summary>
        public EntityDef ResolvedDef { get; private set; } = null;

        /// <summary>
        /// The actor the entity would provide vision for.
        /// <br/>If not set ( = null), the entity will be owned by Gaia.
        /// </summary>
        public Actor Actor { get; init; } = null;
        public Actor ResolvedActor { get; private set; }

        /// <summary>
        /// The constraints defining where the entity can spawn.
        /// </summary>
        public EntitySpawnPositionProperties PositionProperties { get; init; }
        /// <summary>
        /// The final, resolved target node the entity should be spawned on. 
        /// </summary>
        public BlockmapNode ResolvedTargetNode { get; private set; }

        /// <summary>
        /// Flag if the spawn rotation should be randomized.
        /// </summary>
        public bool RandomRotation { get; init; } = false;
        /// <summary>
        /// The rotation the entity would face when spawned.
        /// </summary>
        public Direction Rotation { get; init; } = Direction.N;
        /// <summary>
        /// The final, resolved rotation the entity should face when spawned. 
        /// </summary>
        public Direction ResolvedRotation { get; private set; }

        /// <summary>
        /// Flag if entity mirroring should be randomized.
        /// </summary>
        public bool RandomMirrored { get; init; } = false;
        /// <summary>
        /// Flag if the entity should be mirrored (x-axis).
        /// </summary>
        public bool Mirrored { get; init; }
        /// <summary>
        /// The final, resolved flag, if the entity should be mirrored when spawned. 
        /// </summary>
        public bool ResolvedMirrored { get; private set; }

        /// <summary>
        /// The custom height of the entity. Only relevant for EntityDefs with VariableHeight = true.
        /// </summary>
        public int CustomHeight { get; init; }
        /// <summary>
        /// The final, resolved height this entity should have when spawned. 
        /// </summary>
        public int ResolvedHeight { get; private set; }

        /// <summary>
        /// String of the entity variant that should be spawned.
        /// </summary>
        public string VariantName { get; init; } = "";

        /// <summary>
        /// Resolved index of the entity variant that should be spawned.
        /// </summary>
        public int ResolvedVariantIndex { get; private set; }

        /// <summary>
        /// Flag if the entity is allowed to be spawned on nodes that already contain other entities.
        /// </summary>
        public bool AllowCollisionWithOtherEntities { get; init; } = false;

        /// <summary>
        /// A collection of nodes that the entity is not allowed to spawn on, and not allowed to move on when checking the roam area.
        /// </summary>
        public List<BlockmapNode> ForbiddenNodes { get; init; } = null;

        /// <summary>
        /// A collection of node tags, where the entity is not allowed to spawn on.
        /// </summary>
        public List<string> ForbiddenNodeTags { get; init; } = null;

        /// <summary>
        /// The amount of nodes the entity needs to be able to roam around on when spawned.
        /// </summary>
        public int RequiredRoamingArea { get; init; } = 0;

        /// <summary>
        /// Internal flag if these properties have been validated. Spawn checks can only be made on validated and resolved SpawnProperties.
        /// </summary>
        public bool IsValidated { get; private set; }
        /// <summary>
        /// Internal flag if these properties have been resolved at least once. Spawn checks can only be made on validated and resolved SpawnProperties.
        /// </summary>
        public bool IsResolved{ get; private set; }

        public EntitySpawnProperties(World world)
        {
            World = world;
        }

        /// <summary>
        /// Checks if all arguments are valid and resolves all arguments (like random selection or variant index).
        /// <br/>If any check fails, this returns false and provides a fail reason.
        /// </summary>
        /// <param name="inWorldGen">Flag if this spawn is executed during world generation. This is relevant because during world generation the navmesh and node mph's (max passable height) have not yet been calculated.</param>
        public bool Validate(bool inWorldGen, out string failReason)
        {
            failReason = "";

            // Check if World exists
            if (World == null)
            {
                failReason = "World is null.";
                return false;
            }

            // Check EntityDef
            if (Def == null && DefProbabilities == null)
            {
                failReason = "Both Def and DefProbabilities is null. One of them needs to be defined.";
                return false;
            }
            if (Def != null && DefProbabilities != null)
            {
                failReason = "Both Def and DefProbabilities is set. Only one of them can be defined.";
                return false;
            }

            // Check if position properties exist
            if (PositionProperties == null)
            {
                failReason = "PositionProperties is null.";
                return false;
            }

            // Validate rotation
            if (!RandomRotation && !HelperFunctions.IsSide(Rotation))
            {
                failReason = $"Rotation is invalid, it may only be None, N, E, S, W. But it was '{Rotation}'.";
                return false;
            }

            // Validate custom height
            if (CustomHeight <= 0)
            {
                if (Def != null && Def.VariableHeight)
                {
                    failReason = $"Entity has a variable height and therefore a positive customHeight needs to be provided, but CustomHeight is set to '{CustomHeight}'.";
                    return false;
                }
                if (DefProbabilities != null)
                {
                    foreach(EntityDef def in DefProbabilities.Keys)
                    {
                        if (def.VariableHeight)
                        {
                            failReason = $"The EntityDef '{def.DefName}' in DefProbabilities has a variable height and therefore a positive customHeight needs to be provided, but CustomHeight is set to '{CustomHeight}'.";
                            return false;
                        }
                    }
                }
            }
            if (CustomHeight > 0)
            {
                if (Def != null && !Def.VariableHeight)
                {
                    failReason = $"Entity does not have a variable height but a positive custom height was provided still, which is illegal. customHeight was '{CustomHeight}'.";
                    return false;
                }
                if (DefProbabilities != null)
                {
                    foreach (EntityDef def in DefProbabilities.Keys)
                    {
                        if (!def.VariableHeight)
                        {
                            failReason = $"The EntityDef '{def.DefName}' in DefProbabilities does not have a variable height but a positive custom height was provided still, which is illegal. customHeight was '{CustomHeight}'.";
                            return false;
                        }
                    }
                }
            }

            // Check required roaming area
            if (RequiredRoamingArea > 0)
            {
                if (inWorldGen)
                {
                    failReason = $"Can't check a required roaming area during world generation because navmesh and node mph's are not yet calculated.";
                    return false;
                }

                if (Def != null && !Def.HasCompProperties<CompProperties_Movement>())
                {
                    failReason = $"Can't check a required roaming area for a non-moving entity.";
                    return false;
                }
                if (DefProbabilities != null)
                {
                    foreach (EntityDef def in DefProbabilities.Keys)
                    {
                        if (!def.HasCompProperties<CompProperties_Movement>())
                        {
                            failReason = $"The EntityDef '{def.DefName}' in DefProbabilities does not have a MovementComp, which it needs to check required roaming area.";
                            return false;
                        }
                    }
                }
            }

            IsValidated = true;
            return true;
        }

        /// <summary>
        /// Resolves all arguments, meaning all random selections (like target node, rotation, mirror, etc.) are reapplied.
        /// </summary>
        public void Resolve()
        {
            // Def
            if (Def != null) ResolvedDef = Def;
            else ResolvedDef = DefProbabilities.GetWeightedRandomElement();

            // Actor
            if (Actor == null) ResolvedActor = World.Gaia;
            else ResolvedActor = Actor;

            // Target node
            ResolvedTargetNode = PositionProperties.GetNewTargetNode(this);

            // Rotation
            if (RandomRotation) ResolvedRotation = HelperFunctions.GetRandomSide();
            else ResolvedRotation = Rotation;

            // Mirrored
            if (RandomMirrored) ResolvedMirrored = Random.value < 0.5f;
            else ResolvedMirrored = Mirrored;

            // Height
            if (ResolvedDef.VariableHeight) ResolvedHeight = CustomHeight;
            else ResolvedHeight = ResolvedDef.Dimensions.y;

            // Variant
            if (VariantName != "")
            {
                EntityVariant variant = ResolvedDef.RenderProperties.Variants.FirstOrDefault(v => v.VariantName == VariantName);
                if (variant != null) ResolvedVariantIndex = ResolvedDef.RenderProperties.Variants.IndexOf(variant);
                else ResolvedVariantIndex = 0; // Fallback when variant name doesn't exist
            }
            else ResolvedVariantIndex = 0;

            IsResolved = true;
        }
    }
}
