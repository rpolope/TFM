using UnityEngine;

using Unity.Mathematics;
using UnityEditor;
using UnityEngine.Serialization;
using static UnityEngine.GUILayout;

namespace Editor
{
    [CustomEditor (typeof (TestLODChange))]
    public class TestLODChangeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI() {

            var lodChanger = (TestLODChange)target;
            var filter = lodChanger.GetComponent<MeshFilter>();

            if (DrawDefaultInspector ())
            {
                if (lodChanger.LODManager is null) return;
                
                filter.mesh = lodChanger.LODManager.ChangeMeshLOD(lodChanger.lodToChange);
            }

            if (Button ("Generate"))
            {
                lodChanger.LODManager = new LODManager(lodChanger.lods);
                filter.mesh = lodChanger.LODManager.ChangeMeshLOD(lodChanger.lodToChange);
            }
        }
    }
}

public class TestLODChange : MonoBehaviour
{
    public int lods = 0;
    public LODManager LODManager;
    public int lodToChange = 1;
}