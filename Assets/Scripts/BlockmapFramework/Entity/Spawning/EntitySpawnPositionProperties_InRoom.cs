using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Tries spawning the entity randomly somewhere in the provided room.
    /// </summary>
    public class EntitySpawnPositionProperties_InRoom : EntitySpawnPositionProperties
    {
        private Room Room;

        public EntitySpawnPositionProperties_InRoom(Room room)
        {
            Room = room;
        }

        public override BlockmapNode GetNewTargetNode(EntitySpawnProperties spawnProps)
        {
            if (Room == null) return null;
            return Room.FloorNodes.RandomElement();
        }
    }
}

