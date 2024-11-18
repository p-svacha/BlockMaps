using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BlockmapFramework;

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
                RenderProperties = new EntityRenderProperties()
                {
                    RenderType = EntityRenderType.StandaloneModel,
                    Model = Resources.Load<Mesh>("BlenderImport/character/character_fbx"),
                },
            }
        };

    }
}
