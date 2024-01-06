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

    #region Direction

    public static Direction GetNextClockwiseDirection8(Direction dir)
    {
        return dir switch
        {
            Direction.N => Direction.NE,
            Direction.NE => Direction.E,
            Direction.E => Direction.SE,
            Direction.SE => Direction.S,
            Direction.S => Direction.SW,
            Direction.SW => Direction.W,
            Direction.W => Direction.NW,
            Direction.NW => Direction.N,
            _ => throw new System.Exception("Direction " + dir.ToString() + " not handled")
        };
    }

    public static Direction GetNextAnticlockwiseDirection8(Direction dir)
    {
        return dir switch
        {
            Direction.N => Direction.NW,
            Direction.NW => Direction.W,
            Direction.W => Direction.SW,
            Direction.SW => Direction.S,
            Direction.S => Direction.SE,
            Direction.SE => Direction.E,
            Direction.E => Direction.NE,
            Direction.NE => Direction.N,
            _ => throw new System.Exception("Direction " + dir.ToString() + " not handled")
        };
    }

    public static Direction GetDirection(Vector2Int from, Vector2Int to)
    {
        if (to == from + new Vector2Int(1, 0)) return Direction.E;
        if (to == from + new Vector2Int(-1, 0)) return Direction.W;
        if (to == from + new Vector2Int(0, 1)) return Direction.N;
        if (to == from + new Vector2Int(0, -1)) return Direction.S;

        if (to == from + new Vector2Int(1, 1)) return Direction.NE;
        if (to == from + new Vector2Int(-1, 1)) return Direction.NW;
        if (to == from + new Vector2Int(1, -1)) return Direction.SE;
        if (to == from + new Vector2Int(-1, -1)) return Direction.SW;
        throw new System.Exception("Position is not adjacent to character position.");
    }

    public static Direction GetOppositeDirection(Direction dir)
    {
        return dir switch
        {
            Direction.N => Direction.S,
            Direction.E => Direction.W,
            Direction.S => Direction.N,
            Direction.W => Direction.E,
            Direction.NE => Direction.SW,
            Direction.NW => Direction.SE,
            Direction.SW => Direction.NE,
            Direction.SE => Direction.NW,
            _ => throw new System.Exception("Direction " + dir.ToString() + " not handled")
        };
    }

    public static List<Direction> GetAllDirections8() => new List<Direction>() { Direction.N, Direction.NE, Direction.E, Direction.SE, Direction.S, Direction.SW, Direction.W, Direction.NW };
    public static List<Direction> GetCorners() => new List<Direction>() { Direction.SW, Direction.SE, Direction.NE, Direction.NW };
    public static List<Direction> GetSides() => new List<Direction>() { Direction.N, Direction.E, Direction.S, Direction.W };
    public static bool IsCorner(Direction dir) => GetCorners().Contains(dir);
    public static bool IsSide(Direction dir) => GetSides().Contains(dir);

    /// <summary>
    /// Returns the corner directions that are relevant for a given direction.
    /// </summary>
    public static List<Direction> GetAffectedCorners(Direction dir)
    {
        if (dir == Direction.None) return new List<Direction> { Direction.NE, Direction.NW, Direction.SW, Direction.SE };
        if (dir == Direction.N) return new List<Direction> { Direction.NE, Direction.NW };
        if (dir == Direction.E) return new List<Direction> { Direction.NE, Direction.SE };
        if (dir == Direction.S) return new List<Direction> { Direction.SW, Direction.SE };
        if (dir == Direction.W) return new List<Direction> { Direction.SW, Direction.NW };
        if (dir == Direction.NW) return new List<Direction>() { Direction.NW };
        if (dir == Direction.NE) return new List<Direction>() { Direction.NE };
        if (dir == Direction.SE) return new List<Direction>() { Direction.SE };
        if (dir == Direction.SW) return new List<Direction>() { Direction.SW };
        throw new System.Exception("Direction " + dir.ToString() + " not handled");
    }
    public static bool DoAffectedCornersOverlap(Direction dir1, Direction dir2) => GetAffectedCorners(dir1).Intersect(GetAffectedCorners(dir2)).Any();

    public static List<Direction> GetAffectedSides(Direction dir)
    {
        if (dir == Direction.None) return new List<Direction> { Direction.N, Direction.E, Direction.S, Direction.W };
        if (dir == Direction.N) return new List<Direction> { Direction.N };
        if (dir == Direction.E) return new List<Direction> { Direction.E };
        if (dir == Direction.S) return new List<Direction> { Direction.S };
        if (dir == Direction.W) return new List<Direction> { Direction.W };
        if (dir == Direction.NW) return new List<Direction>() { Direction.N, Direction.W };
        if (dir == Direction.NE) return new List<Direction>() { Direction.N, Direction.E };
        if (dir == Direction.SE) return new List<Direction>() { Direction.S, Direction.E };
        if (dir == Direction.SW) return new List<Direction>() { Direction.S, Direction.W };
        throw new System.Exception("Direction " + dir.ToString() + " not handled");
    }

    public static List<Direction> GetAffectedDirections(Direction dir)
    {
        if (dir == Direction.None) return GetAllDirections8();
        if (dir == Direction.N) return new List<Direction> { Direction.NW, Direction.N, Direction.NE };
        if (dir == Direction.E) return new List<Direction> { Direction.NE, Direction.E, Direction.SE };
        if (dir == Direction.S) return new List<Direction> { Direction.SW, Direction.S, Direction.SE };
        if (dir == Direction.W) return new List<Direction> { Direction.NW, Direction.W, Direction.SW };
        if (dir == Direction.NW) return new List<Direction>() { Direction.NW, Direction.N, Direction.W };
        if (dir == Direction.NE) return new List<Direction>() { Direction.NE, Direction.N, Direction.E };
        if (dir == Direction.SE) return new List<Direction>() { Direction.SE, Direction.S, Direction.E };
        if (dir == Direction.SW) return new List<Direction>() { Direction.SW, Direction.S, Direction.W };
        throw new System.Exception("Direction " + dir.ToString() + " not handled");
    }

    public static Direction GetMirroredCorner(Direction dir, Direction axis)
    {
        if(axis == Direction.N || axis == Direction.S) // east,west stays the same
        {
            if (dir == Direction.NE) return Direction.SE;
            if (dir == Direction.NW) return Direction.SW;
            if (dir == Direction.SW) return Direction.NW;
            if (dir == Direction.SE) return Direction.NE;
        }
        if (axis == Direction.E || axis == Direction.W) // north,south stays the same
        {
            if (dir == Direction.NE) return Direction.NW;
            if (dir == Direction.NW) return Direction.NE;
            if (dir == Direction.SW) return Direction.SE;
            if (dir == Direction.SE) return Direction.SW;
        }
        throw new System.Exception("axis " + axis.ToString() + " not handled or direction " + dir.ToString() + " not handled");
    }

    /// <summary>
    /// Returns the heights for a flat surface based on its height.
    /// </summary>
    public static Dictionary<Direction, int> GetFlatHeights(int height)
    {
        Dictionary<Direction, int> heights = new Dictionary<Direction, int>();
        foreach (Direction dir in GetCorners()) heights.Add(dir, height);
        return heights;
    }

    /// <summary>
    /// Returns the heights for a sloped surface based on its upwards direction and base height.
    /// </summary>
    public static Dictionary<Direction, int> GetSlopeHeights(int baseHeight, Direction dir)
    {
        Dictionary<Direction, int> heights = new Dictionary<Direction, int>();
        foreach (Direction corner in GetAffectedCorners(dir))
        {
            heights.Add(corner, baseHeight + 1);
            heights.Add(GetOppositeDirection(corner), baseHeight);
        }
        return heights;
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
