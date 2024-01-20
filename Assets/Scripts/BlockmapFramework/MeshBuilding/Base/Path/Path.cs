using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class Path
    {
        public List<PathLine> Points;

        public Path(List<PathLine> points)
        {
            Points = points;
        }

        public void DebugPath(Vector3 offset)
        {
            for (int i = 1; i < Points.Count; i++)
            {
                // center
                Debug.DrawLine(offset + Points[i - 1].Center, offset + Points[i].Center, Color.red, 20f, false);

                // bounds
                Debug.DrawLine(offset + Points[i - 1].Left, offset + Points[i].Left, Color.blue, 20f, false);
                Debug.DrawLine(offset + Points[i - 1].Right, offset + Points[i].Right, Color.blue, 20f, false);
            }
        }
    }
}
