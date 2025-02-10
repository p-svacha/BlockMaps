using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    public class TimeStamp
    {
        public int ValueInSeconds;

        public const int GAME_START_DAY = 1;
        public const int GAME_START_HOUR = 8;
        public const int GAME_START_MINUTE = 0;

        public TimeStamp(int seconds = 0)
        {
            ValueInSeconds = seconds;
        }

        public void IncreaseTime(int seconds)
        {
            ValueInSeconds += seconds;
        }
        public void SetTime(int secondsAbsolute)
        {
            ValueInSeconds = secondsAbsolute;
        }

        public string GetAsString(bool includeDays = true, bool includeHours = true, bool includeMinutes = true, bool includeSeconds = false)
        {
            int seconds = ValueInSeconds % 60;
            int minutes = (GAME_START_MINUTE + (ValueInSeconds / 60)) % 60;
            int hours = (GAME_START_HOUR + (ValueInSeconds / 60 / 60)) % 24;
            int day = GAME_START_DAY + ((GAME_START_HOUR + (ValueInSeconds / 60 / 60)) / 24);

            string txt = "";
            if (includeDays) txt += $"Day {day}";
            if (includeDays && includeHours) txt += ", ";
            if (includeHours) txt += $"{hours.ToString("00")}";
            if (includeHours && includeMinutes) txt += ":";
            if (includeMinutes) txt += $"{minutes.ToString("00")}";
            if (includeMinutes && includeSeconds) txt += ":";
            if (includeSeconds) txt += $"{seconds.ToString("00")}";

            return txt;
        }

        // Operator Overloads
        public static TimeStamp operator +(TimeStamp a, int seconds)
        {
            return new TimeStamp(a.ValueInSeconds + seconds);
        }

        public static TimeStamp operator -(TimeStamp a, int seconds)
        {
            return new TimeStamp(a.ValueInSeconds - seconds);
        }

        public static TimeStamp operator +(TimeStamp a, TimeStamp b)
        {
            return new TimeStamp(a.ValueInSeconds + b.ValueInSeconds);
        }

        public static TimeStamp operator -(TimeStamp a, TimeStamp b)
        {
            return new TimeStamp(a.ValueInSeconds - b.ValueInSeconds);
        }
    }
}
