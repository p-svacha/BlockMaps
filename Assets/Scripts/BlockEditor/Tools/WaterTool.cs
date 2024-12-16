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
        private const int MAX_DEPTH = 3;

        public override EditorToolId Id => EditorToolId.Water;
        public override string Name => "Water";
        public override Sprite Icon => ResourceManager.Singleton.WaterToolSprite;

        private WaterBody CurrentWaterBody;
        private GameObject WaterPreview;
        private Dictionary<GroundNode, Dictionary<int, WaterBody>> Cache;

        [Header("Elements")]
        public TMP_InputField DepthInput;


        public override void Init(BlockEditor editor)
        {
            base.Init(editor);

            Cache = new Dictionary<GroundNode, Dictionary<int, WaterBody>>();

            WaterPreview = new GameObject("Water preview");
            WaterPreview.gameObject.SetActive(false);
        }

        public override void UpdateTool()
        {
            base.UpdateTool();

            if (World.HoveredGroundNode == null) return;

            if (DepthInput.text == "") return;
            int depth = int.Parse(DepthInput.text);
            if (depth < 1 || depth > MAX_DEPTH) return;

            // Retreive from cache
            if (Cache.ContainsKey(World.HoveredGroundNode) && Cache[World.HoveredGroundNode].ContainsKey(depth))
                CurrentWaterBody = Cache[World.HoveredGroundNode][depth];

            // Retreive from calculation
            else
                CurrentWaterBody = World.CanAddWater(World.HoveredGroundNode, maxDepth: depth);

            if (CurrentWaterBody != null) // can add water
            {
                //string s = "";
                //foreach (BlockmapNode n in coveredNodes) s += n.WorldCoordinates.ToString() + " | ";
                //Debug.Log("Water on " + coveredNodes.Count + " nodes: " + s);

                // Update Cache
                foreach (GroundNode coveredNode in CurrentWaterBody.CoveredGroundNodes)
                {
                    if (coveredNode.BaseAltitude != World.HoveredGroundNode.BaseAltitude) continue; // only add nodes to cache with same base height
                    if (!Cache.ContainsKey(coveredNode)) Cache.Add(coveredNode, new Dictionary<int, WaterBody>());
                    if (!Cache[coveredNode].ContainsKey(depth)) Cache[coveredNode].Add(depth, CurrentWaterBody);
                }


                // Preview
                MeshBuilder waterMeshBuilder = new MeshBuilder(WaterPreview);
                WaterMeshGenerator.BuildFullWaterMesh(waterMeshBuilder, CurrentWaterBody);
                waterMeshBuilder.ApplyMesh();

                WaterPreview.gameObject.SetActive(true);
            }

            else // cannot add water
            {
                // Update cache
                if (!Cache.ContainsKey(World.HoveredGroundNode)) Cache.Add(World.HoveredGroundNode, new Dictionary<int, WaterBody>());
                if (!Cache[World.HoveredGroundNode].ContainsKey(depth)) Cache[World.HoveredGroundNode].Add(depth, null); 

                WaterPreview.gameObject.SetActive(false);
            }
        }

        public override void HandleKeyboardInputs()
        {
            // Ctrl + mouse wheel: change depth
            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (Input.mouseScrollDelta.y < 0 && DepthInput.text != "")
                {
                    int depthText = int.Parse(DepthInput.text);
                    if (depthText > 1) depthText--;
                    DepthInput.text = depthText.ToString();
                }
                if (Input.mouseScrollDelta.y > 0 && DepthInput.text != "")
                {
                    int depthText = int.Parse(DepthInput.text);
                    if (depthText < MAX_DEPTH) depthText++;
                    DepthInput.text = depthText.ToString();
                }
            }
        }

        public override void HandleLeftClick()
        {
            if (CurrentWaterBody == null) return;

            World.AddWaterBody(CurrentWaterBody, updateWorld: true);

            Cache.Clear();
        }

        public override void HandleRightClick()
        {
            if (World.HoveredWaterBody == null) return;

            World.RemoveWaterBody(World.HoveredWaterBody, updateWorld: true);

            Cache.Clear();
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
