using System;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TestTools;


namespace BSP.Tests.Editor
{
public class BaseFaceTests
{
    // This runs before each test, ensuring we have a fresh state
    [SetUp]
    public void SetUp()
    {
        Bsp.Init();
    }
    
    /// <summary>
    /// Creates a face with a triangle in Quake's coordinate system (XZY)
    /// In Quake's system, XZ is the ground plane and Y is up
    /// </summary>
    /// <param name="planenum">Plane number to use for the face</param>
    /// <param name="planeside">Plane side to use for the face</param>
    /// <param name="texturenum">Texture number to use for the face</param>
    /// <returns>A face with a triangle in Quake's coordinate system</returns>
    public static face_t GetTriangleQuake(int planenum = 0, int planeside = 0, int texturenum = 1)
    {
        var points = new float3[]
        {
            new float3(0, 0, 0),  // Origin
            new float3(1, 0, 0),  // 1 unit along X
            new float3(0, 0, 1)   // 1 unit along Z
        };
        var face = new face_t
        {
            planenum = planenum,
            planeside = planeside,
            texturenum = texturenum,
            numpoints = points.Length,
            pts = (float3[])points.Clone(),
            normal = CalculateTriangleNormal(points)
        };

        return face;
    }

    
    public static face_t GetTriangleQuakeFloor(int planenum = 0, int planeside = 0, int texturenum = 1)
    {
        var points = new float3[]
        {
            new float3(0, 0, 0), // Origin on floor
            new float3(0, 1, 0), // 1 unit along Y on floor
            new float3(1, 0, 0)  // 1 unit along X on floor
        };
    
        /*
         *    [1] (0, 1, 0)
         *    *
         *    | \
         *    |  \
         *    |   \
         *    *----*
         * [0](0,0,0) [2](1,0,0)
         */
    
        var face = new face_t
        {
            planenum   = planenum,
            planeside  = planeside,
            texturenum = texturenum,
            numpoints  = points.Length,
            pts        = (float3[])points.Clone(),
            normal = CalculateTriangleNormal(points)
        };
        return face;
    }
    
    public static float3 CalculateTriangleNormal(float3[] points)
    {
        // Ensure we have a triangle
        if (points.Length < 3)
            return new float3(0, 0, 0);
        
        // Get two edges of the triangle
        float3 edge1 = points[1] - points[0];
        float3 edge2 = points[2] - points[0];
    
        // Cross product to get the normal (right-hand rule)
        // In Quake, this gives us the direction the face points away from
        float3 normal = math.cross(edge1, edge2);
    
        // Normalize to get a unit vector
        float length = math.sqrt(normal.x * normal.x + normal.y * normal.y + normal.z * normal.z);
        if (length > 0.0001f)
        {
            normal.x /= length;
            normal.y /= length;
            normal.z /= length;
        }
    
        return normal;
    }
    
    /// <summary>
    /// Creates a face with a quad in Quake's coordinate system (XZY)
    /// In Quake's system, XZ is the ground plane and Y is up
    /// </summary>
    /// <param name="planenum">Plane number to use for the face</param>
    /// <param name="planeside">Plane side to use for the face</param>
    /// <param name="texturenum">Texture number to use for the face</param>
    /// <returns>A face with a quad in Quake's coordinate system</returns>
    public static face_t GetQuadQuake(int planenum = 0, int planeside = 0, int texturenum = 1)
    {
        var points = new float3[]
        {
            new float3(0, 0, 0),    // Origin
            new float3(1, 0, 0),    // 1 unit along X
            new float3(1, 0, 1),    // 1 unit along X and Z
            new float3(0, 0, 1)     // 1 unit along Z
        };

        /*
        Z axis
                   ^
                   |
                   |
        (0,0,1) --------- (1,0,1)
           |                  |
           |                  |
           |       XZ         |
           |      plane       |
           |    (Y = 0)       |
           |                  |
        (0,0,0) --------- (1,0,0)
           |
           |
           +-----------------> X axis

          Y axis is perpendicular to this page
          pointing upward (out of the screen)
         
         */
        var face = new face_t
        {
            planenum = planenum,
            planeside = planeside,
            texturenum = texturenum,
            numpoints = points.Length,
            pts = (float3[])points.Clone()
        };

        return face;
    }
    
    /// <summary>
    /// Sets up a proper plane for a Quake-style face on the XZ plane
    /// </summary>
    /// <param name="planenum">The plane number to set up</param>
    public static void SetupQuakePlane(int planenum = 0, float3 normal = default)
    {
        if (normal.Equals(float3.zero))
            normal = new float3(0, 0, 1);
        
        // In Quake, the normal for an XZ plane would point in the Y direction
        Bsp.planes[planenum] = new plane_t { normal = normal, dist = 0 };
        Bsp.numbrushplanes++;
    }
    
    // Legacy method for backward compatibility
    public static face_t GetFace()
    {
        return GetTriangleQuake();
    }
}
}