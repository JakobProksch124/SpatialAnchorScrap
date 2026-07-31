using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PathGenerator : MonoBehaviour
{
    Transform start;
    [SerializeField] Transform target;
    [SerializeField] int subdivisions = 10;
    public bool _pathing = true;
    LineRenderer _lineRenderer;

    [Header("Line Appearance")]
    [Tooltip("Material for the line. Keep its base color white if you want the color below to control the tint cleanly.")]
    [SerializeField] Material lineMaterial;
    [Tooltip("Color applied to the line (multiplied with the material's color)")]
    [SerializeField] Color lineColor = Color.cyan;

    [Header("Arrow Settings")]
    [SerializeField] GameObject arrowHeadPrefab;
    private float arrowSpacing = 6f; // distance between arrows in meters
    float arrowYOffset = 0.02f;      // lift arrows slightly above ground
    private List<GameObject> _spawnedArrows = new List<GameObject>();

    float updateThreshold = 1f; // only recalc if moved more than 1m
    public bool firstDraw = true; // true until the line has been drawn once
    bool isDrawingFirstTime = false;
    private Transform inBetweenTarget;

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

    [Tooltip("How often to recompute the nav path + rebuild the arrows while the user is MOVING " +
             "(seconds). 0.1 = 10x/s reads as smooth; every frame was a big GPU/CPU sink, and " +
             "0.3 looked laggy while walking.")]
    [SerializeField] private float pathUpdateInterval = 0.1f;

    [Tooltip("Skip the rebuild entirely while the user has moved less than this since the last " +
             "rebuild (metres) — standing still costs nothing.")]
    [SerializeField] private float minMoveForUpdate = 0.15f;

    private float _pathTimer;
    private Vector3 _lastPathOrigin = Vector3.positiveInfinity;

    void Update()
    {
        start = Camera.main.transform;
        if (!_pathing || start == null || target == null)
            return;

        // smooth without the cost: rebuild 10x/s while walking, not at all while standing still
        _pathTimer += Time.deltaTime;
        if (_pathTimer < pathUpdateInterval)
            return;
        _pathTimer = 0f;

        if ((start.position - _lastPathOrigin).sqrMagnitude < minMoveForUpdate * minMoveForUpdate)
            return;
        _lastPathOrigin = start.position;

        GetPath();
    }

    public void StartPathing()
    {
        _pathing = true;
    }

    /*void GetPath()
    {
        if (start)
        {
            var path = new NavMeshPath();

            if (NavMesh.CalculatePath(start.position, target.position, NavMesh.AllAreas, path))
                //Debug.Log("[PathGenerator] Path found");
            *//*else
                //Debug.Log("[PathGenerator] No path found");
            if (!target)
                //Debug.Log("[PathGenerator] No target exists");

                _lineRenderer.positionCount = path.corners.Length;*//*

            DrawCentripetalCurve(path.corners);
        } else
        {
            Debug.Log("[PathGenerator] No start exists");
        }
    }*/

    void GetPath()
    {
        if (start == null)
            return;

        List<Vector3> corners = new List<Vector3>();

        if (inBetweenTarget != null)
        {

            NavMeshPath first = new NavMeshPath();
            NavMeshPath second = new NavMeshPath();

            if (!NavMesh.CalculatePath(start.position, inBetweenTarget.position,
                                       NavMesh.AllAreas, first))
                return;
        

            if (!NavMesh.CalculatePath(inBetweenTarget.position, target.position,
                                       NavMesh.AllAreas, second))
                return;
        

            corners.AddRange(first.corners);

            // Skip duplicate point where the two paths meet.
            for (int i = 1; i < second.corners.Length; i++)
                corners.Add(second.corners[i]);
        }
        else
        {
            NavMeshPath path = new NavMeshPath();

            if (!NavMesh.CalculatePath(start.position, target.position,
                                       NavMesh.AllAreas, path))
                return;

            corners.AddRange(path.corners);
        }

        if (firstDraw)
        {
            if (!isDrawingFirstTime)
                StartCoroutine(DrawCentripetalCurveCoroutine(corners.ToArray()));
        }
        else
        {
            DrawCentripetalCurveInstant(corners.ToArray());
        }
    }

    void DrawCentripetalCurveInstant(Vector3[] controlPoints)
    {
        if (controlPoints.Length < 2) return;

        List<Vector3> smoothPoints = new List<Vector3>();

        for (int i = 0; i < controlPoints.Length - 1; i++)
        {
            Vector3 p0 = i == 0 ? controlPoints[i] : controlPoints[i - 1];
            Vector3 p1 = controlPoints[i];
            Vector3 p2 = controlPoints[i + 1];
            Vector3 p3 = i + 2 < controlPoints.Length ? controlPoints[i + 2] : controlPoints[i + 1];

            const float t0 = 0.0f;
            float t1 = GetT(t0, p0, p1);
            float t2 = GetT(t1, p1, p2);
            float t3 = GetT(t2, p2, p3);

            for (float t = t1; t < t2; t += (t2 - t1) / subdivisions)
            {
                Vector3 a1 = (t1 - t) / (t1 - t0) * p0 + (t - t0) / (t1 - t0) * p1;
                Vector3 a2 = (t2 - t) / (t2 - t1) * p1 + (t - t1) / (t2 - t1) * p2;
                Vector3 a3 = (t3 - t) / (t3 - t2) * p2 + (t - t2) / (t3 - t2) * p3;

                Vector3 b1 = (t2 - t) / (t2 - t0) * a1 + (t - t0) / (t2 - t0) * a2;
                Vector3 b2 = (t3 - t) / (t3 - t1) * a2 + (t - t1) / (t3 - t1) * a3;

                Vector3 c = (t2 - t) / (t2 - t1) * b1 + (t - t1) / (t2 - t1) * b2;

                smoothPoints.Add(c);
            }
        }

        _lineRenderer.positionCount = smoothPoints.Count;
        _lineRenderer.SetPositions(smoothPoints.ToArray());

        // Place arrows along full path
        PlaceArrowsAlongPath(smoothPoints);
    }

    IEnumerator DrawCentripetalCurveCoroutine(Vector3[] controlPoints)
    {
        isDrawingFirstTime = true;

        if (controlPoints.Length < 2) yield break;

        List<Vector3> smoothPoints = new List<Vector3>();
        _lineRenderer.positionCount = 0;

        ClearArrows(); // remove any previous arrows

        float accumulatedDistance = 0f;
        float nextArrowDistance = arrowSpacing;

        for (int i = 0; i < controlPoints.Length - 1; i++)
        {
            Vector3 p0 = i == 0 ? controlPoints[i] : controlPoints[i - 1];
            Vector3 p1 = controlPoints[i];
            Vector3 p2 = controlPoints[i + 1];
            Vector3 p3 = i + 2 < controlPoints.Length ? controlPoints[i + 2] : controlPoints[i + 1];

            const float t0 = 0.0f;
            float t1 = GetT(t0, p0, p1);
            float t2 = GetT(t1, p1, p2);
            float t3 = GetT(t2, p2, p3);

            for (float t = t1; t < t2; t += (t2 - t1) / subdivisions)
            {
                Vector3 a1 = (t1 - t) / (t1 - t0) * p0 + (t - t0) / (t1 - t0) * p1;
                Vector3 a2 = (t2 - t) / (t2 - t1) * p1 + (t - t1) / (t2 - t1) * p2;
                Vector3 a3 = (t3 - t) / (t3 - t2) * p2 + (t - t2) / (t3 - t2) * p3;

                Vector3 b1 = (t2 - t) / (t2 - t0) * a1 + (t - t0) / (t2 - t0) * a2;
                Vector3 b2 = (t3 - t) / (t3 - t1) * a2 + (t - t1) / (t3 - t1) * a3;

                Vector3 c = (t2 - t) / (t2 - t1) * b1 + (t - t1) / (t2 - t1) * b2;

                smoothPoints.Add(c);
                _lineRenderer.positionCount = smoothPoints.Count;
                _lineRenderer.SetPositions(smoothPoints.ToArray());

                // Arrow placement
                if (arrowHeadPrefab != null && smoothPoints.Count > 1)
                {
                    Vector3 prev = smoothPoints[smoothPoints.Count - 2];
                    Vector3 current = smoothPoints[smoothPoints.Count - 1];
                    float segmentDistance = Vector3.Distance(prev, current);

                    while (accumulatedDistance + segmentDistance >= nextArrowDistance)
                    {
                        float remaining = nextArrowDistance - accumulatedDistance;
                        float tArrow = remaining / segmentDistance;
                        Vector3 pos = Vector3.Lerp(prev, current, tArrow);
                        Vector3 dir = (current - prev).normalized;
                        pos.y += arrowYOffset;

                        GameObject arrow = Instantiate(
                            arrowHeadPrefab,
                            pos,
                            Quaternion.LookRotation(dir) * Quaternion.Euler(-90f, -90f, 0)
                        );
                        _spawnedArrows.Add(arrow);
                        nextArrowDistance += arrowSpacing;
                    }

                    accumulatedDistance += segmentDistance;
                }

                yield return new WaitForSeconds(0.05f); // wait a frame to animate drawing
            }
        }
        isDrawingFirstTime = false;
        firstDraw = false;
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
                    Quaternion.LookRotation(direction) * Quaternion.Euler(-90f, -90f, 0)
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

    public void AddInbetweenTarget(Transform newTarget)
    {
        
        Debug.Log("Setting InBetween Target");
        if (newTarget != null)
        {
        inBetweenTarget = newTarget;

        }
        else
        {
            Debug.Log("Inbetween Target is null");
        }
    }

    public void ClearInbetweenTarget()
    {
        
        Debug.Log("Clearing InBetween Target");
        inBetweenTarget = null;
    }
}