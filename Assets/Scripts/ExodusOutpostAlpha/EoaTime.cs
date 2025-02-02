using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ExodusOutposAlpha
{
    public class EoaTime
    {
        public int ValueInSeconds;

        public const int GAME_START_HOUR = 8;
        public const int GAME_START_MINUTE = 0;

        public void IncreaseTime(int seconds)
        {
            ValueInSeconds += seconds;
        }
        public void SetTime(int secondsAbsolute)
        {
            ValueInSeconds = secondsAbsolute;
        }

        public string GetAbsoluteTimeString()
        {
            int seconds = ValueInSeconds % 60;
            int minutes = (GAME_START_MINUTE + (ValueInSeconds / 60)) % 60;
            int hours = (GAME_START_HOUR + (ValueInSeconds / 60 / 60)) % 24;
            int day = (GAME_START_HOUR + (ValueInSeconds / 60 / 60)) / 24;

            return $"Day {day}, {hours.ToString("00")}:{minutes.ToString("00")}:{seconds.ToString("00")}";
        }
    }
}
