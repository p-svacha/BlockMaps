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

        public TMP_InputField WorldNameInput;
        public Button LoadButton;
        public Button SaveButton;


        public override void Init(BlockEditor editor)
        {
            base.Init(editor);
            GenerateButton.onClick.AddListener(GenerateButton_OnClick);
            LoadButton.onClick.AddListener(LoadButton_OnClick);
            SaveButton.onClick.AddListener(SaveButton_OnClick);
        }

        private void GenerateButton_OnClick()
        {
            if (ChunkSizeInput.text == "") return;
            int chunkSize = int.Parse(ChunkSizeInput.text);

            if (NumChunksInput.text == "") return;
            int numChunks = int.Parse(NumChunksInput.text);

            if (chunkSize * numChunks > 512) return;

            WorldData data = BaseWorldGenerator.GenerateWorld("TestWorld", chunkSize, numChunks);
            Editor.SetWorld(data);
        }

        private void SaveButton_OnClick()
        {
            if (WorldNameInput.text == "") return;

            WorldData data = Editor.World.Save();
            data.Name = WorldNameInput.text;
            JsonUtilities.SaveWorld(data);
        }

        private void LoadButton_OnClick()
        {
            if (WorldNameInput.text == "") return;

            WorldData data = JsonUtilities.LoadWorld(WorldNameInput.text);
            if (data == null) return;
            Editor.SetWorld(data);
        }
    }
}
