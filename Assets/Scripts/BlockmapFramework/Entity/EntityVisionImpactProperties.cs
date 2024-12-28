using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Property within an EntityDef that contains all information of how an entity affects the vision of others.
    /// </summary>
    public class EntityVisionImpactProperties
    {
        /// <summary>
        /// If true, the entity will block incoming vision rays and stuff behind will not be visible.
        /// <br/>If false, the entity will be see-through.
        /// </summary>
        public bool BlocksVision { get; init; } = true;

        /// <summary>
        /// How this entity affects the vision of other entities.
        /// </summary>
        public VisionColliderType VisionColliderType { get; init; } = VisionColliderType.FullBox;

        /// <summary>
        /// If VisionImpact is set to BlockPerNode, the vision collider height of specific local coordinates of the entity (SW -> NE) can be overwritten here.
        /// <br/>The default value is the height of the entity (Dimensions.y).
        /// </summary>
        public Dictionary<Vector2Int, int> VisionBlockHeights { get; init; } = new Dictionary<Vector2Int, int>();

        /// <summary>
        /// Creates new EntityVisionImpactProperties
        /// </summary>
        public EntityVisionImpactProperties() { }

        /// <summary>
        /// Creates a deep copy of existing EntityVisionImpactProperties.
        /// </summary>
        public EntityVisionImpactProperties(EntityVisionImpactProperties orig)
        {
            BlocksVision = orig.BlocksVision;
            VisionColliderType = orig.VisionColliderType;
            VisionBlockHeights = orig.VisionBlockHeights.ToDictionary(x => new Vector2Int(x.Key.x, x.Key.y), x => x.Value);
        }
    }
}