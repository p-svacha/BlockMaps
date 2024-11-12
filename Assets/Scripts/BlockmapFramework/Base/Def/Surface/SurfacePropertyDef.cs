using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// The definition of a ruleset of how surfaces behave. Each SurfaceDef has its behaviour defined by a SurfacePropertyDef and multiple SurfaceDefs can use the same SurfacePropertyDef.
    /// </summary>
    public class SurfacePropertyDef : Def
    {
        /// <summary>
        /// How much a surface with this property slows down movement. Not allowed to exceed 1.
        /// </summary>
        public float MovementSpeedModifier { get; init; } = 1f;

        /// <summary>
        /// If a surface with this property can be painted on nodes in the editor.
        /// </summary>
        public bool Paintable { get; init; } = true;

        public override bool Validate()
        {
            if (MovementSpeedModifier <= 0f) throw new Exception(LoadingErrorPrefix + "MovementSpeedModifier must be greater than 0.");
            if (MovementSpeedModifier > 1f) throw new Exception(LoadingErrorPrefix + "MovementSpeedModifier must not be greater than 1 since that would break pathfinding.");
            return base.Validate();
        }
    }
}
