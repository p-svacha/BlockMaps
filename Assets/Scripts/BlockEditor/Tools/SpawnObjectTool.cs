using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
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
        public TMP_Dropdown PlayerDropdown;
        public UI_SelectionPanel EntitySelection;

        public override void Init(BlockEditor editor)
        {
            base.Init(editor);

            CurrentRotation = Direction.N;

            EntitySelection.Clear();
            foreach (StaticEntity e in editor.StaticEntities)
            {
                EntitySelection.AddElement(e.GetThumbnail(), Color.white, e.Name, () => SelectEntity(e));
            }

            EntitySelection.SelectFirstElement();
        }

        public override void OnNewWorld()
        {
            // Player Dropdown
            PlayerDropdown.ClearOptions();
            List<string> playerOptions = World.GetAllActors().Select(x => x.Name).ToList();
            PlayerDropdown.AddOptions(playerOptions);
        }

        public override void UpdateTool()
        {
            // Rotation change inputs
            if (Input.GetKeyDown(KeyCode.X)) CurrentRotation = HelperFunctions.GetNextSideDirection(CurrentRotation);
            if (Input.GetKeyDown(KeyCode.Y)) CurrentRotation = HelperFunctions.GetPreviousSideDirection(CurrentRotation);

            // Preview
            if (World.HoveredNode != null)
            {
                bool canPlace = World.CanSpawnEntity(SelectedEntity, World.HoveredNode, CurrentRotation);

                BuildPreview.gameObject.SetActive(true);
                BuildPreview.transform.position = BuildPreview.GetWorldPosition(World, World.HoveredNode, CurrentRotation);
                BuildPreview.transform.rotation = HelperFunctions.Get2dRotationByDirection(CurrentRotation);

                foreach(Material mat in BuildPreview.GetComponent<MeshRenderer>().materials)
                    mat.color = canPlace ? Color.green : Color.red;
            }
            else BuildPreview.gameObject.SetActive(false);
        }

        public override void HandleLeftClick()
        {
            if (World.HoveredNode == null) return;
            if (!World.CanSpawnEntity(SelectedEntity, World.HoveredNode, CurrentRotation)) return;

            Actor owner = World.GetActor(PlayerDropdown.options[PlayerDropdown.value].text);
            World.SpawnEntity(SelectedEntity, World.HoveredNode, CurrentRotation, owner);
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
