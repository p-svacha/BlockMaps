using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Class used to group all information about what an entity sees and explores at once.
    /// </summary>
    public class VisionData
    {
        public Dictionary<BlockmapNode, VisionType> NodeVision;
        public Dictionary<Entity, VisionType> EntityVision;
        public Dictionary<Wall, VisionType> WallVision;

        public VisionData()
        {
            NodeVision = new Dictionary<BlockmapNode, VisionType>();
            EntityVision = new Dictionary<Entity, VisionType>();
            WallVision = new Dictionary<Wall, VisionType>();
        }

        public void AddVisionData(VisionData data)
        {
            foreach(var vis in data.NodeVision)
            {
                if (vis.Value == VisionType.Visible) AddVisibleNode(vis.Key);
                else if (vis.Value == VisionType.FogOfWar) AddExploredNode(vis.Key);
            }
            foreach (var vis in data.EntityVision)
            {
                if (vis.Value == VisionType.Visible) AddVisibleEntity(vis.Key);
            }
            foreach (var vis in data.WallVision)
            {
                if (vis.Value == VisionType.Visible) AddVisibleWall(vis.Key);
                else if (vis.Value == VisionType.FogOfWar) AddExploredWall(vis.Key);
            }
        }

        public void AddVisibleNode(BlockmapNode n)
        {
            NodeVision[n] = VisionType.Visible;
        }
        public void AddVisibleEntity(Entity e)
        {
            EntityVision[e] = VisionType.Visible;
        }
        public void AddVisibleWall(Wall w)
        {
            WallVision[w] = VisionType.Visible;
        }

        public void AddExploredNode(BlockmapNode n)
        {
            if (!NodeVision.ContainsKey(n)) NodeVision[n] = VisionType.FogOfWar;
        }
        public void AddExploredWall(Wall w)
        {
            if (!WallVision.ContainsKey(w)) WallVision[w] = VisionType.FogOfWar;
        }

        public HashSet<BlockmapNode> VisibleNodes => GetVisibleObjects(NodeVision);
        public HashSet<Entity> VisibleEntities => GetVisibleObjects(EntityVision);
        public HashSet<Wall> VisibleWalls => GetVisibleObjects(WallVision);
        private HashSet<T> GetVisibleObjects<T>(Dictionary<T, VisionType> dictionary) => dictionary.Where(x => x.Value == VisionType.Visible).Select(x => x.Key).ToHashSet();

        public HashSet<BlockmapNode> ExploredNodes => GetExploredObjects(NodeVision);
        public HashSet<Wall> ExploredWalls => GetExploredObjects(WallVision);
        private HashSet<T> GetExploredObjects<T>(Dictionary<T, VisionType> dictionary) => dictionary.Where(x => x.Value == VisionType.FogOfWar).Select(x => x.Key).ToHashSet();
    }
}
