using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// An actor inside a BlockMap world.
    /// </summary>
    public class Actor
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public World World { get; private set; }
        public Color Color { get; private set; }

        public List<Entity> Entities { get; private set; }

        public Actor(World world, int id, string name, Color color)
        {
            World = world;
            Id = id;
            Name = name;
            Color = color;

            Entities = new List<Entity>();
        }

        #region Save / Load

        public static Actor Load(World world, ActorData data)
        {
            return new Actor(world, data.Id, data.Name, new Color(data.ColorR, data.ColorG, data.ColorB));
        }

        public ActorData Save()
        {
            return new ActorData
            {
                Id = Id,
                Name = Name,
                ColorR = Color.r,
                ColorG = Color.g,
                ColorB = Color.b
            };
        }

        #endregion
    }
}
