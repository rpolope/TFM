using Unity.Mathematics;
using UnityEditor;
using static UnityEngine.GUILayout;

namespace Editor
{
	[CustomEditor (typeof (MapVisualizer))]
	public class MapVisualizerEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI() {

			var visualizer = (MapVisualizer)target;
			
			if (DrawDefaultInspector ()) {
				if (visualizer.autoUpdate) {
					BiomesManager.Initialize(true);
					var mapData = MapGenerator.GenerateMapData(visualizer.meshParameters.resolution, new float2(), visualizer.heightMapParameters, visualizer.moistureMapParameters, visualizer.displayMode);
					MapVisualizer.DrawMapInEditor(visualizer.displayMode, mapData, new TerrainParameters(visualizer.heightMapParameters, visualizer.meshParameters), visualizer);
				}
			}

			if (Button ("Generate")) {
				BiomesManager.Initialize(true);
				var mapData = MapGenerator.GenerateMapData(visualizer.meshParameters.resolution, new float2(), visualizer.heightMapParameters, visualizer.moistureMapParameters, visualizer.displayMode);
				MapVisualizer.DrawMapInEditor(visualizer.displayMode, mapData, new TerrainParameters(visualizer.heightMapParameters, visualizer.meshParameters), visualizer);
			}
		}
	}
}