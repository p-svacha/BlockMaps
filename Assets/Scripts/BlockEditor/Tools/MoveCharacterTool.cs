using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WorldEditor
{
    public class MoveCharacterTool : EditorTool
    {
        public override EditorToolId Id => EditorToolId.MoveCharacter;
        public override string Name => "Move Entity";
        public override Sprite Icon => ResourceManager.Singleton.MoveEntityToolSprite;

        private MovingEntity SelectedEntity;

        private const float PATH_PREVIEW_WIDTH = 0.1f;
        private Color PATH_PREVIEW_COLOR = new Color(1f, 1f, 1f, 0.5f);
        private List<BlockmapNode> TargetPath;
        private LineRenderer PathPreview;

        // Cache
        private BlockmapNode CacheOriginNode;
        private Dictionary<BlockmapNode, List<BlockmapNode>> PathCache = new Dictionary<BlockmapNode, List<BlockmapNode>>();

        public override void Init(BlockEditor editor)
        {
            base.Init(editor);

            GameObject pathPreviewObject = new GameObject("PathPreview");
            PathPreview = pathPreviewObject.AddComponent<LineRenderer>();
            PathPreview.gameObject.SetActive(false);
        }

        public void SelectEntity(MovingEntity e)
        {
            if (SelectedEntity != null) SelectedEntity.SetSelected(false);
            SelectedEntity = e;
            if(SelectedEntity != null) SelectedEntity.SetSelected(true);

            World.SetNavmeshEntity(SelectedEntity);
        }

        public override void UpdateTool()
        {
            UpdatePathPreview();
        }

        private void UpdatePathPreview()
        {
            TargetPath = null;
            PathPreview.gameObject.SetActive(false);

            if (SelectedEntity == null) return;
            if (World.HoveredNode == null) return;
            if (!World.HoveredNode.IsExploredBy(SelectedEntity.Owner)) return;

            if (SelectedEntity.OriginNode != CacheOriginNode) PathCache.Clear(); // Clear cache when changing node
            CacheOriginNode = SelectedEntity.OriginNode;

            if(PathCache.TryGetValue(World.HoveredNode, out List<BlockmapNode> cachedPath)) // A path to the hovered node is cached 
            {
                TargetPath = cachedPath == null ? null : new List<BlockmapNode>(cachedPath);
            }
            else // A path to the hovered node is not yet cached
            {
                TargetPath = Pathfinder.GetPath(SelectedEntity, SelectedEntity.OriginNode, World.HoveredNode);
                PathCache.Add(World.HoveredNode, TargetPath == null ? null : new List<BlockmapNode>(TargetPath)); // add to cache
            }

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
                if (World.HoveredEntity is MovingEntity)
                {
                    SelectEntity((MovingEntity)World.HoveredEntity);
                }
            }
        }

        public override void HandleRightClick()
        {
            if (SelectedEntity != null && TargetPath != null) SelectedEntity.MoveTo(World.HoveredNode);
        }

        public override void OnDeselect()
        {
            SelectEntity(null);
            PathPreview.gameObject.SetActive(false);
        }
    }
}
