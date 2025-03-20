using System.IO;
using System.Linq;
using DefaultNamespace;
using UnityEngine;
using Sledge.Formats.Map.Formats;
using Sledge.Formats.Map.Objects;

namespace QuakeMapVisualization
{
    [RequireComponent(typeof(QuakeMap))]
    public class QuakeMapGizmoVisualizer : MonoBehaviour
    {
        public string   mapPath      = "Assets/Maps/rotation_uv.map";
        public float    scale        = 0.03125f;
        public Color    solidColor   = Color.green;
        public Color    entityColor  = Color.yellow;
        public bool     showNormals  = true;
        public float    normalLength = 16f; // Length of normal visualization
        public QuakeMap QuakeMap;
        
        private void OnDrawGizmos()
        {
            if(QuakeMap == null)
                return;
            var mapFile = QuakeMap.Map;
            
            if (mapFile == null || !enabled) 
                return;
            
            Gizmos.color = solidColor;
            
            // Draw all solids in the worldspawn
            foreach (var mapObject in mapFile.Worldspawn.Children)
            {
                if (mapObject is Solid solid)
                {
                    DrawSolidGizmo(solid, Vector3.zero);
                }
                else if (mapObject is Entity entity)
                {
                    DrawEntityGizmo(entity);
                }
            }
        }
        
        private void DrawSolidGizmo(Solid solid, Vector3 offset)
        {
            foreach (var face in solid.Faces)
            {
                DrawFaceGizmo(face, offset);
            }
        }
        
        private void DrawFaceGizmo(Face face, Vector3 offset)
        {
            if (face.Vertices.Count < 2)
                return;
                
            // Draw edges of the face
            for (int i = 0; i < face.Vertices.Count; i++)
            {
                int nextIndex = (i + 1) % face.Vertices.Count;
                
                // Convert to Unity coordinates (swapping Y and Z)
                Vector3 start = new Vector3(
                    face.Vertices[i].X, 
                    face.Vertices[i].Z, 
                    face.Vertices[i].Y
                ) * scale + offset;
                
                Vector3 end = new Vector3(
                    face.Vertices[nextIndex].X, 
                    face.Vertices[nextIndex].Z, 
                    face.Vertices[nextIndex].Y
                ) * scale + offset;
                
                Gizmos.DrawLine(start, end);
            }
            
            // Optionally draw the face normal
            if (showNormals && face.Vertices.Count > 0)
            {
                // Calculate center of the face
                Vector3 center = Vector3.zero;
                foreach (var vertex in face.Vertices)
                {
                    center += new Vector3(vertex.X, vertex.Z, vertex.Y);
                }
                center = (center / face.Vertices.Count) * scale + offset;
                
                // Draw normal
                Vector3 normal = new Vector3(
                    face.Plane.Normal.X,
                    face.Plane.Normal.Z,
                    face.Plane.Normal.Y
                ).normalized;
                
                // Special color for normals
                Color currentColor = Gizmos.color;
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(center, center + normal * normalLength * scale);
                Gizmos.color = currentColor;
                
                // Optionally show texture axes
                if (face.UAxis != null && face.VAxis != null)
                {
                    Vector3 uAxis = new Vector3(face.UAxis.X, face.UAxis.Z, face.UAxis.Y).normalized;
                    Vector3 vAxis = new Vector3(face.VAxis.X, face.VAxis.Z, face.VAxis.Y).normalized;
                    
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(center, center + uAxis * normalLength * scale * 0.5f);
                    
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(center, center + vAxis * normalLength * scale * 0.5f);
                    
                    Gizmos.color = currentColor;
                }
            }
        }
        
        private void DrawEntityGizmo(Entity entity)
        {
            // Parse origin if available
            Vector3 entityPosition = Vector3.zero;
            if (entity.Properties.TryGetValue("origin", out string originStr))
            {
                string[] parts = originStr.Split(' ');
                if (parts.Length == 3 && 
                    float.TryParse(parts[0], out float x) && 
                    float.TryParse(parts[1], out float y) &&
                    float.TryParse(parts[2], out float z))
                {
                    // Convert to Unity coordinates
                    entityPosition = new Vector3(x, z, y) * scale;
                }
            }
            
            // Store current color and set entity color
            Color currentColor = Gizmos.color;
            Gizmos.color = entityColor;
            
            // Draw entity marker (a small cube)
            Gizmos.DrawWireCube(entityPosition, Vector3.one * 16f * scale);
            
            // Draw entity name
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(entityPosition, entity.ClassName);
            #endif
            
            // Draw child solids if any
            foreach (var child in entity.Children)
            {
                if (child is Solid childSolid)
                {
                    Gizmos.color = Color.cyan; // Different color for brush entities
                    DrawSolidGizmo(childSolid, entityPosition);
                }
            }
            
            // Restore color
            Gizmos.color = currentColor;
        }
    }
}