using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class Ladder
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

        public Ladder(BlockmapNode source, BlockmapNode target, Direction side)
        {
            Bottom = source;
            Top = target;
            Side = side;
            MaxHeight = target.GetMaxHeight(HelperFunctions.GetOppositeDirection(side));
            MinHeight = source.GetMinHeight(side);
            Height = MaxHeight - MinHeight;
        }
        public Ladder(Ladder source) // copy constructor
        {
            Bottom = source.Bottom;
            Top = source.Top;
            Side = source.Side;
            MaxHeight = source.MaxHeight;
            MinHeight = source.MinHeight;
            Height = source.Height;
        }

        /// <summary>
        /// Register in world.
        /// </summary>
        public void Init()
        {
            Bottom.Ladders.Add(Side, this);
            Entity = LadderMeshGenerator.GenerateLadderObject(World, this);
            World.SpawnEntity(Entity, Bottom, World.Gaia);
        }
    }
}
