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
        public override int HotkeyNumber => 3;

        private SurfaceId SelectedSurface;
        private Dictionary<SurfaceId, UI_SurfaceElement> SurfaceButtons;

        [Header("Prefabs")]
        public UI_SurfaceElement SurfacePrefab;

        [Header("Elements")]
        public GameObject SurfaceContainer;

        private const int ELEMENTS_PER_ROW = 6;

        public override void Init(BlockEditor editor)
        {
            base.Init(editor);

            SurfaceButtons = new Dictionary<SurfaceId, UI_SurfaceElement>();

            int counter = 0;
            foreach(Surface s in SurfaceManager.Instance.GetAllSurfaces())
            {
                int childIndex = counter / ELEMENTS_PER_ROW;
                UI_SurfaceElement elem = Instantiate(SurfacePrefab, SurfaceContainer.transform.GetChild(childIndex));
                elem.Init(this, s.Id);
                SurfaceButtons.Add(s.Id, elem);
                counter++;
            }

            SelectSurface(SurfaceId.Grass);
        }

        public void SelectSurface(SurfaceId surface)
        {
            SurfaceButtons[SelectedSurface].SetSelected(false);
            SelectedSurface = surface;
            SurfaceButtons[SelectedSurface].SetSelected(true);
        }

        public override void UpdateTool()
        {
            if (World.HoveredSurfaceNode != null)
                World.HoveredSurfaceNode.ShowOverlay(ResourceManager.Singleton.TileSelector, Color.white);
        }

        public override void HandleLeftClick()
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
