using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WorldEditor
{
    public class SurfacePaintTool : EditorTool
    {
        public override EditorToolId Id => EditorToolId.SurfacePaint;
        public override string Name => "Surface Painting";
        public override Sprite Icon => ResourceManager.Singleton.SurfaceToolSprite;

        private SurfaceId SelectedSurface;

        [Header("Elements")]
        public UI_SelectionPanel SelectionPanel;

        public override void Init(BlockEditor editor)
        {
            base.Init(editor);

            SelectionPanel.Clear();

            foreach(Surface s in SurfaceManager.Instance.GetAllSurfaces())
            {
                SelectionPanel.AddElement(null, s.PreviewColor, s.Name, () => SelectSurface(s.Id));
            }

            SelectionPanel.SelectFirstElement();
        }

        public void SelectSurface(SurfaceId surface)
        {
            SelectedSurface = surface;
        }

        public override void UpdateTool()
        {
            // Update tile overlay
            if (World.HoveredDynamicNode != null)
                World.HoveredDynamicNode.ShowOverlay(ResourceManager.Singleton.TileSelector, Color.white);
        }

        public override void HandleLeftDrag()
        {
            if (World.HoveredDynamicNode != null)
            {
                World.SetSurface(World.HoveredDynamicNode, SelectedSurface);

                // Update overlay
                World.HoveredDynamicNode.ShowOverlay(ResourceManager.Singleton.TileSelector, Color.white);
            }
        }

        public override void OnHoveredNodeChanged(BlockmapNode oldNode, BlockmapNode newNode)
        {
            // Hide previous node overlay
            if (oldNode != null) oldNode.ShowOverlay(false);
        }
        public override void OnDeselect()
        {
            // Hide node overlay
            if (World.HoveredNode != null) World.HoveredNode.ShowOverlay(false);
        }
    }
}
