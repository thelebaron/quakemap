using System.Collections.Generic;
using Sledge.Formats.Map.Objects;
using Unity.Mathematics;
using UnityEngine;
using Mesh = UnityEngine.Mesh;

namespace ScriptsSandbox.QuakeMap
{
    public class QFace : MonoBehaviour
    {
        public string TextureName;

        public Vector3 UAxis;
        public Vector3 VAxis;
        public float   XScale;
        public float   YScale;
        public float   XShift;
        public float   YShift;
        public float   Rotation;
        
        public List<QFace> connectedFaces = new List<QFace>();

        // converted data
        public Vector3 Normal;
        
        public void Copy(Face face)
        {
            TextureName = face.TextureName;
            UAxis       = face.UAxis.AsVector3();
            VAxis       = face.VAxis.AsVector3();
            XScale      = face.XScale;
            YScale      = face.YScale;
            XShift      = face.XShift;
            YShift      = face.YShift;
            Rotation    = face.Rotation;
        }
        [ContextMenu("GetUvScale")]
        void GetUvScale()
        {
            var mesh      = GetComponent<MeshFilter>().sharedMesh;
            var uvs = mesh.uv;
            Debug.Log(math.distance(uvs[0], uvs[1]));
        }
        
        [ContextMenu("Redo UV")]
        void ReUv()
        {
            var mesh      = GetComponent<MeshFilter>().sharedMesh;
            var uvs       = new Vector2[mesh.vertices.Length];
            var vertices  = mesh.vertices;
            
            
            var triangles = mesh.triangles;

            // Calculate average normal of the mesh
            var avgNormal = CalculateAverageNormal(mesh);

            // Find the most appropriate up vector (we'll use world up as default)
            var upVector = Vector3.up;

            // If normal is too close to up vector, use forward instead
            if (Vector3.Dot(avgNormal.normalized, upVector) > 0.9f)
            {
                upVector = Vector3.forward;
            }

            // Calculate right vector (perpendicular to normal and up)
            var rightVector = Vector3.Cross(upVector, avgNormal).normalized;

            // Recalculate the actual up vector to ensure orthogonality
            upVector = Vector3.Cross(avgNormal, rightVector).normalized;

            // Apply the planar projection
            uvs = CreateWorldPlanarProjection(vertices, mesh.bounds, rightVector, upVector);
            
            // after projecting, this seems to be the magic trick
            uvs = ScaleBy(uvs, 0.5f);
            uvs = FlipX(uvs);
            // Center and normalize UVs to make them visible
            //uvs = AlignUVsToBottomLeft(uvs);
            uvs = Shift(uvs);
            
            // Apply rotation
            uvs = Rotate(uvs, new Vector2(), Rotation);

            // Apply additional transformations from QFace properties
            uvs = ApplyUVTransformations(uvs);
            
            // Apply the new UVs to the mesh
            mesh.uv = uvs;

            // Ensure the mesh gets updated
            if (Application.isEditor)
            {
                UnityEditor.EditorUtility.SetDirty(mesh);
            }
        }
        
        
        private Vector2[] CreatePlanarProjection(Vector3[] vertices, Transform meshTransform, Vector3 rightVector, Vector3 upVector, float worldUnitsPerUV = 1.0f)
        {
            Vector2[] uvs = new Vector2[vertices.Length];
    
            // Convert vertices to world space
            Vector3[] worldVertices = new Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                worldVertices[i] = meshTransform.TransformPoint(vertices[i]);
            }
    
            // Calculate world-space bounds center
            Bounds worldBounds = new Bounds(worldVertices[0], Vector3.zero);
            foreach (Vector3 v in worldVertices)
            {
                worldBounds.Encapsulate(v);
            }
    
            // Calculate UVs directly based on world position and a fixed scale
            for (int i = 0; i < worldVertices.Length; i++)
            {
                // This is the key change - directly use world position divided by a fixed world unit size
                // This ensures that as the object scales, the UVs will scale accordingly
                Vector3 worldPos = worldVertices[i] - worldBounds.center;
        
                float u = Vector3.Dot(worldPos, rightVector) / worldUnitsPerUV;
                float v = Vector3.Dot(worldPos, upVector)    / worldUnitsPerUV;
        
                uvs[i] = new Vector2(u, v);
            }
    
            return uvs;
        }
        private Vector2[] CreateWorldPlanarProjection(Vector3[] vertices, Bounds bounds, Vector3 rightVector, Vector3 upVector)
        {
            Vector2[] uvs = new Vector2[vertices.Length];

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 worldPos = vertices[i]; // Use world position directly
                float   u        = Vector3.Dot(worldPos, rightVector);
                float   v        = Vector3.Dot(worldPos, upVector);

                uvs[i] = new Vector2(u, v);
            }

