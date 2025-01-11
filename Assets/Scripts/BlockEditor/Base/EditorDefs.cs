using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BlockmapFramework;
using BlockmapFramework.WorldGeneration;
using static BlockmapFramework.Defs.GlobalEntityDefs;

namespace WorldEditor
{
    public static class EditorDefs
    {
        public static List<EntityDef> EntityDefs = new List<EntityDef>()
        {
            new EntityDef()
            {
                DefName = "EditorDynamicCharacter",
                Label = "Dynamic Character",
                Description = "A character whose moving attributes can be set dynamically in the editor",
                UiPreviewSprite = HelperFunctions.TextureToSprite("Editor/Icons/DynamicCharacter"),
                EntityClass = typeof(EditorMovingEntity),
                VariableHeight = true,
                Impassable = false,
                RenderProperties = new EntityRenderProperties()
                {
                    RenderType = EntityRenderType.StandaloneModel,
                    Model = Resources.Load<GameObject>(EntityModelPath + "character/character_fbx"),
                },
                Components = new List<CompProperties>()
                {
                    new CompProperties_Movement() { },
                },
            }
        };

        public static List<WorldModifierDef> WorldModifierDefs = new List<WorldModifierDef>()
        {
            new WorldModifierDef()
            {
                DefName = "Desert_BaseHeightMap",
                Label = "set desert base heightmap",
                Description = "Applies the base height map of the desert world generator to the whole map.",
                ModifierAction = WorldGenerator_Desert.ApplyBaseHeightmap,
            },

            new WorldModifierDef()
            {
                DefName = "Desert_SetSurface",
                Label = "set desert base surface",
                Description = "Applies the base surfaces of the desert world generator to the whole map.",
                ModifierAction = WorldGenerator_Desert.SetBaseSurfaces,
            },

            new WorldModifierDef()
            {
                DefName = "Desert_AddDunes",
                Label = "add dunes",
                Description = "Adds sandy dunes to some parts of the map.",
                ModifierAction = (w) => WorldGenerator_Desert.AddDunes(w, new()),
            },

            new WorldModifierDef()
            {
                DefName = "Desert_AddShrubs",
                Label = "add desert shrubs",
                Description = "Adds desert shrubs to some parts of the map.",
                ModifierAction = (w) => WorldGenerator_Desert.AddShrubClusters(w, new()),
            },

            new WorldModifierDef()
            {
                DefName = "Desert_AddMesas",
                Label = "add mesas",
                Description = "Adds some mesas (elevated flat mountains) scattered around the map.",
                ModifierAction = (w) => WorldGenerator_Desert.AddMesas(w, new()),
            },
        };
    }
}
