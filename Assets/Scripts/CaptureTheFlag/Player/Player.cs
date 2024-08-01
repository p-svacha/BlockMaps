using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    public class Player
    {
        public CTFGame Game;
        public Actor Actor;
        public Entity Flag;
        public List<Character> Characters;
        public Zone JailZone;
        public Zone FlagZone;
        public Player Opponent;

        public Player(Actor actor, Zone jailZone, Zone flagZone)
        {
            Actor = actor;

            Characters = new List<Character>();
            foreach (Entity e in actor.Entities)
            {
                if (e.TypeId == CTFMapGenerator.FLAG_ID) Flag = e;
                if (e.TryGetComponent(out Character c)) Characters.Add(c);
            }

            JailZone = jailZone;
            FlagZone = flagZone;
        }

        public virtual void OnStartGame(CTFGame game)
        {
            Game = game;
            foreach (Character c in Characters) c.OnStartGame(Game, this, Opponent);
        }

        #region Getters

        public World World => Actor.World;

        #endregion
    }
}
