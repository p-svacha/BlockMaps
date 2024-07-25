using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.WorldGeneration
{
    /// <summary>
    /// An XCOM inspired map generator that first splits the world into different 2d parcels of certain types. Parcels then get filled by their own parcel generators based on the type.
    /// </summary>
    public class ParcelMapGenerator : WorldGenerator
    {
        public override string Name => "Parcels";

        private ParcelGeneratorStep GenerationStep;

        private int CurrentParcelIndex = 0;
        private List<Parcel> Parcels;

        // Parcel probabilities
        private Dictionary<ParcelType, float> ParcelTable = new Dictionary<ParcelType, float>()
        {
            { ParcelType.Park, 1f },
            { ParcelType.UrbanBlock, 1f },
            { ParcelType.Industry, 1f },
            { ParcelType.Forest, 1f },
        };

        protected override void OnGenerationStart()
        {
            Parcels = new List<Parcel>();
            CurrentParcelIndex = 0;

            GenerationStep = ParcelGeneratorStep.SplitMapIntoParcels;
        }

        protected override void OnUpdate()
        {
            switch (GenerationStep)
            {
                case ParcelGeneratorStep.SplitMapIntoParcels:
                    SplitMapIntoParcels(WorldSize, WorldSize);
                    GenerationStep = ParcelGeneratorStep.FillParcels;
                    break;

                case ParcelGeneratorStep.FillParcels:
                    if (CurrentParcelIndex == Parcels.Count) GenerationStep = ParcelGeneratorStep.Done;
                    else
                    {
                        Parcels[CurrentParcelIndex].Generate();
                        CurrentParcelIndex++;
                    }
                    break;

                case ParcelGeneratorStep.Done:
                    FinishGeneration();
                    break;
            }
        }

        /// <summary>
        /// Splits the maps into random rectangular parcels.
        /// </summary>
        private void SplitMapIntoParcels(int width, int height)
        {
            Vector2Int initialPosition = new Vector2Int(0, 0);
            Vector2Int initialDimensions = new Vector2Int(width, height);
            SplitParcel(initialPosition, initialDimensions);
        }

        private void SplitParcel(Vector2Int position, Vector2Int dimensions)
        {
            // Define a minimum size to stop further splitting
            const int minWidth = 30;
            const int minHeight = 30;

            if (dimensions.x <= minWidth * 2 && dimensions.y <= minHeight * 2)
            {
                // Create and add the parcel if it's small enough to stop splitting
                Parcels.Add(GetRandomParcel(position, dimensions)); // Use a specific type of parcel here
                return;
            }

            bool splitHorizontally = dimensions.x > minWidth * 2 && (dimensions.y <= minHeight * 2 || Random.value > 0.5f);

            if (splitHorizontally)
            {
                // Split horizontally
                int splitX = Random.Range(minWidth, (int)dimensions.x - minWidth);

                Vector2Int leftDimensions = new Vector2Int(splitX, dimensions.y);
                Vector2Int rightDimensions = new Vector2Int(dimensions.x - splitX, dimensions.y);

                SplitParcel(position, leftDimensions);
                SplitParcel(new Vector2Int(position.x + splitX, position.y), rightDimensions);
            }
            else
            {
                // Split vertically
                int splitY = Random.Range(minHeight, (int)dimensions.y - minHeight);

                Vector2Int bottomDimensions = new Vector2Int(dimensions.x, splitY);
                Vector2Int topDimensions = new Vector2Int(dimensions.x, dimensions.y - splitY);

                SplitParcel(position, bottomDimensions);
                SplitParcel(new Vector2Int(position.x, position.y + splitY), topDimensions);
            }
        }

        private Parcel GetRandomParcel(Vector2Int position, Vector2Int dimensions)
        {
            ParcelType randomType = HelperFunctions.GetWeightedRandomElement(ParcelTable);

            return randomType switch
            {
                ParcelType.Park => new PRC001_Park(World, position, dimensions),
                ParcelType.UrbanBlock => new PRC002_UrbanBlock(World, position, dimensions),
                ParcelType.Industry => new PRC003_Industry(World, position, dimensions),
                ParcelType.Forest => new PRC004_Forest(World, position, dimensions),
                _ => throw new System.Exception("ParcelType initialization for " + randomType.ToString() + " not handled.")
            };
        }


        private enum ParcelGeneratorStep
        {
            SplitMapIntoParcels,
            FillParcels,
            Done
        }
    }
}
