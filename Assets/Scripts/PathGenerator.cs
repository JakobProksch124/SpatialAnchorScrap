using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PathGenerator : MonoBehaviour
{
    Transform start;
    [SerializeField] Transform target;
    
    [SerializeField] int subdivisions = 10;

    [Header("Line Appearance")]
    [Tooltip("Material for the line. Keep its base color white if you want the color below to control the tint cleanly.")]
    [SerializeField] Material lineMaterial;
    [Tooltip("Color applied to the line (multiplied with the material's color)")]
    [SerializeField] Color lineColor = Color.cyan;

    [Header("Arrow Settings")]
    [SerializeField] GameObject arrowHeadPrefab;
    [SerializeField] float arrowSpacing = 2f;      // distance between arrows in meters
    [SerializeField] float arrowYOffset = 0.02f;      // lift arrows slightly above ground

    private List<GameObject> _spawnedArrows = new List<GameObject>();

    LineRenderer _lineRenderer;

    bool _pathing = true;




    void Start()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.alignment = LineAlignment.View;
        _lineRenderer.useWorldSpace = true;

        // Apply material if assigned
        if (lineMaterial != null)
        {
            _lineRenderer.material = lineMaterial;
        }

        // Apply color via gradient (more reliable than startColor/endColor)
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(lineColor, 0f), new GradientColorKey(lineColor, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(lineColor.a, 0f), new GradientAlphaKey(lineColor.a, 1f) }
        );
        _lineRenderer.colorGradient = gradient;

        // Also set material color so it doesn't multiply with a non-white tint
        if (_lineRenderer.material != null)
        {
            _lineRenderer.material.color = Color.white;
        }
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
                //Debug.Log("[PathGenerator] Path found");
            /*else
                //Debug.Log("[PathGenerator] No path found");
            if (!target)
                //Debug.Log("[PathGenerator] No target exists");

                _lineRenderer.positionCount = path.corners.Length;*/

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
        PlaceArrowsAlongPath(smoothPoints);
    }



    static float GetT(float t, Vector3 p0, Vector3 p1)
    {
        var distance = Vector3.Distance(p0, p1);
        if (distance < 1e-5f)
            distance = 1e-5f;
        return Mathf.Pow(distance, 0.5f) + t;
    }

    void PlaceArrowsAlongPath(List<Vector3> pathPoints)
    {
        if (arrowHeadPrefab == null || pathPoints.Count < 2)
            return;

        ClearArrows();

        float accumulatedDistance = 0f;
        float nextArrowDistance = arrowSpacing;

        for (int i = 1; i < pathPoints.Count; i++)
        {
            Vector3 prev = pathPoints[i - 1];
            Vector3 current = pathPoints[i];

            float segmentDistance = Vector3.Distance(prev, current);

            while (accumulatedDistance + segmentDistance >= nextArrowDistance)
            {
                float remaining = nextArrowDistance - accumulatedDistance;
                float t = remaining / segmentDistance;

                Vector3 position = Vector3.Lerp(prev, current, t);
                Vector3 direction = (current - prev).normalized;

                position.y += arrowYOffset;

                GameObject arrow = Instantiate(
                    arrowHeadPrefab,
                    position,
                    Quaternion.LookRotation(direction)
                );

                _spawnedArrows.Add(arrow);

                nextArrowDistance += arrowSpacing;
            }

            accumulatedDistance += segmentDistance;
        }
    }

    public void ClearArrows()
    {
        if (_spawnedArrows == null)
            return;

        foreach (var arrow in _spawnedArrows)
        {
            if (arrow != null)
                Destroy(arrow);
        }

        _spawnedArrows.Clear();
    }
}
