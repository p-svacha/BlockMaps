using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class LadderEntity : Entity
    {
        public const string LADDER_ENTITY_NAME = "ladder";
        public Ladder Ladder { get; private set; }

        public void Init(Ladder ladder)
        {
            Ladder = ladder;

            // Attributes
            Name = "Ladder";
            TypeId = LADDER_ENTITY_NAME + "_" + ladder.Top.Id;
            Dimensions = new Vector3Int(1, ladder.Height, 1);
            BlocksVision = false;
            IsPassable = true;
        }

        public override int MinHeight => Ladder.MinHeight;
        public override int MaxHeight => Ladder.MaxHeight;

        public override Vector3 GetWorldPosition(World world, BlockmapNode originNode, Direction rotation)
        {
            Vector3 nodeCenter = originNode.GetCenterWorldPosition();
            float worldHeight = Ladder.MinHeight * World.TILE_HEIGHT;
            return new Vector3(nodeCenter.x, worldHeight, nodeCenter.z);
        }

        public override void UpdateEntity() { }
    }
}
