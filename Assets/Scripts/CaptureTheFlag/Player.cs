using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    public class Player
    {
        public Actor Actor;
        public List<Character> Characters;

        public Player(Actor actor)
        {
            Actor = actor;

            Characters = new List<Character>();
            foreach(Entity e in actor.Entities)
            {
                if (e.TryGetComponent(out Character c)) Characters.Add(c);
            }
        }
    }
}
