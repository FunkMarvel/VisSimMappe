// //////////////////////////////////////////////////////////////////////////
// //////////////////////////////
// //FileName: BallPhysics.cs
// //FileType: Visual C# Source file
// //Author : Anders P. Åsbø
// //Created On : 14/09/2023
// //Last Modified On : 20/09/2023
// //Copy Rights : Anders P. Åsbø
// //Description :
// //////////////////////////////////////////////////////////////////////////
// //////////////////////////////

using UnityEngine;

public class BallPhysics : MonoBehaviour
{
    [Header("References")] [SerializeField]
    private GameObject triangleSurfaceRef; // reference triangle surface, editable in engine.

    [Header("Physical properties")] [SerializeField] [Min(1e-6f)]
    private float mass = 1; // mass of ball, editable in engine.

    [SerializeField] [Min(0)] private float radius = 1; // radius of ball, editable in engine.
    [SerializeField] [Min(0)] private float rollingResistance; // friction, editable in engine.
    [SerializeField] [Range(0, 1)] private float bounciness = 1; // bounciness, editable in engine.

    // booleans for reference handling:
    private bool _hasSurfaceRef;
    private bool _outOfBounds;

    // internal variable for drawing contact point:
    private Vector3 _prevContact = Vector3.zero;
    private float _elapsedTimeSinceContact;

    // reference to instance of triangle surface:
    private TriangleSurface _triangleSurface;

    // physics velocity:
    private Vector3 _velocity = Vector3.zero;

    /// <summary>
    ///     Setup before first frame.
    /// </summary>
    private void Awake()
    {
        // makes sure surface reference is set in engine editor:
        _hasSurfaceRef = triangleSurfaceRef != null;

        if (!_hasSurfaceRef)
        {
            Debug.LogWarning($"{gameObject.name} has no reference to surface!");
            return;
        }

        // retrieves instance of triangleSurface:
        _triangleSurface = triangleSurfaceRef.GetComponent<TriangleSurface>();
        _hasSurfaceRef = _triangleSurface != null;

        if (!_hasSurfaceRef) Debug.LogWarning($"{gameObject.name} has no reference to surface!");
    }

    /// <summary>   
    ///     Physics update loop.
    /// </summary>
    private void FixedUpdate()
    {
        // caching transform, and position to avoid access overhead:
        var transform1 = transform;
        var position = transform1.position;

        // sets rolling resistance to 0 when not in motion:
        var rollingCoefficient = _velocity.magnitude > 1e-15f ? rollingResistance : 0;

        // gravity force, Physics.gravity is the acceleration-vector [0, -9.81, 0]:
        var netForce = Physics.gravity * mass;

        if (_hasSurfaceRef)
        {
            // get current step and next step contact points:
            var hit = _triangleSurface.GetCollision(position);
            var nextHit = _triangleSurface.GetCollision(position + _velocity * Time.fixedDeltaTime);
            _prevContact = nextHit.Point; // store contact point for debug drawing.

            var distVec = position - hit.Point;
            var dist = distVec.magnitude;

            if (dist <= radius) // check if actually colliding
            {
                _elapsedTimeSinceContact += Time.fixedDeltaTime;

                var parallelVelocity = Vector3.ProjectOnPlane(_velocity, hit.HitNormal);
                var parallelUnit = parallelVelocity.normalized;

                var reflectNorm = (hit.HitNormal + nextHit.HitNormal).normalized;
                var normalChange = Vector3.Dot(
                    Vector3.Cross(hit.HitNormal, nextHit.HitNormal),
                    Vector3.Cross(hit.HitNormal, parallelUnit).normalized
                    );

                if (bounciness <= 0f && normalChange < 0f) // reflect velocity when switching triangle:
                    _velocity -= 2 * Vector3.Dot(_velocity, reflectNorm) * reflectNorm;
                else // bouncing when not switching triangle:
                    _velocity = -bounciness * Vector3.Dot(_velocity, hit.HitNormal) * hit.HitNormal + parallelVelocity;
                    

                // add normal-force:
                var normalForceMagnitude = Vector3.Dot(netForce, hit.HitNormal);
                netForce -= (hit.HitNormal - rollingCoefficient * parallelUnit) * normalForceMagnitude;
                transform1.position = 0.5f*(hit.Point + nextHit.Point) + radius * reflectNorm;
            }
            // Debug.Log($"Normal: {hit.HitNormal}");

            // remove physics-script from ball if out of bounds:
            if (!_outOfBounds && Mathf.Approximately(hit.HitNormal.sqrMagnitude, 0f))
            {
                _outOfBounds = true;
                Destroy(this);
            }
            
            // integrate position with Forward-Euler:
            var acceleration = netForce / mass;
            _velocity += acceleration * Time.fixedDeltaTime;

            transform1.Translate(_velocity * Time.fixedDeltaTime);

            // log position:
            // Debug.Log($"Position: {position} | " +
            //           $"Velocity {_velocity.magnitude:F4} | " +
            //           $"Acceleration {acceleration.magnitude:F4} | " +
            //           $"Time since contact {_elapsedTimeSinceContact:F4}");
        }
    }

    /// <summary>
    ///     Draw projected contact point in editor-view:
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(_prevContact, 0.1f);
    }
}