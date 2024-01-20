using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BlockmapFramework
{
    /// <summary>
    /// A PathLine represents a cross-section of a path at a certain position that stretches across the path from the left end to the right end.
    /// The elevation is the same across all of a path line.
    /// </summary>
    public class PathLine
    {
        /// <summary>
        /// Center point of the PathLine. Point in the middle of the path.
        /// </summary>
        public Vector3 Center { get; private set; }

        /// <summary>
        /// Leftmost point on the PathLine.
        /// </summary>
        public Vector3 Left { get; private set; }

        /// <summary>
        /// Rightmost point on the PathLine.
        /// </summary>
        public Vector3 Right { get; private set; }

        /// <summary>
        /// World angle in degrees towards the next path line.
        /// </summary>
        public float Angle { get; private set; }

        /// <summary>
        /// Width of the path from left to right.
        /// </summary>
        public float Width { get; private set; }

        public PathLine(Vector3 center, float angle, float width)
        {
            Center = center;
            Angle = angle;
            Width = width;
            Left = GetPosition(0f);
            Right = GetPosition(1f);
        }

        /// <summary>
        /// Returns a position on the PathLine, whereas relativePos is the factor (0-1) that defines the position 
        /// <br/> from the furthest left point (0) to furthest right point (1) regarding the width. 
        /// </summary>
        public Vector3 GetPosition(float relativePos)
        {
            float relativeWidth = -(Width / 2) + (Width * relativePos);
            return new Vector3(
                Center.x + (relativeWidth * Mathf.Sin(Mathf.Deg2Rad * (Angle + 90))),
                Center.y,
                Center.z + (relativeWidth * Mathf.Cos(Mathf.Deg2Rad * (Angle + 90)))
                );
        }
    }
}
