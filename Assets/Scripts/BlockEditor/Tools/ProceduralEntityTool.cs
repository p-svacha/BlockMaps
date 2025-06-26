using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorldEditor
{
    public class ProceduralEntityTool : EditorTool
    {
        private const Direction DEFAULT_ROTATION = Direction.N;

        public override EditorToolId Id => EditorToolId.ProceduralEntity;
        public override string Name => "Place Procedural Object";
        public override Sprite Icon => ResourceManager.LoadSprite(IconBasePath + "ProceduralEntity");

        private GameObject BuildPreview;
        private EntityDef SelectedEntity;
        private EntitySpawnProperties SpawnProperties;

        [Header("Elements")]
        public TMP_Dropdown PlayerDropdown;
        public TMP_InputField HeightInput;
        public UI_SelectionPanel EntitySelection;
        public Toggle AllowCollisionsToggle;

        public override void Init(BlockEditor editor)
        {
            base.Init(editor);
            
            EntitySelection.Clear();
            foreach (EntityDef def in DefDatabase<EntityDef>.AllDefs.Where(x => x.RenderProperties.RenderType == EntityRenderType.Batch))
            {
                EntitySelection.AddElement(def.UiSprite, Color.white, def.LabelCap, () => SelectEntity(def));
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

        public void SelectEntity(EntityDef def)
        {
            SelectedEntity = def;
        }

        public override void UpdateTool()
        {
            // Preview
            if (World.HoveredNode != null)
            {
                if (HeightInput.text == "") return;
                int height = int.Parse(HeightInput.text);
                Actor owner = World.GetActor(PlayerDropdown.options[PlayerDropdown.value].text);

                // Check if can spawn
                Color c = Color.white;
                SpawnProperties = new EntitySpawnProperties(World)
                {
                    Def = SelectedEntity,
                    Actor = owner,
                    PositionProperties = new EntitySpawnPositionProperties_ExactlyOnNode(World.HoveredNode),
                    CustomHeight = height,
                    AllowCollisionWithOtherEntities = AllowCollisionsToggle.isOn,
                };
                SpawnProperties.Validate(inWorldGen: false, out _);
                SpawnProperties.Resolve();

                if (!EntitySpawner.CanSpawnEntity(SpawnProperties, inWorldGen: false, out string failReason))
                {
                    c = Color.red;
                    Tooltip.Instance.Init(Tooltip.TooltipType.TextOnly, "", failReason);
                }
                else Tooltip.Instance.gameObject.SetActive(false);

                // Build Preview
                BuildPreview.SetActive(true);
                BuildPreview.transform.position = World.HoveredNode.Chunk.WorldPosition;
                MeshBuilder previewMeshBuilder = new MeshBuilder(BuildPreview);
                SelectedEntity.RenderProperties.BatchRenderFunction(previewMeshBuilder, World.HoveredNode, height, true);
                previewMeshBuilder.ApplyMesh(addCollider: false, castShadows: false);
                BuildPreview.GetComponent<MeshRenderer>().material.color = c;
            }
            else BuildPreview.SetActive(false);
        }

        public override void HandleKeyboardInputs()
        {
            // Ctrl + mouse wheel: change height
            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (Input.mouseScrollDelta.y < 0 && HeightInput.text != "")
                {
                    int height = int.Parse(HeightInput.text);
                    if (height > 1) height--;
                    HeightInput.text = height.ToString();
                }
                if (Input.mouseScrollDelta.y > 0 && HeightInput.text != "")
                {
                    int height = int.Parse(HeightInput.text);
                    height++;
                    HeightInput.text = height.ToString();
                }
            }

            // C: Allow collisions
            if (Input.GetKeyDown(KeyCode.C)) SetAllowCollisions(!AllowCollisionsToggle.isOn);
        }

        private void SetAllowCollisions(bool value)
        {
            AllowCollisionsToggle.isOn = value;
        }

        public override void HandleLeftClick()
        {
            EntitySpawner.TrySpawnEntity(SpawnProperties, inWorldGen: false, updateWorld: true);
        }

        public override void HandleRightClick()
        {
            if (World.HoveredEntity == null) return;
            if (World.HoveredEntity.Def.RenderProperties.RenderType != EntityRenderType.Batch) return;

            World.RemoveEntity(World.HoveredEntity, updateWorld: true);
        }

        public override void OnSelect()
        {
            BuildPreview = new GameObject("ProcEntityPreview");
        }
        public override void OnDeselect()
        {
            GameObject.Destroy(BuildPreview);
            if (World.HoveredNode != null) World.HoveredNode.ShowOverlay(false);
            Tooltip.Instance.gameObject.SetActive(false);
        }
    }
}
