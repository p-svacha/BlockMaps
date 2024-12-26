using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    public class MatchSettings
    {
        public int ChosenWorldGeneratorIndex;
        public string ChosenWorldGeneratorOption;

        public int ChosenMapSizeIndex;
        public string ChosenMapSizeOption;

        public MatchSettings() { }
        public MatchSettings(int[] settings)
        {
            ChosenWorldGeneratorIndex = settings[0];
            ChosenMapSizeIndex = settings[1];
        }

        public int[] ToIntArray()
        {
            return new int[]
            {
                ChosenWorldGeneratorIndex,
                ChosenMapSizeIndex
            };
        }
    }
}
