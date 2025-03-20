using System.Collections.Generic;
using UnityEngine;

namespace ScriptsSandbox.QuakeMap
{
    public static partial class Tb
    {
        private static void doObj()
        {
            using var objWriter = new System.IO.StreamWriter("output.obj");
            using var mtlWriter = new System.IO.StreamWriter("output.mtl");

            // Write MTL reference
            objWriter.WriteLine("mtllib output.mtl");

            // Write vertex positions with coordinate transformations
            objWriter.WriteLine("# vertices");
            foreach (var vertex in m_vertices.List)
            {
                // Apply same transformation as TrenchBroom: swap Y and Z, negate Y
                objWriter.WriteLine($"v {vertex.x} {vertex.z} {-vertex.y}");
            }

            objWriter.WriteLine();

            // Write texture coordinates with Y flipped
            objWriter.WriteLine("# texture coordinates");
            foreach (var uv in m_uvCoords.List)
            {
                // Apply same transformation as TrenchBroom: negate Y
                objWriter.WriteLine($"vt {uv.x} {-uv.y}");
            }

            objWriter.WriteLine();

            // Write normals with coordinate transformations
            objWriter.WriteLine("# normals");
            foreach (var normal in m_normals.List)
            {
                // Apply same transformation as TrenchBroom: swap Y and Z, negate Y
                objWriter.WriteLine($"vn {normal.x} {normal.z} {-normal.y}");
            }

            objWriter.WriteLine();

            // Write each brush as an object
            foreach (var brush in m_objects)
            {
                objWriter.WriteLine($"o entity{brush.entityIndex}_brush{brush.brushIndex}");

                foreach (var face in brush.faces)
                {
                    // Write material reference
                    string materialName = !string.IsNullOrEmpty(face.materialName) ? face.materialName : "default";
                    objWriter.WriteLine($"usemtl {materialName}");

                    // Write face with vertex/uv/normal indices
                    objWriter.Write("f");
                    foreach (var vertex in face.verts)
                    {
                        // OBJ indices are 1-based, so add 1 to all indices
                        objWriter.Write($" {vertex.Vertex + 1}/{vertex.UvCoords + 1}/{vertex.Normal + 1}");
                    }

                    objWriter.WriteLine();
                }

                objWriter.WriteLine();
            }

            // Write the MTL file with all used materials
            var usedMaterials = new HashSet<string>();
            foreach (var brush in m_objects)
            {
                foreach (var face in brush.faces)
                {
                    if (!string.IsNullOrEmpty(face.materialName))
                    {
                        usedMaterials.Add(face.materialName);
                    }
                }
            }

            // Write default material
            mtlWriter.WriteLine("newmtl default");
            mtlWriter.WriteLine();

            // Write all other materials
            foreach (var material in usedMaterials)
            {
                mtlWriter.WriteLine($"newmtl {material}");
                // In a real implementation, you would add texture references here:
                // mtlWriter.WriteLine($"map_Kd {material}.png");
                mtlWriter.WriteLine();
            }

            Debug.Log($"OBJ file written with {m_vertices.List.Count} vertices, {m_uvCoords.List.Count} UVs, {m_normals.List.Count} normals, and {m_objects.Count} objects");
        }
    }
}