using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using UnityEngine;
using UnityEngine.UI;

public class LOD
{
    public int lod;
    public Mesh mesh;
    public float distanceThreshold;
    
    private int powerOf2Value;

    public LOD(int lod)
    {
        this.lod = lod;
        powerOf2Value = (1 << lod);
        distanceThreshold = CalculateDistanceThreshold();
    }

    private float CalculateDistanceThreshold()
    {
        int levelsOfDetail = LandscapeManager.Instance.terrainParameters.meshParameters.levelsOfDetail;

        int sum = 0;
        for (int lod = 0; lod < levelsOfDetail; lod++)
        {
            sum *= (1 << lod);
        }

        float lodRealtiveDist = LandscapeManager.maxViewDst / sum;

        return lodRealtiveDist * powerOf2Value;
    }
}
