// //////////////////////////////////////////////////////////////////////////
// //////////////////////////////
// //FileName: Contact.cs
// //FileType: Visual C# Source file
// //Author : Anders P. Åsbø
// //Created On : 20/09/2023
// //Last Modified On : 20/09/2023
// //Copy Rights : Anders P. Åsbø
// //Description : Struct for handling collision data.
// //////////////////////////////////////////////////////////////////////////
// //////////////////////////////

using UnityEngine;

/// <summary>
///     Struct containing read-only data for collision contacts.
/// </summary>
public struct Contact
{
    public Contact(Vector3 point, Vector3 hitNormal)
    {
        Point = point;
        HitNormal = hitNormal;
    }

    /// <summary>
    ///     Read-only Location of collision contact in world-space.
    /// </summary>
    public Vector3 Point { get; }

    /// <summary>
    ///     Read-only unit normal at contact-point.
    /// </summary>
    public Vector3 HitNormal { get; }
}