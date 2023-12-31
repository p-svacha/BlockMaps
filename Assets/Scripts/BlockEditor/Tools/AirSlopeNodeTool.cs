using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BlockmapFramework;

namespace WorldEditor
{
    public class AirSlopeNodeTool : EditorTool
    {
        public override EditorToolId Id => EditorToolId.AirSlopeNode;
        public override string Name => "Build Air Slope Node";
        public override Sprite Icon => ResourceManager.Singleton.AirSlopeNodeSprite;

        private GameObject BuildPreview;
        private int BuildRotation;
        private int BuildHeight;
        public Direction BuildRotationDirection;

        public override void Init(BlockEditor editor)
        {
            base.Init(editor);

            BuildHeight = 3;
            BuildRotation = 0;
            BuildRotationDirection = GetDirectionFromRotation(BuildRotation);
        }

        public override void UpdateTool()
        {
            UpdatePreview();
            HandleInputs();
        }

        private void UpdatePreview()
        {
            if (World.HoveredSurfaceNode != null)
            {
                Vector3 hoverPos = World.HoveredSurfaceNode.GetCenterWorldPosition();
                BuildPreview.transform.position = new Vector3(hoverPos.x, World.TILE_HEIGHT * BuildHeight + World.TILE_HEIGHT * 0.5f, hoverPos.z);
                BuildPreview.transform.rotation = Quaternion.Euler(0f, BuildRotation, 0f);
                BuildPreview.GetComponentInChildren<MeshRenderer>().material.color = World.CanBuildAirSlope(World.HoveredWorldCoordinates, BuildHeight, BuildRotationDirection) ? Color.white : Color.red;
            }
        }

        private void HandleInputs()
        {
            // Change height
            if (Input.GetKeyDown(KeyCode.R)) SetHeight(BuildHeight + 1);
            if (Input.GetKeyDown(KeyCode.F)) SetHeight(BuildHeight - 1);

            // Rotate
            if (Input.GetKeyDown(KeyCode.X))
            {
                BuildRotation = (BuildRotation + 90) % 360;
                BuildRotationDirection = GetDirectionFromRotation(BuildRotation);
            }
        }

        public override void HandleLeftClick()
        {
            if (World.HoveredSurfaceNode != null && World.CanBuildAirSlope(World.HoveredWorldCoordinates, BuildHeight, BuildRotationDirection))
                World.BuildAirSlope(World.HoveredWorldCoordinates, BuildHeight, BuildRotationDirection);
        }

        public override void OnSelect()
        {
            BuildPreview = new GameObject("ArrowContainer");
            GameObject arrowObject = GameObject.Instantiate(Editor.ArrowPrefab, BuildPreview.transform);
            arrowObject.transform.localPosition = new Vector3(-0.5f, -0.5f, -1.9f);
            arrowObject.transform.localRotation = Quaternion.Euler(22.5f, 180f, 0f);
            arrowObject.transform.localScale = new Vector3(1f, 1f, 1.8f);
            BuildPreview.transform.localScale = new Vector3(1.5f, 0.25f, 0.25f);
        }
        public override void OnDeselect()
        {
            GameObject.Destroy(BuildPreview);
            BuildPreview = null;
        }

        private Direction GetDirectionFromRotation(int rotation)
        {
            if (rotation == 0) return Direction.N;
            if (rotation == 90) return Direction.E;
            if (rotation == 180) return Direction.S;
            if (rotation == 270) return Direction.W;
            return Direction.None;
        }
        private void SetHeight(int value)
        {
            BuildHeight = value;
            BuildHeight = Mathf.Clamp(BuildHeight, 0, World.MAX_HEIGHT);
        }
    }
}
