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
        public override int HotkeyNumber => 8;

        private StaticEntity BuildPreview;

        private const int ELEMENTS_PER_ROW = 6;
        private StaticEntity SelectedEntity;
        private Dictionary<StaticEntity, UI_SelectionElement> EntityButtons;

        [Header("Prefabs")]
        public UI_SelectionElement SelectionElement;

        [Header("Elements")]
        public GameObject ObjectContainer;

        public override void Init(BlockEditor editor)
        {
            base.Init(editor);

            // Init selection buttons
            EntityButtons = new Dictionary<StaticEntity, UI_SelectionElement>();
            int counter = 0;
            foreach (StaticEntity e in editor.StaticEntities)
            {
                int childIndex = counter / ELEMENTS_PER_ROW;
                UI_SelectionElement elem = Instantiate(SelectionElement, ObjectContainer.transform.GetChild(childIndex));

                Texture2D previewThumbnail = AssetPreview.GetAssetPreview(e.gameObject);
                Sprite icon = null;
                if(previewThumbnail != null)
                    icon = Sprite.Create(previewThumbnail, new Rect(0.0f, 0.0f, previewThumbnail.width, previewThumbnail.height), new Vector2(0.5f, 0.5f), 100.0f);

                elem.Init(icon, Color.white, e.name, () => SelectEntity(e));
                EntityButtons.Add(e, elem);
                counter++;
            }
            SelectEntity(editor.StaticEntities[0]);
        }

        public override void UpdateTool()
        {
            if (World.HoveredNode != null)
            {
                bool canPlace = World.CanPlaceEntity(SelectedEntity, World.HoveredNode);

                BuildPreview.gameObject.SetActive(true);
                BuildPreview.transform.position = BuildPreview.GetWorldPosition(World, World.HoveredNode);
                BuildPreview.GetComponent<MeshRenderer>().material.color = canPlace ? Color.green : Color.red;
            }
            else BuildPreview.gameObject.SetActive(false);
        }

        public override void HandleLeftClick()
        {
            if (World.HoveredNode == null) return;
            if (!World.CanPlaceEntity(SelectedEntity, World.HoveredNode)) return;

            StaticEntity newEntity = Instantiate(SelectedEntity, World.transform);
            World.SpawnEntity(newEntity, World.HoveredNode, World.Gaia);
        }

        public void SelectEntity(StaticEntity e)
        {
            // Update button
            if(SelectedEntity != null) EntityButtons[SelectedEntity].SetSelected(false);
            SelectedEntity = e;
            EntityButtons[SelectedEntity].SetSelected(true);

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
