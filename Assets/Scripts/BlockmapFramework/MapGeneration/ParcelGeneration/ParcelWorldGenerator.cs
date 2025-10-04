using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework.WorldGeneration
{
    /// <summary>
    /// An XCOM inspired map generator that first splits the world into different rectangular 2d parcels of certain types.
    /// <br/>Parcels then get filled by their own parcel generators based on the type.
    /// </summary>
    public abstract class ParcelWorldGenerator : WorldGenerator
    {
        /// <summary>
        /// The list of all ParcelGenDefs that can appear in this world. Called once at the start of the generation.
        /// </summary>
        protected abstract List<ParcelGenDef> GetParcelGenDefs();

        /// <summary>
        /// Cached list of all ParcelGenDefs that can appear in the world.
        /// </summary>
        private List<ParcelGenDef> ParcelGenDefs;

        /// <summary>
        /// The list of all connections between ParcelGenDefs that can appear in this world. Called once at the start of the generation.
        /// </summary>
        protected abstract List<GatewayDef> GetGatewayDefs();

        /// <summary>
        /// Cached performant map containing all gateway options.
        /// </summary>
        private Dictionary<(string, string), GatewayDef> GatewayMap;



        /// <summary>
        /// Map containing all parcels in the world (key) with information of what kind of region (ParcelGenDef) should be generated in that parcel. 
        /// </summary>
        private Dictionary<Parcel, ParcelGenDef> Parcels;

        /// <summary>
        /// List containing all parcel borders.
        /// </summary>
        private List<ParcelBorder> ParcelBorders;

        /// <summary>
        /// Map containing all parcel borders, grouped by parcel.
        /// </summary>
        private Dictionary<Parcel, List<ParcelBorder>> BordersByParcel;

        /// <summary>
        /// Map containing all gateways, grouped by parcel.
        /// </summary>
        private Dictionary<Parcel, List<GatewayInfo>> GatewaysByParcel;

        /// <summary>
        /// All border infos grouped by parcel.
        /// </summary>
        private Dictionary<Parcel, List<BorderInfo>> BorderInfosByParcel;


        protected override List<System.Action> GetGenerationSteps()
        {
            return new List<System.Action>()
            {
                SplitMapIntoParcels,
                IdentifyBorders,
                AssignParcels,
                BuildBorderInfos,
                PlanGateways,
                FillParcels,
            };
        }

        #region Steps

        protected override void OnGenerationStart()
        {
            Parcels = new Dictionary<Parcel, ParcelGenDef>();
            ParcelBorders = new List<ParcelBorder>();
            BordersByParcel = new Dictionary<Parcel, List<ParcelBorder>>();
            BorderInfosByParcel = new Dictionary<Parcel, List<BorderInfo>>();

            ParcelGenDefs = GetParcelGenDefs();

            // Build gateway map
            GatewayMap = new Dictionary<(string, string), GatewayDef>();
            List<GatewayDef> gatewayDefs = GetGatewayDefs();
            foreach (var g in gatewayDefs)
            {
                GatewayMap[(g.ParcelGenDef1, g.ParcelGenDef2)] = g;
                GatewayMap[(g.ParcelGenDef2, g.ParcelGenDef1)] = g;
            }
        }

        /// <summary>
        /// Splits the whole world into individual rectangular parcels.
        /// </summary>
        private void SplitMapIntoParcels()
        {
            SplitMapIntoParcels(WorldSize, WorldSize);
        }

        /// <summary>
        /// Assigns each parcel a ParcelGenDef that defines what kind of region is generated in that parcel.
        /// </summary>
        private void AssignParcels()
        {
            List<Parcel> parcels = Parcels.Keys.ToList();
            // Precompute eligible defs per parcel
            var eligible = parcels.ToDictionary(
                p => p,
                p => ParcelGenDefs.Where(d => d.DoesFulfillConstraints(p.Dimensions)).ToList()
            );

            // Sanity: if any parcel has no eligible defs, resplit or adjust min sizes. For now, throw:
            foreach (var kv in eligible)
                if (kv.Value.Count == 0)
                    throw new System.Exception($"No eligible ParcelGenDef fits parcel {kv.Key.Position} {kv.Key.Dimensions}");

            // Greedy MRV (smallest domain first) + weighted choice
            foreach (var p in parcels.OrderBy(p => eligible[p].Count))
            {
                var candidates = eligible[p];
                var weightTable = candidates.ToDictionary(x => x, x => x.Commonness);
                Parcels[p] = weightTable.GetWeightedRandomElement();
            }
        }

        /// <summary>
        /// Fills all parcels by applying the ParcelGenerator for each parcel.
        /// </summary>
        private void FillParcels()
        {
            foreach (KeyValuePair<Parcel, ParcelGenDef> p in Parcels)
            {
                Parcel parcel = p.Key;
                ParcelGenDef def = p.Value;
                ParcelGenerator generator = (ParcelGenerator)System.Activator.CreateInstance(def.GeneratorClass);
                List<GatewayInfo> gates = GatewaysByParcel[parcel];
                List<BorderInfo> borders = BorderInfosByParcel[parcel];
                generator.FillParcel(World, def, parcel, gates, borders);
            }
        }

        #endregion

        #region Binary Space Partitioning

        /// <summary>
        /// Splits the maps into random rectangular parcels.
        /// </summary>
        private void SplitMapIntoParcels(int width, int height)
        {
            Vector2Int initialPosition = new Vector2Int(0, 0);
            Vector2Int initialDimensions = new Vector2Int(width, height);
            SplitParcel(initialPosition, initialDimensions);
        }

        /// <summary>
        /// Splits a parcel into 2 smaller parcels along an axis, broadly respecting constraints of the defined ParcelGenDefs.
        /// </summary>
        private void SplitParcel(Vector2Int position, Vector2Int dimensions)
        {
            if (!HasEligibleParcelGenDefs(dimensions))
            {
                if (!CouldBecomeEligibleBySplitting(dimensions))
                {
                    Debug.Log($"Dead rect (no mins reachable): {position} - {dimensions}");
                    Parcels.Add(new Parcel(position, dimensions), null);
                    return;
                }
            }
            else
            {
                // Do NOT stop if every plausible def is exceeded by current dims.
                if (!MustSplitByMax(dimensions) && PreferStop(dimensions))
                {
                    Parcels.Add(new Parcel(position, dimensions), null);
                    return;
                }
            }

            List<int> splitsX = ValidSplitsX(dimensions);
            List<int> splitsY = ValidSplitsY(dimensions);

            if (splitsX.Count == 0 && splitsY.Count == 0)
            {
                Parcels.Add(new Parcel(position, dimensions), null);
                return;
            }

            bool doX;
            if (splitsX.Count == 0) doX = false;
            else if (splitsY.Count == 0) doX = true;
            else
            {
                float px = splitsX.Count / (float)(splitsX.Count + splitsY.Count);
                doX = Random.value < px;
            }

            if (doX)
            {
                int splitX = splitsX[Random.Range(0, splitsX.Count)];
                Vector2Int leftDims = new Vector2Int(splitX, dimensions.y);
                Vector2Int rightDims = new Vector2Int(dimensions.x - splitX, dimensions.y);

                SplitParcel(position, leftDims);
                SplitParcel(new Vector2Int(position.x + splitX, position.y), rightDims);
            }
            else
            {
                int splitY = splitsY[Random.Range(0, splitsY.Count)];
                Vector2Int bottomDims = new Vector2Int(dimensions.x, splitY);
                Vector2Int topDims = new Vector2Int(dimensions.x, dimensions.y - splitY);

                SplitParcel(position, bottomDims);
                SplitParcel(new Vector2Int(position.x, position.y + splitY), topDims);
            }
        }



        /// <summary>
        /// Checks and returns if any defined ParcelGenDef could be placed on a parcel with the given dimensions.
        /// </summary>
        private bool HasEligibleParcelGenDefs(Vector2Int dimensions)
        {
            return ParcelGenDefs.Any(d => d.DoesFulfillConstraints(dimensions));
        }

        private List<int> ValidSplitsX(Vector2Int dimensions)
        {
            var list = new List<int>();
            for (int splitX = 1; splitX <= dimensions.x - 1; splitX++)
            {
                var left = new Vector2Int(splitX, dimensions.y);
                var right = new Vector2Int(dimensions.x - splitX, dimensions.y);
                if (CouldBecomeEligibleBySplitting(left) && CouldBecomeEligibleBySplitting(right))
                    list.Add(splitX);
            }
            return list;
        }

        private List<int> ValidSplitsY(Vector2Int dimensions)
        {
            var list = new List<int>();
            for (int splitY = 1; splitY <= dimensions.y - 1; splitY++)
            {
                var bottom = new Vector2Int(dimensions.x, splitY);
                var top = new Vector2Int(dimensions.x, dimensions.y - splitY);
                if (CouldBecomeEligibleBySplitting(bottom) && CouldBecomeEligibleBySplitting(top))
                    list.Add(splitY);
            }
            return list;
        }

        /// True if this rectangle could be split down to satisfy at least one ParcelGenDef.
        /// We ignore max constraints (we can always split smaller), but mins must be reachable now
        /// because we cannot grow.
        private bool CouldBecomeEligibleBySplitting(Vector2Int dims)
        {
            int a = Mathf.Min(dims.x, dims.y);
            int b = Mathf.Max(dims.x, dims.y);

            foreach (var d in ParcelGenDefs)
            {
                int minShort = Mathf.Max(0, d.MinSizeShortSide);
                int minLong = Mathf.Max(0, d.MinSizeLongSide);

                // If the def has no mins, it's always potentially reachable by splitting.
                if (minShort == 0 && minLong == 0) return true;

                // We can only ever reduce sizes, so both mins must be <= current sides in some orientation.
                if (a >= minShort && b >= minLong) return true;
            }
            return false;
        }

        /// <summary>
        /// Heuristic: prefer stopping when this parcel can host any "large-ish" def.
        /// Large-ness approximated by the largest MinSizeLongSide among defs that fit.
        /// </summary>
        private bool PreferStop(Vector2Int dims, float bias = 0.65f)
        {
            List<ParcelGenDef> elig = ParcelGenDefs.Where(d => d.DoesFulfillConstraints(dims)).ToList();
            if (elig.Count == 0) return false;

            // If the current rect still exceeds every candidate's max, do not stop.
            if (MustSplitByMax(dims)) return false;

            int maxLongNeeded = elig.Max(d => Mathf.Max(d.MinSizeLongSide, 0));
            if (maxLongNeeded <= 0) return false;

            int longSide = Mathf.Max(dims.x, dims.y);
            return longSide >= maxLongNeeded && Random.value < bias;
        }

        /// <summary>
        /// True if, among defs that already pass MIN constraints for these dims,
        /// the current rectangle exceeds every candidate's MAX constraints.
        /// If any candidate has no max or the dims are within its max, we do NOT force a split.
        /// </summary>
        private bool MustSplitByMax(Vector2Int dims)
        {
            int a = Mathf.Min(dims.x, dims.y);
            int b = Mathf.Max(dims.x, dims.y);

            bool foundCandidate = false;
            foreach (ParcelGenDef d in ParcelGenDefs)
            {
                int minShort = d.MinSizeShortSide > 0 ? d.MinSizeShortSide : 0;
                int minLong = d.MinSizeLongSide > 0 ? d.MinSizeLongSide : 0;

                // Only consider defs whose mins are already satisfied (we can always shrink later, not grow).
                if (a < minShort || b < minLong) continue;
                foundCandidate = true;

                bool exceedsShort = d.MaxSizeShortSide > 0 && a > d.MaxSizeShortSide;
                bool exceedsLong = d.MaxSizeLongSide > 0 && b > d.MaxSizeLongSide;

                // If any candidate is NOT exceeded, we must NOT force a split.
                if (!exceedsShort && !exceedsLong)
                    return false;
            }

            // If there were no candidates passing mins, we do not force split here (the mins logic elsewhere handles it).
            return foundCandidate;
        }

        #endregion

        #region Gateways / Connectivity

        /// <summary>
        /// Identifies all parcel adjacencies and borders.
        /// </summary>
        private void IdentifyBorders()
        {
            ParcelBorders = new List<ParcelBorder>();
            BordersByParcel = Parcels.Keys.ToDictionary(p => p, _ => new List<ParcelBorder>());

            List<Parcel> parcels = Parcels.Keys.ToList();
            // Because BSP partitions perfectly, neighbors meet on full edges; still compute overlaps robustly.
            for (int i = 0; i < parcels.Count; i++)
                for (int j = i + 1; j < parcels.Count; j++)
                {
                    Parcel a = parcels[i];
                    Parcel b = parcels[j];

                    // Horizontal neighbors (A east edge touches B west edge)
                    bool verticalOverlap = !(a.MinY >= b.MaxY || a.MaxY <= b.MinY);
                    if (verticalOverlap)
                    {
                        if (a.MaxX == b.MinX) AddAdj(a, b, Direction.E, Direction.W,
                            Mathf.Max(a.MinY, b.MinY), Mathf.Max(a.MinY, b.MinY),
                            Mathf.Min(a.MaxY, b.MaxY) - Mathf.Max(a.MinY, b.MinY));

                        if (b.MaxX == a.MinX) AddAdj(b, a, Direction.E, Direction.W,
                            Mathf.Max(a.MinY, b.MinY), Mathf.Max(a.MinY, b.MinY),
                            Mathf.Min(a.MaxY, b.MaxY) - Mathf.Max(a.MinY, b.MinY));
                    }

                    // Vertical neighbors (A north edge touches B south edge)
                    bool horizontalOverlap = !(a.MinX >= b.MaxX || a.MaxX <= b.MinX);
                    if (horizontalOverlap)
                    {
                        if (a.MaxY == b.MinY) AddAdj(a, b, Direction.N, Direction.S,
                            Mathf.Max(a.MinX, b.MinX), Mathf.Max(a.MinX, b.MinX),
                            Mathf.Min(a.MaxX, b.MaxX) - Mathf.Max(a.MinX, b.MinX));

                        if (b.MaxY == a.MinY) AddAdj(b, a, Direction.N, Direction.S,
                            Mathf.Max(a.MinX, b.MinX), Mathf.Max(a.MinX, b.MinX),
                            Mathf.Min(a.MaxX, b.MaxX) - Mathf.Max(a.MinX, b.MinX));
                    }
                }

            void AddAdj(Parcel A, Parcel B, Direction sideA, Direction sideB, int startWorld, int startWorldB, int len)
            {
                if (len <= 0) return;
                int offsetA = (sideA == Direction.N || sideA == Direction.S) ? (startWorld - A.MinX) : (startWorld - A.MinY);
                int offsetB = (sideB == Direction.N || sideB == Direction.S) ? (startWorldB - B.MinX) : (startWorldB - B.MinY);

                ParcelBorder border = new ParcelBorder
                {
                    A = A,
                    B = B,
                    SideOnA = sideA,
                    SideOnB = sideB,
                    SharedStartA = offsetA,
                    SharedStartB = offsetB,
                    SharedLength = len
                };
                ParcelBorders.Add(border);
                BordersByParcel[A].Add(border);
                BordersByParcel[B].Add(border);
            }
        }

        private void BuildBorderInfos()
        {
            foreach (Parcel p in Parcels.Keys)
                if (!BorderInfosByParcel.ContainsKey(p))
                    BorderInfosByParcel[p] = new List<BorderInfo>();

            // Neighbour borders -> two BorderInfos per ParcelBorder
            for (int i = 0; i < ParcelBorders.Count; i++)
            {
                ParcelBorder b = ParcelBorders[i];

                BorderInfo biA = new BorderInfo
                {
                    SourceParcel = b.A,
                    TargetParcel = b.B,
                    TargetParcelGenDef = Parcels[b.B], // now safe
                    Side = b.SideOnA,
                    Offset = b.SharedStartA,
                    Length = b.SharedLength
                };
                BorderInfosByParcel[b.A].Add(biA);

                BorderInfo biB = new BorderInfo
                {
                    SourceParcel = b.B,
                    TargetParcel = b.A,
                    TargetParcelGenDef = Parcels[b.A], // now safe
                    Side = b.SideOnB,
                    Offset = b.SharedStartB,
                    Length = b.SharedLength
                };
                BorderInfosByParcel[b.B].Add(biB);
            }

            // World-perimeter borders (TargetParcel = null)
            foreach (KeyValuePair<Parcel, ParcelGenDef> kv in Parcels)
            {
                Parcel p = kv.Key;

                if (p.MinX == 0)
                    BorderInfosByParcel[p].Add(new BorderInfo { SourceParcel = p, TargetParcel = null, Side = Direction.W, Offset = 0, Length = p.Dimensions.y });

                if (p.MaxX == WorldSize)
                    BorderInfosByParcel[p].Add(new BorderInfo { SourceParcel = p, TargetParcel = null, Side = Direction.E, Offset = 0, Length = p.Dimensions.y });

                if (p.MinY == 0)
                    BorderInfosByParcel[p].Add(new BorderInfo { SourceParcel = p, TargetParcel = null, Side = Direction.S, Offset = 0, Length = p.Dimensions.x });

                if (p.MaxY == WorldSize)
                    BorderInfosByParcel[p].Add(new BorderInfo { SourceParcel = p, TargetParcel = null, Side = Direction.N, Offset = 0, Length = p.Dimensions.x });
            }
        }

        private void PlanGateways()
        {
            GatewaysByParcel = Parcels.Keys.ToDictionary(p => p, p => new List<GatewayInfo>());

            for (int i = 0; i < ParcelBorders.Count; i++)
            {
                ParcelBorder border = ParcelBorders[i];

                ParcelGenDef defA = Parcels[border.A];
                ParcelGenDef defB = Parcels[border.B];

                GatewayDef gdef;
                if (!GatewayMap.TryGetValue((defA.DefName, defB.DefName), out gdef))
                    continue;

                // Corner-safe usable length (excludes both corners)
                int usableLen = border.SharedLength - 2;

                // ----- Fully-open attempt first, now also excluding corners -----
                bool placedFullyOpen = false;
                if (gdef.FullyOpenChance > 0f && Random.value < gdef.FullyOpenChance)
                {
                    if (usableLen >= 1)
                    {
                        int startA = border.SharedStartA + 1; // skip west/south corner by 1
                        int startB = border.SharedStartB + 1; // aligned on the opposite side
                        int length = usableLen;               // stop before east/north corner

                        AddGateway(border.A, border.SideOnA, startA, length, border.B, true);
                        AddGateway(border.B, border.SideOnB, startB, length, border.A, true);
                        placedFullyOpen = true;
                    }
                }

                if (placedFullyOpen)
                    continue; // do not place default segments if the fully-open was placed

                // ----- Default segment placement (still corner-safe) -----
                if (usableLen <= 0)
                    continue;

                int minLen = gdef.MinSize > 0 ? gdef.MinSize : 1;
                int maxLen = gdef.MaxSize > 0 ? gdef.MaxSize : usableLen;

                if (minLen > usableLen)
                    continue;
                if (maxLen < minLen)
                    maxLen = minLen;
                if (maxLen > usableLen)
                    maxLen = usableLen;

                int maxAmount = gdef.MaxAmount >= 0 ? gdef.MaxAmount : int.MaxValue;
                if (maxAmount == 0)
                    continue; // default gateways forbidden

                List<(int startLocal, int length)> segments = PlaceGatewaysAlongBorder(
                    usableStartInclusive: 1,
                    usableEndInclusive: border.SharedLength - 2,
                    minLen: minLen,
                    maxLen: maxLen,
                    maxAmount: maxAmount,
                    placeProbability: 0.5f
                );

                for (int s = 0; s < segments.Count; s++)
                {
                    int startLocal = segments[s].startLocal;
                    int length = segments[s].length;

                    int startA = border.SharedStartA + startLocal;
                    int startB = border.SharedStartB + startLocal;

                    AddGateway(border.A, border.SideOnA, startA, length, border.B, false);
                    AddGateway(border.B, border.SideOnB, startB, length, border.A, false);
                }
            }
        }

        /// <summary>
        /// Tiles [usableStartInclusive .. usableEndInclusive] with up to maxAmount segments,
        /// segment length in [minLen .. maxLen], keeping a 1-tile gap between segments,
        /// and allowing stochastic skipping so 0 gateways remain possible.
        /// </summary>
        private List<(int startLocal, int length)> PlaceGatewaysAlongBorder(
            int usableStartInclusive,
            int usableEndInclusive,
            int minLen,
            int maxLen,
            int maxAmount,
            float placeProbability
        )
        {
            List<(int startLocal, int length)> result = new List<(int startLocal, int length)>();

            int i = usableStartInclusive;
            while (i <= usableEndInclusive && result.Count < maxAmount)
            {
                int remaining = usableEndInclusive - i + 1;
                if (remaining < minLen)
                    break;

                bool placeHere = Random.value < placeProbability;
                if (!placeHere)
                {
                    i += 1; // advance to keep variation
                    continue;
                }

                int maxLenHere = Mathf.Min(maxLen, remaining);
                int length = Random.Range(minLen, maxLenHere + 1);

                // Place segment [i .. i+length-1]
                result.Add((i, length));

                // Enforce one-tile gap after each placed segment
                i += length + 1;
            }

            return result;
        }

        private void AddGateway(Parcel p, Direction side, int offset, int length, Parcel target, bool isFullyOpen)
        {
            GatewaysByParcel[p].Add(new GatewayInfo
            {
                SourceParcel = p,
                TargetParcel = target,
                Side = side,
                Offset = offset,
                Length = length,
                IsFullyOpenGateway = isFullyOpen
            });
        }

        #endregion

    }
}
