using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    public class Player
    {
        public Actor Actor;
        public Entity Flag;
        public List<Character> Characters;
        public Zone JailZone;

        public Player(Actor actor)
        {
            Actor = actor;

            Characters = new List<Character>();
            foreach (Entity e in actor.Entities)
            {
                if (e.TypeId == CTFMapGenerator.FLAG_ID) Flag = e;
                if (e.TryGetComponent(out Character c)) Characters.Add(c);
            }

            // Create jail zone - should be near flag but not too near
            HashSet<Vector2Int> jailZoneCoords = new HashSet<Vector2Int>();
            int flagDistanceX, flagDistanceY;
            do
            {
                flagDistanceX = Random.Range(CTFGame.JAIL_ZONE_MIN_FLAG_DISTANCE, CTFGame.JAIL_ZONE_MAX_FLAG_DISTANCE + 1);
                flagDistanceY = Random.Range(CTFGame.JAIL_ZONE_MIN_FLAG_DISTANCE, CTFGame.JAIL_ZONE_MAX_FLAG_DISTANCE + 1);
                if (Random.value < 0.5f) flagDistanceX *= -1;
                if (Random.value < 0.5f) flagDistanceY *= -1;
            }
            while (Flag.OriginNode.WorldCoordinates.x + flagDistanceX < World.MinX + CTFGame.JAIL_ZONE_RADIUS ||
                Flag.OriginNode.WorldCoordinates.x + flagDistanceX > World.MaxX - CTFGame.JAIL_ZONE_RADIUS - 1 ||
                Flag.OriginNode.WorldCoordinates.y + flagDistanceY < World.MinY + CTFGame.JAIL_ZONE_RADIUS ||
                Flag.OriginNode.WorldCoordinates.y + flagDistanceY > World.MaxY - CTFGame.JAIL_ZONE_RADIUS - 1);

            Vector2Int jailZoneCenter = Flag.OriginNode.WorldCoordinates + new Vector2Int(flagDistanceX, flagDistanceY);
            for(int x = -(CTFGame.JAIL_ZONE_RADIUS - 1); x < CTFGame.JAIL_ZONE_RADIUS; x++)
            {
                for (int y = -(CTFGame.JAIL_ZONE_RADIUS - 1); y < CTFGame.JAIL_ZONE_RADIUS; y++)
                {
                    jailZoneCoords.Add(jailZoneCenter + new Vector2Int(x, y));
                }
            }
            JailZone = new Zone(World, jailZoneCoords);
            JailZone.DrawBorders(true);
        }

        #region Getters

        public World World => Actor.World;

        #endregion
    }
}
