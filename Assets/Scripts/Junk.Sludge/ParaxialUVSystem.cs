using Sledge.Formats.Map.Objects;
using Unity.Mathematics;
using UnityEngine;
using Plane = Unity.Mathematics.Geometry.Plane;

namespace Junk.Sludge
{
    public static class Paraxial
    {
        /// <summary>
        /// this method provides a way to round a number to a specific number of decimal places, with the added ability to apply an offset before the rounding operation.
        /// </summary>
        public static double Correct(double value, int precision = 4, double offset = 0.0)
        {
            double factor = math.pow(10, precision); 
            return math.round((value + offset) * factor) / factor;
        }

        public static double2 Correct(double2 vector, int precision = 4, double offset = 0.0)
        {
            double factor = math.pow(10, precision); return new double2( math.round((vector.x + offset) * factor) / factor, math.round((vector.y + offset) * factor) / factor );
        }

        public static double3 Correct(double3 vector, int precision = 4, double offset = 0.0)
        {
            double factor = math.pow(10, precision); 
            return new double3( math.round((vector.x + offset) * factor) / factor, math.round((vector.y + offset) * factor) / factor, math.round((vector.z + offset) * factor) / factor );
        }
        
        public class ParaxialUVCoordSystem
        {
            public int     m_index = 0;
            public double3 m_uAxis;
            public double3 m_vAxis;
            
            public ParaxialUVCoordSystem(float3 normal,  BrushFaceAttributes attribs)
            {
                setRotation(normal, 0.0f, (float)attribs.Rotation);
            }
            
            public float2 uvCoords(float3 point, BrushFaceAttributes attribs, float2 textureSize)
            {
                var uv = (computeUVCoords(point, attribs.Scale()) + attribs.Offset()) / textureSize;
                return uv;
            }
            
            float2 computeUVCoords(float3 point, float2 scale)
            {
                var s = math.dot(point, m_uAxis)  / (scale.x == 0 ? 1e-5f : scale.x);
                var t =  math.dot(point, m_vAxis) / (scale.y == 0 ? 1e-5f : scale.y);
                return new float2((float)s, (float)t);
            }
            
            public void setRotation(float3 normal, float oldAngle, float newAngle)
            {
                m_index               = planeNormalIndex(normal);
                (m_uAxis, m_vAxis, _) = axes(m_index);
                (m_uAxis, m_vAxis)    = rotateAxes((float3)m_uAxis, (float3)m_vAxis, math.radians((double)newAngle), m_index);
            }
            
            
            private (double3, double3) rotateAxes(
                float3 uAxis, 
                float3 vAxis, 
                double angleInRadians, 
                long   planeNormIndex)
            {
                var rotAxis = math.cross(
                    BaseAxes[planeNormIndex * 3 + 2], 
                    BaseAxes[planeNormIndex * 3 + 1]);
    
                var rot = quaternion.AxisAngle(
                    (float3)rotAxis, 
                    (float)angleInRadians);

                // Converting to Unity's math handling
                double3 rotatedUAxis = Correct(math.rotate(rot, (float3)uAxis));
                double3 rotatedVAxis = Correct(math.rotate(rot, (float3)vAxis));
    
                return (rotatedUAxis, rotatedVAxis);
            }

        }
        
        // Structures used in the original code
        // this uses floating point math
        struct ParaxialAttribs
        {
            public float  rotation;
            public float2 scale;
            public float2 offset;
            
            public static ParaxialAttribs Default()
            {
                var attribs = new ParaxialAttribs
                {
                    rotation = 0.0f,
                    scale    = new float2(1,1),
                    offset   = float2.zero
                };
                return attribs;
            }
        }
        
        struct ParaxialAttribsNoOffset
        {
            public float  rotate;
            public float2 scale;

            public static ParaxialAttribsNoOffset Default()
            {
                var attribs = new ParaxialAttribsNoOffset
                {
                    rotate = 0.0f,
                    scale  = new float2(1,1)
                };
                return attribs;
            }
        }
    
        // resides in brush file
        public class BrushFaceAttributes
        {
            public string MaterialName ;
            public double XOffset      ;
            public double YOffset      ;
            public double Rotation     ;
            public double XScale       ;
            public double YScale       ;
            public double XShift       ;   // Only for map formats other than Quake
            public double YShift       ;   // Only for map formats other than Quake
            public int    SurfaceContents; // Optional, Daikatana
            public int    SurfaceFlags ;   // Optional, Daikatana
            public double SurfaceValue ;   // Optional, Daikatana
            public Color  Color        ;   // Optional, Daikatana

