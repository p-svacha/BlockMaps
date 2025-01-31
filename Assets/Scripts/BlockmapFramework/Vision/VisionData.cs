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
        public Dictionary<BlockmapNode, VisibilityType> NodeVision;
        public Dictionary<Entity, VisibilityType> EntityVision;
        public Dictionary<Wall, VisibilityType> WallVision;

        public VisionData()
        {
            NodeVision = new Dictionary<BlockmapNode, VisibilityType>();
            EntityVision = new Dictionary<Entity, VisibilityType>();
            WallVision = new Dictionary<Wall, VisibilityType>();
        }

        public void AddVisionData(VisionData data)
        {
            foreach(var vis in data.NodeVision)
            {
                if (vis.Value == VisibilityType.Visible) AddVisibleNode(vis.Key);
                else if (vis.Value == VisibilityType.FogOfWar) AddExploredNode(vis.Key);
            }
            foreach (var vis in data.EntityVision)
            {
                if (vis.Value == VisibilityType.Visible) AddVisibleEntity(vis.Key);
            }
            foreach (var vis in data.WallVision)
            {
                if (vis.Value == VisibilityType.Visible) AddVisibleWall(vis.Key);
                else if (vis.Value == VisibilityType.FogOfWar) AddExploredWall(vis.Key);
            }
        }

        public void AddVisibleNode(BlockmapNode n)
        {
            NodeVision[n] = VisibilityType.Visible;
        }
        public void AddVisibleEntity(Entity e)
        {
            EntityVision[e] = VisibilityType.Visible;
        }
        public void AddVisibleWall(Wall w)
        {
            WallVision[w] = VisibilityType.Visible;
        }

        public void AddExploredNode(BlockmapNode n)
        {
            if (n == null) return;
            if (!NodeVision.ContainsKey(n)) NodeVision[n] = VisibilityType.FogOfWar;
        }
        public void AddExploredWall(Wall w)
        {
            if (!WallVision.ContainsKey(w)) WallVision[w] = VisibilityType.FogOfWar;
        }

        public HashSet<BlockmapNode> VisibleNodes => GetVisibleObjects(NodeVision);
        public HashSet<Entity> VisibleEntities => GetVisibleObjects(EntityVision);
        public HashSet<Wall> VisibleWalls => GetVisibleObjects(WallVision);
        private HashSet<T> GetVisibleObjects<T>(Dictionary<T, VisibilityType> dictionary) => dictionary.Where(x => x.Value == VisibilityType.Visible).Select(x => x.Key).ToHashSet();

        public HashSet<BlockmapNode> ExploredNodes => GetExploredObjects(NodeVision);
        public HashSet<Wall> ExploredWalls => GetExploredObjects(WallVision);
        private HashSet<T> GetExploredObjects<T>(Dictionary<T, VisibilityType> dictionary) => dictionary.Where(x => x.Value == VisibilityType.FogOfWar).Select(x => x.Key).ToHashSet();

        public override string ToString()
        {
            return $"{NodeVision.Where(x => x.Value == VisibilityType.Visible).Count()} nodes are visible. \n{NodeVision.Where(x => x.Value == VisibilityType.FogOfWar).Count()} nodes are explored.\n{EntityVision.Where(x => x.Value == VisibilityType.Visible).Count()} entities are visible. \n{EntityVision.Where(x => x.Value == VisibilityType.FogOfWar).Count()} entities are explored.\n{WallVision.Where(x => x.Value == VisibilityType.Visible).Count()} walls are visible. \n{WallVision.Where(x => x.Value == VisibilityType.FogOfWar).Count()} walls are explored.";
        }
    }
}
