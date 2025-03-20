using System;
using UnityEngine;

namespace Mapping
{
    public class UvUtility
    {
        /// <summary>
        /// Calculates UV coordinates for a face based on TrenchBroom's OBJ export UV calculation.
        /// </summary>
        /// <param name="face">The face data from Sledge format</param>
        /// <param name="vertices">Vertex positions in Unity space (same as passed to mesh)</param>
        /// <param name="textureSize">Size of the texture in pixels</param>
        /// <returns>Array of UV coordinates matching the vertices array</returns>
        public static Vector2[] CalculateUVs(Sledge.Formats.Map.Objects.Face face, Vector3[] vertices, Vector2 textureSize)
        {
            var uvs = new Vector2[vertices.Length];
            
            // Convert texture axes to Unity space (swapping Y and Z)
            var uAxis = new Vector3(face.UAxis.X, face.UAxis.Z, -face.UAxis.Y);
            var vAxis = new Vector3(face.VAxis.X, face.VAxis.Z, -face.VAxis.Y);
            
            for (int i = 0; i < vertices.Length; i++)
            {
                // Step 1: Compute dot products with the axes
                float u = Vector3.Dot(vertices[i], uAxis / face.XScale);
                float v = Vector3.Dot(vertices[i], vAxis / face.YScale);
                
                // Step 2: Apply offset
                u += face.XShift;
                v += face.YShift;
                
                // Step 3: Normalize by texture size
                u /= textureSize.x;
                v /= textureSize.y;
                
                // Step 4: Invert Y as TrenchBroom does in OBJ export
                uvs[i] = new Vector2(u, -v);
            }

            return uvs;
        }
    }
}