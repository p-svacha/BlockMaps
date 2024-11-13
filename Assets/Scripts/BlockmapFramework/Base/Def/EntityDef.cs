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
        /// The class that will be instantiated when making thing Thing.
        /// </summary>
        public Type EntityClass { get; init; } = null;

        /// <summary>
        /// The way this thing is rendered in the world, either as an individual object or as part of a batch object.
        /// </summary>
        public EntityRenderType RenderType { get; init; } = EntityRenderType.NoRender;
    }
}
