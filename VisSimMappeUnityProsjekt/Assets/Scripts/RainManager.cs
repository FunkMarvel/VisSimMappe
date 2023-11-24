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

using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

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

public class SplineData
{
    // ReSharper disable once MemberCanBePrivate.Global
    public BSplineCurve Spline { get; private set; }
    public List<Vector2> ControlPoints { get; set; } = new List<Vector2>();
    public LineRenderer Line { get; set; }

    public void GenerateSpline(int degree)
    {
        Spline = new BSplineCurve(degree, ControlPoints);
    }

    public void DrawSpline(int numPointsToDraw, TriangleSurface surface)
    {
        var positions = new Vector3[numPointsToDraw];
        var dt = (Spline.End - Spline.Start) / numPointsToDraw;
        var t = Spline.Start;
        
        for (var i = 0; i < numPointsToDraw; i++)
        {
            var pos = Spline.Evaluate(t).XZToVector3();
            var hit = surface.GetCollision(pos, false);
            positions[i] = Mathf.Approximately(hit.HitNormal.sqrMagnitude, 0.0f) ? pos: hit.Point;
            t += dt;
        }

        Line.positionCount = numPointsToDraw;
        Line.SetPositions(positions);
        // ControlPoints.Clear();
    }

    public Tuple<Vector3, Vector3> GetClosestPoint(Vector3 pos, TriangleSurface surface)
    {
        var t0 = Spline.Start;
        var t1 = Spline.End;
        var t01 = 0.5f * (t1 - t0);
        var point = pos.XZToVector2();
        var onSpline = Spline.Evaluate(t0);
        var tangent = Spline.Tangent(t0);
        var diff = onSpline - point;
        var dot = Vector2.Dot(diff, tangent);
        var i = 0;
        const int limit = 100;
        // Debug.Log($"dot = {dot}, t0 {t0}, t01 {t01}, t1 {t1}");
        
        while (Mathf.Abs(dot) > 1e-4f && i++ < limit)
        {
            onSpline = Spline.Evaluate(t01);
            diff = onSpline - point;
            tangent = Spline.Tangent(t01);
            dot = Vector2.Dot(diff, tangent);
            // Debug.Log($"dot = {dot}, t0 {t0}, t01 {t01}, t1 {t1}");
            
            if (dot < 0f)
            {
                t0 = t01;
                t01 = 0.5f * (t1 + t0);
            }
            else
            {
                t1 = t01;
                t01 = 0.5f * (t1 + t0);
            }
        }
        
        var hit = surface.GetCollision(onSpline.XZToVector3(), false);
        pos = Mathf.Approximately(hit.HitNormal.sqrMagnitude, 0.0f) ? onSpline.XZToVector3(): hit.Point;
        var tan = Mathf.Approximately(hit.HitNormal.sqrMagnitude, 0.0f) ? tangent.XZToVector3(): Vector3.ProjectOnPlane(tangent.XZToVector3(), hit.HitNormal).normalized;
        return new Tuple<Vector3, Vector3>(pos, tan);
    }
}

public class RainManager : MonoBehaviour
{
    [Header("Rain Simulation")]
    [SerializeField] [Min(0)] private int numRainDrops;
    [SerializeField] private GameObject rainDropPrefab;
    [SerializeField] private GameObject surfaceObject;

    [Header("Visualization")]
    [SerializeField] [Min(1)] private int degree = 2;
    [SerializeField] [Min(0)] private float simDuration;
    [SerializeField] [Min(1)] private int numControlPoints;
    [SerializeField] [Min(1)] private int numPointsToDraw;
    [SerializeField] private GameObject lineObject;

    [Header("Ball")] [SerializeField] private GameObject ballObject;
    private BallPhysics _ball;
    private bool _hasBall;

    private List<GameObject> _drops;
    private List<SplineData> _splines = new List<SplineData>();

    private float _sampleTime;
    private float _timer;
    private float _sampleTimer;
    
