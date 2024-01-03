using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BlockmapFramework;

namespace WorldEditor
{
    public class AirNodeTool : EditorTool
    {
        public override EditorToolId Id => EditorToolId.AirNode;
        public override string Name => "Build Air Node";
        public override Sprite Icon => ResourceManager.Singleton.AirNodeSprite;

        private GameObject BuildPreview;
        private int BuildHeight;

        public override void Init(BlockEditor editor)
        {
            base.Init(editor);

            BuildHeight = 5;
        }

        public override void UpdateTool()
        {
            UpdatePreview();
            HandleInputs();
        }

        private void UpdatePreview()
        {
            if (World.HoveredNode != null)
            {
                Vector3 hoverPos = World.HoveredNode.GetCenterWorldPosition();
                BuildPreview.transform.position = new Vector3(hoverPos.x, World.TILE_HEIGHT * BuildHeight + World.TILE_HEIGHT * 0.1f, hoverPos.z);
                BuildPreview.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                BuildPreview.GetComponentInChildren<MeshRenderer>().material.color = World.CanBuildAirPath(World.HoveredWorldCoordinates, BuildHeight) ? Color.white : Color.red;
            }
        }

        private void HandleInputs()
        {
            // Change height
            if (Input.GetKeyDown(KeyCode.R)) SetHeight(BuildHeight + 1);
            if (Input.GetKeyDown(KeyCode.F)) SetHeight(BuildHeight - 1);
        }

        public override void HandleLeftClick()
        {
            if (World.HoveredNode != null && World.CanBuildAirPath(World.HoveredWorldCoordinates, BuildHeight))
                World.BuildAirPath(World.HoveredWorldCoordinates, BuildHeight);
        }

        public override void OnSelect()
        {
            BuildPreview = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject.Destroy(BuildPreview.GetComponent<BoxCollider>());
            BuildPreview.transform.localScale = new Vector3(1f, World.TILE_HEIGHT * 0.1f, 1f);
        }
        public override void OnDeselect()
        {
            GameObject.Destroy(BuildPreview);
            BuildPreview = null;
        }

        private void SetHeight(int value)
        {
            BuildHeight = value;
            BuildHeight = Mathf.Clamp(BuildHeight, 0, World.MAX_HEIGHT);
        }
    }
}
