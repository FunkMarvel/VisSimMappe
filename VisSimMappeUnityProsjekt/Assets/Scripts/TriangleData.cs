// //////////////////////////////////////////////////////////////////////////
// //////////////////////////////
// //FileName: TriangleData.cs
// //FileType: Visual C# Source file
// //Author : Anders P. Åsbø
// //Created On : 20/09/2023
// //Last Modified On : 20/09/2023
// //Copy Rights : Anders P. Åsbø
// //Description : Struct for saving triangle data.
// //////////////////////////////////////////////////////////////////////////
// //////////////////////////////

using UnityEngine;

/// <summary>
///     Struct containing read-only triangulation data for single triangle.
/// </summary>
public struct TriangleData
{
    public TriangleData(int v0, int v1, int v2, int triangle0, int triangle1, int triangle2, Vector3 surfaceNormal, int index)
    {
        SurfaceNormal = surfaceNormal;
        this.index = index;
        Indices = new[] { v0, v1, v2 };
        Neighbours = new[] { triangle0, triangle1, triangle2 };
    }

    /// <summary>
    ///     Read-only vertex indices of triangle.
    /// </summary>
    public int[] Indices { get; }

    /// <summary>
    ///     Read-only indices of neighbouring triangles.
    /// </summary>
    public int[] Neighbours { get; }

    /// <summary>
    ///     Read-only unit normal vector of triangle;
    /// </summary>
    public Vector3 SurfaceNormal { get; }
    
    public int index { get; }
}