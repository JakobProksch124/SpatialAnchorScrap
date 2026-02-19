using System.Collections;
using TMPro;
using UnityEngine;

public class ArrivalCue : MonoBehaviour
{
    [Header("Target & Location")]
    [Tooltip("Name of a child GameObject under this root object where the arrival cue will be placed")]
    public string targetName;

    [Tooltip("Text displayed above the cylinder (e.g. room or building name)")]
    public string locationName = "Location";

    [Header("Proximity")]
    [Tooltip("Distance (meters) at which arrival is triggered (requires distance + gaze)")]
    public float arrivalDistance = 5f;

    [Tooltip("Required dot product of camera forward vs cue direction to trigger (0-1, higher = more precise gaze needed)")]
    public float gazeThreshold = 0.7f;

    [Header("Despawn")]
    [Tooltip("Seconds after arrival before the cue despawns")]
    public float despawnAfter = 3.5f;

    [Header("Appearance")]
    [Tooltip("Vertical offset of the floating text above the cylinder (meters)")]
    public float textOffset = 0.25f;

    [Tooltip("Radius of the cylinder (meters)")]
    public float cylinderRadius = 0.1f;

    [Tooltip("Thickness/depth of the cylinder (meters)")]
    public float cylinderDepth = 0.05f;

    [Tooltip("Font size for the floating text")]
    public float textFontSize = 1.84f;

    [Tooltip("Number of segments around the cylinder circumference (higher = smoother)")]
    public int cylinderSegments = 64;

    [Header("Border")]
    [Tooltip("Intensity of the glowing border emission (0-1)")]
    public float glowIntensity = 0.8f;

    [Header("Rotation Towards User")]
    [Tooltip("If true, the arrival cue slightly rotates to face the user")]
    public bool enableTurnTowardsUser = true;

    [Tooltip("Maximum rotation angle toward user (degrees)")]
    public float turnMaxAngle = 12.5f;

    [Tooltip("Speed of rotation interpolation")]
    public float turnRotationSpeed = 2f;

    [Tooltip("Distance at which rotation starts (meters). 0 = always active")]
    public float turnTriggerDistance = 6f;

    public string message = "Check Companion!";

    // --- Runtime state ---
    private GameObject cueInstance;
    private GameObject cylinderPivotRef;
    private GameObject borderRef;
    private Material borderMaterialRef;
    private TextMeshPro floatingText;
    private Renderer cylinderRenderer;
    private Material cylinderMaterial;
    private Transform playerTransform;
    private bool hasArrived = false;

    // --- Checkmark icon ---
    private GameObject checkmarkRef;
    private Material checkmarkMaterialRef;

    // --- PathGenerator references ---
    private MonoBehaviour pathGenerator;
    private LineRenderer[] pathLineRenderers;

    private static readonly Color DefaultColor = new Color(0.78f, 0.78f, 0.78f); // light grey
    private static readonly Color ArrivedColor = new Color(0.15f, 0.65f, 0.15f);  // green
    private static readonly Color BorderColor = new Color(0.88f, 0.88f, 0.88f);  // slightly lighter grey

    void Start()
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
            playerTransform = mainCam.transform;
        else
            Debug.LogWarning("[ArrivalCue] No main camera found!");

