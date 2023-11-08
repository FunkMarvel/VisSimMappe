// //////////////////////////////////////////////////////////////////////////
// //////////////////////////////
// //FileName: MeshBounds.cs
// //FileType: Visual C# Source file
// //Author : Anders P. Åsbø
// //Created On : 04/11/2023
// //Last Modified On : 04/11/2023
// //Copy Rights : Anders P. Åsbø
// //Description :
// //////////////////////////////////////////////////////////////////////////
// //////////////////////////////

public struct MeshBounds
{
    public MeshBounds(float xMin, float xMax, float yMin, float yMax, float zMin, float zMax)
    {
        XMin = xMin; XMax = xMax;
        YMin = yMin; YMax = yMax;
        ZMin = zMin; ZMax = zMax;
        Width = xMax - xMin;  Height = zMax - zMin;
    }

    public float XMin { get; }
    public float XMax { get; }
    public float YMin { get; }
    public float YMax { get; }
    public float ZMin { get; }
    public float ZMax { get; }
    public float Width { get; }
    public float Height { get; }
}