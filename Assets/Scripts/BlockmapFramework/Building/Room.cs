using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// A room is "nothing" by itself, but simply represents a collection of nodes and walls for easy access.
    /// </summary>
    public class Room : WorldDatabaseObject, ISaveAndLoadable
    {
        private int id;
        public override int Id => id;

        public World World;
        public string Label;
        public string LabelCap => Label.CapitalizeFirst();
        public List<BlockmapNode> FloorNodes;
        public List<Wall> InteriorWalls;

        public Room() { } // Empty constructor used when loading
        public Room(int id, string label, List<BlockmapNode> nodes, List<Wall> interiorWalls)
        {
            this.id = id;
            Label = label;
            FloorNodes = nodes;
            InteriorWalls = interiorWalls;

            Init();
        }

        /// <summary>
        /// Gets called after instancing, either through being spawned or when being loaded.
        /// </summary>
        private void Init()
        {
            // Set references in all nodes and walls of this room
            foreach (BlockmapNode node in FloorNodes) node.SetRoom(this);
            foreach (Wall wall in InteriorWalls) wall.SetInteriorRoom(this);
        }

        #region Save / Load

        public virtual void ExposeDataForSaveAndLoad()
        {
            if (SaveLoadManager.IsLoading) World = SaveLoadManager.LoadingWorld;

            SaveLoadManager.SaveOrLoadPrimitive(ref id, "id");
            SaveLoadManager.SaveOrLoadPrimitive(ref Label, "label");
            SaveLoadManager.SaveOrLoadReferenceList(ref FloorNodes, "floorNodes");
            SaveLoadManager.SaveOrLoadReferenceList(ref InteriorWalls, "interiorWalls");
        }
        public override void PostLoad()
        {
            Init();
        }

        #endregion
    }
}
