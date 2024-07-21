using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace WorldEditor
{
    public class WallTool : EditorTool
    {
        public override EditorToolId Id => EditorToolId.Wall;
        public override string Name => "Build Walls";
        public override Sprite Icon => ResourceManager.Singleton.WallToolSprite;

        private int BuildAltitude => int.Parse(AltitudeInput.text);

        private WallShape SelectedWallShape;
        private WallMaterial SelectedWallMaterial;

        private GameObject BuildPreview;

        [Header("Elements")]
        public UI_SelectionPanel ShapeSelection;
        public UI_SelectionPanel MaterialSelection;
        public TMP_InputField AltitudeInput;

        public override void Init(BlockEditor editor)
        {
            base.Init(editor);

            SetAltitude(5);

            ShapeSelection.Clear();
            foreach (WallShape shape in WallManager.Instance.GetAllWallShapes())
                ShapeSelection.AddElement(shape.PreviewSprite, Color.white, shape.Name, () => SelectWallShape(shape.Id));
            ShapeSelection.SelectFirstElement();

            MaterialSelection.Clear();
            foreach (WallMaterial material in WallManager.Instance.GetAllWallMaterials())
                MaterialSelection.AddElement(HelperFunctions.Texture2DToSprite((Texture2D)material.Material.mainTexture), Color.white, material.Name, () => SelectWallMaterial(material.Id));
            MaterialSelection.SelectFirstElement();
        }

        private void SelectWallShape(WallShapeId wallShape)
        {
            SelectedWallShape = WallManager.Instance.GetWallShape(wallShape);
        }
        private void SelectWallMaterial(WallMaterialId wallMaterial)
        {
            SelectedWallMaterial = WallManager.Instance.GetWallMaterial(wallMaterial);
        }

        public override void UpdateTool()
        {
            HandleInputs();
            UpdatePreview();
        }

        private void HandleInputs()
        {
            // Ctrl + mouse wheel: change altitude
            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (Input.mouseScrollDelta.y < 0) SetAltitude(BuildAltitude - 1);
                if (Input.mouseScrollDelta.y > 0) SetAltitude(BuildAltitude + 1);
            }
        }

        private void SetAltitude(int altitude)
        {
            if (altitude < 0) altitude = 0;
            if (altitude > World.MAX_ALTITUDE) altitude = World.MAX_ALTITUDE;
            AltitudeInput.text = altitude.ToString();
            Editor.AltitudeHelperPlane.transform.position = new Vector3(Editor.AltitudeHelperPlane.transform.position.x, BuildAltitude * World.TILE_HEIGHT, Editor.AltitudeHelperPlane.transform.position.z);
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
            Direction side = World.GetNodeHoverModeSides(BuildAltitude);

            // Get if we can build on current hovered position
            Color c = Color.white;
            if (!World.CanBuildWall(globalCellPosition, side)) c = Color.red;

            // Build preview mesh
            MeshBuilder previewMeshBuilder = new MeshBuilder(BuildPreview);
            WallMeshGenerator.DrawWall(previewMeshBuilder, localCellPosition, side, SelectedWallShape, SelectedWallMaterial, isPreview: true);
            previewMeshBuilder.ApplyMesh(addCollider: false, castShadows: false);
            BuildPreview.GetComponent<MeshRenderer>().material.color = c;
            BuildPreview.transform.position = World.GetChunk(hoveredCoordinates).WorldPosition; // Position is always based on chunk since there's 1 mesh per chunk
        }

        public override void HandleLeftClick()
        {
            // Calculate position data
            Vector2Int hoveredCoordinates = World.GetHoveredCoordinates(BuildAltitude);
            Vector3Int globalCellPosition = new Vector3Int(hoveredCoordinates.x, BuildAltitude, hoveredCoordinates.y);
            Direction side = World.GetNodeHoverModeSides(BuildAltitude);

            // Make checks if we can build
            if (!World.IsInWorld(hoveredCoordinates)) return;
            if (!World.CanBuildWall(globalCellPosition, side)) return;

            // BUILD THE WALL
            World.BuildWall(globalCellPosition, side, SelectedWallShape, SelectedWallMaterial);
        }

        public override void HandleRightClick()
        {
            // todo
        }


        public override void OnSelect()
        {
            BuildPreview = new GameObject("WallPreview");
            Editor.AltitudeHelperPlane.SetActive(true);
        }
        public override void OnDeselect()
        {
            GameObject.Destroy(BuildPreview);
            Editor.AltitudeHelperPlane.SetActive(false);
        }
    }
}
