using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace WorldEditor
{
    public class SpawnCharacterTool : EditorTool
    {
        public override EditorToolId Id => EditorToolId.SpawnCharacter;
        public override string Name => "Spawn Character";
        public override Sprite Icon => ResourceManager.Singleton.MovingEntitySprite;
        public override int HotkeyNumber => 7;

        [Header("Elements")]
        public TMP_InputField SpeedInput;
        public TMP_InputField VisionInput;
        public TMP_InputField HeightInput;

        public override void UpdateTool()
        {
            if (World.HoveredNode != null)
            {
                World.HoveredNode.ShowOverlay(ResourceManager.Singleton.TileSelector, Color.white);
            }
        }

        public override void HandleLeftClick()
        {
            if (World.HoveredNode == null) return;

            BlockmapNode spawnNode = World.HoveredNode;

            GameObject characterContainer = new GameObject("Character");
            characterContainer.transform.SetParent(World.transform);

            EditorMovingEntity newCharacter = Instantiate(Editor.CharacterPrefab, characterContainer.transform);
            float speed = float.Parse(SpeedInput.text);
            float vision = float.Parse(VisionInput.text);
            int height = int.Parse(HeightInput.text);
            newCharacter.PreInit(speed, vision, height);
            World.SpawnEntity(newCharacter, spawnNode, Editor.EditorPlayer);
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
