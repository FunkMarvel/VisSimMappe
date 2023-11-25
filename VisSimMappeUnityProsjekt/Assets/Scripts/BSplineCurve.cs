// //////////////////////////////////////////////////////////////////////////
// //////////////////////////////
// //FileName: BSplineCurve.cs
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

public class BSplineCurve
{
    private readonly List<Vector2> _controlPoints;
    private readonly List<float> _knotVector;

    public BSplineCurve(int degree, List<Vector2> controlPoints, List<float> knots)
    {
        Debug.Assert(degree + controlPoints.Count + 1 == knots.Count,
            $"number of knots ({knots.Count}) does not match n + d + 1 ({degree + controlPoints.Count + 1})");

        Degree = degree;
        _knotVector = knots;
        _controlPoints = controlPoints;
    }
    
    public BSplineCurve(int degree, List<Vector2> controlPoints)
    {
        Debug.Assert(degree + 1 <= controlPoints.Count,
            $"Not enough control points for degree {degree} B-Spline");

        Degree = degree;
        _controlPoints = controlPoints;

        var numKnots = degree + controlPoints.Count + 1;
        var numInternalKnots = numKnots - 2 * (degree + 1);
        
        _knotVector = new List<float>(numKnots);
        var lastKnot = 0;
        for (int i = 0; i < degree+1; i++)
        {
            _knotVector.Add(0);
        }

        for (int i = 0; i < numInternalKnots; i++)
        {
            lastKnot = i + 1;
            _knotVector.Add(lastKnot);
        }

        for (int i = 0; i < degree+1; i++)
        {
            _knotVector.Add(lastKnot+1);
        }
    }

    public int Degree { get; }

    private int FindKnotInterval(float t)
    {
        return FindKnotInterval(t, Degree, _controlPoints, _knotVector);
    }
    
    private static int FindKnotInterval(float t, int d, IReadOnlyCollection<Vector2> c, IReadOnlyList<float> T)
    {
        var mu = T.Count - 1;
        while (t < T[mu]) mu--;

        if (mu >= c.Count)
        {
            mu = c.Count - 1;
        } else if (mu < 0)
        {
            mu = d;
        }
        
        return mu;
    }

    public Vector2 Evaluate(float t)
    {
        var mu = FindKnotInterval(t);
        return Eval(t, Degree, mu, _controlPoints, _knotVector);
    }
    
    private static Vector2 Eval(float t, int d, int mu, IReadOnlyList<Vector2> c, IReadOnlyList<float> T)
    {
        var localPoints = new List<Vector2>(d + 1);
        
        for (var i = d; i >= 0; i--) localPoints.Add(c[mu - i]);

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

    public Vector2 Tangent(float t)
    {
        const float dt = 0.5f;
        if (t < Start)
        {
            t = Start;
        }
        
        if (t > End)
        {
            t = End - dt;
        }
        
        return (Evaluate(t + dt) - Evaluate(t)).normalized;
    }

    public float Start
    {
        get => _knotVector[0];
    }

    public float End
    {
        get => _knotVector[^1];
    }
}