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
			BiomesManager.Initialize();
			
			if (DrawDefaultInspector ()) {
				if (visualizer.autoUpdate) {
					var mapData = visualizer.GenerateMapData(visualizer.meshParameters.resolution, new float2(), visualizer.heightMapParameters, visualizer.moistureMapParameters);
					MapDisplay.DrawMapInEditor(visualizer.displayMode, mapData, new TerrainParameters(visualizer.heightMapParameters, visualizer.meshParameters), visualizer);
				}
			}

			if (Button ("Generate")) {
				var mapData = visualizer.GenerateMapData(visualizer.meshParameters.resolution, new float2(), visualizer.heightMapParameters, visualizer.moistureMapParameters);
				MapDisplay.DrawMapInEditor(visualizer.displayMode, mapData, new TerrainParameters(visualizer.heightMapParameters, visualizer.meshParameters), visualizer);
			}
		}
	}
}