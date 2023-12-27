using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// An actor inside a BlockMap world.
    /// </summary>
    public class Player
    {
        public int Id { get; private set; }
        public string Name { get; private set; }

        public Player(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
