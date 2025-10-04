using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.WorldGeneration
{
    /// <summary>
    /// Border segment along a parcel side that touches another parcel.
    /// Mirrors GatewayInfo but represents the full shared edge segment.
    /// </summary>
    public class BorderInfo
    {
        public Parcel SourceParcel { get; init; }
        public Parcel TargetParcel { get; init; }
        public ParcelGenDef TargetParcelGenDef { get; init; }
        public Direction Side { get; init; }
        public int Offset { get; init; }   // tiles from SourceParcel local origin along Side
        public int Length { get; init; }   // number of tiles on this border segment

        public bool IsWorldPerimeterBorder => TargetParcel == null;

        /// <summary>
        /// World-space interior coordinates of this border segment on the SourceParcel (one-tile thick).
        /// </summary>
        public List<Vector2Int> GetBorderCoordinates()
        {
            Parcel p = SourceParcel;
            List<Vector2Int> coords = new List<Vector2Int>(Length);

            switch (Side)
            {
                case Direction.N:
                    {
                        int y = p.MaxY - 1;
                        int startX = p.MinX + Offset;
                        for (int i = 0; i < Length; i++) coords.Add(new Vector2Int(startX + i, y));
                        break;
                    }
                case Direction.S:
                    {
                        int y = p.MinY;
                        int startX = p.MinX + Offset;
                        for (int i = 0; i < Length; i++) coords.Add(new Vector2Int(startX + i, y));
                        break;
                    }
                case Direction.E:
                    {
                        int x = p.MaxX - 1;
                        int startY = p.MinY + Offset;
                        for (int i = 0; i < Length; i++) coords.Add(new Vector2Int(x, startY + i));
                        break;
                    }
                case Direction.W:
                    {
                        int x = p.MinX;
                        int startY = p.MinY + Offset;
                        for (int i = 0; i < Length; i++) coords.Add(new Vector2Int(x, startY + i));
                        break;
                    }
            }
            return coords;
        }

        /// <summary>
        /// The immediately adjacent coordinates on the neighbor side.
        /// </summary>
        public List<Vector2Int> GetOppositeCoordinates()
        {
            List<Vector2Int> inside = GetBorderCoordinates();
            Vector2Int delta = Side switch
            {
                Direction.N => new Vector2Int(0, +1),
                Direction.S => new Vector2Int(0, -1),
                Direction.E => new Vector2Int(+1, 0),
                Direction.W => new Vector2Int(-1, 0),
                _ => Vector2Int.zero
            };
            List<Vector2Int> list = new List<Vector2Int>(inside.Count);
            for (int i = 0; i < inside.Count; i++) list.Add(inside[i] + delta);
            return list;
        }
    }
}
