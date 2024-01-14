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
        /// How many tiles up the ladder goes.
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// GameObject of the ladder.
        /// </summary>
        public LadderReference LadderObject { get; private set; }

        public Ladder(BlockmapNode source, BlockmapNode target, Direction side)
        {
            Bottom = source;
            Top = target;
            Side = side;
            Height = target.GetMaxHeight(HelperFunctions.GetOppositeDirection(side)) - source.GetMinHeight(side);
        }
        public Ladder(Ladder source) // copy constructor
        {
            Bottom = source.Bottom;
            Top = source.Top;
            Side = source.Side;
            Height = source.Height;
        }

        /// <summary>
        /// Register in world.
        /// </summary>
        public void Init()
        {
            World.Ladders.Add(this);
            Bottom.Ladders.Add(Side, this);
            LadderObject = LadderMeshGenerator.GenerateLadderObject(World, this);
        }
    }
}
