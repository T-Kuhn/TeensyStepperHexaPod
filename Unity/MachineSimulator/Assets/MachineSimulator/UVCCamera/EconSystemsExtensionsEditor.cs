using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

namespace MachineSimulator.UVCCamera
{
    [CustomEditor(typeof(EconSystemsExtensions))]
    public class EconSystemsExtensionsEdior : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var script = (EconSystemsExtensions)target;
            if (GUILayout.Button("Initialize", GUILayout.Width(200)))
            {
                script.Initialize();
            }
        }
    }
}

#endif