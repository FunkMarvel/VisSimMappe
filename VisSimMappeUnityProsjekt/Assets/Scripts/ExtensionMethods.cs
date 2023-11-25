// //////////////////////////////////////////////////////////////////////////
// //////////////////////////////
// //FileName: ExtensionMethods.cs
// //FileType: Visual C# Source file
// //Author : Anders P. Åsbø
// //Created On : 13/11/2023
// //Last Modified On : 25/11/2023
// //Copy Rights : Anders P. Åsbø
// //Description :
// //////////////////////////////////////////////////////////////////////////
// //////////////////////////////


using UnityEngine;

/// <summary>
///     Library of custom methods for built in classes.
///     These are called as if they were class member-functions.
///     For example, given a Vector2, the method XZToVector3 can be called as such:
///     Vector2 vec = new Vector2(1,1);
///     Vector3 newVec = vec.XZToVector3();
///     Resulting newVec being the vector3 (1, 0, 1).
/// </summary>
public static class ExtensionMethods
{
    /// <summary>
    ///     Convert Vector2 to Vector3.
    /// </summary>
    /// <param name="vec">input vector2</param>
    /// <returns>new Vector3(vec.x, 0, vec.y)</returns>
    public static Vector3 XZToVector3(this Vector2 vec)
    {
        return new Vector3(vec.x, 0, vec.y);
    }

    /// <summary>
    ///     Convert Vector2 to Vector3.
    /// </summary>
    /// <param name="vec">input vector2</param>
    /// <returns>new Vector3(vec.x, vec.y, 0);</returns>
    public static Vector3 XYToVector3(this Vector2 vec)
    {
        return new Vector3(vec.x, vec.y, 0);
    }

    /// <summary>
    ///     Convert Vector3 to Vector2
    /// </summary>
    /// <param name="vec">input vector</param>
    /// <returns>new Vector2(vec.x, vec.z)</returns>
    public static Vector2 XZToVector2(this Vector3 vec)
    {
        return new Vector2(vec.x, vec.z);
    }

    /// <summary>
    ///     Convert Vector3 to Vector2
    /// </summary>
    /// <param name="vec">input vector</param>
    /// <returns>new Vector2(vec.x, vec.y)</returns>
    public static Vector2 XYToVector2(this Vector3 vec)
    {
        return new Vector2(vec.x, vec.y);
    }
}