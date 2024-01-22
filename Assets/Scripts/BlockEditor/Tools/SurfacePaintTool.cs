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
        private int AreaSize = 1;

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

            SelectionPanel.SelectFirstElement();
        }

        public void SelectSurface(SurfaceId surface)
        {
            SelectedSurface = surface;
        }

        public override void UpdateTool()
        {
            // Ctrl + mouse wheel: change area size
            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (Input.mouseScrollDelta.y < 0 && AreaSize > 1) AreaSize--;
                if (Input.mouseScrollDelta.y > 0 && AreaSize < World.ChunkSize) AreaSize++;
            }

            // Update tile overlay
            if (World.HoveredSurfaceNode != null)
                World.HoveredSurfaceNode.ShowOverlay(ResourceManager.Singleton.TileSelector, Color.white, AreaSize);
        }

        public override void HandleLeftDrag()
        {
            if (World.HoveredSurfaceNode != null)
            {
                List<SurfaceNode> nodes = World.GetSurfaceNodes(World.HoveredSurfaceNode.WorldCoordinates, AreaSize, AreaSize).Where(x => x.Surface.Id != SelectedSurface).ToList();
                foreach (SurfaceNode node in nodes) node.SetSurface(SelectedSurface);

                // Manuall redraw world in one step
                World.RedrawNodesAround(World.HoveredSurfaceNode.WorldCoordinates, AreaSize, AreaSize);

                // Update overlay
                World.HoveredSurfaceNode.ShowOverlay(ResourceManager.Singleton.TileSelector, Color.white, AreaSize);

                // Update navmesh preview
                World.UpdateNavmeshDisplayDelayed();
            }
        }

        public override void OnHoveredSurfaceNodeChanged(SurfaceNode oldNode, SurfaceNode newNode)
        {
            // Hide overlay from all chunks around previously hovered node
            if (oldNode != null)
                foreach (Chunk chunk in World.GetChunks(oldNode.Chunk.Coordinates, 2, 2))
                    chunk.SurfaceMesh.ShowOverlay(false);
        }

        public override void OnDeselect()
        {
            if (World.HoveredSurfaceNode != null) World.HoveredSurfaceNode.ShowOverlay(false);
        }
    }
}
