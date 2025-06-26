using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using UnityEditor;

namespace WorldEditor
{
    public class SpawnCharacterTool : EditorTool
    {
        public override EditorToolId Id => EditorToolId.SpawnCharacter;
        public override string Name => "Spawn Character";
        public override Sprite Icon => ResourceManager.LoadSprite(IconBasePath + "SpawnCharacter");

        private EntityDef SelectedEntity;

        [Header("Elements")]
        public TMP_Dropdown PlayerDropdown;
        public TMP_InputField SpeedInput;
        public TMP_InputField VisionInput;
        public TMP_InputField HeightInput;
        public Toggle CanSwimToggle;
        public TMP_Dropdown ClimbingSkillDropdown;
        public TMP_InputField MaxHopUpDistanceInput;
        public TMP_InputField MaxHopDownDistanceInput;

        public UI_SelectionPanel EntitySelection;

        public override void Init(BlockEditor editor)
        {
            base.Init(editor);

            EntitySelection.Clear();
            foreach (EntityDef def in DefDatabase<EntityDef>.AllDefs.Where(x => x.Components.Any(x => x is CompProperties_Movement)))
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

            if(def == EntityDefOf.EditorDynamicCharacter)
            {
                SetAttributesInteractable(true);
            }

            else // Fixed preset
            {
                SetAttributesInteractable(false);
                DisplayAttributesOf(def);
            }
        }

        private void SetAttributesInteractable(bool value)
        {
            SpeedInput.interactable = value;
            VisionInput.interactable = value;
            HeightInput.interactable = value;
            CanSwimToggle.interactable = value;
            ClimbingSkillDropdown.interactable = value;
            MaxHopUpDistanceInput.interactable = value;
            MaxHopDownDistanceInput.interactable = value;
        }
        private void DisplayAttributesOf(EntityDef def)
        {
            CompProperties_Movement movementProps = (CompProperties_Movement)def.Components.First(x => x is CompProperties_Movement);
            SpeedInput.text = movementProps.MovementSpeed.ToString();
            VisionInput.text = def.VisionRange.ToString();
            HeightInput.text = def.Dimensions.y.ToString();
            CanSwimToggle.isOn = movementProps.CanSwim;
            ClimbingSkillDropdown.value = (int)movementProps.ClimbingSkill;
            MaxHopUpDistanceInput.text = movementProps.MaxHopUpDistance.ToString();
            MaxHopDownDistanceInput.text = movementProps.MaxHopDownDistance.ToString();
        }

        public override void UpdateTool()
        {
            if (World.HoveredNode != null)
            {
                bool canSpawn = World.HoveredNode.IsPassable();
                World.HoveredNode.ShowOverlay(ResourceManager.FullTileSelector, canSpawn ? Color.white : Color.red);
            }
        }

        public override void HandleLeftClick()
        {
            if (World.HoveredNode == null) return;
            if (!World.HoveredNode.IsPassable()) return;

            BlockmapNode spawnNode = World.HoveredNode;
            Actor owner = World.GetActor(PlayerDropdown.options[PlayerDropdown.value].text);

            if (SelectedEntity == EntityDefOf.EditorDynamicCharacter)
            {
                float speed = float.Parse(SpeedInput.text);
                float vision = float.Parse(VisionInput.text);
                bool canSwim = CanSwimToggle.isOn;
                ClimbingCategory climbingSkill = (ClimbingCategory)ClimbingSkillDropdown.value;
                int height = int.Parse(HeightInput.text);
                if (height > Comp_Movement.MaxEntityHeight) return;
                int maxHopUpDistance = int.Parse(MaxHopUpDistanceInput.text);
                int maxHopDownDistance = int.Parse(MaxHopDownDistanceInput.text);
                World.SpawnEntity(SelectedEntity, spawnNode, Direction.N, isMirrored: false, owner, updateWorld: true, height, preInit: e => ((EditorMovingEntity)e).PreInit(speed, vision, canSwim, climbingSkill, maxHopUpDistance, maxHopDownDistance));
            }
            else World.SpawnEntity(SelectedEntity, spawnNode, Direction.N, isMirrored: false, owner, updateWorld: true);
        }
        public override void HandleRightClick()
        {
            if (World.HoveredEntity == null) return;
            if (!World.HoveredEntity.HasComponent<Comp_Movement>()) return;

            World.RemoveEntity(World.HoveredEntity, updateWorld: true);
        }

        public override void OnHoveredNodeChanged(BlockmapNode oldNode, BlockmapNode newNode)
        {
            if (oldNode != null) oldNode.ShowOverlay(false);
            if (newNode != null) newNode.ShowOverlay(true);
        }

        public override void OnDeselect()
        {
            if (World.HoveredNode != null) World.HoveredNode.ShowOverlay(false);
        }
    }
}
