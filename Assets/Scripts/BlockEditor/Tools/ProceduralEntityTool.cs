using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace WorldEditor
{
    public class ProceduralEntityTool : EditorTool
    {
        private const Direction DEFAULT_ROTATION = Direction.N;

        public override EditorToolId Id => EditorToolId.ProceduralEntity;
        public override string Name => "Place Procedural Object";
        public override Sprite Icon => ResourceManager.Singleton.ProceduralEntitySprite;

        private GameObject BuildPreview;
        private ProceduralEntity SelectedEntity;

        [Header("Elements")]
        public TMP_Dropdown PlayerDropdown;
        public TMP_InputField HeightInput;
        public UI_SelectionPanel EntitySelection;

        public override void Init(BlockEditor editor)
        {
            base.Init(editor);
            
            EntitySelection.Clear();
            foreach (ProceduralEntity e in editor.EntityLibrary.ProceduralEntities.Values)
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

        public void SelectEntity(ProceduralEntity e)
        {
            SelectedEntity = e;
        }

        public override void UpdateTool()
        {
            // Ctrl + mouse wheel: change height
            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (Input.mouseScrollDelta.y < 0 && HeightInput.text != "")
                {
                    int height = int.Parse(HeightInput.text);
                    if (height > 1) height--;
                    HeightInput.text = height.ToString();
                    SelectedEntity.SetHeight(height);
                }
                if (Input.mouseScrollDelta.y > 0 && HeightInput.text != "")
                {
                    int height = int.Parse(HeightInput.text);
                    height++;
                    HeightInput.text = height.ToString();
                    SelectedEntity.SetHeight(height);
                }
            }

            // Preview
            if (World.HoveredNode != null)
            {
                if (HeightInput.text == "") return;
                int height = int.Parse(HeightInput.text);

                Color c = Color.white;
                if (!World.CanSpawnEntity(SelectedEntity, World.HoveredNode, DEFAULT_ROTATION)) c = Color.red;

                // Build Preview
                BuildPreview.SetActive(true);
                BuildPreview.transform.position = World.HoveredNode.Chunk.WorldPosition;
                MeshBuilder previewMeshBuilder = new MeshBuilder(BuildPreview);
                SelectedEntity.BuildMesh(previewMeshBuilder, World.HoveredNode, height, isPreview: true);
                previewMeshBuilder.ApplyMesh(addCollider: false, castShadows: false);
                BuildPreview.GetComponent<MeshRenderer>().material.color = c;
            }
            else BuildPreview.SetActive(false);
        }

        public override void HandleLeftClick()
        {
            if (World.HoveredNode == null) return;
            if (!World.CanSpawnEntity(SelectedEntity, World.HoveredNode, DEFAULT_ROTATION)) return;
            if (HeightInput.text == "") return;

            int height = int.Parse(HeightInput.text);
            ProceduralEntity instance = SelectedEntity.GetInstance(height);
            Actor owner = World.GetActor(PlayerDropdown.options[PlayerDropdown.value].text);

            World.SpawnEntity(instance, World.HoveredNode, DEFAULT_ROTATION, owner, isInstance: true);
        }

        public override void HandleRightClick()
        {
            if (World.HoveredEntity == null) return;
            if (!(World.HoveredEntity is ProceduralEntity)) return;

            World.RemoveEntity(World.HoveredEntity);
        }

        public override void OnSelect()
        {
            BuildPreview = new GameObject("ProcEntityPreview");
        }
        public override void OnDeselect()
        {
            GameObject.Destroy(BuildPreview);
            if (World.HoveredNode != null) World.HoveredNode.ShowOverlay(false);
        }
    }
}