            public float2  Scale()  => new((float)XScale, (float)YScale);
            public float2  Offset() => new ((float)XOffset,(float) YOffset);
            public double2 Shift()  => new double2(XShift, YShift);
            
            public static BrushFaceAttributes GetFromFace(Face face)
            {
                return new BrushFaceAttributes
                {
                    // Map Surface properties to BrushFaceAttributes
                    MaterialName    = face.TextureName,  // TextureName maps to MaterialName
                    XOffset         = face.XShift,       // XShift likely aligns with XOffset
                    YOffset         = face.YShift,       // YShift likely aligns with YOffset
                    Rotation        = face.Rotation,     // Direct mapping
                    XScale          = face.XScale,       // Direct mapping (float to double)
                    YScale          = face.YScale,       // Direct mapping (float to double)
                    XShift          = face.XShift,       // Direct mapping for non-Quake formats
                    YShift          = face.YShift,       // Direct mapping for non-Quake formats
                    SurfaceContents = face.ContentFlags, // ContentFlags maps to SurfaceContents
                    SurfaceFlags    = face.SurfaceFlags, // Direct mapping
                    SurfaceValue    = face.Value,        // Value maps to SurfaceValue
                    Color           = default
                };
            }
        }
        private static readonly float3[] BaseAxes = new float3[]
        {
            // Original: (0.0f, 0.0f, 1.0f) - Z-up normal
            // Transformed: (0.0f, 1.0f, 0.0f) - Y-up normal
            new float3(0.0f, 1.0f, 0.0f),  // 0
            
            // Original: (1.0f, 0.0f, 0.0f) - X-right uAxis (unchanged)
            new float3(1.0f, 0.0f, 0.0f),  // 1
            
            // Original: (0.0f, -1.0f, 0.0f) - -Y forward vAxis
            // Transformed: (0.0f, 0.0f, -1.0f) - -Z forward vAxis
            new float3(0.0f, 0.0f, -1.0f), // 2
            
            // Original: (0.0f, 0.0f, -1.0f) - -Z down normal
            // Transformed: (0.0f, -1.0f, 0.0f) - -Y down normal
            new float3(0.0f, -1.0f, 0.0f), // 3
            
            // Original: (1.0f, 0.0f, 0.0f) - X-right uAxis (unchanged)
            new float3(1.0f, 0.0f, 0.0f),  // 4
            
            // Original: (0.0f, -1.0f, 0.0f) - -Y forward vAxis
            // Transformed: (0.0f, 0.0f, -1.0f) - -Z forward vAxis
            new float3(0.0f, 0.0f, -1.0f), // 5
            
            // Original: (1.0f, 0.0f, 0.0f) - X-right normal
            new float3(1.0f, 0.0f, 0.0f),  // 6
            
            // Original: (0.0f, 1.0f, 0.0f) - Y-up uAxis
            // Transformed: (0.0f, 0.0f, 1.0f) - Z-up uAxis
            new float3(0.0f, 0.0f, 1.0f),  // 7
            
            // Original: (0.0f, 0.0f, -1.0f) - -Z back vAxis
            // Transformed: (0.0f, -1.0f, 0.0f) - -Y back vAxis
            new float3(0.0f, -1.0f, 0.0f), // 8
            
            // Original: (-1.0f, 0.0f, 0.0f) - -X left normal
            new float3(-1.0f, 0.0f, 0.0f), // 9
            
            // Original: (0.0f, 1.0f, 0.0f) - Y-up uAxis
            // Transformed: (0.0f, 0.0f, 1.0f) - Z-up uAxis
            new float3(0.0f, 0.0f, 1.0f),  // 10
            
            // Original: (0.0f, 0.0f, -1.0f) - -Z back vAxis
            // Transformed: (0.0f, -1.0f, 0.0f) - -Y back vAxis
            new float3(0.0f, -1.0f, 0.0f), // 11
            
            // Original: (0.0f, 1.0f, 0.0f) - Y-up normal
            // Transformed: (0.0f, 0.0f, 1.0f) - Z-up normal
            new float3(0.0f, 0.0f, 1.0f),  // 12
            
            // Original: (1.0f, 0.0f, 0.0f) - X-right uAxis (unchanged)
            new float3(1.0f, 0.0f, 0.0f),  // 13
            
            // Original: (0.0f, 0.0f, -1.0f) - -Z back vAxis
            // Transformed: (0.0f, -1.0f, 0.0f) - -Y back vAxis
            new float3(0.0f, -1.0f, 0.0f), // 14
            
            // Original: (0.0f, -1.0f, 0.0f) - -Y down normal
            // Transformed: (0.0f, 0.0f, -1.0f) - -Z down normal
            new float3(0.0f, 0.0f, -1.0f), // 15
            
            // Original: (1.0f, 0.0f, 0.0f) - X-right uAxis (unchanged)
            new float3(1.0f, 0.0f, 0.0f),  // 16
            
            // Original: (0.0f, 0.0f, -1.0f) - -Z back vAxis
            // Transformed: (0.0f, -1.0f, 0.0f) - -Y back vAxis
            new float3(0.0f, -1.0f, 0.0f)  // 17
        };
        // BaseAxes array that is used in the TrenchBroom implementation
        // This matches the axes array in ParaxialUVCoordSystem.cpp
        private static readonly float3[] TBBaseAxes = new float3[]
        {
            new float3(0.0f, 0.0f, 1.0f),  // 0
            new float3(1.0f, 0.0f, 0.0f),  // 1
            new float3(0.0f, -1.0f, 0.0f), // 2
            new float3(0.0f, 0.0f, -1.0f), // 3
            new float3(1.0f, 0.0f, 0.0f),  // 4
            new float3(0.0f, -1.0f, 0.0f), // 5
            new float3(1.0f, 0.0f, 0.0f),  // 6
            new float3(0.0f, 1.0f, 0.0f),  // 7
            new float3(0.0f, 0.0f, -1.0f), // 8
            new float3(-1.0f, 0.0f, 0.0f), // 9
            new float3(0.0f, 1.0f, 0.0f),  // 10
            new float3(0.0f, 0.0f, -1.0f), // 11
            new float3(0.0f, 1.0f, 0.0f),  // 12
            new float3(1.0f, 0.0f, 0.0f),  // 13
            new float3(0.0f, 0.0f, -1.0f), // 14
            new float3(0.0f, -1.0f, 0.0f), // 15
            new float3(1.0f, 0.0f, 0.0f),  // 16
            new float3(0.0f, 0.0f, -1.0f)  // 17
        };
        
