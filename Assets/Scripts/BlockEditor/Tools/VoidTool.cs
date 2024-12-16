using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BlockmapFramework;

namespace WorldEditor
{
    public class VoidTool : EditorTool
    {
        public override EditorToolId Id => EditorToolId.Void;
        public override string Name => "Void Tool";
        public override Sprite Icon => ResourceManager.Singleton.VoidToolSprite;

        private GameObject CoordinatesPreview;
        private Vector2Int HoveredCoordinates;
        private GroundNode HoveredCoordinatesGroundNode;
        private int UnvoidAltitude => int.Parse(AltitudeInput.text);
        private bool IsDragSetVoid;

        [Header("Elements")]
        public TMP_InputField AltitudeInput;
        public Toggle HelperGridToggle;

        public override void Init(BlockEditor editor)
        {
            base.Init(editor);
            HelperGridToggle.onValueChanged.AddListener((b) => ShowHelperGrid(b));

            SetAltitude(5);
        }

        public override void UpdateTool()
        {
            UpdateHoveredCoordinates();
            UpdatePreview();
        }

        private void UpdateHoveredCoordinates()
        {
            HoveredCoordinates = World.GetHoveredCoordinates(UnvoidAltitude);
            HoveredCoordinatesGroundNode = World.GetGroundNode(HoveredCoordinates);
        }

        private void UpdatePreview()
        {
            CoordinatesPreview.transform.position = new Vector3(HoveredCoordinates.x + 0.5f, World.NodeHeight * UnvoidAltitude + World.NodeHeight * 0.1f, HoveredCoordinates.y + 0.5f);
            CoordinatesPreview.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

            Color previewColor;
            if (HoveredCoordinatesGroundNode == null) previewColor = Color.red;
            else if (HoveredCoordinatesGroundNode.SurfaceDef == SurfaceDefOf.Void) previewColor = Color.white;
            else previewColor = Color.gray;

            CoordinatesPreview.GetComponentInChildren<MeshRenderer>().material.color = previewColor;
        }

        public override void HandleKeyboardInputs()
        {
            // Ctrl + mouse wheel: change altitude
            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (Input.mouseScrollDelta.y < 0) SetAltitude(UnvoidAltitude - 1);
                if (Input.mouseScrollDelta.y > 0) SetAltitude(UnvoidAltitude + 1);
            }

            // H: Toggle helper grid
            if (Input.GetKeyDown(KeyCode.H)) ShowHelperGrid(!HelperGridToggle.isOn);
        }

        private void SetAltitude(int altitude)
        {
            if (altitude < 0) altitude = 0;
            if (altitude > World.MAX_ALTITUDE) altitude = World.MAX_ALTITUDE;
            AltitudeInput.text = altitude.ToString();
            Editor.AltitudeHelperPlane.transform.position = new Vector3(Editor.AltitudeHelperPlane.transform.position.x, UnvoidAltitude * World.NodeHeight, Editor.AltitudeHelperPlane.transform.position.z);
        }

        private void ShowHelperGrid(bool show)
        {
            Editor.AltitudeHelperPlane.SetActive(show);
            HelperGridToggle.isOn = show;
        }

        public override void HandleLeftClick()
        {
            if (HoveredCoordinatesGroundNode == null) return;

            IsDragSetVoid = !HoveredCoordinatesGroundNode.IsVoid;
            if (HoveredCoordinatesGroundNode.IsVoid) World.UnsetGroundNodeAsVoid(HoveredCoordinatesGroundNode, UnvoidAltitude, updateWorld: true);
            else World.SetGroundNodeAsVoid(HoveredCoordinatesGroundNode, updateWorld: true);
        }

        public override void HandleLeftDrag()
        {
            if (HoveredCoordinatesGroundNode == null) return;
            if (!IsDragSetVoid && HoveredCoordinatesGroundNode.IsVoid) World.UnsetGroundNodeAsVoid(HoveredCoordinatesGroundNode, UnvoidAltitude, updateWorld: true);
            else if (IsDragSetVoid && !HoveredCoordinatesGroundNode.IsVoid) World.SetGroundNodeAsVoid(HoveredCoordinatesGroundNode, updateWorld: true);
        }

        public override void OnSelect()
        {
            CoordinatesPreview = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject.Destroy(CoordinatesPreview.GetComponent<BoxCollider>());
            CoordinatesPreview.transform.localScale = new Vector3(1f, World.NodeHeight * 0.1f, 1f);

            ShowHelperGrid(HelperGridToggle.isOn);
            SetAltitude(int.Parse(AltitudeInput.text));
        }
        public override void OnDeselect()
        {
            GameObject.Destroy(CoordinatesPreview);
            CoordinatesPreview = null;

            Editor.AltitudeHelperPlane.SetActive(false);
        }
    }
}
