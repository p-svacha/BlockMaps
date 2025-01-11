using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace WorldEditor
{
    public class SpawnObjectTool : EditorTool
    {
        public override EditorToolId Id => EditorToolId.SpawnObject;
        public override string Name => "Spawn Object";
        public override Sprite Icon => ResourceManager.Singleton.StaticEntitySprite;

        private GameObject BuildPreview;

        private EntityDef SelectedEntity;
        private Direction CurrentRotation;

        [Header("Elements")]
        public TMP_Dropdown PlayerDropdown;
        public UI_SelectionPanel EntitySelection;
        public Toggle MirrorToggle;
        public TMP_Dropdown VariantDropdown;

        public override void Init(BlockEditor editor)
        {
            base.Init(editor);

            CurrentRotation = Direction.N;

            EntitySelection.Clear();
            foreach (EntityDef def in DefDatabase<EntityDef>.AllDefs.Where(x => x.RenderProperties.RenderType == EntityRenderType.StandaloneModel && !x.Components.Any(y => y is CompProperties_Movement)))
            {
                EntitySelection.AddElement(def.UiPreviewSprite, Color.white, def.LabelCap, () => SelectEntity(def));
            }

            EntitySelection.SelectFirstElement();
        }

        public override void OnNewWorld()
        {
            // Player Dropdown
            PlayerDropdown.ClearOptions();
            List<string> playerOptions = World.GetAllActors().Select(x => x.Label).ToList();
            PlayerDropdown.AddOptions(playerOptions);
        }

        public override void UpdateTool()
        {
            // Preview
            if (World.HoveredNode != null)
            {
                bool canPlace = World.CanSpawnEntity(SelectedEntity, World.HoveredNode, CurrentRotation, MirrorToggle.isOn, allowCollisions: true);

                BuildPreview.gameObject.SetActive(true);
                BuildPreview.transform.position = SelectedEntity.RenderProperties.GetWorldPositionFunction(SelectedEntity, World, World.HoveredNode, CurrentRotation, SelectedEntity.Dimensions.y, false);
                BuildPreview.transform.rotation = HelperFunctions.Get2dRotationByDirection(CurrentRotation);
                BuildPreview.transform.localScale = SelectedEntity.RenderProperties.ModelScale;
                if (MirrorToggle.isOn) HelperFunctions.SetAsMirrored(BuildPreview);

                foreach (Material mat in BuildPreview.GetComponent<MeshRenderer>().materials)
                    mat.color = canPlace ? Color.green : Color.red;
            }
            else BuildPreview.gameObject.SetActive(false);
        }

        public override void HandleKeyboardInputs()
        {
            // X / Y - Rotate object
            if (Input.GetKeyDown(KeyCode.X)) CurrentRotation = HelperFunctions.GetNextSideDirection(CurrentRotation);
            if (Input.GetKeyDown(KeyCode.Y)) CurrentRotation = HelperFunctions.GetPreviousSideDirection(CurrentRotation);

            // M: Toggle mirrored
            if (Input.GetKeyDown(KeyCode.M)) SetMirrored(!MirrorToggle.isOn);
        }

        private void SetMirrored(bool show)
        {
            MirrorToggle.isOn = show;
        }

        public override void HandleLeftClick()
        {
            if (World.HoveredNode == null) return;
            if (!World.CanSpawnEntity(SelectedEntity, World.HoveredNode, CurrentRotation, MirrorToggle.isOn, allowCollisions: true)) return;

            Actor owner = World.GetActor(PlayerDropdown.options[PlayerDropdown.value].text);
            World.SpawnEntity(SelectedEntity, World.HoveredNode, CurrentRotation, MirrorToggle.isOn, owner, updateWorld: true, variantIndex: VariantDropdown.value);
        }

        public override void HandleRightClick()
        {
            if (World.HoveredEntity == null) return;
            if (!World.HoveredEntity.IsStandaloneEntity) return;
                
            World.RemoveEntity(World.HoveredEntity, updateWorld: true);
        }

        public void SelectEntity(EntityDef def)
        {
            SelectedEntity = def;

            // Update preview
            if (BuildPreview != null)
            {
                // Get the mesh and materials from the prefab
                MeshFilter prefabMeshFilter = def.RenderProperties.Model.GetComponent<MeshFilter>();
                MeshRenderer prefabMeshRenderer = def.RenderProperties.Model.GetComponent<MeshRenderer>();

                if (prefabMeshFilter != null && prefabMeshRenderer != null)
                {
                    // Apply mesh
                    BuildPreview.GetComponent<MeshFilter>().mesh = prefabMeshFilter.sharedMesh;

                    // Apply materials
                    MeshRenderer previewRenderer = BuildPreview.GetComponent<MeshRenderer>();
                    previewRenderer.sharedMaterials = prefabMeshRenderer.sharedMaterials;
                    foreach (Material mat in previewRenderer.materials) mat.SetFloat("_UseTextures", 0);
                }

                BuildPreview.gameObject.SetActive(false);
            }

            // Update variant dropdown
            string prevSelectedOption = VariantDropdown.options.Count == 0 ? "" : VariantDropdown.options[VariantDropdown.value].text;
            VariantDropdown.ClearOptions();
            if(SelectedEntity.RenderProperties.Variants.Count == 0) // No variants
            {
                VariantDropdown.AddOptions(new List<string> { "Default" });
                VariantDropdown.interactable = false;
            }
            else
            {
                VariantDropdown.AddOptions(SelectedEntity.RenderProperties.Variants.Select(v => v.VariantName).ToList());
                VariantDropdown.interactable = true;

                // If there's a variant with the same name as the previously selected variant, select it again
                EntityVariant matchingVariant = SelectedEntity.RenderProperties.Variants.FirstOrDefault(v => v.VariantName == prevSelectedOption);
                if (matchingVariant != null) VariantDropdown.value = SelectedEntity.RenderProperties.Variants.IndexOf(matchingVariant);
            }
        }

        public override void OnSelect()
        {
            BuildPreview = new GameObject("BuildPreview");
            BuildPreview.AddComponent<MeshFilter>();
            BuildPreview.AddComponent<MeshRenderer>();

            SelectEntity(SelectedEntity);
        }
        public override void OnDeselect()
        {
            if (BuildPreview != null) GameObject.Destroy(BuildPreview.gameObject);
            BuildPreview = null;
        }
    }
}
