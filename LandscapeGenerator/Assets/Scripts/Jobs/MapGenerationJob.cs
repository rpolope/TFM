using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Jobs
{
    [BurstCompile]
    public struct MapGenerationJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] 
        public NativeArray<float> Map;
        public int Resolution;
        public float2 Centre;
        public NoiseParameters Parameters;

        public void Execute(int threadIndex)
        {
            int x = threadIndex % Resolution;
            int y = threadIndex / Resolution;
            
            float2 pos = new float2(x, y) + Centre;

            float value = NoiseGenerator.GetNoiseValue(pos, Parameters);
            
            Map[threadIndex] = value;
        }
    }
}