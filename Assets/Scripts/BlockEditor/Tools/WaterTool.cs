using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BlockmapFramework;
using System.Linq;
using TMPro;

namespace WorldEditor
{
    public class WaterTool : EditorTool
    {
        public override EditorToolId Id => EditorToolId.Water;
        public override string Name => "Water";
        public override Sprite Icon => ResourceManager.Singleton.WaterToolSprite;

        private GameObject WaterPreview;
        private Dictionary<SurfaceNode, Dictionary<int, List<SurfaceNode>>> Cache;

        [Header("Elements")]
        public TMP_InputField DepthInput;


        public override void Init(BlockEditor editor)
        {
            base.Init(editor);

            Cache = new Dictionary<SurfaceNode, Dictionary<int, List<SurfaceNode>>>();

            WaterPreview = Instantiate(BlockmapFramework.ResourceManager.Singleton.WaterPlane);
            WaterPreview.gameObject.SetActive(false);
        }

        public override void UpdateTool()
        {
            base.UpdateTool();

            if (World.HoveredSurfaceNode == null) return;

            if (DepthInput.text == "") return;
            int depth = int.Parse(DepthInput.text);
            if (depth < 1 || depth > 3) return;

            List<SurfaceNode> coveredNodes;
            bool canAddWater;

            // Retreive from cache
            if (Cache.ContainsKey(World.HoveredSurfaceNode) && Cache[World.HoveredSurfaceNode].ContainsKey(depth))
            {
                coveredNodes = Cache[World.HoveredSurfaceNode][depth];
                canAddWater = coveredNodes != null;
            }

            // Retreive from calculation
            else
            {
                canAddWater = World.CanAddWater(World.HoveredSurfaceNode, maxDepth: depth, out coveredNodes);
            }


            if(canAddWater)
            {
                //string s = "";
                //foreach (BlockmapNode n in coveredNodes) s += n.WorldCoordinates.ToString() + " | ";
                //Debug.Log("Water on " + coveredNodes.Count + " nodes: " + s);

                // Update Cache
                foreach (SurfaceNode coveredNode in coveredNodes)
                {
                    if (coveredNode.BaseHeight != World.HoveredSurfaceNode.BaseHeight) continue; // only add nodes to cache with same base height
                    if (!Cache.ContainsKey(coveredNode)) Cache.Add(coveredNode, new Dictionary<int, List<SurfaceNode>>());
                    if (!Cache[coveredNode].ContainsKey(depth)) Cache[coveredNode].Add(depth, coveredNodes);
                }
                

                // Preview
                WaterPreview.gameObject.SetActive(true);

                // Get dimensions of water plane
                int minX = coveredNodes.Min(x => x.WorldCoordinates.x);
                int maxX = coveredNodes.Max(x => x.WorldCoordinates.x) + 1;
                int minY = coveredNodes.Min(x => x.WorldCoordinates.y);
                int maxY = coveredNodes.Max(x => x.WorldCoordinates.y) + 1;

                // Position
                float x = (minX + maxX) / 2f;
                float y = (minY + maxY) / 2f;
                float height = World.HoveredSurfaceNode.BaseWorldHeight + (World.WATER_HEIGHT * World.TILE_HEIGHT);
                Vector3 hoveredNodePos = World.HoveredSurfaceNode.GetCenterWorldPosition();
                Vector3 previewPos = new Vector3(x, height, y);
                WaterPreview.transform.position = previewPos;

                // Scale
                float xScale = maxX - minX;
                float yScale = maxY - minY;
                WaterPreview.transform.localScale = new Vector3(xScale * 0.1f, 1f, yScale * 0.1f);
            }

            else
            {
                // Update cache
                if (!Cache.ContainsKey(World.HoveredSurfaceNode)) Cache.Add(World.HoveredSurfaceNode, new Dictionary<int, List<SurfaceNode>>());
                if (!Cache[World.HoveredSurfaceNode].ContainsKey(depth)) Cache[World.HoveredSurfaceNode].Add(depth, null); 

                WaterPreview.gameObject.SetActive(false);
            }
        }

        public override void OnSelect()
        {
            Cache.Clear();
        }

        public override void OnDeselect()
        {
            WaterPreview.gameObject.SetActive(false);
        }
    }
}
