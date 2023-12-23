using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlockmapFramework;

namespace WorldEditor
{
    public class WorldGenTool : EditorTool
    {
        public override EditorToolId Id => EditorToolId.WorldGen;
        public override string Name => "World Generation";
        public override Sprite Icon => ResourceManager.Singleton.WorldGenSprite;
        public override int HotkeyNumber => 1;

        [Header("Elements")]
        public TMP_InputField ChunkSizeInput;
        public TMP_InputField NumChunksInput;
        public Button GenerateButton;

        public override void Init(BlockEditor editor)
        {
            base.Init(editor);
            GenerateButton.onClick.AddListener(GenerateButton_OnClick);
        }

        private void GenerateButton_OnClick()
        {
            if (ChunkSizeInput.text == "") return;
            int chunkSize = int.Parse(ChunkSizeInput.text);

            if (NumChunksInput.text == "") return;
            int numChunks = int.Parse(NumChunksInput.text);

            if (chunkSize * numChunks > 300) return;

            WorldData data = BaseWorldGenerator.GenerateWorld("TestWorld", chunkSize, numChunks);
            Editor.SetWorld(data);
        }
    }
}
