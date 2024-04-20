//
// using UnityEngine;
// #if UNITY_EDITOR
// using UnityEditor;
// #endif
//
// #if UNITY_EDITOR
//     [CustomEditor(typeof(NoiseProperties))]
//     public class NoiseMapVisualizerEditor : Editor
//     {
//         public override void OnInspectorGUI()
//         {
//             DrawDefaultInspector();
//             
//             if (GUILayout.Button("Generate Texture"))
//             {
//                 MapVisualizer.Instance.VisualizeMap();
//             }
//         }
//     }
// #endif