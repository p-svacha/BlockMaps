using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// The definition of a surface. Each node has a particular surface that defines how the node is rendered and how it behaves.
    /// Each different walkable texure/material is represented by one instance of a SurfaceDef that contains all information and logic of how it is drawn in the world.
    /// </summary>
    public class SurfaceDef : Def
    {
        /// <summary>
        /// If true, characters can't pass nodes with this surface.
        /// </summary>
        public bool Impassable { get; init; } = false;

        /// <summary>
        /// How much a surface with this property slows down movement. Not allowed to exceed 1.
        /// </summary>
        public float MovementSpeedModifier { get; init; } = 1f;

        /// <summary>
        /// If a surface with this property can be painted on nodes in the editor.
        /// </summary>
        public bool Paintable { get; init; } = true;

        /// <summary>
        /// Property that contains all rules of how nodes with this surface should be rendered in the world.
        /// </summary>
        public SurfaceRenderProperties RenderProperties { get; init; } = null;

        public override bool Validate()
        {
            if (RenderProperties.Type == SurfaceRenderType.FlatBlendableSurface && RenderProperties.SurfaceReferenceMaterial == null) ThrowValidationError("SurfaceDefs that have a RenderType of FlatBlendableSurface need to have a SurfaceReferenceMaterial.");
            if (RenderProperties.Type == SurfaceRenderType.CustomMeshGeneration && RenderProperties.CustomRenderFunction == null) ThrowValidationError("SurfaceDefs that have a RenderType of CustomMeshGeneration need to have a CustomRenderFunction.");
            if (MovementSpeedModifier <= 0f) ThrowValidationError("MovementSpeedModifier must be greater than 0.");
            if (MovementSpeedModifier > 1f) ThrowValidationError("MovementSpeedModifier must not be greater than 1 since that would break pathfinding.");
            return base.Validate();
        }
    }
}
