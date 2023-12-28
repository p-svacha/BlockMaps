using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace WorldEditor
{
    public class SpawnEntityTool : EditorTool
    {
        public override EditorToolId Id => EditorToolId.SpawnEntity;
        public override string Name => "Spawn Entity";
        public override Sprite Icon => ResourceManager.Singleton.MovingEntitySprite;
        public override int HotkeyNumber => 7;

        [Header("Elements")]
        public TMP_InputField SpeedInput;
        public TMP_InputField VisionInput;

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

            EditorEntity newCharacter = Instantiate(Editor.CharacterPrefab, characterContainer.transform);
            float speed = float.Parse(SpeedInput.text);
            float vision = float.Parse(VisionInput.text);
            newCharacter.Init(World, spawnNode, new bool[,,] { { { true } } }, World.Gaia, speed, vision);
            World.AddEntity(newCharacter);
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