        /// <summary>
        /// Determines the index of the plane normal in the BaseAxes array
        /// This matches the TrenchBroom implementation in ParaxialUVCoordSystem::planeNormalIndex
        /// </summary>
        public static int planeNormalIndex(float3 normal)
        {
            int   bestIndex = 0;
            float bestDot   = 0;
            // We have 6 sets, each storing normal/u/v in sequence => 6 * 3 = 18 total
            // The normal is at BaseAxes[i*3 + 0].
            for (int i = 0; i < 6; i++)
            {
                var curDot = math.dot(normal, BaseAxes[i*3 + 0]);
                if (curDot > bestDot)
                {
                    bestDot   = curDot;
                    bestIndex = i;
                }
            }
            return bestIndex;
        }
        
        // Returns (uAxis, vAxis, planeNormal)
        public static (float3, float3, float3) axes(int index)
        {
            return (
                BaseAxes[index *3 + 1],
                BaseAxes[index *3 + 2],
                BaseAxes[index *3 + 0]
            );
        }
        
        static (float3, float3, float3) uvAxesFromFacePlane(Plane plane)
        {
            int index = planeNormalIndex(plane.Normal);
            var (u, v, p) = axes(index);
            return ((u), (v), -(p));
        }
        
        static float2 projectToAxisPlane(float3 snappedNormal, float3 point)
        {
            var (s, t) = getSTAxes(snappedNormal);
            return new float2(point[s], point[t]);
        }

        static (int, int) getSTAxes(float3 snappedNormal)
        {
            // If normal.x != 0 => use (1,2), if normal.y != 0 => use (0,2), else => (0,1)
            if (math.abs(snappedNormal.x) > 1e-6f) return (1,2);
            if (math.abs(snappedNormal.y) > 1e-6f) return (0,2);
            return (0,1);
        }
        
        static float2 getUVCoordsAtPoint(ParaxialAttribs attribs, Plane facePlane, float3 point)
        {
            var tempAttribs = new BrushFaceAttributes();
            tempAttribs.Rotation = attribs.rotation;
            tempAttribs.XScale = attribs.scale.x;
            tempAttribs.YScale = attribs.scale.y;
            tempAttribs.XOffset = attribs.offset.x;
            tempAttribs.YOffset = attribs.offset.y;

            var temp = new ParaxialUVCoordSystem(facePlane.Normal, tempAttribs);
            return temp.uvCoords(point, tempAttribs, new float2(1,1));
        }
        
