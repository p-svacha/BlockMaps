using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// The definition of a surface. Each node has a particular surface.
    /// Each different walkable texure/material is represented by one instance of a SurfaceDef that contains all information and logic of how it is drawn in the world.
    /// </summary>
    public class SurfaceDef : Def
    {
        /// <summary>
        /// The DefName of the SurfacePropertyDef that has to be set when defining the SurfaceDef.
        /// </summary>
        public string SurfacePropertyDefName { get; init; } = null;

        /// <summary>
        /// The SurfacePropertyDef contains all rules of how the surface behaves gameplay-wise and interacts with other systems and mechanics.
        /// </summary>
        public SurfacePropertyDef Properties { get; private set; } = null;

        /// <summary>
        /// Property that contains all rules of how nodes with this surface should be rendered in the world.
        /// </summary>
        public SurfaceRenderProperties RenderProperties { get; init; } = null;

        public override void ResolveReferences()
        {
            Properties = DefDatabase<SurfacePropertyDef>.GetNamed(SurfacePropertyDefName);
        }

        public override bool Validate()
        {
            if (RenderProperties.Type == SurfaceRenderType.FlatBlendableSurface && RenderProperties.SurfaceReferenceMaterial == null) throw new System.Exception($"{LoadingErrorPrefix}SurfaceDefs that have a RenderType of FlatBlendableSurface need to have a SurfaceReferenceMaterial.");
            if (RenderProperties.Type == SurfaceRenderType.CustomMeshGeneration && RenderProperties.CustomRenderFunction == null) throw new System.Exception($"{LoadingErrorPrefix}SurfaceDefs that have a RenderType of CustomMeshGeneration need to have a CustomRenderFunction.");
            return base.Validate();
        }
    }
}
