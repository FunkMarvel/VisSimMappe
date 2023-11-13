// //////////////////////////////////////////////////////////////////////////
// //////////////////////////////
// //FileName: TriangleSurface.cs
// //FileType: Visual C# Source file
// //Author : Anders P. Åsbø
// //Created On : 14/09/2023
// //Last Modified On : 20/09/2023
// //Copy Rights : Anders P. Åsbø
// //Description : Class for creating dynamic triangle surfaces at runtime.
// //////////////////////////////////////////////////////////////////////////
// //////////////////////////////

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
///     Class for creating triangle-surface from data files.
/// </summary>
public class TriangleSurface : MonoBehaviour
{
    // properties that are set in editor:
    [SerializeField] private TextAsset vertexFile; // reference to text-file with vertices.
    [SerializeField] private TextAsset indexFile; // reference to text-file with triangulation-data.
    [SerializeField] private Material material; // reference to Unity-material to color mesh with.
    [SerializeField] [Min(1e-6f)] private float resolution = 5.0f;
    private TriangleData _currentTriangle; // for keeping track of ball.
    private MeshBounds _bounds;

    // for checking if mesh has been generated.
    private bool _hasMesh;

    /// <summary>
    ///     Array of vertices.
    /// </summary>
    private Vector3[] Vertices { get; set; }

    /// <summary>
    ///     Dynamic array with triangulation data.
    /// </summary>
    private List<TriangleData> Triangles { get; set; }

    private void Awake()
    {
        // run after every object in scene is created, but before first frame.

        if (!_hasMesh)
        {
            CreateSurface(); // create mesh if necessary.
            _hasMesh = true;
        }

        _currentTriangle = Triangles[0];
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(new Vector3(_bounds.XMin, _bounds.YMax, _bounds.ZMin), 1f);
    }

    /// <summary>
    ///     Project position onto surface along surface normal.
    /// </summary>
    /// <param name="position">Position to find contact of.</param>
    /// <returns>struct with contact point and hit normal. If hit normal is zero vector, then no hit was found.</returns>
    public Contact GetCollision(Vector3 position, bool correctedPos = true)
    {
        // exploit regularity of grid in xz-plane to hash to correct quad:
        var quadsPerStrip = Mathf.FloorToInt(_bounds.Height / resolution);
        var i = Mathf.FloorToInt((position.x - _bounds.XMin) / resolution);
        var j = Mathf.FloorToInt((position.z - _bounds.ZMin) / resolution);
        var triangleIdx = 2 * (j + i * quadsPerStrip);
        
        // ensure index within bounds.
        triangleIdx = triangleIdx < 0 || triangleIdx > Triangles.Count - 1 ? 0 : triangleIdx; 

        _currentTriangle = Triangles[triangleIdx];
        // Debug.Log($"i = {i} | j = {j} | numY = {verticesPerStrip} | calc tri = {triangleIdx} | act tri = {_currentTriangle.index}");
        
        
        while (true)  // search loop.
        {
            // get vertices of current triangle:
            Vector3 p = Vertices[_currentTriangle.Indices[0]],
                q = Vertices[_currentTriangle.Indices[1]],
                r = Vertices[_currentTriangle.Indices[2]];

            var uvw = GetBarycentricCoordinates(position, p, q, r);
            if (uvw is { x: >= 0, y: >= 0, z: >= 0 }) // check if inside triangle
            {
                // calculating point on surface directly below given position:
                var normal = _currentTriangle.SurfaceNormal;
                var hitPosition = uvw.x * p + uvw.y * q + uvw.z * r;

                if (!correctedPos) return new Contact(hitPosition, normal);
                
                // correcting projected point to be closest point from position to surface:
                var diffVec = hitPosition - position;
                hitPosition = position + Vector3.Dot(diffVec, normal) * normal;
                
                return new Contact(hitPosition, normal);
            }

            int opposingIndex;

            // if not inside triangle, find neighbour triangle opposite vertex with smallest coordinate:
            if (uvw.x <= uvw.y && uvw.x <= uvw.z)
                opposingIndex = 0;
            else if (uvw.y <= uvw.z)
                opposingIndex = 1;
            else
                opposingIndex = 2;

            if (_currentTriangle.Neighbours[opposingIndex] >= 0)
            {
                _currentTriangle = Triangles[_currentTriangle.Neighbours[opposingIndex]];
                continue; // if neighbour triangle exists, jump to next iteration of loop.
            }

            // if neighbour triangle was not found, return contact with normal vector set to zero vector.
            Debug.LogWarning("Warning, contact point out of bounds!");
            return new Contact(Vector3.zero, Vector3.zero);
        }
    }

