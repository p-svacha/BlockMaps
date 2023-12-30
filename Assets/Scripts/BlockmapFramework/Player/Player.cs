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
        public World World { get; private set; }

        public Player(World world, int id, string name)
        {
            World = world;
            Id = id;
            Name = name;
        }

        #region Save / Load

        public static Player Load(World world, PlayerData data)
        {
            return new Player(world, data.Id, data.Name);
        }

        public PlayerData Save()
        {
            return new PlayerData
            {
                Id = Id,
                Name = Name
            };
        }

        #endregion
    }
}