            return uvs;
        }
        
        private Vector2[] CreatePlanarProjection(Vector3[] vertices, Bounds bounds, Vector3 rightVector, Vector3 upVector)
        {
            Vector2[] uvs = new Vector2[vertices.Length];

            // First pass: project onto right/up vectors and track min/max for each.
            float minU = float.MaxValue, maxU = float.MinValue;
            float minV = float.MaxValue, maxV = float.MinValue;

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 localPos = vertices[i] - bounds.center;
                float   u        = Vector3.Dot(localPos, rightVector);
                float   v        = Vector3.Dot(localPos, upVector);

                if (u < minU) minU = u;
                if (u > maxU) maxU = u;
                if (v < minV) minV = v;
                if (v > maxV) maxV = v;

                uvs[i] = new Vector2(u, v);
            }

            // Calculate the max dimension to preserve aspect ratio
            float rangeU       = maxU - minU;
            float rangeV       = maxV - minV;
            float maxDimension = Mathf.Max(rangeU, rangeV);

            // Avoid dividing by zero
            if (maxDimension < 1e-6f) maxDimension = 1e-6f;

            // Second pass: normalize using the max dimension to preserve aspect ratio
            /*for (int i = 0; i < uvs.Length; i++)
            {
                float u = (uvs[i].x - minU) / maxDimension;
                float v = (uvs[i].y - minV) / maxDimension;
                uvs[i] = new Vector2(u, v);
            }*/

            return uvs;
        }

        private Vector2[] AlignUVsToBottomLeft(Vector2[] uvs)
        {
            if (uvs.Length == 0) return uvs;

            // Find the minimum X and Y values
            float minX = float.MaxValue;
            float minY = float.MaxValue;

            for (int i = 0; i < uvs.Length; i++)
            {
                var uv                = uvs[i];
                if (uv.x < minX) minX = uv.x;
                if (uv.y < minY) minY = uv.y;
            }

            // Create an offset vector that will align the leftmost and bottommost values to 0
            var offset = new Vector2(minX, minY);

            // Apply the offset to all UVs
            Vector2[] alignedUVs = new Vector2[uvs.Length];
            for (int i = 0; i < uvs.Length; i++)
            {
                alignedUVs[i] = uvs[i] - offset;
            }

            return alignedUVs;
        }
        

        // Method to apply scale and shift transformations
        private Vector2[] ApplyUVTransformations(Vector2[] uvs)
        {
            //uvs = FlipY(uvs);
            //uvs = AlignUVsToBottomLeft(uvs);
            uvs = Shift(uvs);
            uvs = Scale(uvs);
            
            return uvs;
        }
        // Method to apply scale transformation
        private Vector2[] ScaleBy(Vector2[] uvs, float size)
        {
            for (int i = 0; i < uvs.Length; i++)
            {
                uvs[i] = new Vector2(
                    uvs[i].x *= size,
                    uvs[i].y *= size
                );
            }
            return uvs;
        }
        // Method to apply scale transformation
        private Vector2[] Scale(Vector2[] uvs)
        {
            for (int i = 0; i < uvs.Length; i++)
            {
                uvs[i] = new Vector2(
                    uvs[i].x *= XScale,
                    uvs[i].y *= YScale
                );
            }
            return uvs;
        }
        // flip y axis
        private Vector2[] FlipY(Vector2[] uvs)
        {
            for (int i = 0; i < uvs.Length; i++)
            {
                uvs[i] = new Vector2(
                    uvs[i].x,
                    uvs[i].y *= -1
                );
            }

            return uvs;
        }
        // flip x axis
        private Vector2[] FlipX(Vector2[] uvs)
        {
            for (int i = 0; i < uvs.Length; i++)
            {
                uvs[i] = new Vector2(
                    uvs[i].x *= -1,
                    uvs[i].y
                );
            }

            return uvs;
        }
        // Method to apply Quake-style UV shifting based on texture size
        private Vector2[] Shift(Vector2[] uvs)
        {
            // Using constant texture size of 64x64 as specified
            const float textureSize = 64f;
    
            Vector2[] shiftedUVs = new Vector2[uvs.Length];

            for (int i = 0; i < uvs.Length; i++)
            {
                shiftedUVs[i] = new Vector2(
                    uvs[i].x + (XShift / textureSize),
                    uvs[i].y + (YShift / textureSize)
                );
            }

            return shiftedUVs;
        }
        
        private Vector2[] Rotate(Vector2[] uvs, float2 center, float rotation)
        {
            // Apply rotation to each UV coordinate
            for (int i = 0; i < uvs.Length; i++)
            {
                var uv = uvs[i];
                uvs[i] = RotateDegrees(uv, center, rotation);
            }

            return uvs;
        }
        public static float2 RotateDegrees(float2 uv, float2 center, float rotation)
        {
            // Convert rotation from degrees to radians
            rotation = rotation * (math.PI / 180.0f);
        
            // Offset UV by center point
            uv -= center;
        
            // Calculate sine and cosine of rotation angle
            float s = math.sin(rotation);
            float c = math.cos(rotation);
        
            // Create rotation matrix
            float2x2 rMatrix = new float2x2(c, -s, s, c);
        
            // Apply the same transformations as in the shader function
            rMatrix *= 0.5f;
            rMatrix += 0.5f;
            rMatrix =  rMatrix * 2 - 1;
        
            // Apply rotation matrix to UV
            uv = math.mul(rMatrix, uv);
        
            // Restore UV position relative to center
            uv += center;
        
            return uv;
        }
        
        // Helper method to calculate the average normal of a mesh
        private Vector3 CalculateAverageNormal(Mesh mesh)
        {
            Vector3[] normals   = mesh.normals;
            Vector3   avgNormal = Vector3.zero;

            if (normals != null && normals.Length > 0)
            {
                for (int i = 0; i < normals.Length; i++)
                {
                    avgNormal += normals[i];
                }

                avgNormal /= normals.Length;
            }
            else
            {
                // If no normals are available, calculate them from triangles
                int[]     triangles = mesh.triangles;
                Vector3[] vertices  = mesh.vertices;

                for (int i = 0; i < triangles.Length; i += 3)
                {
                    Vector3 v1 = vertices[triangles[i]];
                    Vector3 v2 = vertices[triangles[i + 1]];
                    Vector3 v3 = vertices[triangles[i + 2]];

                    Vector3 normal = Vector3.Cross(v2 - v1, v3 - v1).normalized;
                    avgNormal += normal;
                }

                avgNormal /= (triangles.Length / 3);
            }

            return avgNormal.normalized;
        }
    }
}