    /// <summary>
    ///     calculate surface normal of triangle from vertices.
    /// </summary>
    /// <param name="p">index of first vertex</param>
    /// <param name="q">index of second vertex</param>
    /// <param name="r">index of third vertex</param>
    /// <returns>Left-handed surface-normal of triangle</returns>
    private Vector3 GetNormalFromTri(int p, int q, int r)
    {
        return Vector3.Cross(Vertices[q] - Vertices[p],
            Vertices[r] - Vertices[p]).normalized;
    }

    /// <summary>
    ///     Calculate barycentric coordinates of point x in triangle with vertices p, q and r.
    /// </summary>
    /// <param name="x">point to locate</param>
    /// <param name="p">first vertex</param>
    /// <param name="q">second vertex</param>
    /// <param name="r">third vertex</param>
    /// <returns>vector with [u, v, w]</returns>
    private static Vector3 GetBarycentricCoordinates(Vector3 x, Vector3 p, Vector3 q, Vector3 r)
    {
        var uvw = Vector3.zero;

        Vector3 pq = q - p, pr = r - p, px = x - p;

        var signedArea = pq.x * pr.z - pr.x * pq.z;
        uvw.y = (px.x * pr.z - pr.x * px.z) / signedArea;
        uvw.z = (pq.x * px.z - px.x * pq.z) / signedArea;
        uvw.x = 1.0f - uvw.y - uvw.z;

        return uvw;
    }

    /// <summary>
    ///     Create surface from datafiles.
    /// </summary>
    private void CreateSurface()
    {
        if (vertexFile == null || indexFile == null)
        {
            Debug.LogWarning("vertexFile or indexFile is null!");
            return;
        }

        // add components for rendering mesh.
        var filter = gameObject.AddComponent<MeshFilter>();
        var meshRenderer = gameObject.AddComponent<MeshRenderer>();

        // assign mesh to render components.
        filter.sharedMesh = GenerateMesh();

        // use chosen material, or default material if nothing is chosen.
        meshRenderer.sharedMaterial = material != null
            ? material
            : AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");

        _hasMesh = true; // tell gizmos to stop reading data when simulation is running.
    }

