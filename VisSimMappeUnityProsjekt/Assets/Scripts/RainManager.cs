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
using Random = UnityEngine.Random;

public struct SpawnBox
{
    /// <summary>
    ///     Data-container for area to spawn rain in.
    /// </summary>
    /// <param name="center">center of the box</param>
    /// <param name="extents">side-lengths of the box</param>
    public SpawnBox(Vector3 center, Vector3 extents)
    {
        Center = center;
        XLimits = new Vector2(Center.x - 0.5f * extents.x, Center.x + 0.5f * extents.x);
        YLimits = new Vector2(Center.y - 0.5f * extents.y, Center.y + 0.5f * extents.y);
        ZLimits = new Vector2(Center.z - 0.5f * extents.z, Center.z + 0.5f * extents.z);
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
    [Header("Rain Simulation")] [SerializeField] [Min(0)]
    private int numRainDrops;

    [SerializeField] private GameObject rainDropPrefab;
    [SerializeField] private GameObject surfaceObject;

    [Header("Visualization")] [SerializeField] [Min(1)]
    private int degree = 2;

    [SerializeField] [Min(0)] private float simDuration;
    [SerializeField] [Min(1)] private int numControlPoints;
    [SerializeField] [Min(1)] private int numPointsToDraw;
    [SerializeField] private GameObject lineObject;

    [Header("Ball")] [SerializeField] private GameObject ballObject;
    [Header("Fluid physics on ball")]
    [SerializeField] [Min(0f)] private float ballCoefficientOfDrag = 6f;
    [SerializeField] [Min(0f)] private float flowEffectRadius = 5f;
    [SerializeField] [Min(0f)] private float fluidSpeed = 6f;
    [SerializeField] [Min(0f)] private float fluidDensity = 1000;

    private List<GameObject> _drops;
    private bool _hasBall;
    private bool _hasSurface;

    private float _sampleTime;
    private float _sampleTimer;
    private List<SplineData> _splines = new();
    private bool _splinesDrawn;

    private TriangleSurface _surface;
    private float _timer;
    private Tuple<Vector3, Vector3> posTanPair = new(Vector3.zero, Vector3.zero);

    private SpawnBox SpawnVolume { get; set; }

    public bool SimStarted { get; private set; }

    public BallPhysics Ball { get; private set; }

    public bool HasBall => _hasBall;

    private void Awake()
    {
        // get references to needed objects
        _hasSurface = surfaceObject != null;
        if (_hasSurface)
        {
            _surface = surfaceObject.GetComponent<TriangleSurface>();
            _hasSurface = _surface != null;
        }

        // ensure enough control-points for desired splines
        numControlPoints = numControlPoints > degree + 1 ? numControlPoints : degree + 1;
        _sampleTime = simDuration / numControlPoints; // interval to sample at

        // draw spawner area
        var trans = transform;
        SpawnVolume = new SpawnBox(trans.position, trans.localScale);

        _drops = new List<GameObject>(numRainDrops);
        _splines = new List<SplineData>(numRainDrops);
    }

    public void StartRain()
    {
        // spawn rain at random positions within spawn-volume:
        for (var i = 0; i < numRainDrops; i++)
        {
            var obj = Instantiate(rainDropPrefab, transform, true);

            if (obj == null)
                continue;

            var pos = new Vector3(
                Random.Range(SpawnVolume.XLimits.x, SpawnVolume.XLimits.y),
                Random.Range(SpawnVolume.YLimits.x, SpawnVolume.YLimits.y),
                Random.Range(SpawnVolume.ZLimits.x, SpawnVolume.ZLimits.y)
            );
            var ball = obj.GetComponent<BallPhysics>();
            if (ball != null && _hasSurface) ball.Surface = surfaceObject;
            obj.transform.position = pos;
            _drops.Add(obj);
            _splines.Add(new SplineData());
        }

        // start timer:
        _sampleTimer = simDuration;
        SimStarted = true;
        Invoke(nameof(DrawBSpline), simDuration + Time.fixedDeltaTime);
    }

    private void FixedUpdate()
    {
        if (!SimStarted) return;
        // time simulation and log positions.
        if (_timer < simDuration && _sampleTimer > _sampleTime)
        {
            _timer += Time.fixedDeltaTime;
            _sampleTimer = 0;
            for (var i = 0; i < _drops.Count; i++)
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

        // simulate force on ball when splines have been drawn:
        if (_splinesDrawn && HasBall) SimulateExtremeWeather();
    }

    // debug visualizations
    private void OnDrawGizmos()
    {
        var transform1 = transform;
        SpawnVolume = new SpawnBox(transform1.position, transform1.localScale);

        Gizmos.DrawWireCube(SpawnVolume.Center,
            new Vector3(SpawnVolume.Lengths.x, SpawnVolume.Lengths.y, SpawnVolume.Lengths.z));

        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(posTanPair.Item1, 1f);
        Gizmos.DrawLine(posTanPair.Item1, posTanPair.Item1 + 5f * posTanPair.Item2);
    }

    private void SimulateExtremeWeather()
    {
        // access ball size and position:
        var ballPos = Ball.transform.position;
        var direction = Vector3.zero;
        var rad = Ball.Radius;

        var hit = _surface.GetCollision(ballPos);
        foreach (var spline in _splines)
        {
            // find closest point on splines
            posTanPair = spline.GetClosestPoint(ballPos, _surface);

            // average tangents of splines within effect radius of ball
            if (Vector3.Distance(ballPos, posTanPair.Item1) < flowEffectRadius) direction += posTanPair.Item2;
        }

        var relVel = (direction.normalized * fluidSpeed - Ball.Velocity);
        // relVel = Vector3.ProjectOnPlane(relVel, hit.HitNormal);

        // calculate drag-force on ball and add it to ball-object's internal physics:
        var force = DragEquation(relVel, fluidDensity, ballCoefficientOfDrag, Mathf.PI * rad * rad);
        Ball.AddForce(force);
    }

    /// <summary>
    ///     Calculate force on object in water stream.
    /// </summary>
    /// <param name="relativeVelocity">Vector3 - direction of force</param>
    /// <param name="rho">float - density of fluid</param>
    /// <param name="Cd">float - drag-coefficient of object</param>
    /// <param name="A">float - cross-sectional area of object</param>
    /// <returns>Vector3 - Drag force on object</returns>
    private static Vector3 DragEquation(Vector3 relativeVelocity, float rho, float Cd, float A)
    {
        return 0.5f * rho * Cd * A * relativeVelocity.magnitude * relativeVelocity;
    }

    private void DrawBSpline()
    {
        foreach (var drop in _drops)
        {
            drop.SetActive(false);
        }
        foreach (var spline in _splines)
        {
            spline.GenerateSpline(degree);
            spline.Line = Instantiate(lineObject, transform, true).GetComponent<LineRenderer>();
            spline.DrawSpline(numPointsToDraw, _surface);
        }

        print("Drew splines");
        _splinesDrawn = true;
    }

    public void SpawnBall(Vector3 pos)
    {
        if (HasBall)
        {
            Destroy(Ball.gameObject);
        }
        
        var newBall = Instantiate(ballObject, pos, Quaternion.identity);
        _hasBall = newBall != null;
        if (!HasBall) return;
        
        var ball = newBall.GetComponent<BallPhysics>();
        _hasBall = ball != null;
        
        if (!HasBall) return;
        ball.Surface = surfaceObject;
        ball.transform.parent = transform;
        
        Ball = ball;
    }
}