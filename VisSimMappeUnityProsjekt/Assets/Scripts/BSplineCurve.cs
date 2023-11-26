// //////////////////////////////////////////////////////////////////////////
// //////////////////////////////
// //FileName: BSplineCurve.cs
// //FileType: Visual C# Source file
// //Author : Anders P. Åsbø
// //Created On : 14/11/2023
// //Last Modified On : 26/11/2023
// //Copy Rights : Anders P. Åsbø
// //Description :
// //////////////////////////////////////////////////////////////////////////
// //////////////////////////////

using System.Collections.Generic;
using UnityEngine;

public class BSplineCurve
{
    private readonly List<Vector2> _controlPoints;
    private readonly List<float> _knotVector;

    /// <summary>
    ///     Construct B-Spline
    /// </summary>
    /// <param name="degree">int - Degree of spline</param>
    /// <param name="controlPoints">List of Vector2 - control points</param>
    /// <param name="knots">List of floats - knot vector</param>
    public BSplineCurve(int degree, List<Vector2> controlPoints, List<float> knots)
    {
        // error if invalid spline
        Debug.Assert(degree + controlPoints.Count + 1 == knots.Count,
            $"number of knots ({knots.Count}) does not match n + d + 1 ({degree + controlPoints.Count + 1})");

        Degree = degree;
        _knotVector = knots;
        _controlPoints = controlPoints;
    }

    /// <summary>
    ///     Construct uniform B-Spline
    /// </summary>
    /// <param name="degree">int - Degree of spline</param>
    /// <param name="controlPoints">List of Vector2 - control points</param>
    public BSplineCurve(int degree, List<Vector2> controlPoints)
    {
        // raise error on invalid spline
        Debug.Assert(degree + 1 <= controlPoints.Count,
            $"Not enough control points for degree {degree} B-Spline");

        Degree = degree;
        _controlPoints = controlPoints;

        // generate uniform knot vector:
        var numKnots = degree + controlPoints.Count + 1;
        var numInternalKnots = numKnots - 2 * (degree + 1);

        _knotVector = new List<float>(numKnots);
        var lastKnot = 0;
        for (var i = 0; i < degree + 1; i++) _knotVector.Add(0);

        for (var i = 0; i < numInternalKnots; i++)
        {
            lastKnot = i + 1;
            _knotVector.Add(lastKnot);
        }

        for (var i = 0; i < degree + 1; i++) _knotVector.Add(lastKnot + 1);
    }

    public int Degree { get; }

    /// <summary>
    ///     Lowest valid value of parameter.
    /// </summary>
    public float Start => _knotVector[0];

    /// <summary>
    ///     Highest valid value of parameter.
    /// </summary>
    public float End => _knotVector[^1];

    /// <summary>
    ///     Find active knot-interval
    /// </summary>
    /// <param name="t">current parameter value</param>
    /// <returns>int - index of last active control point</returns>
    private int FindKnotInterval(float t)
    {
        return FindKnotInterval(t, Degree, _controlPoints, _knotVector);
    }

    private static int FindKnotInterval(float t, int d, IReadOnlyCollection<Vector2> c, IReadOnlyList<float> T)
    {
        var mu = T.Count - 1;
        while (t < T[mu]) mu--;

        if (mu >= c.Count)
            mu = c.Count - 1;
        else if (mu < 0) mu = d;

        return mu;
    }

    /// <summary>
    ///     Evaluate spline for given parameter value.
    /// </summary>
    /// <param name="t">float</param>
    /// <returns>Vector2 - corresponding point on spline</returns>
    public Vector2 Evaluate(float t)
    {
        var mu = FindKnotInterval(t);
        return Eval(t, Degree, mu, _controlPoints, _knotVector);
    }

    private static Vector2 Eval(float t, int d, int mu, IReadOnlyList<Vector2> c, IReadOnlyList<float> T)
    {
        // temp list for active control point
        var localPoints = new List<Vector2>(d + 1);
        for (var i = d; i >= 0; i--) localPoints.Add(c[mu - i]);

        // use deBoor's algorithm to evaluate
        for (var k = d; k > 0; k--)
        {
            var j = mu - k;
            for (var i = 0; i < k; i++)
            {
                j++;
                var w = (t - T[j]) / (T[j + k] - T[j]);
                localPoints[i] = localPoints[i] * (1 - w) + localPoints[i + 1] * w;
            }
        }

        return localPoints[0];
    }

    /// <summary>
    ///     Get tangent of spline at point
    /// </summary>
    /// <param name="t">float - parameter value</param>
    /// <returns>Vector2 - tangent at t</returns>
    public Vector2 Tangent(float t)
    {
        // approximate tangent by taking small step along spline
        const float dt = 0.5f;
        if (t < Start) t = Start;

        if (t > End) t = End - dt;

        return (Evaluate(t + dt) - Evaluate(t)).normalized;
    }
}