    /// <summary>
    ///     Read vertex-data from file.
    /// </summary>
    private void ReadVertexData()
    {
        // defines which characters to split file into lines on:
        var fileDelimiters = new[] { "\r\n", "\r", "\n" };

        // defines which characters to split each line on:
        var lineDelimiters = new[] { '(', ')', ',' };

        // split file into array of non-empty lines:
        var lines = vertexFile.text.Split(fileDelimiters, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 1)
        {
            Debug.LogWarning($"{vertexFile.name} was empty!");
            return;
        }

        var numVertices = int.Parse(lines[0]);

        if (numVertices < 1)
        {
            Debug.LogWarning($"{vertexFile.name} contains no vertex data!");
            return;
        }

        var vertices = new Vector3[numVertices];
        
        for (var i = 1; i <= numVertices; i++)
        {
            // split line and read coordinates:
            var elements = lines[i].Split(lineDelimiters, StringSplitOptions.RemoveEmptyEntries);
            if (elements.Length < 3)
            {
                Debug.LogWarning($"{vertexFile.name} is missing data on line {i}");
                continue;
            }

            vertices[i - 1] = new Vector3(
                float.Parse(elements[0], CultureInfo.InvariantCulture),
                float.Parse(elements[1], CultureInfo.InvariantCulture),
                float.Parse(elements[2], CultureInfo.InvariantCulture)
            );
        }

        float xmax, ymax, zmax;
        var xmin = xmax = vertices[0].x;
        var ymin = ymax = vertices[0].y;
        var zmin = zmax = vertices[0].z;

        foreach (var vertex in vertices)
        {
            xmin = Mathf.Min(vertex.x, xmin);
            xmax = Mathf.Max(vertex.x, xmax);
            
            ymin = Mathf.Min(vertex.y, ymin);
            ymax = Mathf.Max(vertex.y, ymax);
            
            zmin = Mathf.Min(vertex.z, zmin);
            zmax = Mathf.Max(vertex.z, zmax);
        }

        _bounds = new MeshBounds(xmin, xmax, ymin, ymax, zmin, zmax);

        Vertices = vertices;
    }

    /// <summary>
    ///     Read triangulation data from file.
    /// </summary>
    private void ReadIndexData()
    {
        Triangles = new List<TriangleData>();

        // defines which characters to split file into lines on:
        var fileDelimiters = new[] { "\r\n", "\r", "\n" };

        // defines which characters to split each line on:
        var lineDelimiters = new[] { ' ' };

        // split file into array of non-empty lines:
        var lines = indexFile.text.Split(fileDelimiters, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 1)
        {
            Debug.LogWarning($"{indexFile.name} is empty!");
            return;
        }

        var numTriangles = int.Parse(lines[0]);

        if (numTriangles < 1)
        {
            Debug.LogWarning($"{indexFile.name} contains no triangle data");
            return;
        }

        for (var i = 1; i <= numTriangles; i++)
        {
            // split line into numbers and add to list of triangles
            var elements = lines[i].Split(lineDelimiters, StringSplitOptions.RemoveEmptyEntries);
            if (elements.Length < 6)
            {
                Debug.LogWarning($"{vertexFile.name} is missing data on line {i}");
                continue;
            }

            int p = int.Parse(elements[0]),
                q = int.Parse(elements[1]),
                r = int.Parse(elements[2]);

            Triangles.Add(new TriangleData(
                p, // vertex
                q, // vertex
                r, // vertex
                int.Parse(elements[3]), // neighbour
                int.Parse(elements[4]), // neighbour
                int.Parse(elements[5]), // neighbour
                GetNormalFromTri(p, q, r), // surface normal
                i-1
            ));
        }
    }

    /// <summary>
    ///     Flatten triangle data into single index-array.
    /// </summary>
    /// <returns>array with indices of triangle-vertices</returns>
    private int[] GenerateIndexArray()
    {
        var indices = new int[Triangles.Count * 3];

        for (var i = 0; i < Triangles.Count; i++)
        for (var j = 0; j < 3; j++)
            indices[3 * i + j] = Triangles[i].Indices[j];

        return indices;
    }

    /// <summary>
    ///     Create mesh from vertex data.
    /// </summary>
    /// <returns>Mesh object</returns>
    private Mesh GenerateMesh()
    {
        // read data:
        ReadVertexData();
        ReadIndexData();
        
        // set vertex and index arrays:
        var newMesh = new Mesh();
        newMesh.indexFormat = IndexFormat.UInt32;
        newMesh.vertices = Vertices;
        newMesh.SetIndices(GenerateIndexArray(), MeshTopology.Triangles, 0);  // flatten index-data to single static array

        // mesh object requires internal normals and tangents:
        newMesh.RecalculateNormals();
        newMesh.RecalculateTangents();

        return newMesh;
    }
}