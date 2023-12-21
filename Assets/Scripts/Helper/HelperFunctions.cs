using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class HelperFunctions
{
    #region Math

    /// <summary>
    /// Modulo that handles negative values in a logical way.
    /// </summary>
    public static int Mod(int x, int m)
    {
        return (x % m + m) % m;
    }

    public static float SmoothLerp(float start, float end, float t)
    {
        t = Mathf.Clamp01(t);
        t = t * t * (3f - 2f * t);
        return Mathf.Lerp(start, end, t);
    }

    #endregion

    #region Random

    public static T GetWeightedRandomElement<T>(Dictionary<T, int> weightDictionary)
    {
        int probabilitySum = weightDictionary.Sum(x => x.Value);
        int rng = Random.Range(0, probabilitySum);
        int tmpSum = 0;
        foreach (KeyValuePair<T, int> kvp in weightDictionary)
        {
            tmpSum += kvp.Value;
            if (rng < tmpSum) return kvp.Key;
        }
        throw new System.Exception();
    }

    public static T GetWeightedRandomElement<T>(Dictionary<T, float> weightDictionary)
    {
        float probabilitySum = weightDictionary.Sum(x => x.Value);
        float rng = Random.Range(0, probabilitySum);
        float tmpSum = 0;
        foreach (KeyValuePair<T, float> kvp in weightDictionary)
        {
            tmpSum += kvp.Value;
            if (rng < tmpSum) return kvp.Key;
        }
        throw new System.Exception();
    }

    /// <summary>
    /// Returns a random number in a gaussian distribution. About 2/3 of generated numbers are within the standard deviation of the mean.
    /// </summary>
    public static float NextGaussian(float mean, float standard_deviation)
    {
        return mean + NextGaussian() * standard_deviation;
    }

    private static float NextGaussian()
    {
        float v1, v2, s;
        do
        {
            v1 = 2.0f * Random.Range(0f, 1f) - 1.0f;
            v2 = 2.0f * Random.Range(0f, 1f) - 1.0f;
            s = v1 * v1 + v2 * v2;
        } while (s >= 1.0f || s == 0f);
        s = Mathf.Sqrt((-2.0f * Mathf.Log(s)) / s);

        return v1 * s;
    }

    #endregion   

    #region UI

    /// <summary>
    /// Destroys all children of a GameObject immediately.
    /// </summary>
    public static void DestroyAllChildredImmediately(GameObject obj, int skipElements = 0)
    {
        int numChildren = obj.transform.childCount;
        for (int i = skipElements; i < numChildren; i++) GameObject.DestroyImmediate(obj.transform.GetChild(0).gameObject);
    }

    public static Sprite Texture2DToSprite(Texture2D tex)
    {
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }

    /// <summary>
    /// Sets the Left, Right, Top and Bottom attribute of a RectTransform
    /// </summary>
    public static void SetRectTransformMargins(RectTransform rt, float left, float right, float top, float bottom)
    {
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    public static void SetLeft(RectTransform rt, float left)
    {
        rt.offsetMin = new Vector2(left, rt.offsetMin.y);
    }

    public static void SetRight(RectTransform rt, float right)
    {
        rt.offsetMax = new Vector2(-right, rt.offsetMax.y);
    }

    public static void SetTop(RectTransform rt, float top)
    {
        rt.offsetMax = new Vector2(rt.offsetMax.x, -top);
    }

    public static void SetBottom(RectTransform rt, float bottom)
    {
        rt.offsetMin = new Vector2(rt.offsetMin.x, bottom);
    }

    #endregion
}
