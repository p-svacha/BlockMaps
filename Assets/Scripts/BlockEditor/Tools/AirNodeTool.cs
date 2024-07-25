using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BlockmapFramework;
using TMPro;
using UnityEngine.UI;

namespace WorldEditor
{
    public class AirNodeTool : EditorTool
    {
        public override EditorToolId Id => EditorToolId.AirNode;
        public override string Name => "Build Air Node";
        public override Sprite Icon => ResourceManager.Singleton.AirNodeSprite;

        private GameObject BuildPreview;
        private SurfaceId SelectedSurface;
        private Vector2Int HoveredCoordinates;
        private int BuildAltitude => int.Parse(AltitudeInput.text);

        [Header("Elements")]
        public UI_SelectionPanel SelectionPanel;
        public TMP_InputField AltitudeInput;
        public Toggle HelperGridToggle;

        public override void Init(BlockEditor editor)
        {
            base.Init(editor);

            SetAltitude(5);

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
        }

        private void UpdateHoveredCoordinates()
        {
            HoveredCoordinates = World.GetHoveredCoordinates(BuildAltitude);
        }

        private void UpdatePreview()
        {
            BuildPreview.transform.position = new Vector3(HoveredCoordinates.x + 0.5f, World.TILE_HEIGHT * BuildAltitude + World.TILE_HEIGHT * 0.1f, HoveredCoordinates.y + 0.5f);
            BuildPreview.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

            Color previewColor = Color.white;
            if (!World.CanBuildAirPath(HoveredCoordinates, BuildAltitude)) previewColor = Color.red;

            BuildPreview.GetComponentInChildren<MeshRenderer>().material.color = previewColor;
        }

        public override void HandleKeyboardInputs()
        {
            // Ctrl + mouse wheel: change altitude
            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (Input.mouseScrollDelta.y < 0) SetAltitude(BuildAltitude - 1);
                if (Input.mouseScrollDelta.y > 0) SetAltitude(BuildAltitude + 1);
            }

            // H: Toggle helper grid
            if (Input.GetKeyDown(KeyCode.H)) ShowHelperGrid(!HelperGridToggle.isOn);
        }

        private void SetAltitude(int altitude)
        {
            if (altitude < 0) altitude = 0;
            if (altitude > World.MAX_ALTITUDE) altitude = World.MAX_ALTITUDE;
            AltitudeInput.text = altitude.ToString();
            Editor.AltitudeHelperPlane.transform.position = new Vector3(Editor.AltitudeHelperPlane.transform.position.x, BuildAltitude * World.TILE_HEIGHT, Editor.AltitudeHelperPlane.transform.position.z);
        }

        private void ShowHelperGrid(bool show)
        {
            Editor.AltitudeHelperPlane.SetActive(show);
            HelperGridToggle.isOn = show;
        }

        public override void HandleLeftClick()
        {
            if (World.CanBuildAirPath(HoveredCoordinates, BuildAltitude))
                World.BuildAirPath(HoveredCoordinates, BuildAltitude, SelectedSurface);
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

            ShowHelperGrid(HelperGridToggle.isOn);
            SetAltitude(int.Parse(AltitudeInput.text));
        }
        public override void OnDeselect()
        {
            GameObject.Destroy(BuildPreview);
            BuildPreview = null;

            Editor.AltitudeHelperPlane.SetActive(false);
        }
    }
}
