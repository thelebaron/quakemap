using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace BSP
{
    public static partial class Bsp
    {
        static float POINT_MERGE_EPSILON = 0.01f;
        
        /// <summary>
        /// Attempts to merge two faces that share a plane.
        /// Faces must be on the same plane and have same texture properties to be considered.
        /// Based on the TryMerge implementation in Quake's qbsp.
        /// </summary>
        /// <param name="f1">First face to try to merge (will become merged face)</param>
        /// <param name="f2">Second face to try to merge (will be discarded if merge succeeds)</param>
        /// <returns>True if faces were successfully merged, false otherwise</returns>
        public static bool TryMerge(face_t f1, face_t f2)
        {
            // Can't merge faces on different planes or with different properties
            if (f1.planenum   != f2.planenum  ||
                f1.planeside  != f2.planeside ||
                f1.texturenum != f2.texturenum)
            {
                UnityEngine.Debug.Log($"TryMerge failed: "       +
                    $"planenum({f1.planenum},{f2.planenum}) "    +
                    $"planeside({f1.planeside},{f2.planeside}) " +
                    $"texturenum({f1.texturenum},{f2.texturenum})");
                return false;
            }

            // Check if faces share at least one edge point (required for merging)
            bool canMerge = false;
            for (int i = 0; i < f1.numpoints; i++)
            {
                int    nexti = (i + 1) % f1.numpoints;
                float3 p1    = f1.pts[i];
                float3 p2    = f1.pts[nexti];

                for (int j = 0; j < f2.numpoints; j++)
                {
                    int    nextj = (j + 1) % f2.numpoints;
                    float3 p3    = f2.pts[j];
                    float3 p4    = f2.pts[nextj];

                    // Check if any edges overlap - they share the same points in either direction
                    if ((PointsEqual(p1, p3, POINT_MERGE_EPSILON) && PointsEqual(p2, p4, POINT_MERGE_EPSILON)) ||
                        (PointsEqual(p1, p4, POINT_MERGE_EPSILON) && PointsEqual(p2, p3, POINT_MERGE_EPSILON)))
                    {
                        canMerge = true;
                        break;
                    }
                }

                if (canMerge) break;
            }

            if (!canMerge)
            {
                
                Debug.Log("cant merge");
                return false;
            }

            // Build a set of unique points combining both faces
            List<float3> mergedPoints = new List<float3>();

            // Add all points from f1
            for (int i = 0; i < f1.numpoints; i++)
            {
                mergedPoints.Add(f1.pts[i]);
            }

            // Add non-duplicate points from f2
            for (int i = 0; i < f2.numpoints; i++)
            {
                bool isDuplicate = false;
                for (int j = 0; j < mergedPoints.Count; j++)
                {
                    if (PointsEqual(f2.pts[i], mergedPoints[j], POINT_MERGE_EPSILON))
                    {
                        isDuplicate = true;
                        break;
                    }
                }

                if (!isDuplicate)
                {
                    mergedPoints.Add(f2.pts[i]);
                }
            }

            // We need at least 3 points to form a valid face
            if (mergedPoints.Count < 3)
            {
                Debug.Log("mergedPoints.Count < 3");
                return false;
            }

            // Get the face normal (should be the same for both faces since they're on the same plane)
            plane_t plane = planes[f1.planenum];

            // Now reorder the points to form a proper convex polygon
            // Find the center for sorting by angle
            float3 center = float3.zero;
            foreach (float3 pt in mergedPoints)
            {
                center += pt;
            }

            center /= mergedPoints.Count;

            // Create reference vectors in the plane for angle calculations
            float3 normal  = plane.normal;
            float3 refVec1 = GetPerpendicularVector(normal);
            float3 refVec2 = math.cross(normal, refVec1);

            // Sort points by angle around the center
            List<KeyValuePair<float, float3>> sortedPoints = new List<KeyValuePair<float, float3>>();

            foreach (float3 pt in mergedPoints)
            {
                float3 dir   = pt - center;
                float  angle = math.atan2(math.dot(dir, refVec2), math.dot(dir, refVec1));
                sortedPoints.Add(new KeyValuePair<float, float3>(angle, pt));
            }

            // Sort by angle
            sortedPoints.Sort((a, b) => a.Key.CompareTo(b.Key));

            // Build the merged face points
            float3[] newPoints = new float3[sortedPoints.Count];
            for (int i = 0; i < sortedPoints.Count; i++)
            {
                newPoints[i] = sortedPoints[i].Value;
            }

            // Check if the resulting polygon is convex
            if (!IsConvexPolygon(newPoints, normal))
            {
                
                Debug.Log("IsConvexPolygon false");
                return false;
            }

            // The merge was successful, update f1 with the merged points
            f1.pts       = newPoints;
            f1.numpoints = newPoints.Length;

            // The caller will need to handle removing f2 from whatever list it was in
            return true;
        }
        
        /// <summary>
        /// Attempts to merge a face with any appropriate face in a list.
        /// Continues trying to merge until no more merges are possible.
        /// </summary>
        /// <param name="face">The face to merge into the list</param>
        /// <param name="faceList">The list of faces to merge with</param>
        /// <returns>True if the face was merged with any faces in the list</returns>
        public static bool MergeFaceToList(face_t face, List<face_t> faceList)
        {
            if (face == null || faceList == null)
                return false;

            bool anyMerges = false; // Track if any merges happened at all
            bool merged;
            
            do
            {
                merged = false;
                
                // Try to merge with each face in the list
                for (int i = 0; i < faceList.Count; i++)
                {
                    face_t listFace = faceList[i];
                    
                    if (TryMerge(face, listFace))
                    {
                        // Face was merged with listFace
                        // Remove listFace from the list and continue trying to merge face
                        faceList.RemoveAt(i);
                        merged = true;
                        anyMerges = true;
                        i--; // Adjust index since we removed an item
                        break;
                    }
                    
                    if (TryMerge(listFace, face))
                    {
                        // listFace was merged with face
                        // face is no longer valid, and listFace has been updated
                        face = listFace;
                        merged = true;
                        anyMerges = true;
                        break;
                    }
                }
            } while (merged); // Continue as long as we found a merge in this pass
            
            // Add the face to the list if it wasn't fully merged
            if (anyMerges)
            {
                // Only add the face if it was modified but not completely consumed
                faceList.Add(face);
            }
            else
            {
                // No merges happened, add the original face
                faceList.Add(face);
            }
            
            return anyMerges;
        }

        /// <summary>
        /// Helper method to remove a face from a linked list
        /// </summary>
        private static face_t RemoveFaceFromList(face_t face, face_t list)
        {
            if (face == list)
            {
                // It's the head of the list
                return face.next;
            }
            
            // Find face in the list
            for (face_t f = list; f != null; f = f.next)
            {
                if (f.next == face)
                {
                    // Remove face from the list
                    f.next = face.next;
                    break;
                }
            }
            
            return list;
        }

        public static bool PointsEqual(float3 a, float3 b, float epsilon)
        {
            return math.lengthsq(a - b) < epsilon * epsilon;
        }

        private static float3 GetPerpendicularVector(float3 normal)
        {
            float3 perp;

            // Find which axis has the smallest component in the normal
            if (math.abs(normal.x) < math.abs(normal.y) && math.abs(normal.x) < math.abs(normal.z))
            {
                perp = new float3(1, 0, 0); // Use X-axis
            }
            else if (math.abs(normal.y) < math.abs(normal.z))
            {
                perp = new float3(0, 1, 0); // Use Y-axis
            }
            else
            {
                perp = new float3(0, 0, 1); // Use Z-axis
            }

            // Make it perpendicular to normal
            perp = math.normalize(perp - normal * math.dot(perp, normal));
            return perp;
        }

        private static bool IsConvexPolygon(float3[] points, float3 normal)
        {
            int numPoints = points.Length;
            Debug.Log("IsConvexPolygon checking " + numPoints + " points");
    
            if (numPoints < 3)
                return false;

            for (int i = 0; i < numPoints; i++)
            {
                int next = (i + 1) % numPoints;

                // Get the edge vector
                float3 edge = points[next] - points[i];

                // Get the edge normal (in the plane)
                float3 edgeNormal = math.normalize(math.cross(edge, normal));
        
                Debug.Log("Edge " + i + " to " + next + ": " + edge + ", normal: " + edgeNormal);

                // Test all other points against this edge
                // They should all be on the same side
                float dist = math.dot(edgeNormal, points[i]);

                for (int j = 0; j < numPoints; j++)
                {
                    if (j == i || j == next)
                        continue;

                    float d = math.dot(edgeNormal, points[j]) - dist;
                    Debug.Log("  Point "                      + j + ": " + points[j] + " dot: " + d);
            
                    if (d < -ON_EPSILON)
                    {
                        Debug.Log("  FAIL: Point is on wrong side (d = " + d + ", -ON_EPSILON = " + (-ON_EPSILON) + ")");
                        return false; // Point is on wrong side - not convex
                    }
                }
            }

            return true;
        }
    }
}