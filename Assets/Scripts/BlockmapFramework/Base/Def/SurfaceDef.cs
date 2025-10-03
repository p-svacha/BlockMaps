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

        /// <summary>
        /// Returns the full path (within Resources) of the main material used for this surface.
        /// </summary>
        public string GetFullMaterialResourcePath()
        {
            if (RenderProperties.Type == SurfaceRenderType.Default_Blend) return "Materials/BlendSurfaceReferenceMaterials/" + RenderProperties.MaterialName;
            if (RenderProperties.Type == SurfaceRenderType.Default_NoBlend) return "Materials/NodeMaterials/" + RenderProperties.MaterialName;

            throw new Exception($"Can't call FullMaterialResourcePath() on SurfaceDef {DefName} with Type {RenderProperties.Type} because that type doesn't use a main material.");
        }

        public override bool Validate()
        {
            if ((RenderProperties.Type == SurfaceRenderType.Default_Blend || RenderProperties.Type == SurfaceRenderType.Default_NoBlend)  && RenderProperties.MaterialName == null) ThrowValidationError("SurfaceDefs that have a default RenderType need to have a Material defined.");
            if (RenderProperties.Type == SurfaceRenderType.CustomMeshGeneration && RenderProperties.CustomRenderFunction == null) ThrowValidationError("SurfaceDefs that have a RenderType of CustomMeshGeneration need to have a CustomRenderFunction.");
            if (MovementSpeedModifier <= 0f) ThrowValidationError("MovementSpeedModifier must be greater than 0.");
            if (MovementSpeedModifier > 1f) ThrowValidationError("MovementSpeedModifier must not be greater than 1 since that would break pathfinding.");
            return base.Validate();
        }

        /// <summary>
        /// Returns the texture used on this SurfaceDef as a sprite.
        /// For SurfaceDefs with simple meshes (Default_Blend & Default_NoBlend) it creates the sprite from the main texture of the material used.
        /// For complex SurfaceDefs with CustomMeshGeneration it return the UiSprite as defined in the Def.
        /// </summary>
        public Sprite GetPreviewSprite()
        {
            if (RenderProperties.Type == SurfaceRenderType.Default_Blend || RenderProperties.Type == SurfaceRenderType.Default_NoBlend)
            {
                if (cachedSprite == null)
                {
                    cachedSprite = HelperFunctions.TextureToSprite(MaterialManager.LoadMaterial(GetFullMaterialResourcePath()).mainTexture);
                }

                return cachedSprite;
            }

            return UiSprite;
        }
        private Sprite cachedSprite;
    }
}
