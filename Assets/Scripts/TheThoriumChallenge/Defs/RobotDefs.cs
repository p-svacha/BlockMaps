using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static BlockmapFramework.Defs.GlobalEntityDefs;

namespace TheThoriumChallenge
{
    public static class RobotDefs
    {
        public static List<EntityDef> Defs
        {
            get
            {
                return new List<EntityDef>()
                {
                    new EntityDef()
                    {
                        EntityClass = typeof(Creature),
                        DefName = "Drone",
                        Label = "drone",
                        UiSprite = Resources.Load<Sprite>("ExodusOutpostAlpha/EntityPreviewImages/Drone"),
                        Dimensions = new Vector3Int(1, 2, 1),
                        Impassable = false,
                        BlocksVision = false,
                        ExploredBehaviour = ExploredBehaviour.None,
                        RenderProperties = new EntityRenderProperties()
                        {
                            RenderType = EntityRenderType.StandaloneModel,
                            Model = Resources.Load<GameObject>(EntityModelPath + "robots/drone_01_fbx"),
                        },
                        Components = new List<CompProperties>()
                        {
                            new CompProperties_Movement(),
                        }
                    }
                };
            }
        }
    }
}
