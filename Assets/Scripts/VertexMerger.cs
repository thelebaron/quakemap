using UnityEngine;
using System.Collections.Generic;

public class VertexMerger : MonoBehaviour
{
    [Tooltip("Maximum distance between vertices to be merged")]
    public float mergeDistance = 0.01f;
    
    [Tooltip("Material to use for the merged mesh (leave empty to use the first child's material)")]
    public Material sharedMaterial;

    // Structure to hold vertex data
    private class VertexData
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector2 uv;
        public Color color;
        public int originalIndex;

        public VertexData(Vector3 pos, Vector3 norm, Vector2 uvCoord, Color col, int index)
        {
            position = pos;
            normal = norm;
            uv = uvCoord;
            color = col;
            originalIndex = index;
        }
    }

    // Method to merge vertices from all child meshes
    public void MergeChildMeshes()
    {
        // Find all children with MeshFilter components
        List<MeshFilter> childMeshFilters = new List<MeshFilter>();
        foreach (Transform child in transform)
        {
            MeshFilter meshFilter = child.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                childMeshFilters.Add(meshFilter);
            }
        }

        if (childMeshFilters.Count == 0)
        {
            Debug.LogError("No mesh filters found in children.");
            return;
        }

        // Combine meshes first
        CombineInstance[] combineInstances = new CombineInstance[childMeshFilters.Count];
        for (int i = 0; i < childMeshFilters.Count; i++)
        {
            combineInstances[i].mesh = childMeshFilters[i].sharedMesh;
            combineInstances[i].transform = childMeshFilters[i].transform.localToWorldMatrix * transform.worldToLocalMatrix;
        }

        // Create combined mesh
        Mesh combinedMesh = new Mesh();
        combinedMesh.name = "Combined_Mesh";
        combinedMesh.CombineMeshes(combineInstances, true);

        // Process the combined mesh to merge vertices
        Mesh finalMesh = ProcessMesh(combinedMesh);
        finalMesh.name = gameObject.name + "_Merged";

        // Create or get MeshFilter component on this GameObject
        MeshFilter parentMeshFilter = GetComponent<MeshFilter>();
        if (parentMeshFilter == null)
        {
            parentMeshFilter = gameObject.AddComponent<MeshFilter>();
        }
        parentMeshFilter.sharedMesh = finalMesh;

        // Create or get MeshRenderer component on this GameObject
        MeshRenderer parentMeshRenderer = GetComponent<MeshRenderer>();
        if (parentMeshRenderer == null)
        {
            parentMeshRenderer = gameObject.AddComponent<MeshRenderer>();
        }

        // Set material from parameter or first child's material
        if (sharedMaterial != null)
        {
            parentMeshRenderer.sharedMaterial = sharedMaterial;
        }
        else if (childMeshFilters.Count > 0)
        {
            MeshRenderer firstChildRenderer = childMeshFilters[0].GetComponent<MeshRenderer>();
            if (firstChildRenderer != null)
            {
                parentMeshRenderer.sharedMaterials = firstChildRenderer.sharedMaterials;
            }
        }

        // Destroy child GameObjects
        foreach (MeshFilter childFilter in childMeshFilters)
        {
            DestroyImmediate(childFilter.gameObject);
        }

        Debug.Log($"Mesh combining and vertex merging complete. Merged {childMeshFilters.Count} meshes into one with {finalMesh.vertexCount} vertices.");
    }

    private Mesh ProcessMesh(Mesh originalMesh)
    {
        // Get mesh data
        Vector3[] vertices = originalMesh.vertices;
        Vector3[] normals = originalMesh.normals.Length > 0 ? originalMesh.normals : new Vector3[vertices.Length];
        Vector2[] uvs = originalMesh.uv.Length > 0 ? originalMesh.uv : new Vector2[vertices.Length];
        Color[] colors = originalMesh.colors.Length > 0 ? originalMesh.colors : new Color[vertices.Length];
        int[] triangles = originalMesh.triangles;

        // Create a spatial hash to efficiently find nearby vertices
        Dictionary<Vector3Int, List<int>> spatialHash = new Dictionary<Vector3Int, List<int>>();
        float inverseMergeDistance = 1.0f / mergeDistance;
        
        // Hash each vertex position
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3Int cell = new Vector3Int(
                Mathf.FloorToInt(vertices[i].x * inverseMergeDistance),
                Mathf.FloorToInt(vertices[i].y * inverseMergeDistance),
                Mathf.FloorToInt(vertices[i].z * inverseMergeDistance)
            );
            
            if (!spatialHash.ContainsKey(cell))
            {
                spatialHash[cell] = new List<int>();
            }
            spatialHash[cell].Add(i);
        }
        
        // Create mapping from old vertex index to new vertex index
        Dictionary<int, int> vertexMapping = new Dictionary<int, int>();
        List<VertexData> uniqueVertices = new List<VertexData>();
        float sqrMergeDistance = mergeDistance * mergeDistance;
        
        // For each original vertex
        for (int i = 0; i < vertices.Length; i++)
        {
            // Skip if already processed
            if (vertexMapping.ContainsKey(i))
                continue;
                
            Vector3 position = vertices[i];
            Vector3Int cell = new Vector3Int(
                Mathf.FloorToInt(position.x * inverseMergeDistance),
                Mathf.FloorToInt(position.y * inverseMergeDistance),
                Mathf.FloorToInt(position.z * inverseMergeDistance)
            );
            
            // Check 27 neighboring cells (3x3x3 grid)
            List<int> nearbyVertices = new List<int>();
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        Vector3Int neighborCell = new Vector3Int(cell.x + x, cell.y + y, cell.z + z);
                        if (spatialHash.ContainsKey(neighborCell))
                        {
                            nearbyVertices.AddRange(spatialHash[neighborCell]);
                        }
                    }
                }
            }
            
            // Create the first representative vertex from current vertex
            int newVertexIndex = uniqueVertices.Count;
            uniqueVertices.Add(new VertexData(
                position,
                normals[i],
                uvs[i],
                i < colors.Length ? colors[i] : Color.white,
                i
            ));
            
            vertexMapping[i] = newVertexIndex;
            
            // Find vertices to merge with this one
            for (int j = 0; j < nearbyVertices.Count; j++)
            {
                int otherIndex = nearbyVertices[j];
                
                // Skip if already processed or same as current vertex
                if (otherIndex <= i || vertexMapping.ContainsKey(otherIndex))
                    continue;
                    
                // Check if within merge distance
                if (Vector3.SqrMagnitude(position - vertices[otherIndex]) <= sqrMergeDistance)
                {
                    // Use same position (averaged/merged) but KEEP ORIGINAL UVs
                    vertexMapping[otherIndex] = newVertexIndex;
                    
                    // Update position to average (helps preserve mesh shape)
                    uniqueVertices[newVertexIndex].position = (uniqueVertices[newVertexIndex].position + vertices[otherIndex]) * 0.5f;
                    
                    // Normals are averaged for smooth shading
                    uniqueVertices[newVertexIndex].normal = (uniqueVertices[newVertexIndex].normal + normals[otherIndex]).normalized;
                }
            }
        }
        
        // Create new vertex arrays
        Vector3[] newVertices = new Vector3[uniqueVertices.Count];
        Vector3[] newNormals = new Vector3[uniqueVertices.Count];
        Vector2[] newUVs = new Vector2[uniqueVertices.Count];
        Color[] newColors = colors.Length > 0 ? new Color[uniqueVertices.Count] : null;
        
        for (int i = 0; i < uniqueVertices.Count; i++)
        {
            newVertices[i] = uniqueVertices[i].position;
            newNormals[i] = uniqueVertices[i].normal;
            newUVs[i] = uniqueVertices[i].uv;
            if (newColors != null)
            {
                newColors[i] = uniqueVertices[i].color;
            }
        }
        
        // Create new triangle array that refers to the new vertex indices
        int[] newTriangles = new int[triangles.Length];
        
        // UVs are preserved by creating new vertices for different UV mapping
        Dictionary<int, Dictionary<Vector2, int>> vertexUVMapping = new Dictionary<int, Dictionary<Vector2, int>>();
        
        for (int i = 0; i < triangles.Length; i += 3)
        {
            for (int j = 0; j < 3; j++)
            {
                int oldIndex = triangles[i + j];
                int mergedIndex = vertexMapping[oldIndex];
                Vector2 originalUV = uvs[oldIndex];
                
                // Check if this position has this UV already
                if (!vertexUVMapping.ContainsKey(mergedIndex))
                {
                    vertexUVMapping[mergedIndex] = new Dictionary<Vector2, int>(new Vector2EqualityComparer());
                }
                
                if (!vertexUVMapping[mergedIndex].ContainsKey(originalUV))
                {
                    // First time seeing this UV for this position
                    vertexUVMapping[mergedIndex][originalUV] = mergedIndex;
                }
                else
                {
                    // Use the existing vertex with this UV
                    newTriangles[i + j] = vertexUVMapping[mergedIndex][originalUV];
                    continue;
                }
                
                // If the current vertex already has a different UV assigned, create a new vertex
                if (Vector2.SqrMagnitude(newUVs[mergedIndex] - originalUV) > 0.00001f && 
                    vertexUVMapping[mergedIndex].Count > 1)
                {
                    // Different UV for same position - create new vertex
                    int newIndex = newVertices.Length;
                    System.Array.Resize(ref newVertices, newVertices.Length + 1);
                    System.Array.Resize(ref newNormals, newNormals.Length + 1); 
                    System.Array.Resize(ref newUVs, newUVs.Length + 1);
                    if (newColors != null)
                    {
                        System.Array.Resize(ref newColors, newColors.Length + 1);
                    }
                    
                    newVertices[newIndex] = newVertices[mergedIndex];
                    newNormals[newIndex] = newNormals[mergedIndex];
                    newUVs[newIndex] = originalUV;
                    if (newColors != null)
                    {
                        newColors[newIndex] = newColors[mergedIndex];
                    }
                    
                    vertexUVMapping[mergedIndex][originalUV] = newIndex;
                    newTriangles[i + j] = newIndex;
                }
                else
                {
                    // Use the merged vertex (first occurrence)
                    newUVs[mergedIndex] = originalUV; // Ensure UV is set
                    newTriangles[i + j] = mergedIndex;
                }
            }
        }
        
        // Create new mesh
        Mesh newMesh = new Mesh();
        newMesh.name = originalMesh.name + "_Merged";
        
        // Set mesh data
        newMesh.vertices = newVertices;
        newMesh.normals = newNormals;
        newMesh.uv = newUVs;
        if (newColors != null && newColors.Length == newVertices.Length)
        {
            newMesh.colors = newColors;
        }
        newMesh.triangles = newTriangles;
        
        // Recalculate mesh properties
        newMesh.RecalculateBounds();
        if (normals.Length == 0)
        {
            newMesh.RecalculateNormals();
        }
        newMesh.RecalculateTangents();
        
        return newMesh;
    }

    // Helper class for comparing Vector2 values with a small epsilon
    private class Vector2EqualityComparer : IEqualityComparer<Vector2>
    {
        private const float Epsilon = 0.00001f;
        
        public bool Equals(Vector2 x, Vector2 y)
        {
            return Vector2.SqrMagnitude(x - y) < Epsilon;
        }
        
        public int GetHashCode(Vector2 obj)
        {
            return (Mathf.RoundToInt(obj.x * 1000) * 31 + Mathf.RoundToInt(obj.y * 1000)).GetHashCode();
        }
    }
}