        static float clockwiseDegreesBetween(float2 start, float2 end)
        {
            start = math.normalizesafe(start);
            end   = math.normalizesafe(end);

            var cosAngle        = math.max(-1.0f, math.min(1.0f, math.dot(start, end)));
            var unsignedDegrees = math.degrees(math.acos(cosAngle));

            if (unsignedDegrees < 0.000001f)
            {
                return 0.0f;
            }

            // get a normal for the rotation plane using the right-hand rule if this is pointing up
            // (vm::vec3f(0,0,1)), it's counterclockwise rotation. if this is pointing down
            // (vm::vec3f(0,0,-1)), it's clockwise rotation.
            var rotationNormal = math.normalizesafe(math.cross(new float3(start, 0.0f), new float3(end, 0.0f)));

            var normalsCosAngle = math.dot(rotationNormal, new float3(0, 0, 1));
            if (normalsCosAngle >= 0)
            {
                // counterclockwise rotation
                return -unsignedDegrees;
            }
            // clockwise rotation
            return unsignedDegrees;
        }
        
        static float mat2x2_extract_rotation_degrees(float2x2 m)
        {
            // The code transforms (1,0) and sees how it's rotated
            float2 point    = math.mul(m, new float2(1,0));
            float  rotation = math.atan2(point.y, point.x);
            return math.degrees(rotation);
        }
        
        static float2x2 mat2x2_rotation_degrees(float degreesVal)
        {
            float r = math.radians(degreesVal);
            float c = math.cos(r);
            float s = math.sin(r);
            return new float2x2(c, -s, s,  c);
        }
        
