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
        public override Sprite Icon => ResourceManager.LoadSprite(IconBasePath + "SurfacePaint");

        private SurfaceDef SelectedSurface;

        [Header("Elements")]
        public UI_SelectionPanel SelectionPanel;

        public override void Init(BlockEditor editor)
        {
            base.Init(editor);

            SelectionPanel.Clear();
            foreach (SurfaceDef def in DefDatabase<SurfaceDef>.AllDefs.Where(x => x.Paintable))
            {
                SelectionPanel.AddElement(def.GetPreviewSprite(), Color.white, def.LabelCap, () => SelectSurface(def));
            }
            SelectionPanel.SelectFirstElement();
        }

        public void SelectSurface(SurfaceDef def)
        {
            SelectedSurface = def;
        }

        public override void UpdateTool()
        {
            // Update tile overlay
            if (World.HoveredDynamicNode != null)
                World.HoveredDynamicNode.ShowOverlay(ResourceManager.FullTileSelector, Color.white);
        }

        public override void HandleLeftDrag()
        {
            if (World.HoveredDynamicNode != null)
            {
                if (World.HoveredDynamicNode.SurfaceDef == SelectedSurface) return;

                World.SetSurface(World.HoveredDynamicNode, SelectedSurface, updateWorld: true);

                // Update overlay
                World.HoveredDynamicNode.ShowOverlay(ResourceManager.FullTileSelector, Color.white);
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
