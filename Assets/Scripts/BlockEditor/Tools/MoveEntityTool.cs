using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WorldEditor
{
    public class MoveEntityTool : EditorTool
    {
        public override EditorToolId Id => EditorToolId.MoveEntity;
        public override string Name => "Move Entity";
        public override Sprite Icon => ResourceManager.Singleton.MoveEntityToolSprite;
        public override int HotkeyNumber => 9;

        private EditorMovingEntity SelectedEntity;

        private const float PATH_PREVIEW_WIDTH = 0.1f;
        private Color PATH_PREVIEW_COLOR = new Color(1f, 1f, 1f, 0.5f);
        private List<BlockmapNode> TargetPath;
        private LineRenderer PathPreview;

        private bool IsShowingNavmesh;

        public override void Init(BlockEditor editor)
        {
            base.Init(editor);

            GameObject pathPreviewObject = new GameObject("PathPreview");
            PathPreview = pathPreviewObject.AddComponent<LineRenderer>();
            PathPreview.gameObject.SetActive(false);
        }

        public void SelectEntity(EditorMovingEntity e)
        {
            if (SelectedEntity != null) SelectedEntity.SetSelected(false);
            SelectedEntity = e;
            if(SelectedEntity != null) SelectedEntity.SetSelected(true);

            if (SelectedEntity == null) IsShowingNavmesh = false;
            UpdateNavmeshVisualization();
        }

        public override void UpdateTool()
        {
            UpdatePathPreview();

            if (Input.GetKeyDown(KeyCode.N)) ToggleNavmesh();
        }

        private void UpdatePathPreview()
        {
            TargetPath = null;
            PathPreview.gameObject.SetActive(false);

            if (SelectedEntity == null) return;
            if (World.HoveredNode == null) return;
            if (!World.HoveredNode.IsExploredBy(SelectedEntity.Player)) return;

            TargetPath = Pathfinder.GetPath(SelectedEntity, SelectedEntity.OriginNode, World.HoveredNode);
            if (TargetPath == null) return;

            PathPreview.gameObject.SetActive(true);
            Pathfinder.ShowPathPreview(PathPreview, TargetPath, PATH_PREVIEW_WIDTH, PATH_PREVIEW_COLOR);
        }

        public override void HandleLeftClick()
        {
            // De-select entity
            SelectEntity(null);

            // Select entity
            if (World.HoveredEntity != null)
            {
                if (World.HoveredEntity is EditorMovingEntity)
                {
                    SelectEntity((EditorMovingEntity)World.HoveredEntity);
                }
            }
        }

        public override void HandleRightClick()
        {
            if (SelectedEntity != null && TargetPath != null) SelectedEntity.SetTargetPath(TargetPath);
        }

        public override void OnDeselect()
        {
            SelectEntity(null);
            PathPreview.gameObject.SetActive(false);
        }

        private void ToggleNavmesh()
        {
            IsShowingNavmesh = !IsShowingNavmesh;
            UpdateNavmeshVisualization();
        }
        private void UpdateNavmeshVisualization()
        {
            if (IsShowingNavmesh && SelectedEntity != null) PathfindingGraphVisualizer.Singleton.VisualizeGraph(World, SelectedEntity);
            else PathfindingGraphVisualizer.Singleton.ClearVisualization();
        }
    }
}
