using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.Defs
{
    /// <summary>
    /// The collection of all FenceDefs that are useful globally for all projects.
    /// </summary>
    public static class GlobalFenceDefs
    {
        public static List<FenceDef> Defs = new List<FenceDef>()
        {
            new FenceDef()
            {
                DefName = "WoodenFence",
                Label = "wooden Fence",
                Description = "A simple wooden fence.",
                UiPreviewSprite = HelperFunctions.TextureToSprite(GlobalEntityDefs.ThumbnailBasePath + "Fences/WoodenFence"),
                GenerateMeshFunction = MeshGen_WoodenFence.DrawMesh,
            }
        };
    }
}