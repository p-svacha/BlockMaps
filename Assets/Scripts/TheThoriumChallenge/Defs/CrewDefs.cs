using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static BlockmapFramework.Defs.GlobalEntityDefs;

namespace TheThoriumChallenge
{
    public static class CrewDefs
    {
        private static EntityDef CrewMemberBase = new EntityDef()
        {
            EntityClass = typeof(Creature),
            UiSprite = Resources.Load<Sprite>("CaptureTheFlag/Characters/human_avatar"),
            Dimensions = new Vector3Int(1, 3, 1),
            Impassable = false,
            BlocksVision = false,
            WaterBehaviour = WaterBehaviour.HalfBelowWaterSurface,
            RenderProperties = new EntityRenderProperties()
            {
                RenderType = EntityRenderType.StandaloneModel,
                Model = Resources.Load<GameObject>(EntityModelPath + "human/human_fbx"),
                PlayerColorMaterialIndex = 0,
                PositionType = PositionType.CenterPoint,
            },
            Components = new List<CompProperties>()
            {
                new CompProperties_Movement()
            },
        };

        public static List<EntityDef> Defs
        {
            get
            {
                return new List<EntityDef>()
                {
                    new EntityDef(CrewMemberBase)
                    {
                        DefName = "CrewMember",
                        Label = "crew member"
                    },
                };
            }
        }
    }
}
