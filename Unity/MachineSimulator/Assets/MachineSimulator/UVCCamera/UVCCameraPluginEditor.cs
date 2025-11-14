using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

namespace MachineSimulator.UVCCamera
{
    [CustomEditor(typeof(UVCCameraPlugin))]
    public class UVCCameraPluginEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var script = (UVCCameraPlugin) target;
            if (GUILayout.Button("GetProperties", GUILayout.Width(200)))
            {
                script.GetCameraProperties();
            }

            if (GUILayout.Button("SetProperties", GUILayout.Width(200)))
            {
                script.SetCameraProperties();
            }
        }
    }
}
#endif
