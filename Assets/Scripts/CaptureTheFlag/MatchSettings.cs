using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    public class MatchSettings
    {
        public string ChosenWorldGeneratorOption;
        public WorldGenerator WorldGenerator;

        public string ChosenMapSizeOption;
        public int MapSize;

        public MatchSettings(string chosenWorldGeneratorOption, WorldGenerator worldGenerator, string chosenMapSizeOption, int mapSize)
        {
            ChosenWorldGeneratorOption = chosenWorldGeneratorOption;
            WorldGenerator = worldGenerator;
            ChosenMapSizeOption = chosenMapSizeOption;
            MapSize = mapSize;
        }
    }
}
