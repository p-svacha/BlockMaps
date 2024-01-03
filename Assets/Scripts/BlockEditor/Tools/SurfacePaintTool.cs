using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
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

            foreach(Surface s in SurfaceManager.Instance.GetPaintableSurfaces())
            {
                SelectionPanel.AddElement(null, s.Color, s.Name, () => SelectSurface(s.Id));
            }

            SelectSurface(SurfaceId.Grass);
        }

        public void SelectSurface(SurfaceId surface)
        {
            SelectedSurface = surface;
        }

        public override void UpdateTool()
        {
            if (World.HoveredSurfaceNode != null)
                World.HoveredSurfaceNode.ShowOverlay(ResourceManager.Singleton.TileSelector, Color.white);
        }

        public override void HandleLeftDrag()
        {
            if (World.HoveredSurfaceNode != null) World.SetSurface(World.HoveredSurfaceNode, SelectedSurface);
        }

        public override void OnHoveredSurfaceNodeChanged(SurfaceNode oldNode, SurfaceNode newNode)
        {
            if (oldNode != null) oldNode.ShowOverlay(false);
            if (newNode != null) newNode.ShowOverlay(true);
        }

        public override void OnDeselect()
        {
            if (World.HoveredSurfaceNode != null) World.HoveredSurfaceNode.ShowOverlay(false);
        }
    }
}
