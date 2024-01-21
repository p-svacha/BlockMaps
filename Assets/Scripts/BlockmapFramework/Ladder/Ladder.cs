using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class Ladder : IClimbable
    {
        public World World => Bottom.World;

        /// <summary>
        /// Node that the bottom of the ladder is standing on
        /// </summary>
        public BlockmapNode Bottom { get; private set; }
        /// <summary>
        /// Node that the top of the ladder leads to. Always adjacent to Source.
        /// </summary>
        public BlockmapNode Top { get; private set; }

        /// <summary>
        /// The side that the ladder stands on on Source. [N/E/S/W]
        /// </summary>
        public Direction Side { get; private set; }

        /// <summary>
        /// Height at which the ladder starts.
        /// </summary>
        public int MinHeight { get; private set; }

        /// <summary>
        /// Height at which the ladder ends.
        /// </summary>
        public int MaxHeight { get; private set; }

        /// <summary>
        /// How many tiles up the ladder goes.
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// GameObject of the ladder.
        /// </summary>
        public LadderEntity Entity { get; private set; }

        // IClimbable
        public ClimbingCategory SkillRequirement => ClimbingCategory.Basic;
        public int MaxClimbHeight(ClimbingCategory skill) => World.MAX_HEIGHT;
        public float CostUp => 1.6f;
        public float CostDown => 1.3f;
        public float SpeedUp => 0.65f;
        public float SpeedDown => 0.75f;
        public float TransformOffset => LadderMeshGenerator.LADDER_POLE_SIZE;
        public Direction ClimbSide => Side;

        public Ladder(BlockmapNode source, BlockmapNode target, Direction side)
        {
            Bottom = source;
            Top = target;
            Side = side;
            MaxHeight = target.GetMaxHeight(HelperFunctions.GetOppositeDirection(side));
            MinHeight = source.GetMinHeight(side);
            Height = MaxHeight - MinHeight;
        }

        /// <summary>
        /// Register in world.
        /// </summary>
        public void Init()
        {
            Bottom.SourceLadders.Add(Side, this);
            Top.TargetLadders.Add(HelperFunctions.GetOppositeDirection(Side), this);
            Entity = LadderMeshGenerator.GenerateLadderObject(this);
            World.SpawnEntity(Entity, Bottom, Side, World.Gaia);
        }
    }
}
