using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.WorldGeneration
{
    public class ParcelWorldGenerator_Pool : ParcelWorldGenerator
    {
        public override string Label => "fy_pool_day (Parcel)";
        public override string Description => "Generator for creating fy_pool_day like worlds with a parcel system";
        public override bool StartAsVoid => true;

        public static int BASE_ALTITUDE = 10;

        // Areas
        private const string WARDROBE = "Wardrobe";
        private const string POOLAREA = "PoolArea";
        private const string SHOWERS = "Showers";

        protected override List<ParcelGenDef> GetParcelGenDefs()
        {
            return new List<ParcelGenDef>()
            {
                new ParcelGenDef()
                {
                    DefName = WARDROBE,
                    GeneratorClass = typeof(ParcelGenerator_Wardrobe),
                    MinSizeShortSide = 8,
                    MinSizeLongSide = 20,
                    MaxSizeShortSide = 12,
                    MaxSizeLongSide = 25,
                },
                
                new ParcelGenDef()
                {
                    DefName = POOLAREA,
                    GeneratorClass = typeof(ParcelGenerator_PoolArea),
                    MinSizeShortSide = 16,
                    MinSizeLongSide = 20,
                    MaxSizeShortSide = 32,
                    MaxSizeLongSide = 40,
                },

                new ParcelGenDef()
                {
                    DefName = SHOWERS,
                    GeneratorClass = typeof(ParcelGenerator_Showers),
                    MinSizeShortSide = 5,
                    MinSizeLongSide = 10,
                    MaxSizeShortSide = 10,
                    MaxSizeLongSide = 20,
                },
            };
        }

        protected override List<GatewayDef> GetGatewayDefs()
        {
            return new List<GatewayDef>()
            {
                new GatewayDef()
                {
                    ParcelGenDef1 = WARDROBE,
                    ParcelGenDef2 = POOLAREA,
                    MinSize = 3,
                },

                new GatewayDef()
                {
                    ParcelGenDef1 = WARDROBE,
                    ParcelGenDef2 = SHOWERS,
                    MinSize = 3,
                },

                new GatewayDef()
                {
                    ParcelGenDef1 = SHOWERS,
                    ParcelGenDef2 = POOLAREA,
                    MinSize = 2,
                },
            };
        }
    }
}
