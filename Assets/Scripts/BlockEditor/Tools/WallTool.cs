using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorldEditor
{
    public class WallTool : EditorTool
    {
        public override EditorToolId Id => EditorToolId.Wall;
        public override string Name => "Build Walls";
        public override Sprite Icon => ResourceManager.LoadSprite(IconBasePath + "Wall");

        private int BuildAltitude => int.Parse(AltitudeInput.text);

        private WallShapeDef SelectedWallShape;
        private WallMaterialDef SelectedWallMaterial;

        private GameObject BuildPreview;

        [Header("Elements")]
        public UI_SelectionPanel ShapeSelection;
        public UI_SelectionPanel MaterialSelection;
        public TMP_InputField AltitudeInput;
        public Toggle HelperGridToggle;
        public Toggle MirrorToggle;

        public override void Init(BlockEditor editor)
        {
            base.Init(editor);
            HelperGridToggle.onValueChanged.AddListener((b) => ShowHelperGrid(b));

            SetAltitude(5);

            ShapeSelection.Clear();
            foreach (WallShapeDef def in DefDatabase<WallShapeDef>.AllDefs)
                ShapeSelection.AddElement(def.UiSprite, Color.white, def.Label, () => SelectWallShape(def));
            ShapeSelection.SelectFirstElement();

            MaterialSelection.Clear();
            foreach (WallMaterialDef def in DefDatabase<WallMaterialDef>.AllDefs)
                MaterialSelection.AddElement(def.UiSprite, Color.white, def.Label, () => SelectWallMaterial(def));
            MaterialSelection.SelectFirstElement();
        }

        private void SelectWallShape(WallShapeDef def)
        {
            SelectedWallShape = def;
        }
        private void SelectWallMaterial(WallMaterialDef def)
        {
            SelectedWallMaterial = def;
        }

        public override void UpdateTool()
        {
            UpdatePreview();
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

            // M: Toggle mirrored
            if (Input.GetKeyDown(KeyCode.M)) SetMirrored(!MirrorToggle.isOn);
        }

        private void SetAltitude(int altitude)
        {
            if (altitude < 0) altitude = 0;
            if (altitude > World.MAX_ALTITUDE) altitude = World.MAX_ALTITUDE;
            AltitudeInput.text = altitude.ToString();
            Editor.AltitudeHelperPlane.transform.position = new Vector3(Editor.AltitudeHelperPlane.transform.position.x, BuildAltitude * World.NodeHeight, Editor.AltitudeHelperPlane.transform.position.z);
        }

        private void ShowHelperGrid(bool show)
        {
            Editor.AltitudeHelperPlane.SetActive(show);
            HelperGridToggle.isOn = show;
        }

        private void SetMirrored(bool show)
        {
            MirrorToggle.isOn = show;
        }

        private void UpdatePreview()
        {
            // Get hovered coordinates
            Vector2Int hoveredCoordinates = World.GetHoveredCoordinates(BuildAltitude);

            // Check if we are hovering in the world
            if (!World.IsInWorld(hoveredCoordinates))
            {
                BuildPreview.SetActive(false);
                return;
            }
            BuildPreview.SetActive(true);

            // Calculate position data
            Vector3Int globalCellPosition = new Vector3Int(hoveredCoordinates.x, BuildAltitude, hoveredCoordinates.y);
            Vector3Int localCellPosition = World.GetLocalCellCoordinates(globalCellPosition);

            // Get wall side
            Direction side;
            if (SelectedWallShape.IsCornerShape) side = World.GetNodeHoverModeCorners(BuildAltitude);
            else side = World.GetNodeHoverModeSides(BuildAltitude);

            // Get if we can build on current hovered position
            Color c = Color.white;
            if (!World.CanBuildWall(globalCellPosition, side)) c = Color.red;

            // Build preview mesh
            MeshBuilder previewMeshBuilder = new MeshBuilder(BuildPreview);
            WallMeshGenerator.DrawWall(World, previewMeshBuilder, globalCellPosition, localCellPosition, side, SelectedWallShape, SelectedWallMaterial, MirrorToggle.isOn, isPreview: true);
            previewMeshBuilder.ApplyMesh(addCollider: false, castShadows: false);
            BuildPreview.GetComponent<MeshRenderer>().material.color = c;
            BuildPreview.transform.position = World.GetChunk(hoveredCoordinates).WorldPosition; // Position is always based on chunk since there's 1 mesh per chunk
        }

        public override void HandleLeftClick()
        {
            // Calculate position data
            Vector2Int hoveredCoordinates = World.GetHoveredCoordinates(BuildAltitude);
            Vector3Int globalCellPosition = new Vector3Int(hoveredCoordinates.x, BuildAltitude, hoveredCoordinates.y);

            // Get wall side
            Direction side;
            if (SelectedWallShape.IsCornerShape) side = World.GetNodeHoverModeCorners(BuildAltitude);
            else side = World.GetNodeHoverModeSides(BuildAltitude);

            // Make checks if we can build
            if (!World.IsInWorld(hoveredCoordinates)) return;
            if (!World.CanBuildWall(globalCellPosition, side)) return;

            // BUILD THE WALL
            World.BuildWall(globalCellPosition, side, SelectedWallShape, SelectedWallMaterial, updateWorld: true, MirrorToggle.isOn);
        }

        public override void HandleRightClick()
        {
            if (World.HoveredWall == null) return;

            World.RemoveWall(World.HoveredWall, updateWorld: true);
        }

        public override void HandleMiddleClick()
        {
            Wall targetWall = World.HoveredWall;
            if (targetWall == null) return;

            World.RemoveWall(targetWall, updateWorld: true);
            World.BuildWall(targetWall.GlobalCellCoordinates, targetWall.Side, SelectedWallShape, SelectedWallMaterial, MirrorToggle.isOn);
        }


        public override void OnSelect()
        {
            BuildPreview = new GameObject("WallPreview");

            ShowHelperGrid(HelperGridToggle.isOn);
            SetAltitude(int.Parse(AltitudeInput.text));
        }
        public override void OnDeselect()
        {
            GameObject.Destroy(BuildPreview);
            Editor.AltitudeHelperPlane.SetActive(false);
        }
    }
}
