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
        private Vector2Int HoveredCoordinates;

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
            UpdateHoveredCoordinates();
            UpdatePreview();
            HandleInputs();
        }

        // Sets the current coordinates from intersecting mouse hover with an invisible plane on the build height
        private void UpdateHoveredCoordinates()
        {
            var plane = new Plane(Vector3.up, new Vector3(0f, BuildHeight * World.TILE_HEIGHT, 0f));
            var ray = World.Camera.Camera.ScreenPointToRay(Input.mousePosition);
            if (plane.Raycast(ray, out float distance))
            {
                Vector3 hitPoint = ray.GetPoint(distance);
                HoveredCoordinates = World.GetWorldCoordinates(hitPoint);
            }
        }

        private void UpdatePreview()
        {
            BuildPreview.transform.position = new Vector3(HoveredCoordinates.x + 0.5f, World.TILE_HEIGHT * BuildHeight + World.TILE_HEIGHT * 0.1f, HoveredCoordinates.y + 0.5f);
            BuildPreview.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

            Color previewColor = Color.white;
            if (!World.CanBuildAirPath(HoveredCoordinates, BuildHeight)) previewColor = Color.red;

            BuildPreview.GetComponentInChildren<MeshRenderer>().material.color = previewColor;
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
            if (World.CanBuildAirPath(HoveredCoordinates, BuildHeight))
                World.BuildAirPath(HoveredCoordinates, BuildHeight, SelectedSurface);
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
