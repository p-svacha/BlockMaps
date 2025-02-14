using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static BlockmapFramework.Defs.GlobalEntityDefs;

namespace CaptureTheFlag
{
    public static class EntityDefs
    {
        public static List<EntityDef> GetCharacterDefs()
        {
            EntityDef HumanBase = new EntityDef()
            {
                EntityClass = typeof(CtfCharacter),
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
                    new CompProperties_Skills()
                    {
                        InitialSkillLevels = new Dictionary<SkillDef, int>()
                        {
                            { SkillDefOf.Running, 10 },
                            { SkillDefOf.Vision, 8 },
                            { SkillDefOf.Endurance, 14 },
                            { SkillDefOf.Regeneration, 10 },
                            { SkillDefOf.Climbing, 6 },
                            { SkillDefOf.Swimming, 6 },
                            { SkillDefOf.Vaulting, 8 },
                        },
                    },
                    new CompProperties_Movement()
                    {
                        ClimbingSkill = ClimbingCategory.Intermediate,
                    },
                    new CompProperties_Stats() { },
                    new CompProperties_CtfCharacter()
                    {
                        Avatar = Resources.Load<Sprite>("CaptureTheFlag/Characters/human_avatar"),
                        CanUseDoors = true,
                    },
                },
            };

            EntityDef DogBase = new EntityDef()
            {
                EntityClass = typeof(CtfCharacter),
                UiSprite = Resources.Load<Sprite>("CaptureTheFlag/Characters/dog_avatar"),
                Impassable = false,
                BlocksVision = false,
                WaterBehaviour = WaterBehaviour.HalfBelowWaterSurface,
                RenderProperties = new EntityRenderProperties()
                {
                    RenderType = EntityRenderType.StandaloneModel,
                    Model = Resources.Load<GameObject>(EntityModelPath + "dog/dog_2_fbx"),
                    PlayerColorMaterialIndex = 1,
                    PositionType = PositionType.CenterPoint,
                },
                Components = new List<CompProperties>()
                {
                    new CompProperties_Skills()
                    {
                        InitialSkillLevels = new Dictionary<SkillDef, int>()
                        {
                            { SkillDefOf.Running, 15 },
                            { SkillDefOf.Vision, 5 },
                            { SkillDefOf.Endurance, 14 },
                            { SkillDefOf.Regeneration, 8 },
                            { SkillDefOf.Climbing, 0 },
                            { SkillDefOf.Swimming, 12 },
                            { SkillDefOf.Vaulting, 5 },
                        },
                    },
                    new CompProperties_Movement() { },
                    new CompProperties_Stats() { },
                    new CompProperties_CtfCharacter()
                    {
                        Avatar = Resources.Load<Sprite>("CaptureTheFlag/Characters/dog_avatar"),
                        CanUseDoors = false,
                    },
                },
            };

            EntityDef alberto = new EntityDef(HumanBase)
            {
                DefName = "Human1",
                Label = "alberto",
                Description = "Very good and fast climber."
            };
            alberto.GetCompProperties<CompProperties_Skills>().InitialSkillLevels[SkillDefOf.Climbing] = 20;

            EntityDef usain = new EntityDef(HumanBase)
            {
                DefName = "Human2",
                Label = "usain",
                Description = "Tall guy who is extremely fast but low stamina, meaning he needs to rest often.",
                Dimensions = new Vector3Int(1, 4, 1),
            };
            usain.RenderProperties.ModelScale = new Vector3(1f, 1.3f, 1f);
            usain.GetCompProperties<CompProperties_Skills>().InitialSkillLevels[SkillDefOf.Running] = 20;
            usain.GetCompProperties<CompProperties_Skills>().InitialSkillLevels[SkillDefOf.Endurance] = 10;

            EntityDef eluid = new EntityDef(HumanBase)
            {
                DefName = "Human3",
                Label = "eluid",
                Description = "Quite fast and very high stamina and regeneration, meaning he will almost never need rest."
            };
            eluid.GetCompProperties<CompProperties_Skills>().InitialSkillLevels[SkillDefOf.Running] = 12;
            eluid.GetCompProperties<CompProperties_Skills>().InitialSkillLevels[SkillDefOf.Endurance] = 20;
            eluid.GetCompProperties<CompProperties_Skills>().InitialSkillLevels[SkillDefOf.Regeneration] = 20;

            EntityDef veronica = new EntityDef(HumanBase)
            {
                DefName = "Human4",
                Label = "veronica",
                Description = "Extremely good vision, making her a great scout."
            };
            veronica.GetCompProperties<CompProperties_Skills>().InitialSkillLevels[SkillDefOf.Vision] = 14;

            EntityDef katie = new EntityDef(HumanBase)
            {
                DefName = "Human5",
                Label = "katie",
                Description = "Very fast swimmer with above-average stamina."
            };
            katie.GetCompProperties<CompProperties_Skills>().InitialSkillLevels[SkillDefOf.Endurance] = 16;
            katie.GetCompProperties<CompProperties_Skills>().InitialSkillLevels[SkillDefOf.Swimming] = 20;

            EntityDef yaroslava = new EntityDef(HumanBase)
            {
                DefName = "Human6",
                Label = "yaroslava",
                Description = "Very skilled at jumping over high obstacles and onto high platforms. She can also absorb higher drops."
            };
            yaroslava.GetCompProperties<CompProperties_Skills>().InitialSkillLevels[SkillDefOf.Vaulting] = 20;

            EntityDef blotto = new EntityDef(DogBase)
            {
                DefName = "Dog1",
                Label = "chevap",
                Description = "Bigger dog that can run fast, but low vision and needs to rest for longer periods of time.",
                Dimensions = new Vector3Int(1, 2, 1),
            };
            blotto.RenderProperties.ModelScale = new Vector3(1f, 1.8f, 1f);
            blotto.GetCompProperties<CompProperties_Skills>().InitialSkillLevels[SkillDefOf.Vaulting] = 10;
            blotto.GetCompProperties<CompProperties_Skills>().InitialSkillLevels[SkillDefOf.Regeneration] = 6;

            EntityDef pierette = new EntityDef(DogBase)
            {
                DefName = "Dog2",
                Label = "cici",
                Description = "Smaller dog with high stamina, but once that's gone she will need to rest long."
            };
            pierette.GetCompProperties<CompProperties_Skills>().InitialSkillLevels[SkillDefOf.Endurance] = 18;
            pierette.GetCompProperties<CompProperties_Skills>().InitialSkillLevels[SkillDefOf.Running] = 12;

            return new List<EntityDef>()
            {
                alberto,
                usain,
                eluid,
                veronica,
                katie,
                yaroslava,
                blotto,
                pierette
            };
        }

        public static List<EntityDef> GetObjectDefs()
        {
            return new List<EntityDef>() {
                new EntityDef()
                {
                    DefName = "Flag",
                    Label = "flag",
                    Description = "The flag to protect or capture.",
                    UiSprite = HelperFunctions.GetAssetPreviewSprite(EntityModelPath + "flag/flag_fbx"),
                    EntityClass = typeof(Entity),
                    Dimensions = new Vector3Int(1, 2, 1),
                    Impassable = false,
                    BlocksVision = false,
                    WaterBehaviour = WaterBehaviour.AboveWater,
                    RenderProperties = new EntityRenderProperties()
                    {
                        RenderType = EntityRenderType.StandaloneModel,
                        Model = Resources.Load<GameObject>(EntityModelPath + "flag/flag_fbx"),
                        PlayerColorMaterialIndex = 1,
                    },
                },
            };
        }
    }
}
