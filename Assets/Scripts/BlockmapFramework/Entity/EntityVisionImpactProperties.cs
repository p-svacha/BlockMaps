using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Property within an EntityDef that contains all information of how an entity affects the vision of others.
    /// </summary>
    public class EntityVisionImpactProperties
    {
        /// <summary>
        /// How this entity affects the vision of other entities.
        /// </summary>
        public EntityVisionImpact ImpactType { get; init; } = EntityVisionImpact.FullBlock;

        /// <summary>
        /// If VisionImpact is set to BlockPerNode, the vision collider height of specific local coordinates of the entity (SW -> NE) can be overwritten here.
        /// <br/>The default value is the height of the entity (Dimensions.y).
        /// </summary>
        public Dictionary<Vector2Int, int> VisionBlockHeights { get; init; } = new Dictionary<Vector2Int, int>();
    }
}