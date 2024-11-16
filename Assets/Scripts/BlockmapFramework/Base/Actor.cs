using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// An actor inside a BlockMap world.
    /// </summary>
    public class Actor : ISaveAndLoadable
    {
        public int Id;
        public string Name;
        public World World { get; private set; }
        public Color Color;

        public List<Entity> Entities { get; private set; }

        public Actor() { }
        public Actor(World world, int id, string name, Color color)
        {
            OnCreateOrLoad(world);

            Id = id;
            Name = name;
            Color = color;
        }

        public void OnCreateOrLoad(World world)
        {
            World = world;
            Entities = new List<Entity>();
        }

        #region Save / Load

        public void ExposeDataForSaveAndLoad()
        {
            SaveLoadManager.SaveOrLoadInt(ref Id, "id");
            SaveLoadManager.SaveOrLoadString(ref Name, "name");
            SaveLoadManager.SaveOrLoadColor(ref Color, "color");
        }

        #endregion
    }
}
