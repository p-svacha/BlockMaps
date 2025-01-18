using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static BlockmapFramework.Defs.GlobalEntityDefs;

namespace CaptureTheFlag
{
    public static class ItemDefs
    {
        private const string ItemTexturePath = "CaptureTheFlag/Items/";

        private static EntityDef ItemBaseDef = new EntityDef()
        {
            EntityClass = typeof(CtfItem),
            Dimensions = new Vector3Int(1, 2, 1),
            Impassable = false,
            BlocksVision = false,
            CanBeHeldByOtherEntities = true,
            ExploredBehaviour = ExploredBehaviour.ExploredUntilNotSeenOnLastKnownPosition,
            RenderProperties = new EntityRenderProperties()
            {
                RenderType = EntityRenderType.StandaloneModel,
                Model = Resources.Load<GameObject>("CaptureTheFlag/Models/item_frame_fbx"),
                PositionType = PositionType.HighestPoint,
            }
        };

        public static List<EntityDef> Defs = new List<EntityDef>()
        {
            new EntityDef(ItemBaseDef)
            {
                DefName = "CtfItem_Apple",
                Label = "apple",
                Description = "Restores 20 stamina upon consumption",
                UiSprite = HelperFunctions.TextureToSprite(Resources.Load<Texture>(ItemTexturePath + "Apple")),
                Components = new List<CompProperties>()
                {
                    new CompProperties_CtfItem()
                    {
                        CompClass = typeof(Comp_CtfItem_Apple),
                        ItemTexture = Resources.Load<Texture>(ItemTexturePath + "Apple"),
                    }
                }
            }
        };
    }
}
