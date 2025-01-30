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
        public TMP_InputField NumChunksInput;

        public TMP_InputField SeedInput;
        public Toggle SeedRandomizeToggle;

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
            List<string> generators = editor.Generators.Select(x => x.Label).ToList();
            GeneratorDropdown.AddOptions(generators);

            UpdateLoadWorldDropdown();
        }

        public override void UpdateTool()
        {
            if (ActiveGenerator != null)
            {
                if (ActiveGenerator.IsDone)
                {
                    Editor.SetAndInitializeWorld(ActiveGenerator.World, OnWorldInitializationDone);
                    ActiveGenerator = null;
                }
                else ActiveGenerator.UpdateGeneration();
            }
        }

        private void UpdateLoadWorldDropdown(string initValue = "")
        {
            LoadDropdown.ClearOptions();

            string[] fullPaths = Directory.GetFiles(SaveLoadManager.SaveDataPath, "*.xml");
            SavedWorlds = fullPaths.Select(x => System.IO.Path.GetFileNameWithoutExtension(x)).ToList();
            LoadDropdown.AddOptions(SavedWorlds);

            if (initValue != "") LoadDropdown.value = LoadDropdown.options.IndexOf(LoadDropdown.options.First(x => x.text == initValue));
        }

        public void GenerateButton_OnClick()
        {
            if (ActiveGenerator != null) return; // Disabled while in generation process

            if (NumChunksInput.text == "") return;
            int numChunks = int.Parse(NumChunksInput.text);

            if (World.ChunkSize * numChunks > WorldGenerator.MAX_WORLD_SIZE) return;
            if (numChunks <= 0) return;

            if (World != null) Editor.DestroyWorld();
            WorldGenerator selectedGenerator = Editor.Generators[GeneratorDropdown.value];

            ActiveGenerator = selectedGenerator;
            bool generateRandomSeed = SeedRandomizeToggle.isOn || SeedInput.text == "";
            int seed = generateRandomSeed ? WorldGenerator.GetRandomSeed() : int.Parse(SeedInput.text);
            SeedInput.text = seed.ToString(); // Put seed in seed input field
            ActiveGenerator.StartGeneration(numChunks, seed);
        }

        private void OnWorldInitializationDone()
        {
            // Default draw mode
            World.ShowTextures(true);
            World.ShowTileBlending(true);
        }

        private void SaveButton_OnClick()
        {
            if (ActiveGenerator != null) return; // Disabled while in generation process
            if (SaveNameInput.text == "") return;

            World.Name = SaveNameInput.text;
            SaveLoadManager.Save(Editor.World, SaveNameInput.text);
            UpdateLoadWorldDropdown(initValue: SaveNameInput.text);
        }

        private void LoadButton_OnClick()
        {
            
            if (ActiveGenerator != null) return; // Disabled while in generation process
            if (SavedWorlds.Count == 0) return;

            string worldToLoad = SavedWorlds[LoadDropdown.value];

            World loadedWorld = SaveLoadManager.Load(worldToLoad);
            Editor.SetAndInitializeWorld(loadedWorld, OnWorldInitializationDone);

            SaveNameInput.text = worldToLoad;
            
        }
    }
}
