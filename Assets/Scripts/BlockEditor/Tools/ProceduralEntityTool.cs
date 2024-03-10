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
        public UI_SelectionPanel EntitySelection;

        public override void Init(BlockEditor editor)
        {
            base.Init(editor);
            
            EntitySelection.Clear();
            foreach (ProceduralEntity e in editor.EntityLibrary.ProceduralEntities.Values)
            {
                Texture2D previewThumbnail = e.GetEditorThumbnail();
                Sprite icon = null;
                if (previewThumbnail != null)
                    icon = Sprite.Create(previewThumbnail, new Rect(0.0f, 0.0f, previewThumbnail.width, previewThumbnail.height), new Vector2(0.5f, 0.5f), 100.0f);
                EntitySelection.AddElement(icon, Color.white, e.Name, () => SelectEntity(e));
            }

            EntitySelection.SelectFirstElement();
        }

        public override void OnNewWorld()
        {
            // Player Dropdown
            PlayerDropdown.ClearOptions();
            List<string> playerOptions = World.Actors.Values.Select(x => x.Name).ToList();
            PlayerDropdown.AddOptions(playerOptions);
        }

        public void SelectEntity(ProceduralEntity e)
        {
            SelectedEntity = e;
        }

        public override void UpdateTool()
        {
            // Preview
            if (World.HoveredNode != null)
            {
                //if (HeightInput.text == "") return;
                //int height = int.Parse(HeightInput.text);

                //Texture2D overlayTexture = ResourceManager.Singleton.GetTileSelector(World.NodeHoverMode8);

                Color c = Color.white;
                if (!World.CanSpawnEntity(SelectedEntity, World.HoveredNode, DEFAULT_ROTATION)) c = Color.red;

                // Build Preview
                BuildPreview.SetActive(true);
                BuildPreview.transform.position = World.HoveredNode.Chunk.WorldPosition;
                MeshBuilder previewMeshBuilder = new MeshBuilder(BuildPreview);
                SelectedEntity.BuildMesh(previewMeshBuilder, World.HoveredNode, isPreview: true);
                previewMeshBuilder.ApplyMesh(addCollider: false, castShadows: false);
                BuildPreview.GetComponent<MeshRenderer>().material.color = c;
            }
            else BuildPreview.SetActive(false);
        }

        public override void HandleLeftClick()
        {
            if (World.HoveredNode == null) return;
            if (!World.CanSpawnEntity(SelectedEntity, World.HoveredNode, DEFAULT_ROTATION)) return;

            ProceduralEntity instance = SelectedEntity.GetInstance();
            Actor owner = World.Actors.Values.ToList()[PlayerDropdown.value];

            World.SpawnEntity(instance, World.HoveredNode, DEFAULT_ROTATION, owner, isInstance: true);
        }

        public override void HandleRightClick()
        {
            if (World.HoveredEntity != null) World.RemoveEntity(World.HoveredEntity);
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
