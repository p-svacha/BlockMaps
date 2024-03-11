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
        public override Sprite Icon => ResourceManager.Singleton.MovingEntitySprite;

        private int SelectedEntityIndex;

        [Header("Elements")]
        public TMP_Dropdown PlayerDropdown;
        public TMP_InputField SpeedInput;
        public TMP_InputField VisionInput;
        public TMP_InputField HeightInput;
        public Toggle CanSwimToggle;
        public TMP_Dropdown ClimbingSkillDropdown;

        public UI_SelectionPanel EntitySelection;

        public override void Init(BlockEditor editor)
        {
            base.Init(editor);

            EntitySelection.Clear();
            // Add variable MovingEntity to selection
            EntitySelection.AddElement(ResourceManager.Singleton.DynamicEntitySprite, Color.white, "Dynamic", () => SelectEntity(0));

            // Add preset
            for (int i = 0; i < editor.MovingEntityPresets.Count; i++)
            {
                int j = i;
                MovingEntity preset = editor.MovingEntityPresets[j];
                EntitySelection.AddElement(preset.GetThumbnail(), Color.white, preset.name, () => SelectEntity(j + 1));
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

        public void SelectEntity(int index)
        {
            SelectedEntityIndex = index;

            if(index == 0) // Dynamic preset
            {
                SetAttributesInteractable(true);
            }

            else // Fixed preset
            {
                SetAttributesInteractable(false);
                DisplayAttributesOf(Editor.MovingEntityPresets[index - 1]);
            }
        }

        private void SetAttributesInteractable(bool value)
        {
            SpeedInput.interactable = value;
            VisionInput.interactable = value;
            HeightInput.interactable = value;
            CanSwimToggle.interactable = value;
            ClimbingSkillDropdown.interactable = value;
        }
        private void DisplayAttributesOf(MovingEntity e)
        {
            SpeedInput.text = e.MovementSpeed.ToString();
            VisionInput.text = e.VisionRange.ToString();
            HeightInput.text = e.Height.ToString();
            CanSwimToggle.isOn = e.CanSwim;
            ClimbingSkillDropdown.value = (int)e.ClimbingSkill;
        }

        public override void UpdateTool()
        {
            if (World.HoveredNode != null)
            {
                bool canSpawn = World.HoveredNode.IsPassable(Editor.CharacterPrefab);
                World.HoveredNode.ShowOverlay(ResourceManager.Singleton.TileSelector, canSpawn ? Color.white : Color.red);
            }
        }

        public override void HandleLeftClick()
        {
            if (World.HoveredNode == null) return;
            if (!World.HoveredNode.IsPassable(Editor.CharacterPrefab)) return;

            BlockmapNode spawnNode = World.HoveredNode;
            Actor owner = World.Actors.Values.ToList()[PlayerDropdown.value];

            if (SelectedEntityIndex == 0) // Dynamic preset
            {
                EditorMovingEntity newCharacter = Instantiate(Editor.CharacterPrefab);
                float speed = float.Parse(SpeedInput.text);
                float vision = float.Parse(VisionInput.text);
                int height = int.Parse(HeightInput.text);
                bool canSwim = CanSwimToggle.isOn;
                ClimbingCategory climbingSkill = (ClimbingCategory)ClimbingSkillDropdown.value;
                newCharacter.PreInit(speed, vision, height, canSwim, climbingSkill);

                World.SpawnEntity(newCharacter, spawnNode, Direction.N, owner, isInstance: true);
            }
            else // Fixed preset
            {
                World.SpawnEntity(Editor.MovingEntityPresets[SelectedEntityIndex - 1], spawnNode, Direction.N, owner, updateWorld: true);
            }
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
