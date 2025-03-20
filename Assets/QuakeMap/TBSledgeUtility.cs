using System.Collections.Generic;
using Sledge.Formats.Map.Objects;
using Unity.Mathematics;
using UnityEngine;

// ReSharper disable RedundantNameQualifier

namespace ScriptsSandbox.QuakeMap
{
    public static class SledgeConversion
    {
        public static brushsolid GetBrushSolid(this Sledge.Formats.Map.Objects.Solid sSolid)
        {
            var brush = new brushsolid();
            brush.faces = sSolid.Faces.GetBrushFaces();
            
            return brush;
        }
        
        // CONVERSION STUFF
        private static List<brushFace> GetBrushFaces(this List<Sledge.Formats.Map.Objects.Face> sFaces)
        {
            var brushFaces = new List<brushFace>();
            foreach (var face in sFaces) 
                brushFaces.Add(face.GetBrushFace());
            return brushFaces;
        }
        
        private static brushFace GetBrushFace(this Sledge.Formats.Map.Objects.Face sFace)
        {
            var face = new brushFace
            {
                plane      = sFace.Plane.GetPlane(),
                points     = sFace.Vertices.GetPoints(),
                attributes = sFace.GetAttributes()
            };
            return face;
        }

        private static attributes GetAttributes(this Sledge.Formats.Map.Objects.Surface sSurface)
        {
            var attributes = new attributes
            {
                scale        = new float2(sSurface.XScale, sSurface.YScale),
                offset       = new float2(sSurface.XShift, sSurface.YShift),
                uAxis        = new float3(sSurface.UAxis.X, sSurface.UAxis.Y, sSurface.UAxis.Z),
                vAxis        = new float3(sSurface.VAxis.X, sSurface.VAxis.Y, sSurface.VAxis.Z),
                rotation     = sSurface.Rotation,
                materialname = sSurface.TextureName,
                // these parts need to be fixed later
                textureSize  = new float2(64, 64),
                color        = new Color(1, 1, 1, 1)
            };

            return attributes;
        }

        private static List<float3> GetPoints(this List<System.Numerics.Vector3> vertices)
        {
            var points = new List<float3>();
            for (var index = 0; index < vertices.Count; index++)
            {
                var v = vertices[index];
                points.Add(new float3(v.X, v.Y, v.Z));
            }

            return points;
        }
        
        private static Unity.Mathematics.Geometry.Plane GetPlane(this System.Numerics.Plane nPlane)
        {
            return new Unity.Mathematics.Geometry.Plane(new float3(nPlane.Normal.X, nPlane.Normal.Z, nPlane.Normal.Y), nPlane.D);
        }
    }
}