        static ParaxialAttribsNoOffset extractParaxialAttribs(float2x2 M, Plane facePlane, bool preserveU)
        {
            // Check for shear
            var uVec = new float2(M.c0.x, M.c1.x);
            var vVec = new float2(M.c0.y, M.c1.y);

            var cosAngle = math.dot(math.normalizesafe(uVec), math.normalizesafe(vVec));
            if (math.abs(cosAngle) > 0.001f)
            {
                // has shear
                if (preserveU)
                {
                    float degreesToV = clockwiseDegreesBetween(uVec, vVec);
                    bool clockwise = (degreesToV > 0f);

                    // turn 90 degrees from xVec
                    var crossBase = new float3(0,0, clockwise ? -1f : 1f);
                    var newVdir3  = math.normalizesafe(math.cross(crossBase, new float3(uVec,0)));
                    var newVdir   = new float2(newVdir3.x, newVdir3.y);
                    
                    // scalar projection of the old vVec onto newVDir to get the new vScale
                    float newVscale = math.dot(vVec, newVdir);
                    vVec = newVdir * newVscale;
                }
                else
                {
                    float degToU = clockwiseDegreesBetween(vVec, uVec);
                    bool clockwise = (degToU > 0f);

                    // turn 90 degrees from Yvec
                    var crossBase = new float3(0,0, clockwise ? -1f : 1f);
                    var newUdir3  = math.normalize(math.cross(crossBase, new float3(vVec,0)));
                    var newUdir   = new float2(newUdir3.x, newUdir3.y);

                    // scalar projection of the old uVec onto newUDir to get the new uScale
                    float newUscale = math.dot(uVec, newUdir);
                    uVec = newUdir * newUscale;
                }

                // Overwrite M
                M.c0 = new float2(uVec.x, uVec.y);
                M.c1 = new float2(vVec.x, vVec.y);
            }

            var absUScale = math.sqrt(uVec.x *uVec.x + uVec.y *uVec.y);
            var absVScale = math.sqrt(vVec.x *vVec.x + vVec.y *vVec.y);

            float2x2 applyAbsScaleM = new float2x2(absUScale, 0, 0, absVScale);
            // get plane uv axes
            var (faceU, faceV, snappedNormal) = uvAxesFromFacePlane(facePlane);
            float2 uAxis2 = projectToAxisPlane(snappedNormal, faceU);
            float2 vAxis2 = projectToAxisPlane(snappedNormal, faceV);
            float2x2 axisFlipsM = new float2x2(uAxis2.x, vAxis2.x, uAxis2.y, vAxis2.y);

            float2x2? applyAbsScaleMInv = math.inverse(applyAbsScaleM);
            float2x2? axisFlipsMInv     = math.inverse(axisFlipsM);
            if (applyAbsScaleM.Equals(float2x2.zero) || applyAbsScaleMInv.Equals(float2x2.zero))
                return default;
            
            float2x2 flipRotate = math.mul(math.mul(applyAbsScaleMInv.Value, M), axisFlipsMInv.Value);

            // We don't know the signs on the scales, which will mess up figuring out the rotation,
            // so try all 4 combinations
            float[] signVals = new float[] { -1f, 1f };
            for (int i=0; i<signVals.Length; i++)
            {
                float uSign = signVals[i];
                for (int j=0; j<signVals.Length; j++)
                {
                    // "apply" - matrix constructed to apply a guessed value
                    // "guess" - this matrix might not be what we think

                    float vSign = signVals[j];
                    float2x2 applyGuessedFlipM = new float2x2(uSign, 0, 0, vSign);

                    float2x2 inv = math.inverse(applyGuessedFlipM);
                    if (inv.Equals(float2x2.zero))
                        continue;

                    float2x2 rotateMGuess = math.mul(inv, flipRotate);
                    float    angleGuess   = mat2x2_extract_rotation_degrees(rotateMGuess);

                    float2x2 applyAngleGuessM = mat2x2_rotation_degrees(angleGuess);
                    float2x2 Mguess = math.mul(math.mul(math.mul(applyGuessedFlipM, applyAbsScaleM), applyAngleGuessM), axisFlipsM);

                    // Compare with M
                    if (math.abs(M.c0.x - Mguess.c0.x) < 0.001f && math.abs(M.c0.y - Mguess.c0.y) < 0.001f &&
                        math.abs(M.c1.x - Mguess.c1.x) < 0.001f && math.abs(M.c1.y - Mguess.c1.y) < 0.001f)
                    {
                        var found = ParaxialAttribsNoOffset.Default();
                        found.rotate = angleGuess;
                        // scale is (uSign/absU, vSign/absV)
                        // note: original code had "uScaleSign / float(absUScale)"
                        float su = (absUScale < 1e-6f) ? 1f : (uSign / absUScale);
                        float sv = (absVScale < 1e-6f) ? 1f : (vSign / absVScale);
                        found.scale = new float2(su, sv);
                        
                        return found;
                    }
                }
            }
            return default;
        }

        
         static ParaxialAttribs uvCoordMatrixToParaxial(
            Plane faceplane,
            float4x4 worldToUVSpace,
            float3[] facePoints)
        {
            // get the un-rotated axes from face
            var (unrotU, unrotV, snappedNormal) = uvAxesFromFacePlane(faceplane);

            // get the uv's of the 3 reference points
            float2[] facepointsUVs = new float2[3];
            for (int i=0; i<3; i++)
            {
                float4 w = math.mul(worldToUVSpace, new float4(facePoints[i], 1));
                facepointsUVs[i] = new float2(w.x, w.y);
            }

            // project the 3 reference points onto axis plane
            float2[] facepointsProjected = new float2[3];
            for (int i=0; i<3; i++)
                facepointsProjected[i] = projectToAxisPlane(snappedNormal, facePoints[i]);

            // form 2 vectors
            float2 p0p1 = facepointsProjected[1] - facepointsProjected[0];
            float2 p0p2 = facepointsProjected[2] - facepointsProjected[0];
            float2 p0p1UV = facepointsUVs[1] - facepointsUVs[0];
            float2 p0p2UV = facepointsUVs[2] - facepointsUVs[0];

            // build M
            float4x4 M = new float4x4(
                p0p1.x, p0p1.y, 0,         0,
                0,       0,      p0p1.x,  p0p1.y,
                p0p2.x, p0p2.y, 0,         0,
                0,       0,      p0p2.x,  p0p2.y
            );

            float4x4 Minv = math.inverse(M);
            if (Minv.Equals(float4x4.zero))//!Minv.HasValue))
                return default;

            float4 abcd = math.mul(Minv, new float4(p0p1UV.x, p0p1UV.y, p0p2UV.x, p0p2UV.y));
            float2x2 uvPlaneToUV = new float2x2(abcd.x, abcd.y, abcd.z, abcd.w);

            // Try extracting a ParaxialAttribsNoOffset
            var result = extractParaxialAttribs(uvPlaneToUV, faceplane, false);
            if (result.scale.Equals(float2.zero))
            {
                Debug.LogError("fixme");
                return default;
            }

            // figure out texture offset by testing one point
            ParaxialAttribsNoOffset r = result;
            float3 testPoint = facePoints[0];

            float2 testActualUV = getUVCoordsAtPoint(
                new ParaxialAttribs { rotation = r.rotate, scale = r.scale, offset=float2.zero },
                faceplane, testPoint
            );
            float4 w2            = math.mul(worldToUVSpace, new float4(testPoint,1));
            float2 testDesiredUV = new float2(w2.x, w2.y);

            float2 off = testDesiredUV - testActualUV;

            ParaxialAttribs finalVal;
            finalVal.rotation = r.rotate;
            finalVal.scale = r.scale;
            finalVal.offset = off;
            return finalVal;
        }

    }
}