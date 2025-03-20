using ScriptsSandbox.Util;
using UnityEditor;
using UnityEngine;

namespace MapTools.Editor
{
    [CustomEditor(typeof(GeometryBuilder))]
    public class MapGeneratorInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var quakeMap = target as GeometryBuilder;
            
            // button
            if(GUILayout.Button("Generate Map"))
            {
                quakeMap.Generate();
            }
        }
    }
}