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
			MapDisplay.MeshFilter = mapGenerator.gameObject.GetComponent<MeshFilter>();
			MapDisplay.MeshRenderer = mapGenerator.gameObject.GetComponent<MeshRenderer>();
			MapDisplay.TextureRender = mapGenerator.gameObject.GetComponent<Renderer>();
		
			if (DrawDefaultInspector ()) {
				if (mapGenerator.autoUpdate) {
					var mapData = MapGenerator.GenerateMapData(mapGenerator.mapParameters.meshParameters.resolution, new float2(), mapGenerator.mapParameters.noiseParameters);
					MapDisplay.DrawMapInEditor(mapGenerator.drawMode, mapData, mapGenerator.mapParameters);
				}
			}

			if (GUILayout.Button ("Generate")) {
				var mapData = MapGenerator.GenerateMapData(mapGenerator.mapParameters.meshParameters.resolution, new float2(), mapGenerator.mapParameters.noiseParameters);
				MapDisplay.DrawMapInEditor(mapGenerator.drawMode, mapData, mapGenerator.mapParameters);		
			}
		}
	}
}