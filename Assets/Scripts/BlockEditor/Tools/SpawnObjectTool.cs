using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace WorldEditor
{
    public class SpawnObjectTool : EditorTool
    {
        public override EditorToolId Id => EditorToolId.SpawnObject;
        public override string Name => "Spawn Object";
        public override Sprite Icon => ResourceManager.Singleton.StaticEntitySprite;

        private StaticEntity BuildPreview;

        private StaticEntity SelectedEntity;
        private Direction CurrentRotation;

        [Header("Elements")]
        public UI_SelectionPanel EntitySelection;

        public override void Init(BlockEditor editor)
        {
            base.Init(editor);

            CurrentRotation = Direction.N;

            EntitySelection.Clear();
            foreach (StaticEntity e in editor.StaticEntities)
            {
                Texture2D previewThumbnail = AssetPreview.GetAssetPreview(e.gameObject);
                Sprite icon = null;
                if (previewThumbnail != null)
                    icon = Sprite.Create(previewThumbnail, new Rect(0.0f, 0.0f, previewThumbnail.width, previewThumbnail.height), new Vector2(0.5f, 0.5f), 100.0f);
                EntitySelection.AddElement(icon, Color.white, e.name, () => SelectEntity(e));
            }

            EntitySelection.SelectFirstElement();
        }

        public override void UpdateTool()
        {
            // Rotation change inputs
            if (Input.GetKeyDown(KeyCode.X)) CurrentRotation = HelperFunctions.GetNextSideDirection(CurrentRotation);
            if (Input.GetKeyDown(KeyCode.Y)) CurrentRotation = HelperFunctions.GetPreviousSideDirection(CurrentRotation);

            // Preview
            if (World.HoveredNode != null)
            {
                bool canPlace = World.CanPlaceEntity(SelectedEntity, World.HoveredNode, CurrentRotation);

                BuildPreview.gameObject.SetActive(true);
                BuildPreview.transform.position = BuildPreview.GetWorldPosition(World, World.HoveredNode, CurrentRotation);
                BuildPreview.transform.rotation = HelperFunctions.Get2dRotationByDirection(CurrentRotation);
                BuildPreview.GetComponent<MeshRenderer>().material.color = canPlace ? Color.green : Color.red;
            }
            else BuildPreview.gameObject.SetActive(false);
        }

        public override void HandleLeftClick()
        {
            if (World.HoveredNode == null) return;
            if (!World.CanPlaceEntity(SelectedEntity, World.HoveredNode, CurrentRotation)) return;

            StaticEntity newEntity = Instantiate(SelectedEntity, World.transform);
            World.SpawnEntity(newEntity, World.HoveredNode, CurrentRotation, World.Gaia);
        }

        public override void HandleRightClick()
        {
            if (World.HoveredEntity != null) World.RemoveEntity(World.HoveredEntity);
        }

        public void SelectEntity(StaticEntity e)
        {
            SelectedEntity = e;

            // Update preview
            if(BuildPreview != null) GameObject.Destroy(BuildPreview.gameObject);
            BuildPreview = Instantiate(SelectedEntity);
            BuildPreview.gameObject.SetActive(false);
        }

        public override void OnSelect()
        {
            SelectEntity(SelectedEntity);
        }
        public override void OnDeselect()
        {
            if (BuildPreview != null) GameObject.Destroy(BuildPreview.gameObject);
            BuildPreview = null;
        }
    }
}
