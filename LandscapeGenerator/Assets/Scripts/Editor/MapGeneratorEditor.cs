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
					var mapData = mapGenerator.GenerateMapData(mapGenerator.terrainData.parameters.resolution, mapGenerator.noiseData.parameters);
					MapDisplay.DrawMapInEditor(mapGenerator.drawMode, mapData, mapGenerator.GetTerrainParameters());
				}
			}
			if (GUILayout.Button ("Generate"))
			{
				var mapData = mapGenerator.GenerateMapData(mapGenerator.terrainData.parameters.resolution, mapGenerator.noiseData.parameters);
				MapDisplay.DrawMapInEditor(mapGenerator.drawMode, mapData, mapGenerator.GetTerrainParameters());

				if (mapGenerator.drawMode.Equals(DrawMode.Mesh))
				{
					Water.Instantiate(mapGenerator.terrainData.parameters.waterLevel,
						mapGenerator.transform,
						TerrainChunksManager.TerrainChunk.WorldSize);
				}
			}
		}
	}
}