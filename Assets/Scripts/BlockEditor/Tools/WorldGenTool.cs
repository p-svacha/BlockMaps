using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlockmapFramework;
using System.Linq;
using System.IO;
using BlockmapFramework.WorldGeneration;

namespace WorldEditor
{
    public class WorldGenTool : EditorTool
    {
        public override EditorToolId Id => EditorToolId.WorldGen;
        public override string Name => "World Generation";
        public override Sprite Icon => ResourceManager.Singleton.WorldGenSprite;

        private List<string> SavedWorlds;
        private WorldGenerator ActiveGenerator;

        [Header("Elements")]
        public TMP_InputField ChunkSizeInput;
        public TMP_InputField NumChunksInput;

        public TMP_Dropdown GeneratorDropdown;
        public Button GenerateButton;

        public TMP_Dropdown LoadDropdown;
        public Button LoadButton;

        public TMP_InputField SaveNameInput;
        public Button SaveButton;


        public override void Init(BlockEditor editor)
        {
            base.Init(editor);
            GenerateButton.onClick.AddListener(GenerateButton_OnClick);
            LoadButton.onClick.AddListener(LoadButton_OnClick);
            SaveButton.onClick.AddListener(SaveButton_OnClick);

            // Generator dropdown
            List<string> generators = editor.Generators.Select(x => x.Name).ToList();
            GeneratorDropdown.AddOptions(generators);

            UpdateLoadWorldDropdown();
        }

        public override void UpdateTool()
        {
            if (ActiveGenerator != null)
            {
                if (ActiveGenerator.IsDone)
                {
                    Editor.SetWorld(ActiveGenerator.World);
                    ActiveGenerator = null;
                }
                else ActiveGenerator.UpdateGeneration();
            }
        }

        private void UpdateLoadWorldDropdown(string initValue = "")
        {
            LoadDropdown.ClearOptions();

            string[] fullPaths = Directory.GetFiles(JsonUtilities.DATA_FILES_PATH, "*.json");
            SavedWorlds = fullPaths.Select(x => System.IO.Path.GetFileNameWithoutExtension(x)).ToList();
            LoadDropdown.AddOptions(SavedWorlds);

            if (initValue != "") LoadDropdown.value = LoadDropdown.options.IndexOf(LoadDropdown.options.First(x => x.text == initValue));
        }

        public void GenerateButton_OnClick()
        {
            if (ActiveGenerator != null) return; // Disabled while in generation process

            if (ChunkSizeInput.text == "") return;
            int chunkSize = int.Parse(ChunkSizeInput.text);

            if (NumChunksInput.text == "") return;
            int numChunks = int.Parse(NumChunksInput.text);

            if (chunkSize * numChunks > 1024) return;
            if (chunkSize * numChunks < 32) return;

            if (World != null) Destroy(World.gameObject);
            WorldGenerator selectedGenerator = Editor.Generators[GeneratorDropdown.value];

            ActiveGenerator = selectedGenerator;
            ActiveGenerator.InitGeneration(chunkSize, numChunks);
        }

        private void SaveButton_OnClick()
        {
            if (ActiveGenerator != null) return; // Disabled while in generation process
            if (SaveNameInput.text == "") return;

            WorldData data = Editor.World.Save();
            data.Name = SaveNameInput.text;
            JsonUtilities.SaveWorld(data);

            UpdateLoadWorldDropdown(initValue: data.Name);
        }

        private void LoadButton_OnClick()
        {
            if (ActiveGenerator != null) return; // Disabled while in generation process
            if (SavedWorlds.Count == 0) return;

            string worldToLoad = SavedWorlds[LoadDropdown.value];

            WorldData data = JsonUtilities.LoadWorld(worldToLoad);
            if (data == null) return;
            Editor.SetWorld(data);

            SaveNameInput.text = worldToLoad;
        }
    }
}