    private TriangleSurface _surface;
    private bool _hasSurface;
    private bool _splinesDrawn;

    private SpawnBox SpawnVolume { get; set; }

    private void Awake()
    {

        _hasSurface = surfaceObject != null;
        if (_hasSurface)
        {
            _surface = surfaceObject.GetComponent<TriangleSurface>();
            _hasSurface = _surface != null;
        }

        _hasBall = ballObject != null;
        if (_hasBall)
        {
            _ball = ballObject.GetComponent<BallPhysics>();
        }

        numControlPoints = numControlPoints > degree + 1 ? numControlPoints : degree + 1;
        _sampleTime = simDuration / numControlPoints;
        
        var trans = transform;
        SpawnVolume = new SpawnBox(trans.position, trans.localScale);
        
        _drops = new List<GameObject>(numRainDrops);
        _splines = new List<SplineData>(numRainDrops);
        
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
            if (ball != null && _hasSurface) ball.triangleSurfaceRef = surfaceObject;
            obj.transform.position = pos;
            _drops.Add(obj);
            _splines.Add(new SplineData());
        }

        _sampleTimer = simDuration;
        Invoke(nameof(DrawBSpline), simDuration + Time.fixedDeltaTime);
    }

    private void FixedUpdate()
    {
        if (_timer < simDuration && _sampleTimer > _sampleTime)
        {
            _timer += Time.fixedDeltaTime;
            _sampleTimer = 0;
            for (int i = 0; i < _drops.Count; i++)
            {
                var pos = _drops[i].transform.position.XZToVector2();
                _splines[i].ControlPoints.Add(pos);
            }
        }
        else if (_timer < simDuration)
        {
            _timer += Time.fixedDeltaTime;
            _sampleTimer += Time.fixedDeltaTime;
        }

        if (_splinesDrawn)
        {
            SimulateExtremeWeather();
        }
    }

    private void SimulateExtremeWeather()
    {
        var ballPos = _ball.transform.position;
        var direction = Vector3.zero;
        var rad = _ball.Radius;

        foreach (var spline in _splines)
        {
            var pointAndTangent = spline.GetClosestPoint(ballPos, _surface);
            if (Vector3.Distance(ballPos, pointAndTangent.Item1) < 5f)
            {
                direction += pointAndTangent.Item2;
            }
        }
        
        var force = DragEquation(direction, 1000f, 6f, 0.47f, Mathf.PI*rad*rad);
        _ball.AddForce(force);
    }

    /// <summary>
    /// Calculate force on object in water stream.
    /// </summary>
    /// <param name="Direction">Vector3 - direction of force</param>
    /// <param name="rho">float - density of fluid</param>
    /// <param name="v">float - speed of fluid</param>
    /// <param name="Cd">float - drag-coefficient of object</param>
    /// <param name="A">float - cross-sectional area of object</param>
    /// <returns>Vector3 - Drag force on object</returns>
    private static Vector3 DragEquation(Vector3 Direction, float rho, float v, float Cd, float A)
    {
        return 0.5f * rho * v * v * Cd * A * Direction.normalized;
    }

    private void DrawBSpline()
    {
        foreach (var spline in _splines)
        {
            spline.GenerateSpline(degree);
            spline.Line = Instantiate(lineObject).GetComponent<LineRenderer>();
            spline.DrawSpline(numPointsToDraw, _surface);
            // spline.Tangent = Instantiate(lineObject).GetComponent<LineRenderer>();
            // spline.DrawTangents(0.5f*(_splines[0].Spline.End - _splines[0].Spline.Start), _surface);
        }
        print("Drew spline");
        _splinesDrawn = true;
    }

    private void OnDrawGizmos()
    {
        var transform1 = transform;
        SpawnVolume = new SpawnBox(transform1.position, transform1.localScale);

        Gizmos.DrawWireCube(SpawnVolume.Center,
            new Vector3(SpawnVolume.Lengths.x, SpawnVolume.Lengths.y, SpawnVolume.Lengths.z));
    }
}