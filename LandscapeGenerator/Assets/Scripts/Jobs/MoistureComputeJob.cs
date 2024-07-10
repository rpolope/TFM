using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Jobs
{
    public struct MoistureComputeJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<Color> MoistureTextureData;

        public int TextureWidth;
        public int TextureHeight;
        public int MapWidth;
        public int MapHeight;

        [WriteOnly]
        public NativeArray<float> MoistureMap;

        public void Execute(int index)
        {
            int chunkX = index % MapWidth;
            int chunkY = index / MapWidth;

            int chunkPixelWidth = TextureWidth / MapWidth;
            int chunkPixelHeight = TextureHeight / MapHeight;

            int startX = chunkX * chunkPixelWidth;
            int startY = chunkY * chunkPixelHeight;
            int endX = startX + chunkPixelWidth;
            int endY = startY + chunkPixelHeight;

            Dictionary<float, int> moistureFrequency = new Dictionary<float, int>();

            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    int pixelIndex = y * TextureWidth + x;
                    float moistureValue = MoistureTextureData[pixelIndex].grayscale;

                    if (moistureFrequency.ContainsKey(moistureValue))
                    {
                        moistureFrequency[moistureValue]++;
                    }
                    else
                    {
                        moistureFrequency[moistureValue] = 1;
                    }
                }
            }

            float predominantMoisture = moistureFrequency.OrderByDescending(m => m.Value).First().Key;
            MoistureMap[index] = predominantMoisture;
        }
    }
}