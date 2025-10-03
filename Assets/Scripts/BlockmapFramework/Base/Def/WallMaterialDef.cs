using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// The definition of a wall material. Each wall consists of a combination of shape + material.
    /// </summary>
    public class WallMaterialDef : Def
    {
        public override Sprite UiSprite { get => HelperFunctions.TextureToSprite(Material.mainTexture); }

        /// <summary>
        /// Name of the Unity material used to render this WallMaterial. 
        /// </summary>
        public string MaterialName { get; init; } = null;

        /// <summary>
        /// The minimun climbing skill a character needs to be able to climb a wall with this material.
        /// </summary>
        public ClimbingCategory ClimbSkillRequirement { get; init; } = ClimbingCategory.Unclimbable;

        /// <summary>
        /// The cost of climbing up one wall piece with this material. Must be at least 1.
        /// </summary>
        public float ClimbCostUp { get; init; } = 2.5f;

        /// <summary>
        /// The cost of climbing down one wall piece with this material. Must be at least 1.
        /// </summary>
        public float ClimbCostDown { get; init; } = 1.5f;



        /// <summary>
        /// The material that is used to render walls with this material.
        /// </summary>
        public Material Material => MaterialManager.LoadMaterial($"Materials/NodeMaterials/{MaterialName}");
    }
}
