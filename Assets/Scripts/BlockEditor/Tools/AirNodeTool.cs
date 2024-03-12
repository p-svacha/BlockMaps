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
        private SurfaceId SelectedSurface;

        [Header("Elements")]
        public UI_SelectionPanel SelectionPanel;

        public override void Init(BlockEditor editor)
        {
            base.Init(editor);

            BuildHeight = 5;

            SelectionPanel.Clear();
            foreach (Surface s in SurfaceManager.Instance.GetAllSurfaces())
            {
                SelectionPanel.AddElement(null, s.Color, s.Name, () => SelectSurface(s.Id));
            }
            SelectionPanel.SelectFirstElement();
        }

        public void SelectSurface(SurfaceId surface)
        {
            SelectedSurface = surface;
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

                Color previewColor = Color.white;
                if (!World.CanBuildAirPath(World.HoveredWorldCoordinates, BuildHeight)) previewColor = Color.red;
                if (World.HoveredAirNode != null && World.CanRemoveAirNode(World.HoveredAirNode)) previewColor = new Color(1f, 0.5f, 0f);

                BuildPreview.GetComponentInChildren<MeshRenderer>().material.color = previewColor;
            }
        }

        private void HandleInputs()
        {
            // Ctrl + mouse wheel: change height
            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (Input.mouseScrollDelta.y < 0 && BuildHeight > 1) BuildHeight--;
                if (Input.mouseScrollDelta.y > 0 && BuildHeight < World.MAX_HEIGHT - 1) BuildHeight++;
            }
        }

        public override void HandleLeftClick()
        {
            if (World.HoveredNode != null && World.CanBuildAirPath(World.HoveredWorldCoordinates, BuildHeight))
                World.BuildAirPath(World.HoveredWorldCoordinates, BuildHeight, SelectedSurface);
        }

        public override void HandleRightClick()
        {
            if (World.HoveredAirNode != null && World.CanRemoveAirNode(World.HoveredAirNode)) World.RemoveAirNode(World.HoveredAirNode);
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
    }
}
