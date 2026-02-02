using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PathGenerator : MonoBehaviour
{
    Transform start;
    [SerializeField] Transform target;
    
    [SerializeField] int subdivisions = 10;
    
    LineRenderer _lineRenderer;

    bool _pathing;
    
    void Start()
    {
        _lineRenderer = GetComponent<LineRenderer>();
    }

    void Update()
    {
        start = Camera.main.transform;
        if (_pathing)
            GetPath();
    }
    
    public void StartPathing()
    {
        _pathing = true;
    }
    
    void GetPath()
    {
        if (start)
        {
            var path = new NavMeshPath();

            if (NavMesh.CalculatePath(start.position, target.position, NavMesh.AllAreas, path))
                Debug.Log("[PathGenerator] Path found");
            else
                Debug.Log("[PathGenerator] No path found");
            if (!target)
                Debug.Log("[PathGenerator] No target exists");

                _lineRenderer.positionCount = path.corners.Length;

            DrawCentripetalCurve(path.corners);
        } else
        {
            Debug.Log("[PathGenerator] No start exists");
        }
    }
    
    void DrawCentripetalCurve(Vector3[] controlPoints)
    {
        if (controlPoints.Length < 2) return;

        var smoothPoints = new List<Vector3>();

        for (var i = 0; i < controlPoints.Length - 1; i++)
        {
            var p0 = i == 0 ? controlPoints[i] : controlPoints[i - 1];
            var p1 = controlPoints[i];
            var p2 = controlPoints[i + 1];
            var p3 = i + 2 < controlPoints.Length ? controlPoints[i + 2] : controlPoints[i + 1];

            const float t0 = 0.0f;
            var t1 = GetT(t0, p0, p1);
            var t2 = GetT(t1, p1, p2);
            var t3 = GetT(t2, p2, p3);

            for (var t = t1; t < t2; t += (t2 - t1) / subdivisions)
            {
                var a1 = (t1 - t) / (t1 - t0) * p0 + (t - t0) / (t1 - t0) * p1;
                var a2 = (t2 - t) / (t2 - t1) * p1 + (t - t1) / (t2 - t1) * p2;
                var a3 = (t3 - t) / (t3 - t2) * p2 + (t - t2) / (t3 - t2) * p3;

                var b1 = (t2 - t) / (t2 - t0) * a1 + (t - t0) / (t2 - t0) * a2;
                var b2 = (t3 - t) / (t3 - t1) * a2 + (t - t1) / (t3 - t1) * a3;

                var c = (t2 - t) / (t2 - t1) * b1 + (t - t1) / (t2 - t1) * b2;

                smoothPoints.Add(c);
            }
        }

        _lineRenderer.positionCount = smoothPoints.Count;
        _lineRenderer.SetPositions(smoothPoints.ToArray());
    }

    static float GetT(float t, Vector3 p0, Vector3 p1)
    {
        var distance = Vector3.Distance(p0, p1);
        if (distance < 1e-5f)
            distance = 1e-5f;
        return Mathf.Pow(distance, 0.5f) + t;
    }
}
