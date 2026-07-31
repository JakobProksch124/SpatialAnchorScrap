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
    [Tooltip("Distance between chevrons along the path (m).")]
    [SerializeField] private float arrowSpacing = 3.5f;
    float arrowYOffset = 0.02f;      // lift arrows slightly above ground
    private List<GameObject> _spawnedArrows = new List<GameObject>();

    [Header("Arrow Look")]
    [Tooltip("Glowing blue chevrons on the path (reference look). OFF = the old arrow prefab.")]
    [SerializeField] bool useProceduralChevrons = true;
    [Tooltip("Glowing blue of the chevrons.")]
    [SerializeField] Color arrowTint = new Color(0.13f, 0.42f, 1f, 1f);
    [Tooltip("Emission strength of the chevrons: subtle glow, NOT a bloom bomb.")]
    [SerializeField] float arrowGlow = 0.35f;
    [Tooltip("Lighter blue of the path ribbon.")]
    [SerializeField] Color pathTint = new Color(0.45f, 0.66f, 1f, 1f);
    [Tooltip("Width of the glowing path ribbon (m).")]
    [SerializeField] float pathWidth = 0.30f;

    [Header("Arrow Flow (organic motion)")]
    [Tooltip("Chevrons drift FORWARD along the path at this speed (m/s) — slower than walking, " +
             "so a walking user slowly catches up to them.")]
    [SerializeField] float arrowFlowSpeed = 0.4f;
    [Tooltip("Hover amplitude (m) — purely UP AND DOWN, never along the path.")]
    [SerializeField] float arrowBobAmplitude = 0.045f;
    [Tooltip("Hover period (s) — slow and calm.")]
    [SerializeField] float arrowBobPeriod = 3.4f;
    [Tooltip("Fade distance (m) at the path ends so the flow wrap is invisible (no backward jump).")]
    [SerializeField] float arrowFadeDistance = 1.6f;

    // the smoothed path the arrows flow along (world positions + cumulative length)
    private readonly List<Vector3> _flowPath = new List<Vector3>();
    private readonly List<float> _flowCum = new List<float>();
    private float _flowTotal;

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

        // reference look: light glowing blue ribbon. Only the COLOUR changes — width, material and
        // alignment stay exactly as authored (touching alignment once made the line invisible).
        if (useProceduralChevrons) lineColor = pathTint;

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

                yield return new WaitForSeconds(0.05f); // wait a frame to animate drawing
            }
        }

        PlaceArrowsAlongPath(smoothPoints); // arrows join once the line has finished drawing
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
        if ((arrowHeadPrefab == null && !useProceduralChevrons) || pathPoints.Count < 2)
        {
            ClearArrows();
            _flowTotal = 0f;
            return;
        }

        // store the smoothed path (positions + cumulative length) — AnimateArrows samples it
        _flowPath.Clear();
        _flowCum.Clear();
        var cum = 0f;
        _flowPath.Add(pathPoints[0]);
        _flowCum.Add(0f);
        for (int i = 1; i < pathPoints.Count; i++)
        {
            cum += Vector3.Distance(pathPoints[i - 1], pathPoints[i]);
            _flowPath.Add(pathPoints[i]);
            _flowCum.Add(cum);
        }
        _flowTotal = cum;

        // pool to the needed count instead of destroy+respawn (cheaper, no flicker)
        var n = Mathf.Max(0, Mathf.FloorToInt(_flowTotal / arrowSpacing));
        while (_spawnedArrows.Count > n)
        {
            var last = _spawnedArrows[_spawnedArrows.Count - 1];
            _spawnedArrows.RemoveAt(_spawnedArrows.Count - 1);
            if (last != null) Destroy(last);
        }
        while (_spawnedArrows.Count < n)
        {
            GameObject arrow;
            if (useProceduralChevrons)
            {
                arrow = ArrowChevron.Create(arrowTint, arrowGlow);
            }
            else
            {
                arrow = Instantiate(arrowHeadPrefab);
                TintArrow(arrow);
            }
            _spawnedArrows.Add(arrow);
        }

        AnimateArrows();
    }

    /// <summary>Places the chevrons flat on the line. STATIC on purpose: distances are measured
    /// from the TARGET (which never moves), so a chevron keeps its world position while the user
    /// walks up to and through it. No drift, no hover — that read as "floating" and cheap.</summary>
    void AnimateArrows()
    {
        if (_spawnedArrows.Count == 0 || _flowTotal <= 0f)
            return;

        const float flow = 0f;
        var fade = Mathf.Max(0.01f, arrowFadeDistance);
        for (int i = 0; i < _spawnedArrows.Count; i++)
        {
            var arrow = _spawnedArrows[i];
            if (arrow == null) continue;

            // Distances are measured from the TARGET (which never moves), not from the user.
            // Measuring from the user made every arrow sit at a fixed distance ahead of them, so
            // the whole chain travelled at exactly walking speed — impossible to catch up with.
            var fromTarget = arrowSpacing * (i + 0.5f) - flow;
            var d = _flowTotal - fromTarget;   // distance along the path from the user
            if (d < 0f || d > _flowTotal) { arrow.transform.localScale = Vector3.zero; continue; }

            SamplePath(d, out var pos, out var dir);

            pos.y += arrowYOffset; // resting ON the line, not floating above it

            // scale-fade near both ends: a chevron shrinks away at the target and grows back in
            // near the user, so the forward flow never shows a backward jump
            var edge = Mathf.Min(d, _flowTotal - d);
            var k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(edge / fade));

            var tr = arrow.transform;
            tr.SetPositionAndRotation(pos, useProceduralChevrons
                ? Quaternion.LookRotation(dir)                                  // mesh points +Z
                : Quaternion.LookRotation(dir) * Quaternion.Euler(-90f, -90f, 0));
            tr.localScale = Vector3.one * k;
        }
    }

    void SamplePath(float d, out Vector3 pos, out Vector3 dir)
    {
        for (int i = 1; i < _flowCum.Count; i++)
        {
            if (_flowCum[i] >= d)
            {
                var seg = _flowCum[i] - _flowCum[i - 1];
                var t = seg > 1e-5f ? (d - _flowCum[i - 1]) / seg : 0f;
                pos = Vector3.Lerp(_flowPath[i - 1], _flowPath[i], t);
                dir = (_flowPath[i] - _flowPath[i - 1]).normalized;
                return;
            }
        }
        pos = _flowPath[_flowPath.Count - 1];
        dir = (_flowPath[_flowPath.Count - 1] - _flowPath[_flowPath.Count - 2]).normalized;
    }

    /// <summary>Glowing tint (base + emission) so the arrows read like the design reference.</summary>
    void TintArrow(GameObject arrow)
    {
        foreach (var r in arrow.GetComponentsInChildren<Renderer>())
            foreach (var m in r.materials)
            {
                m.color = arrowTint;
                if (m.HasProperty("_EmissionColor"))
                {
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", arrowTint * 1.6f);
                }
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