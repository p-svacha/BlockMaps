using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    public class EntityVariant
    {
        /// <summary>
        /// The human-readable string used to identify and describe this variant.
        /// </summary>
        public string VariantName { get; init; } = "";

        /// <summary>
        /// Dictionary containing the all materials that should differ from the default at it is defined in the EntityRenderProperties.Model.
        /// <br/>A variant can define specific materials that overwritten with other materials.
        /// <br/>The key of the dictionary in a variant refers to the material index, and the value to the material that replaces the one in the index.
        /// </summary>
        public Dictionary<int, Material> OverwrittenMaterials { get; init; } = new();

        /// <summary>
        /// Creates a new entity variant.
        /// </summary>
        public EntityVariant() { }

        /// <summary>
        /// Creates a deep copy of an existing entity variant.
        /// </summary>
        public EntityVariant(EntityVariant orig)
        {
            VariantName = orig.VariantName;
            OverwrittenMaterials = orig.OverwrittenMaterials.ToDictionary(x => x.Key, x => x.Value);
        }

    }
}
