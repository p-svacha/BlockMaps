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
        private int id;
        public int Id => id;

        public string Name;
        public World World { get; private set; }
        public Color Color;

        public List<Entity> Entities { get; private set; }

        public Actor() { }
        public Actor(World world, int id, string name, Color color)
        {
            World = world;
            this.id = id;
            Name = name;
            Color = color;

            Init();
        }

        public void PostLoad()
        {
            Init();
        }

        private void Init()
        {
            Entities = new List<Entity>();
        }

        #region Save / Load

        public void ExposeDataForSaveAndLoad()
        {
            if (SaveLoadManager.IsLoading) World = SaveLoadManager.LoadingWorld;

            SaveLoadManager.SaveOrLoadPrimitive(ref id, "id");
            SaveLoadManager.SaveOrLoadPrimitive(ref Name, "name");
            SaveLoadManager.SaveOrLoadColor(ref Color, "color");
        }

        #endregion
    }
}
