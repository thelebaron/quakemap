using System.Collections.Generic;
using System.Linq;
using Mapping;
using Sledge.Formats.Map.Objects;
using Tools;
using Unity.Mathematics;
using UnityEngine;
using Mesh = UnityEngine.Mesh;

namespace ScriptsSandbox.Util
{
    [ExecuteAlways]
    public class GeometryBuilder : MonoBehaviour
    {
        [SerializeField] private float    scale   = 0.03125f;
        [SerializeField] private Material defaultMaterial;

        private QuakeMap QuakeMap;

        private List<GameObject> generatedObjects = new List<GameObject>();

        private void OnEnable()
        {
            QuakeMap = GetComponent<QuakeMap>();
            QuakeMap.LoadMapFile();

            ClearGeneratedObjects();
            CreateMap();
        }

        private void OnDisable()
        {
            ClearGeneratedObjects();
        }

        private void CreateMap()
        {
            // Create a parent object for all the generated meshes
            var mapParent = new GameObject($"QuakeMap_");
            generatedObjects.Add(mapParent);

            mapParent.transform.SetParent(transform);
            GetSolidChildren(QuakeMap.Map.Worldspawn);
        }

        private void GetSolidChildren(MapObject mapObject)
        {
            if (mapObject is not Sledge.Formats.Map.Objects.Entity)
            {
                Debug.Log($"mapObject is not Sledge.Formats.Map.Objects.Entity but is: {mapObject.GetType()} with {mapObject.Children.Count} children");
            }

            var entity = (Entity)mapObject;
            var solids = entity.Children.Where(x => x is Solid).Cast<Solid>();
            //Debug.Log($"Found {solids.Count()} solids");
            foreach (var solid in solids)
                ExtractSolidFaces(solid);
        }

        private void ExtractSolidFaces(Solid solid)
        {
            var faces = solid.Faces;
            //Debug.Log($"Found {solid.Faces.Count()} faces");

            var allQfaces = new List<QFace>();
            
            foreach (var face in faces)
            {
                //Debug.Log($"Found {face.Vertices.Count()} face vertices");
                var go = new GameObject("face");
                generatedObjects.Add(go);
                var meshFilter   = go.AddComponent<MeshFilter>();
                var meshRenderer = go.AddComponent<MeshRenderer>();
                var qFace        = go.AddComponent<QFace>();
                qFace.Copy(face);
                meshFilter.sharedMesh       = CreateFaceMesh(face, qFace);
                meshRenderer.sharedMaterial = defaultMaterial;
                allQfaces.Add(qFace);
                
                go.transform.SetParent(generatedObjects[0].transform);
            }

            foreach (var qFace in allQfaces)
            {
                qFace.connectedFaces = allQfaces;
            }
        }

        private Mesh CreateFaceMesh(Face face, QFace qFace)
        {
            // Get texture dimensions for UV calculation
            int texWidth    = defaultMaterial?.mainTexture != null ? defaultMaterial.mainTexture.width : 64;
            int texHeight   = defaultMaterial?.mainTexture != null ? defaultMaterial.mainTexture.height : 64;
            var textureSize = new int2(texWidth, texHeight);
            var mesh        = new UnityEngine.Mesh();

            // Get the face normal from the plane - use consistent coordinate system
            // INVERT THE NORMAL by negating it
            var normal    = -face.Plane.Normal.AsVector3(true);       // true means swap Y and Z, negated to invert
            var plane     = new Plane(normal, -face.Plane.D * scale); // Scale plane distance and negate it
            {
                qFace.Normal = normal;
            }
            
            // Convert vertices from Sledge to Unity coordinate system (swap Y and Z)
            var vertices = face.Vertices.Select(v => new float3(v.X, v.Z, v.Y)).ToList();
            
            // First calculate UVs for each vertex and store in a dictionary with the vertex as key
            var vertexToUV = new Dictionary<Vector3, Vector2>();
            for (int i = 0; i < vertices.Count; i++)
            {
                var vertex = vertices[i];
                
                // Create the paraxial coordinate system
                var    faceAttribs = Paraxial.BrushFaceAttributes.GetFromFace(face);
                var    uvSystem    = new Paraxial.ParaxialUVCoordSystem(normal, faceAttribs);
                float2 uv          = uvSystem.uvCoords(vertex, faceAttribs, textureSize);
                
                vertexToUV[vertex] = uv;
            }
            
            // Sort vertices in clockwise order for proper triangulation
            // With inverted normal, we'll sort in the same way but interpret winding differently
            vertices = QMeshing.SortVertices(vertices, plane);
            
            // Now assign UVs based on the sorted vertices
            var uvs = new List<Vector2>();
            for (int i = 0; i < vertices.Count; i++)
            {
                var vertexKey = new Vector3(vertices[i].x, vertices[i].y, vertices[i].z);
                uvs.Add(vertexToUV[vertexKey]);
            }
            
            // Create triangles using fan triangulation 
            // INVERTED winding order (counter-clockwise instead of clockwise)
            var triangles = new List<int>();
            for (int i = 1; i < vertices.Count - 1; i++)
            {
                // Reverse the order of indices to flip the triangle face
                triangles.Add(0);
                triangles.Add(i + 1); // Swapped with i
                triangles.Add(i);     // Swapped with i+1
            }
            
            vertices = QMeshing.RescaleVertices(vertices, scale);
            // Convert to Vector3 for Unity mesh
            var verts = new List<Vector3>();
            for (int i = 0; i < vertices.Count; i++)
                verts.Add(vertices[i]);

            mesh.SetVertices(verts);
            mesh.SetTriangles(triangles.ToArray(), 0);
            mesh.uv = uvs.ToArray();
            
            mesh.RecalculateBounds();
            // Let Unity recalculate the normals, which will respect our triangle winding order
            mesh.RecalculateNormals();

            return mesh;
        }

        private void ClearGeneratedObjects()
        {
            foreach (var obj in generatedObjects)
            {
                if (obj != null)
                    DestroyImmediate(obj);
            }

            generatedObjects.Clear();
        }      
        
        public void Generate()
        {
            OnEnable();
        }
    }
}