using UnityEngine;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine.Serialization;

[CustomEditor (typeof (MapGenerator))]
public class MapGeneratorEditor : Editor
{
	public override void OnInspectorGUI() {

		MapGenerator mapGenerator = (MapGenerator)target;
		MapDisplay.meshFilter = mapGenerator.gameObject.GetComponent<MeshFilter>();
		MapDisplay.meshRenderer = mapGenerator.gameObject.GetComponent<MeshRenderer>();
		MapDisplay.textureRender = mapGenerator.gameObject.GetComponent<Renderer>();
		
		if (DrawDefaultInspector ()) {
			if (mapGenerator.autoUpdate) {
				var mapData = mapGenerator.GenerateMapData();
				MapDisplay.DrawMapInEditor(mapGenerator.drawMode, mapData, mapGenerator.mapParameters);
			}
		}

		if (GUILayout.Button ("Generate")) {
			var mapData = mapGenerator.GenerateMapData();
			MapDisplay.DrawMapInEditor(mapGenerator.drawMode, mapData, mapGenerator.mapParameters);		
		}
	}
}