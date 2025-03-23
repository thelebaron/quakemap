using UnityEngine;

public class UvShell : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        FixUVs();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Fixes UVs on a planar mesh to create a clean rectangular UV shell without triangulation.
    /// </summary>
    [ContextMenu("Fix UVs")]
    public void FixUVs()
    {
        // Get the MeshFilter component
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogError("No MeshFilter or mesh found on UvShell object.");
            return;
        }

        // Create a new mesh to avoid modifying the original
        Mesh mesh = Instantiate(meshFilter.sharedMesh);
        
        // Get mesh vertices
        Vector3[] vertices = mesh.vertices;
        if (vertices.Length == 0)
        {
            Debug.LogError("Mesh has no vertices.");
            return;
        }

        // Find the min and max points to determine the bounds of the planar mesh
        Vector3 min = vertices[0];
        Vector3 max = vertices[0];
        
        for (int i = 1; i < vertices.Length; i++)
        {
            min.x = Mathf.Min(min.x, vertices[i].x);
            min.y = Mathf.Min(min.y, vertices[i].y);
            min.z = Mathf.Min(min.z, vertices[i].z);
            
            max.x = Mathf.Max(max.x, vertices[i].x);
            max.y = Mathf.Max(max.y, vertices[i].y);
            max.z = Mathf.Max(max.z, vertices[i].z);
        }
        
        // Determine which axis is constant (the normal direction of the plane)
        float xRange = max.x - min.x;
        float yRange = max.y - min.y;
        float zRange = max.z - min.z;
        
        int normalAxis = 0; // 0 = X, 1 = Y, 2 = Z
        if (xRange < 0.001f) normalAxis = 0;
        else if (yRange < 0.001f) normalAxis = 1;
        else if (zRange < 0.001f) normalAxis = 2;
        
        // Create new UVs based on the planar coordinates
        Vector2[] uvs = new Vector2[vertices.Length];
        
        for (int i = 0; i < vertices.Length; i++)
        {
            switch (normalAxis)
            {
                case 0: // X is constant (YZ plane)
                    uvs[i] = new Vector2(
                        (vertices[i].y - min.y) / (max.y - min.y),
                        (vertices[i].z - min.z) / (max.z - min.z)
                    );
                    break;
                case 1: // Y is constant (XZ plane)
                    uvs[i] = new Vector2(
                        (vertices[i].x - min.x) / (max.x - min.x),
                        (vertices[i].z - min.z) / (max.z - min.z)
                    );
                    break;
                case 2: // Z is constant (XY plane)
                default:
                    uvs[i] = new Vector2(
                        (vertices[i].x - min.x) / (max.x - min.x),
                        (vertices[i].y - min.y) / (max.y - min.y)
                    );
                    break;
            }
        }
        
        // Apply the new UVs to the mesh
        mesh.uv = uvs;
        
        // Assign the modified mesh back to the MeshFilter
        meshFilter.mesh = mesh;
        
        Debug.Log("UVs fixed on planar mesh.");
    }
}