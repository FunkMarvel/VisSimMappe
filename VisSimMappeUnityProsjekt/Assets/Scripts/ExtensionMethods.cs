

using UnityEngine;

public static class ExtensionMethods
{
    public static Vector3 XZToVector3(this Vector2 vec)
    {
        return new Vector3(vec.x, 0, vec.y);
    }
    
    public static Vector3 XYToVector3(this Vector2 vec)
    {
        return new Vector3(vec.x, vec.y, 0);
    }

    public static Vector2 XZToVector2(this Vector3 vec)
    {
        return new Vector2(vec.x, vec.z);
    }
    
    public static Vector2 XYToVector2(this Vector3 vec)
    {
        return new Vector2(vec.x, vec.y);
    }
    
}
