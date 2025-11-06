using UnityEditor;
using UnityEngine;

namespace MeshMerging.Editor
{
    [CustomEditor(typeof(MeshMerger))]
    public class MeshMergerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var meshMerger = (MeshMerger)target;

            if (GUILayout.Button("Merge Meshes"))
            {
                meshMerger.Merge();
            }
        }
    }
}
