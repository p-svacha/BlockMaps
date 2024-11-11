using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public static class StringExtensions
{
    public static string CapitalizeFirst(this string str)
    {
        // Check for null or empty string
        if (string.IsNullOrEmpty(str))
        {
            return str;
        }

        // Check if first letter is already capitalized
        if (char.IsUpper(str[0]))
        {
            return str;
        }

        // Handle single character strings
        if (str.Length == 1)
        {
            return str.ToUpper();
        }

        // Get the first letter index between tags if applicable
        int num = str.FirstLetterBetweenTags();

        // If the first letter is the starting character
        if (num == 0)
        {
            return char.ToUpper(str[num]) + str.Substring(num + 1);
        }

        // Otherwise, capitalize the first letter after the specified index
        return str.Substring(0, num) + char.ToUpper(str[num]) + str.Substring(num + 1);
    }

    public static int FirstLetterBetweenTags(this string str)
    {
        int num = 0;
        if (str[num] == '<' && str.IndexOf('>') > num && num < str.Length - 1 && str[num + 1] != '/')
        {
            num = str.IndexOf('>') + 1;
        }
        return num;
    }
}
