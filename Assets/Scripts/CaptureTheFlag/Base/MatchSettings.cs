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

        // Real values used for match initialization
        public int Seed { get; private set; }
        public int WorldGeneratorIndex { get; private set; }
        public int WorldSize { get; private set; }

        public MatchSettings()
        {
            Seed = new System.Random().Next();
        }

        public MatchSettings(int[] settings)
        {
            Seed = settings[0];
            WorldGeneratorDropdownIndex = settings[1];
            WorldGeneratorIndex = settings[2];
            WorldSizeDropdownIndex = settings[3];
            WorldSize = settings[4];

            WorldGeneratorDropdownOption = WorldGeneratorDropdownIndex == 0 ? "Random" : CtfMatch.WorldGenerators[WorldGeneratorDropdownIndex - 1].Label;
            WorldSizeDropdownOption = WorldSizeDropdownIndex == 0 ? "Random" : CtfMatch.MapSizes.Keys.ToList()[WorldSizeDropdownIndex - 1];
        }

        public void SetWorldGeneratorDropdownIndex(int index)
        {
            WorldGeneratorDropdownIndex = index;
            WorldGeneratorDropdownOption = index == 0 ? "Random" : CtfMatch.WorldGenerators[index - 1].Label;

            if (index == 0) WorldGeneratorIndex = Random.Range(0, CtfMatch.WorldGenerators.Count);
            else WorldGeneratorIndex = index - 1;

        }

        public void SetMapSizeDropdownIndex(int index)
        {
            WorldSizeDropdownIndex = index;
            WorldSizeDropdownOption = index == 0 ? "Random" : CtfMatch.MapSizes.Keys.ToList()[index - 1];

            if (index == 0)
            {
                int worldSizeIndex = Random.Range(0, CtfMatch.MapSizes.Count);
                WorldSize = CtfMatch.MapSizes.Values.ToList()[worldSizeIndex];
            }
            else
            {
                WorldSize = CtfMatch.MapSizes.Values.ToList()[index - 1];
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
                WorldSize
            };
        }
    }
}
