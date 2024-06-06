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

			// BiomesManager.Initialize();
			//
			if (DrawDefaultInspector ()) {
				if (mapGenerator.autoUpdate) {
					var mapData = MapGenerator.GenerateMapData(mapGenerator.terrain.parameters.resolution, mapGenerator.noise.parameters, new Biome(0f, 0f), new float2(1,1));
					MapDisplay.DrawMapInEditor(mapGenerator.drawMode, mapData, mapGenerator.GetTerrainParameters());
				}
			}
			if (GUILayout.Button ("Generate")) {
				var mapData = MapGenerator.GenerateMapData(mapGenerator.terrain.parameters.resolution, mapGenerator.noise.parameters, new Biome(0f, 0f), new float2(1,1));
				MapDisplay.DrawMapInEditor(mapGenerator.drawMode, mapData, mapGenerator.GetTerrainParameters());		
			}
		}
	}
}