// //////////////////////////////////////////////////////////////////////////
// //////////////////////////////
// //FileName: SplineData.cs
// //FileType: Visual C# Source file
// //Author : Anders P. Åsbø
// //Created On : 25/11/2023
// //Last Modified On : 25/11/2023
// //Copy Rights : Anders P. Åsbø
// //Description :
// //////////////////////////////////////////////////////////////////////////
// //////////////////////////////

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Container for the constructing and drawing of splines
/// </summary>
public class SplineData
{
    // ReSharper disable once MemberCanBePrivate.Global
    public BSplineCurve Spline { get; private set; }
    public List<Vector2> ControlPoints { get; set; } = new List<Vector2>();
    public LineRenderer Line { get; set; }

    // Generate splines of given degree from controlpoints
    public void GenerateSpline(int degree)
    {
        Spline = new BSplineCurve(degree, ControlPoints);
    }

    // Draw the spline using line-segments
    public void DrawSpline(int numPointsToDraw, TriangleSurface surface)
    {
        // draw given number of uniform points in parameter-space
        var positions = new Vector3[numPointsToDraw];
        var dt = (Spline.End - Spline.Start) / numPointsToDraw;
        var t = Spline.Start;
        
        for (var i = 0; i < numPointsToDraw; i++)
        {
            var pos = Spline.Evaluate(t).XZToVector3();  // get point on spline in xz-plane
            var hit = surface.GetCollision(pos, false);  // find height of point on surface
            positions[i] = Mathf.Approximately(hit.HitNormal.sqrMagnitude, 0.0f) ? pos: hit.Point;
            t += dt;
        }

        // feed generated points to lineRenderer
        Line.positionCount = numPointsToDraw;
        Line.SetPositions(positions);
    }
    
    public Tuple<Vector3, Vector3> GetClosestPoint(Vector3 pos, TriangleSurface surface)
    {
        // use Newton's Midpoint-rule to find closest point on spline to given position
        var t0 = Spline.Start;
        var t1 = Spline.End;
        var t01 = 0.5f * (t1 - t0);
        var point = pos.XZToVector2();  // given point projected on xz-plane
        var onSpline = Spline.Evaluate(t0);  // first guess at closest point
        var tangent = Spline.Tangent(t0);  // tangent at first guess
        var diff = point - onSpline;  // vector from guess to given point
        var dot = Vector2.Dot(diff, tangent);
        var i = 0;
        const int limit = 50;  // iteration limit
        
        // iterate until tangent and difference vector are perpendicular, or until max iterations are reached
        while (dot*dot > 1e-4f && i++ < limit)
        {
            // get tangent and difference vector at current guess
            onSpline = Spline.Evaluate(t01);
            diff = point - onSpline;
            tangent = Spline.Tangent(t01);
            dot = Vector2.Dot(diff, tangent);
            
            // move guess forward along spline if angle between tangent and difference vector is acute
            if (dot > 0f)
            {
                t0 = t01;
                t01 = 0.5f * (t1 + t0);
            }
            else  // move guess backward if angle is obtuse
            {
                t1 = t01;
                t01 = 0.5f * (t1 + t0);
            }
        }
        
        // project closest point and tangent vector onto surface:
        var hit = surface.GetCollision(onSpline.XZToVector3(), false);
        pos = Mathf.Approximately(hit.HitNormal.sqrMagnitude, 0.0f) ? onSpline.XZToVector3(): hit.Point;
        var tan = Mathf.Approximately(hit.HitNormal.sqrMagnitude, 0.0f) ? tangent.XZToVector3(): Vector3.ProjectOnPlane(tangent.XZToVector3(), hit.HitNormal).normalized;
        return new Tuple<Vector3, Vector3>(pos, tan);  // return tuple of point and tangent.
    }
}