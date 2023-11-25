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
        
        while (Mathf.Abs(dot) > 1e-4f && i++ < limit)
        {
            onSpline = Spline.Evaluate(t01);
            diff = onSpline - point;
            tangent = Spline.Tangent(t01);
            dot = Vector2.Dot(diff, tangent);
            
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