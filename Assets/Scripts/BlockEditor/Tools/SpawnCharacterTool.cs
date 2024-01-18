using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WorldEditor
{
    public class SpawnCharacterTool : EditorTool
    {
        public override EditorToolId Id => EditorToolId.SpawnCharacter;
        public override string Name => "Spawn Character";
        public override Sprite Icon => ResourceManager.Singleton.MovingEntitySprite;

        [Header("Elements")]
        public TMP_InputField SpeedInput;
        public TMP_InputField VisionInput;
        public TMP_InputField HeightInput;
        public Toggle CanSwimToggle;
        public TMP_Dropdown ClimbingSkillDropdown;

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

            GameObject characterContainer = new GameObject("Character");
            characterContainer.transform.SetParent(World.transform);

            EditorMovingEntity newCharacter = Instantiate(Editor.CharacterPrefab, characterContainer.transform);
            float speed = float.Parse(SpeedInput.text);
            float vision = float.Parse(VisionInput.text);
            int height = int.Parse(HeightInput.text);
            bool canSwim = CanSwimToggle.isOn;
            ClimbingCategory climbingSkill = (ClimbingCategory)ClimbingSkillDropdown.value;
            Debug.Log("climbing skill is " + climbingSkill.ToString());
            newCharacter.PreInit(speed, vision, height, canSwim, climbingSkill);

            World.SpawnEntity(newCharacter, spawnNode, Direction.N, Editor.EditorPlayer);
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
