using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class LadderEntity : Entity
    {
        public Ladder Ladder { get; private set; }

        public void Init(Ladder ladder)
        {
            Ladder = ladder;

            // Attributes
            Name = "ladder";
            Dimensions = new Vector3Int(1, ladder.Height, 1);
            Rotation = ladder.Side;
            BlocksVision = false;
            IsPassable = true;
        }

        public override int MinHeight => Ladder.MinHeight;
        public override int MaxHeight => Ladder.MaxHeight;

        public override Vector3 GetWorldPosition(World world, BlockmapNode originNode)
        {
            Vector3 nodeCenter = originNode.GetCenterWorldPosition();
            float worldHeight = Ladder.MinHeight * World.TILE_HEIGHT;
            return new Vector3(nodeCenter.x, worldHeight, nodeCenter.z);
        }

        public override void UpdateEntity() { }
    }
}
