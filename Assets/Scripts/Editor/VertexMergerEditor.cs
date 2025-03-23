#if UNITY_EDITOR
// Editor script for the VertexMerger component
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VertexMerger))]
public class VertexMergerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        VertexMerger vertexMerger = (VertexMerger)target;

        if (GUILayout.Button("Merge Child Meshes"))
        {
            vertexMerger.MergeChildMeshes();
        }
    }
}
#endif