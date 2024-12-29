using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag
{
    /// <summary>
    /// Handles all the stats for characters.
    /// </summary>
    public class Comp_CtfCharacter : EntityComp
    {
        private CompProperties_CtfCharacter Props => (CompProperties_CtfCharacter)props;

        private Dictionary<StatDef, Stat> Stats;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);

            Stats = new Dictionary<StatDef, Stat>();

            CreateStat(StatDefOf.RunningSpeed, Props.RunningSpeed);
            CreateStat(StatDefOf.Vision, Props.Vision);
            CreateStat(StatDefOf.MaxStamina, Props.MaxStamina);
            CreateStat(StatDefOf.StaminaRegeneration, Props.StaminaRegeneration);
            CreateStat(StatDefOf.Climbing, Props.ClimbingSpeedModifier);
            CreateStat(StatDefOf.Swimming, Props.SwimmingSpeedModifier);
            CreateStat(StatDefOf.Jumping, Props.Jumping);
            CreateStat(StatDefOf.Dropping, Props.Dropping);
            CreateStat(StatDefOf.Height, Props.Height);
            CreateStat(StatDefOf.CanUseDoors, Props.CanInteractWithDoors ? 1 : 0);
        }

        private void CreateStat(StatDef def, float value)
        {
            Stats[def] = new Stat(def, value);
        }

        #region Getters

        public Sprite Avatar => Props.Avatar;
        public float MaxActionPoints => Props.MaxActionPoints;

        public float GetStat(StatDef def) => Stats[def].GetValue();
        public List<Stat> GetAllStats() => Stats.Values.ToList();

        #endregion
    }
}
