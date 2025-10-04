using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework.WorldGeneration
{
    public abstract class ParcelGenerator
    {
        protected World World { get; private set; }
        protected ParcelGenDef Def { get; private set; }
        protected Parcel Parcel { get; private set; }
        protected List<GatewayInfo> Gateways { get; private set; }
        protected List<BorderInfo> Borders { get; private set; }
        private Dictionary<Vector2Int, List<BorderInfo>> BordersByCoordinate;

        public void FillParcel(World world, ParcelGenDef def, Parcel parcel, List<GatewayInfo> gates, List<BorderInfo> borders)
        {
            World = world;
            Def = def;
            Parcel = parcel;
            Gateways = gates ?? new List<GatewayInfo>();
            Borders = borders ?? new List<BorderInfo>();
            BuildBorderIndex();
            Generate();
            // MarkGateways(); // debug helper
        }

        protected abstract void Generate();

        protected void CreateGround(int altitude, SurfaceDef surface)
        {
            for (int x = Parcel.Position.x; x < Parcel.Position.x + Parcel.Dimensions.x; x++)
            {
                for (int y = Parcel.Position.y; y < Parcel.Position.y + Parcel.Dimensions.y; y++)
                {
                    World.BuildAirNode(new Vector2Int(x, y), altitude, surface, updateWorld: false);
                }
            }
        }

        protected void MarkGateways()
        {
            for (int i = 0; i < Gateways.Count; i++)
            {
                List<Vector2Int> coords = Gateways[i].GetGatewayCoordinates();
                for (int k = 0; k < coords.Count; k++)
                {
                    AirNode nodeToMark = World.GetAirNodes(coords[k]).First();
                    SurfaceDef surface = Gateways[i].IsFullyOpenGateway ? SurfaceDefOf.Grass : SurfaceDefOf.Sidewalk;

                    nodeToMark.SetSurface(surface);
                }
            }
        }

        #region BorderInfo

        private void BuildBorderIndex()
        {
            BordersByCoordinate = new Dictionary<Vector2Int, List<BorderInfo>>();

            for (int i = 0; i < Borders.Count; i++)
            {
                BorderInfo info = Borders[i];
                List<Vector2Int> coords = info.GetBorderCoordinates();
                for (int k = 0; k < coords.Count; k++)
                {
                    Vector2Int c = coords[k];
                    if (!BordersByCoordinate.TryGetValue(c, out List<BorderInfo> list))
                    {
                        list = new List<BorderInfo>();
                        BordersByCoordinate[c] = list;
                    }
                    list.Add(info);
                }
            }
        }



        #endregion

        #region Getters

        /// <summary>
        /// Returns all border infos that touch this world coordinate from inside the parcel.
        /// Corners yield 2, edges 1, interior 0.
        /// </summary>
        protected List<BorderInfo> GetBorderInfos(Vector2Int worldCoords)
        {
            if (BordersByCoordinate != null && BordersByCoordinate.TryGetValue(worldCoords, out List<BorderInfo> list))
                return list;
            return new List<BorderInfo>(0);
        }

        protected bool IsBorder(Vector2Int worldCoords) => GetBorderInfos(worldCoords).Count > 0;

        protected bool IsGateway(Vector2Int worldCoords) => Gateways.Any(x => x.GetGatewayCoordinates().Contains(worldCoords));
        protected List<GatewayInfo> GetGateways(Vector2Int worldCoords) => Gateways.Where(x => x.GetGatewayCoordinates().Contains(worldCoords)).ToList();

        #endregion
    }
}
