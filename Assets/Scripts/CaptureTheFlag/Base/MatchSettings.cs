using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag
{
    public class MatchSettings
    {
        // Lobby dropdown inputs
        public int WorldGeneratorDropdownIndex { get; private set; }
        public string WorldGeneratorDropdownOption { get; private set; }

        public int WorldSizeDropdownIndex { get; private set; }
        public string WorldSizeDropdownOption { get; private set; }

        public int SpawnTypeDropdownIndex { get; private set; }
        public string SpawnTypeDropdownOption { get; private set; }

        // Real values used for match initialization
        public int Seed { get; private set; }
        public int WorldGeneratorIndex { get; private set; }
        public int WorldSize { get; private set; }
        public CharacterSpawnType SpawnType { get; private set; }

        public MatchSettings()
        {
            Seed = new System.Random().Next();

            SetWorldGeneratorDropdownIndex(0);
            SetMapSizeDropdownIndex(0);
            SetSpawnTypeDropdownIndex(0);
        }

        public MatchSettings(int[] settings)
        {
            Seed = settings[0];
            WorldGeneratorDropdownIndex = settings[1];
            WorldGeneratorIndex = settings[2];
            WorldSizeDropdownIndex = settings[3];
            WorldSize = settings[4];
            SpawnTypeDropdownIndex = settings[5];
            SpawnType = (CharacterSpawnType)settings[6];

            WorldGeneratorDropdownOption = WorldGeneratorDropdownIndex == 0 ? "Random" : CtfMatch.WorldGenerators[WorldGeneratorDropdownIndex - 1].Label;
            WorldSizeDropdownOption = WorldSizeDropdownIndex == 0 ? "Random" : CtfMatch.MapSizes.Keys.ToList()[WorldSizeDropdownIndex - 1];
            SpawnTypeDropdownOption = SpawnTypeDropdownIndex == 0 ? "Random" : HelperFunctions.GetEnumDescription((CharacterSpawnType)(SpawnTypeDropdownIndex - 1));
        }

        public void SetWorldGeneratorDropdownIndex(int index)
        {
            WorldGeneratorDropdownIndex = index;
            WorldGeneratorDropdownOption = index == 0 ? "Random" : CtfMatch.WorldGenerators[index - 1].Label;

            if (index == 0) WorldGeneratorIndex = new System.Random().Next(CtfMatch.WorldGenerators.Count);
            else WorldGeneratorIndex = index - 1;
        }

        public void SetMapSizeDropdownIndex(int index)
        {
            WorldSizeDropdownIndex = index;
            WorldSizeDropdownOption = index == 0 ? "Random" : CtfMatch.MapSizes.Keys.ToList()[index - 1];

            if (index == 0)
            {
                int worldSizeIndex = new System.Random().Next(CtfMatch.MapSizes.Count);
                WorldSize = CtfMatch.MapSizes.Values.ToList()[worldSizeIndex];
            }
            else
            {
                WorldSize = CtfMatch.MapSizes.Values.ToList()[index - 1];
            }
        }

        public void SetSpawnTypeDropdownIndex(int index)
        {
            SpawnTypeDropdownIndex = index;
            SpawnTypeDropdownOption = index == 0 ? "Random" : HelperFunctions.GetEnumDescription((CharacterSpawnType)(index - 1));

            if (index == 0)
            {
                SpawnType = HelperFunctions.GetRandomEnumValue<CharacterSpawnType>();
            }
            else
            {
                SpawnType = (CharacterSpawnType)(index - 1);
            }
        }

        public int[] ToIntArray()
        {
            return new int[]
            {
                Seed,
                WorldGeneratorDropdownIndex,
                WorldGeneratorIndex,
                WorldSizeDropdownIndex,
                WorldSize,
                SpawnTypeDropdownIndex,
                (int)SpawnType,
            };
        }
    }
}
