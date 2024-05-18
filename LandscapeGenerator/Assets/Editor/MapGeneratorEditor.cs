using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Editor
{
	[CustomEditor (typeof (MapGenerator))]
	public class MapGeneratorEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI() {

			MapGenerator mapGenerator = (MapGenerator)target;
		
			if (DrawDefaultInspector ()) {
				if (mapGenerator.autoUpdate) {
					var mapData = MapGenerator.GenerateMapData(mapGenerator.meshParameters.resolution, new float2(), mapGenerator.heightMapParameters, mapGenerator.moistureMapParameters);
					MapDisplay.DrawMapInEditor(mapGenerator.drawMode, mapData, new TerrainParameters(mapGenerator.heightMapParameters, new MeshParameters(0f)), mapGenerator.gameObject);
				}
			}

			if (GUILayout.Button ("Generate")) {
				var mapData = MapGenerator.GenerateMapData(mapGenerator.meshParameters.resolution, new float2(), mapGenerator.heightMapParameters, mapGenerator.moistureMapParameters);
				MapDisplay.DrawMapInEditor(mapGenerator.drawMode, mapData, new TerrainParameters(mapGenerator.heightMapParameters, new MeshParameters(0f)), mapGenerator.gameObject);
				// mapGenerator.GenerateWorldNoiseMap();
			}
		}
	}
}