        // Find PathGenerator component on this GameObject (same pattern as Building_TransitionCues)
        foreach (var component in GetComponents<MonoBehaviour>())
        {
            if (component.GetType().Name == "PathGenerator")
            {
                pathGenerator = component;
                break;
            }
        }
    }

    void Update()
    {
        if (hasArrived || cueInstance == null || playerTransform == null)
            return;

        if (ShouldTriggerArrival())
        {
            hasArrived = true;
            StartCoroutine(OnArrivedSequence());
        }
    }

    // Spawn the arrival cue at the target child location
    public void SpawnArrivalCue()
    {
        // Clean up any existing cue
        if (cueInstance != null)
        {
            Destroy(cueInstance);
        }
        hasArrived = false;

        // Re-enable path generator whenever the arrival cue spawns
        EnablePathGenerator();

        // Find target child
        Transform target = transform.Find(targetName);
        if (target == null)
        {
            Debug.LogError($"[ArrivalCue] Child '{targetName}' not found under '{gameObject.name}'.");
            return;
        }

        // === Root container ===
        cueInstance = new GameObject($"ArrivalCue_{locationName}");
        cueInstance.transform.SetParent(target, false);
        cueInstance.transform.localPosition = Vector3.zero;
        cueInstance.transform.position += new Vector3(0f, 1f, 0f);
        cueInstance.transform.localRotation = Quaternion.identity;

        // === Cylinder ===
        cylinderPivotRef = new GameObject("ArrivalDiscPivot");
        GameObject cylinderPivot = cylinderPivotRef;
        cylinderPivot.transform.SetParent(cueInstance.transform, false);
        cylinderPivot.transform.localPosition = Vector3.zero;
        cylinderPivot.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        GameObject cylinder = CreateSmoothCylinder("ArrivalDisc", cylinderRadius, cylinderDepth / 2f, cylinderSegments);
        cylinder.transform.SetParent(cylinderPivot.transform, false);
        cylinder.transform.localPosition = Vector3.zero;
        cylinder.transform.localRotation = Quaternion.identity;
        cylinder.transform.localScale = Vector3.one;

        // Material — frosted glass style matching TransitionCueFactory
        cylinderMaterial = CreateFrostedGlassMaterial(DefaultColor, 0.7f);
        cylinderRenderer = cylinder.GetComponent<Renderer>();
        cylinderRenderer.material = cylinderMaterial;

        // === Glowing Border ===
        // Slightly larger cylinder behind the main one, with emissive material (same pattern as GlowingBorderEffect)
        float borderScale = 1.05f;
        GameObject borderPivot = new GameObject("BorderPivot");
        borderPivot.transform.SetParent(cueInstance.transform, false);
        borderPivot.transform.localPosition = Vector3.zero;
        borderPivot.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        GameObject border = CreateSmoothCylinder("GlowingBorder", cylinderRadius * borderScale, (cylinderDepth / 2f) * 0.95f, cylinderSegments);
        border.transform.SetParent(borderPivot.transform, false);
        border.transform.localPosition = Vector3.zero;
        border.transform.localRotation = Quaternion.identity;
        border.transform.localScale = Vector3.one;

        // Emissive border material (matches GlowingBorderEffect pattern)
        Material borderMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        borderMat.EnableKeyword("_EMISSION");
        borderMat.SetColor("_BaseColor", BorderColor);
        borderMat.SetColor("_EmissionColor", BorderColor * glowIntensity * 2f);
        borderMat.SetFloat("_Surface", 1); // Transparent
        borderMat.SetFloat("_Smoothness", 0.9f);
        border.GetComponent<Renderer>().material = borderMat;
        borderRef = border;
        borderMaterialRef = borderMat;

        // === Checkmark icon (hidden until arrival) ===
        Texture2D checkmarkTexture = Resources.Load<Texture2D>("CheckmarkIcon");
        if (checkmarkTexture != null)
        {
            GameObject checkmarkQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            checkmarkQuad.name = "CheckmarkIcon";
            checkmarkQuad.transform.SetParent(cylinderPivot.transform, false);

            // Position just above the cylinder's top face; rotate quad to lie flat (face +Y in pivot space)
            checkmarkQuad.transform.localPosition = new Vector3(0f, cylinderDepth / 2f + 0.005f, 0f);
            checkmarkQuad.transform.localRotation = Quaternion.Euler(90f, 0f, 180f);

            // Scale to fit inside the cylinder (slightly smaller than diameter)
            float iconSize = cylinderRadius * 1.2f;
            checkmarkQuad.transform.localScale = new Vector3(iconSize, iconSize, 1f);

            // Remove collider
            Collider checkCollider = checkmarkQuad.GetComponent<Collider>();
            if (checkCollider != null) Destroy(checkCollider);

            // Unlit transparent material — starts fully invisible (alpha 0)
            Material checkMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            checkMat.SetTexture("_BaseMap", checkmarkTexture);
            checkMat.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0f));
            checkMat.SetFloat("_Surface", 1); // Transparent
            checkMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            checkMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            checkMat.SetInt("_ZWrite", 0);
            checkMat.renderQueue = 3200; // Above cylinder (3000) and text (3100)
            checkMat.SetOverrideTag("RenderType", "Transparent");
            checkMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            checkmarkQuad.GetComponent<Renderer>().material = checkMat;
            checkmarkRef = checkmarkQuad;
            checkmarkMaterialRef = checkMat;
        }

        // === Floating text above cylinder ===
        GameObject textObj = new GameObject("LocationText");
        textObj.transform.SetParent(cueInstance.transform, false);
        textObj.transform.localPosition = new Vector3(0, textOffset, 0);
        textObj.transform.localRotation = Quaternion.Euler(0, 180, 0);

        floatingText = textObj.AddComponent<TextMeshPro>();
        floatingText.text = locationName;
        floatingText.fontSize = textFontSize;
        floatingText.fontStyle = FontStyles.Bold;
        floatingText.alignment = TextAlignmentOptions.Center;
        floatingText.color = Color.white;
        floatingText.rectTransform.sizeDelta = new Vector2(2f, 0.5f);
        floatingText.enableAutoSizing = false;
        floatingText.overflowMode = TextOverflowModes.Overflow;

        // Load project font if available
        TMP_FontAsset boldFont = Resources.Load<TMP_FontAsset>("SF-Pro-Display-Bold-Font");
        if (boldFont != null)
            floatingText.font = boldFont;

        // Ensure text renders above transparent cylinder
        if (floatingText.fontMaterial != null)
            floatingText.fontMaterial.renderQueue = 3100;

        // === Rotation towards user ===
        if (enableTurnTowardsUser)
        {
            TurnTowardsUser rotateToUser = cueInstance.AddComponent<TurnTowardsUser>();
            rotateToUser.Initialize(turnMaxAngle, turnRotationSpeed, turnTriggerDistance);
        }
    }

    private bool ShouldTriggerArrival()
    {
        float distance = Vector3.Distance(cueInstance.transform.position, playerTransform.position);
        Debug.Log("distance between: " + distance);
        Debug.Log("threshold: " + arrivalDistance);
        float diff = arrivalDistance - distance;
        Debug.Log("difference: " + diff);
        if (distance > arrivalDistance)
            return false;
        else
        {
            Debug.Log("TRIGGER");
            // Gaze check
            Vector3 toCue = (cueInstance.transform.position - playerTransform.position).normalized;
            float dot = Vector3.Dot(playerTransform.forward, toCue);
            return dot >= gazeThreshold;
        }
    }

    // Cubic ease-in: slow start, accelerates (t^3)
    private static float EaseInCubic(float t) => t * t * t;

    // Cubic ease-out: fast start, decelerates (1-(1-t)^3)
    private static float EaseOutCubic(float t) { float inv = 1f - t; return 1f - inv * inv * inv; }

    // Cubic ease-in-out: smooth both ends
    private static float EaseInOutCubic(float t) => t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

    private IEnumerator OnArrivedSequence()
    {
        DisablePathGenerator();

        // Play arrival sound slightly before the visual animation starts
        PlayArrivalSound();
        yield return new WaitForSeconds(0.2f);

        // Change text immediately
        floatingText.text = message;

        // === Precompute sweep data ===
        MeshFilter mf = cylinderRenderer.GetComponent<MeshFilter>();
        Mesh mesh = mf.mesh;
        Vector3[] verts = mesh.vertices;
        Color[] vertColors = new Color[verts.Length];
        Color cylStartColor = new Color(DefaultColor.r, DefaultColor.g, DefaultColor.b, 0.7f);
        Color cylEndColor = new Color(ArrivedColor.r, ArrivedColor.g, ArrivedColor.b, 0.7f);

        float[] vertDistances = new float[verts.Length];
        float maxDist = 0f;
        for (int i = 0; i < verts.Length; i++)
        {
            float dist = new Vector2(verts[i].x, verts[i].z).magnitude;
            vertDistances[i] = dist;
            if (dist > maxDist) maxDist = dist;
        }
        if (maxDist > 0f)
            for (int i = 0; i < verts.Length; i++)
                vertDistances[i] /= maxDist;

        // === Phase 1: Arrival feedback ===
        // Everything runs in one unified loop over 1.0s:
        //   - Color sweep (cylinder radial + text color in sync)
        //   - Border pulse (spike then decay)
        //   - Scale pop (up then back down)
        float phase1Duration = 1.2f;
        float phase1Timer = 0f;

        Vector3 originalScale = cueInstance.transform.localScale;
        Vector3 popScale = originalScale * 1.2f;
        float popPeakT = 0.35f; // scale peaks at 35% of phase1

        float pulseIntensity = glowIntensity * 5f;
        float normalIntensity = glowIntensity * 2f;
        Color arrivedBorderColor = ArrivedColor;

        // Set border to green + spike immediately
        if (borderMaterialRef != null)
        {
            borderMaterialRef.SetColor("_BaseColor", arrivedBorderColor);
            borderMaterialRef.SetColor("_EmissionColor", arrivedBorderColor * pulseIntensity);
        }

        while (phase1Timer < phase1Duration)
        {
            phase1Timer += Time.deltaTime;
            float t = Mathf.Clamp01(phase1Timer / phase1Duration);
            float tCubic = EaseInOutCubic(t);

            // Color sweep (radial on cylinder)
            for (int i = 0; i < verts.Length; i++)
            {
                float vertT = Mathf.Clamp01((tCubic - vertDistances[i] * 0.7f) / 0.3f);
                float vertEased = EaseOutCubic(vertT);
                vertColors[i] = Color.Lerp(cylStartColor, cylEndColor, vertEased);
            }
            mesh.colors = vertColors;

            // Text color (white → green, synced with sweep)
            if (floatingText != null)
                floatingText.color = Color.Lerp(Color.white, ArrivedColor, EaseOutCubic(tCubic));

            // Border pulse decay 
            if (borderMaterialRef != null)
            {
                float currentIntensity = Mathf.Lerp(pulseIntensity, normalIntensity, EaseOutCubic(t));
                borderMaterialRef.SetColor("_EmissionColor", arrivedBorderColor * currentIntensity);
            }

            // Checkmark fade-in (synced with color sweep)
            if (checkmarkMaterialRef != null)
            {
                Color iconColor = checkmarkMaterialRef.GetColor("_BaseColor");
                iconColor.a = EaseOutCubic(tCubic);
                checkmarkMaterialRef.SetColor("_BaseColor", iconColor);
            }

            // Scale pop
            if (cueInstance != null)
            {
                if (t < popPeakT)
                {
                    // Scale up
                    float popUp = EaseOutCubic(t / popPeakT);
                    cueInstance.transform.localScale = Vector3.Lerp(originalScale, popScale, popUp);
                }
                else
                {
                    // Scale back down
                    float popDown = EaseOutCubic((t - popPeakT) / (1f - popPeakT));
                    cueInstance.transform.localScale = Vector3.Lerp(popScale, originalScale, popDown);
                }
            }

            yield return null;
        }

        // Finalize phase 1
        cylinderMaterial.SetColor("_BaseColor", cylEndColor);
        if (cueInstance != null) cueInstance.transform.localScale = originalScale;

        // === Phase 2: Despawn ===
        // Flows directly from scale pop into shrink & fade 
        float despawnDuration = despawnAfter; //* 0.3f; 

        float despawnTimer = 0f;
        Vector3 textStartPos = floatingText.transform.localPosition;
        Vector3 textEndPos = textStartPos + new Vector3(0, 0.15f, 0);
        Vector3 cylPivotStartScale = cylinderPivotRef != null ? cylinderPivotRef.transform.localScale : Vector3.one;
        Vector3 borderStartScale = borderRef != null ? borderRef.transform.localScale : Vector3.one;
        Color textStartColor = floatingText.color;
        float cylStartAlpha = 0.7f;

        while (despawnTimer < despawnDuration)
        {
            despawnTimer += Time.deltaTime;
            float t = Mathf.Clamp01(despawnTimer / despawnDuration);
            float eased = EaseInCubic(t); // slow start, fast end

            // Text float-up + fade 
            if (floatingText != null)
            {
                floatingText.transform.localPosition = Vector3.Lerp(textStartPos, textEndPos, EaseInOutCubic(t));
                Color fadedText = textStartColor;
                fadedText.a = Mathf.Lerp(1f, 0f, EaseOutCubic(t));
                floatingText.color = fadedText;
            }

            // Cylinder + border shrink
            if (cylinderPivotRef != null)
                cylinderPivotRef.transform.localScale = Vector3.Lerp(cylPivotStartScale, Vector3.zero, eased);
            if (borderRef != null)
                borderRef.transform.localScale = Vector3.Lerp(borderStartScale, Vector3.zero, eased);

            // Cylinder fade
            if (cylinderMaterial != null)
            {
                Color c = cylinderMaterial.GetColor("_BaseColor");
                c.a = Mathf.Lerp(cylStartAlpha, 0f, eased);
                cylinderMaterial.SetColor("_BaseColor", c);
            }

            // Border fade
            if (borderMaterialRef != null)
            {
                Color bc = borderMaterialRef.GetColor("_BaseColor");
                bc.a = Mathf.Lerp(1f, 0f, eased);
                borderMaterialRef.SetColor("_BaseColor", bc);
                borderMaterialRef.SetColor("_EmissionColor", arrivedBorderColor * normalIntensity * (1f - eased));
            }

            // Checkmark fade
            if (checkmarkMaterialRef != null)
            {
                Color iconColor = checkmarkMaterialRef.GetColor("_BaseColor");
                iconColor.a = Mathf.Lerp(1f, 0f, eased);
                checkmarkMaterialRef.SetColor("_BaseColor", iconColor);
            }

            yield return null;
        }

        // Destroy
        if (cueInstance != null)
        {
            Destroy(cueInstance);
            cueInstance = null;
        }
    }

    // Hide and destroy the arrival cue (e.g. when entering VR)
    public void HideArrivalCue()
    {
        if (cueInstance != null)
        {
            Destroy(cueInstance);
            cueInstance = null;
        }
        hasArrived = false;
    }

    private void PlayArrivalSound()
    {
        AudioClip clip = Resources.Load<AudioClip>("Arrival");
        if (clip == null)
        {
            Debug.LogWarning("[ArrivalCue] ArrivalSound not found in Resources/");
            return;
        }

        // Same volume as transition sound effects in TransitionCueConfig
        AudioSource.PlayClipAtPoint(clip, cueInstance.transform.position, 0.035f);
    }

    private void DisablePathGenerator()
    {
        if (pathGenerator != null)
        {
            pathGenerator.enabled = false;

            pathLineRenderers = pathGenerator.GetComponentsInChildren<LineRenderer>();
            foreach (var lineRenderer in pathLineRenderers)
            {
                if (lineRenderer != null)
                {
                    lineRenderer.enabled = false;
                }
            }
        }
    }

    private void EnablePathGenerator()
    {
        if (pathGenerator != null)
        {
            pathGenerator.enabled = true;

            if (pathLineRenderers != null)
            {
                foreach (var lineRenderer in pathLineRenderers)
                {
                    if (lineRenderer != null)
                    {
                        lineRenderer.enabled = true;
                    }
                }
            }
        }
    }

    // Creates a smooth cylinder mesh procedurally with the given radius, half-height, and segment count.
    private static GameObject CreateSmoothCylinder(string name, float radius, float halfHeight, int segments)
    {
        GameObject obj = new GameObject(name);
        MeshFilter mf = obj.AddComponent<MeshFilter>();
        MeshRenderer mr = obj.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();
        mesh.name = "SmoothCylinder";

        // Vertex layout:
        //   Side: 2 rings of (segments+1) verts (top and bottom, with duplicated seam vertex for UVs)
        //   Top cap: 1 center + (segments+1) rim
        //   Bottom cap: 1 center + (segments+1) rim
        int sideVerts = (segments + 1) * 2;
        int capVerts = (segments + 1) + 1; // rim + center
        int totalVerts = sideVerts + capVerts * 2;

        Vector3[] vertices = new Vector3[totalVerts];
        Vector3[] normals = new Vector3[totalVerts];
        Vector2[] uvs = new Vector2[totalVerts];

        int vi = 0;

        // --- Side vertices ---
        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            float u = (float)i / segments;
            Vector3 normal = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)).normalized;

            // Top ring
            vertices[vi] = new Vector3(x, halfHeight, z);
            normals[vi] = normal;
            uvs[vi] = new Vector2(u, 1f);
            vi++;

            // Bottom ring
            vertices[vi] = new Vector3(x, -halfHeight, z);
            normals[vi] = normal;
            uvs[vi] = new Vector2(u, 0f);
            vi++;
        }

        int topCapStart = vi;

        // --- Top cap vertices ---
        // Center
        vertices[vi] = new Vector3(0f, halfHeight, 0f);
        normals[vi] = Vector3.up;
        uvs[vi] = new Vector2(0.5f, 0.5f);
        vi++;
        // Rim
        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;

            vertices[vi] = new Vector3(x, halfHeight, z);
            normals[vi] = Vector3.up;
            uvs[vi] = new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f, Mathf.Sin(angle) * 0.5f + 0.5f);
            vi++;
        }

        int bottomCapStart = vi;

        // --- Bottom cap vertices ---
        // Center
        vertices[vi] = new Vector3(0f, -halfHeight, 0f);
        normals[vi] = Vector3.down;
        uvs[vi] = new Vector2(0.5f, 0.5f);
        vi++;
        // Rim
        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;

            vertices[vi] = new Vector3(x, -halfHeight, z);
            normals[vi] = Vector3.down;
            uvs[vi] = new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f, Mathf.Sin(angle) * 0.5f + 0.5f);
            vi++;
        }

        // --- Triangles ---
        int sideTriCount = segments * 6;
        int capTriCount = segments * 3;
        int[] triangles = new int[sideTriCount + capTriCount * 2];
        int ti = 0;

        // Side quads
        for (int i = 0; i < segments; i++)
        {
            int topLeft = i * 2;
            int bottomLeft = i * 2 + 1;
            int topRight = (i + 1) * 2;
            int bottomRight = (i + 1) * 2 + 1;

            triangles[ti++] = topLeft;
            triangles[ti++] = topRight;
            triangles[ti++] = bottomLeft;

            triangles[ti++] = bottomLeft;
            triangles[ti++] = topRight;
            triangles[ti++] = bottomRight;
        }

        // Top cap (center is topCapStart, rim starts at topCapStart+1)
        int topCenter = topCapStart;
        for (int i = 0; i < segments; i++)
        {
            triangles[ti++] = topCenter;
            triangles[ti++] = topCapStart + 1 + i + 1;
            triangles[ti++] = topCapStart + 1 + i;
        }

        // Bottom cap (center is bottomCapStart, rim starts at bottomCapStart+1)
        int bottomCenter = bottomCapStart;
        for (int i = 0; i < segments; i++)
        {
            triangles[ti++] = bottomCenter;
            triangles[ti++] = bottomCapStart + 1 + i;
            triangles[ti++] = bottomCapStart + 1 + i + 1;
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();

        mf.mesh = mesh;
        return obj;
    }

    private static Material CreateFrostedGlassMaterial(Color color, float alpha)
    {
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));

        Color transparentColor = new Color(color.r, color.g, color.b, alpha);
        mat.SetColor("_BaseColor", transparentColor);

        mat.SetFloat("_Surface", 1); // Transparent
        mat.SetFloat("_Smoothness", 0.85f);
        mat.SetFloat("_Metallic", 0.1f);

        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);

        mat.renderQueue = 3000;
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        return mat;
    }
}
