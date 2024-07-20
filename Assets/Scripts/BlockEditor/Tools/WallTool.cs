using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WorldEditor
{
    public class WallTool : EditorTool
    {
        public override EditorToolId Id => EditorToolId.Wall;
        public override string Name => "Build Walls";
        public override Sprite Icon => ResourceManager.Singleton.WallToolSprite;

        private WallShape SelectedWallShape;
        private WallMaterial SelectedWallMaterial;

        private GameObject BuildPreview;

        [Header("Elements")]
        public UI_SelectionPanel ShapeSelection;
        public UI_SelectionPanel MaterialSelection;

        public override void Init(BlockEditor editor)
        {
            base.Init(editor);

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
            // todo
        }

        public override void HandleLeftClick()
        {
            // todo
        }

        public override void HandleRightClick()
        {
            // todo
        }
    }
}
