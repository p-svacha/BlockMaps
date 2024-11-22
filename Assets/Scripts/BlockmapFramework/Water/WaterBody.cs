using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    public class WaterBody : WorldDatabaseObject, ISaveAndLoadable
    {
        private World World;

        private int id;
        public override int Id => id;
        /// <summary>
        /// The first y coordinate where nodes are not covered anymore by this water body.
        /// </summary>
        public int ShoreHeight;
        public List<WaterNode> WaterNodes;
        public List<GroundNode> CoveredGroundNodes;

        public int MinX { get; private set; }
        public int MaxX { get; private set; }
        public int MinY { get; private set; }
        public int MaxY { get; private set; }

        public WaterBody() { }
        public WaterBody(int id, int shoreHeight, List<WaterNode> waterNodes, List<GroundNode> coveredNodes)
        {
            this.id = id;
            ShoreHeight = shoreHeight;
            WaterNodes = new List<WaterNode>(waterNodes);
            CoveredGroundNodes = new List<GroundNode>(coveredNodes);

            Init();
        }

        public override void PostLoad()
        {
            Init();
        }

        /// <summary>
        /// Gets called after instancing, either through being spawned or when being loaded.
        /// </summary>
        public void Init()
        {
            // Init references
            for (int i = 0; i < WaterNodes.Count; i++) WaterNodes[i].Init(this, CoveredGroundNodes[i]);

            MinX = WaterNodes.Min(x => x.WorldCoordinates.x);
            MaxX = WaterNodes.Max(x => x.WorldCoordinates.x);
            MinY = WaterNodes.Min(x => x.WorldCoordinates.y);
            MaxY = WaterNodes.Max(x => x.WorldCoordinates.y);
        }

        #region Getters

        public float WaterSurfaceWorldHeight => ((ShoreHeight - 1) * World.NodeHeight) + (World.WATER_HEIGHT * World.NodeHeight);

        #endregion

        #region Save / Load

        public virtual void ExposeDataForSaveAndLoad()
        {
            if (SaveLoadManager.IsLoading) World = SaveLoadManager.LoadingWorld;

            SaveLoadManager.SaveOrLoadPrimitive(ref id, "id");
            SaveLoadManager.SaveOrLoadPrimitive(ref ShoreHeight, "shoreHeight");
            SaveLoadManager.SaveOrLoadReferenceList(ref WaterNodes, "waterNodes");
            SaveLoadManager.SaveOrLoadReferenceList(ref CoveredGroundNodes, "coveredGroundNodes");
        }

        #endregion
    }
}
