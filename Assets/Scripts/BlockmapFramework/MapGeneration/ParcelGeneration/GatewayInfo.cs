using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.WorldGeneration
{
    /// <summary>
    /// Object containing information about a specific gateway on a specific parcel created in a ParcelWorldGenerator.
    /// </summary>
    public class GatewayInfo
    {
        /// <summary>
        /// The parcel this gateway is on leading outwards.
        /// </summary>
        public Parcel SourceParcel { get; init; }

        /// <summary>
        /// The parcel this gateway is leading towards.
        /// </summary>
        public Parcel TargetParcel { get; init; }

        /// <summary>
        /// The side this gateway is on on the source parcel.
        /// </summary>
        public Direction Side { get; init; }

        /// <summary>
        /// Tiles from the parcel's (local) origin along that side.
        /// </summary>
        public int Offset { get; init; }

        /// <summary>
        /// Width of the opening along that side (>= 1)
        /// </summary>
        public int Length { get; init; }

        /// <summary>
        /// Flag if this gateway represents a fully open connection on a parcel border.
        /// </summary>
        public bool IsFullyOpenGateway { get; init; }

        /// <summary>
        /// Returns the world-space coordinates of the tiles inside the SourceParcel
        /// that lie directly along this gateway opening (one tile thick).
        /// </summary>
        public List<Vector2Int> GetGatewayCoordinates()
        {
            var p = SourceParcel;
            var coords = new List<Vector2Int>(Length);

            switch (Side)
            {
                case Direction.N:
                    {
                        int y = p.MaxY - 1; // top interior row
                        int startX = p.MinX + Offset;
                        for (int i = 0; i < Length; i++) coords.Add(new Vector2Int(startX + i, y));
                        break;
                    }
                case Direction.S:
                    {
                        int y = p.MinY; // bottom interior row
                        int startX = p.MinX + Offset;
                        for (int i = 0; i < Length; i++) coords.Add(new Vector2Int(startX + i, y));
                        break;
                    }
                case Direction.E:
                    {
                        int x = p.MaxX - 1; // right interior column
                        int startY = p.MinY + Offset;
                        for (int i = 0; i < Length; i++) coords.Add(new Vector2Int(x, startY + i));
                        break;
                    }
                case Direction.W:
                    {
                        int x = p.MinX; // left interior column
                        int startY = p.MinY + Offset;
                        for (int i = 0; i < Length; i++) coords.Add(new Vector2Int(x, startY + i));
                        break;
                    }
            }
            return coords;
        }
    }
}
