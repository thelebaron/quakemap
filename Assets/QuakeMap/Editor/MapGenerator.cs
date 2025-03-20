using ScriptsSandbox.QuakeMap;
using UnityEditor;
using UnityEngine;

namespace QuakeMap.Editor
{
    [CustomEditor(typeof(QuakeMapGenerator))]
    public class MapGeneratorInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var quakeMap = target as QuakeMapGenerator;
            
            // button
            if(GUILayout.Button("Generate Map"))
            {
                quakeMap.Generate();
            }
        }
    }
}