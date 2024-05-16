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
}