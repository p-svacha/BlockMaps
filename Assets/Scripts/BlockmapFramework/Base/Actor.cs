using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// An actor inside a BlockMap world.
    /// </summary>
    public class Actor : WorldDatabaseObject, ISaveAndLoadable
    {
        private int id;
        public override int Id => id;

        public string Label;
        public World World { get; private set; }
        public Color Color;

        public List<Entity> Entities { get; private set; }
        public List<Entity> GhostMarkers { get; private set; }

        public Actor() { }
        public Actor(World world, int id, string name, Color color)
        {
            World = world;
            this.id = id;
            Label = name;
            Color = color;

            Init();
        }

        public override void PostLoad()
        {
            Init();
        }

        private void Init()
        {
            Entities = new List<Entity>();
            GhostMarkers = new List<Entity>();
        }

        #region Save / Load

        public void ExposeDataForSaveAndLoad()
        {
            if (SaveLoadManager.IsLoading) World = SaveLoadManager.LoadingWorld;

            SaveLoadManager.SaveOrLoadPrimitive(ref id, "id");
            SaveLoadManager.SaveOrLoadPrimitive(ref Label, "name");
            SaveLoadManager.SaveOrLoadColor(ref Color, "color");
        }

        #endregion
    }
}
