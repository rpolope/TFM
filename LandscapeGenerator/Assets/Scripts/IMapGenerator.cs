using UnityEngine;

public interface IMapGenerator
{
    public float[] GenerateMap(int size);
    public float[] GenerateContinuousMap(int size, Vector2 coords);
   
}