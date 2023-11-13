// //////////////////////////////////////////////////////////////////////////
// //////////////////////////////
// //FileName: RainManager.cs
// //FileType: Visual C# Source file
// //Author : Anders P. Åsbø
// //Created On : 13/11/2023
// //Last Modified On : 13/11/2023
// //Copy Rights : Anders P. Åsbø
// //Description :
// //////////////////////////////////////////////////////////////////////////
// //////////////////////////////

using System.Collections.Generic;
using UnityEngine;

public struct SpawnBox
{
    public SpawnBox(Vector3 center, Vector3 extents)
    {
        Center = center;
        XLimits = new Vector2(Center.x - 0.5f*extents.x, Center.x + 0.5f*extents.x);
        YLimits = new Vector2(Center.y - 0.5f*extents.y, Center.y + 0.5f*extents.y);
        ZLimits = new Vector2(Center.z - 0.5f*extents.z, Center.z + 0.5f*extents.z);
        Lengths = extents;
    }

    public Vector3 Center { get; }
    public Vector2 XLimits { get; }
    public Vector2 YLimits { get; }
    public Vector2 ZLimits { get; }
    
    public Vector3 Lengths { get; }
}

public class RainManager : MonoBehaviour
{
    [SerializeField] [Min(0)] private int numRainDrops;
    [SerializeField] private GameObject rainDropPrefab;
    [SerializeField] private GameObject surfaceObject;

    private List<GameObject> drops;
    
    public SpawnBox SpawnVolume { get; private set; }

    private void Awake()
    {
        var transform1 = transform;
        SpawnVolume = new SpawnBox(transform1.position, transform1.localScale);
        
        drops = new List<GameObject>(numRainDrops);
        var trans = transform;
        
        for (int i = 0; i < numRainDrops; i++)
        {
            var obj = Object.Instantiate(rainDropPrefab, trans, true);

            if (obj == null)
                continue;
            
            var pos = new Vector3(
                Random.Range(SpawnVolume.XLimits.x, SpawnVolume.XLimits.y),
                Random.Range(SpawnVolume.YLimits.x, SpawnVolume.YLimits.y),
                Random.Range(SpawnVolume.ZLimits.x, SpawnVolume.ZLimits.y)
            );
            var ball = obj.GetComponent<BallPhysics>();
            if (ball != null) ball.triangleSurfaceRef = surfaceObject;
            obj.transform.position = pos;
            drops.Add(obj);
        }
    }

    private void OnDrawGizmos()
    {
        var transform1 = transform;
        SpawnVolume = new SpawnBox(transform1.position, transform1.localScale);

        Gizmos.DrawWireCube(SpawnVolume.Center,
            new Vector3(SpawnVolume.Lengths.x, SpawnVolume.Lengths.y, SpawnVolume.Lengths.z));

        // transform.position = Center = 0.5f * (position1 + position);
    }
}