using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the navigation chevron procedurally: a thick, round-nosed "^" lying flat on the path,
/// slightly extruded, with an HDR-bright unlit blue material so URP bloom makes it glow (the
/// look from Daniel's reference render). Mesh points along +Z, so a plain LookRotation(dir)
/// orients it correctly.
/// </summary>
public static class ArrowChevron
{
    private static Mesh _mesh;
    private static Material _mat;

    public static GameObject Create(Color tint, float glow = 2.4f)
    {
        var go = new GameObject("PathChevron");
        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        mf.sharedMesh = _mesh ??= Build();
        mr.sharedMaterial = _mat ??= MakeMaterial(tint, glow);
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        return go;
    }

    /// <summary>Glossy blue plastic with a soft glow — like the reference render. Deliberately
    /// LIT (so the extrusion catches highlights and reads as a solid arrow) with only mild
    /// emission; an unlit HDR material blooms into a shapeless blob.</summary>
    private static Material MakeMaterial(Color tint, float glow)
    {
        var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var m = new Material(sh);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", tint);
        m.color = tint;
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.85f); // glassy sheen
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        if (m.HasProperty("_EmissionColor"))
        {
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            m.SetColor("_EmissionColor", tint * Mathf.Max(0f, glow)); // subtle self-glow
        }
        return m;
    }

    // ---- geometry ----

    private const float ArmSpan = 0.23f;   // half width of the chevron (m) -> ~0.46 m span
    private const float ArmDepth = 0.20f;  // how far the arms trail behind the nose (m)
    private const float HalfWidth = 0.055f; // stroke half-thickness (m) -> chunky like the render
    private const float Height = 0.05f;    // extrusion (m) — catches a highlight on top
    private const int Samples = 26;        // centreline resolution
    private const int CapSegments = 8;     // rounded end caps

    private static Mesh Build()
    {
        // centreline: quadratic Bezier from the left arm end to the right arm end, pulled toward
        // the nose so the corner stays sharp-ish but rounded (like the reference chevron).
        var left = new Vector3(-ArmSpan, 0f, -ArmDepth);
        var right = new Vector3(ArmSpan, 0f, -ArmDepth);
        var nose = new Vector3(0f, 0f, 0f);
        var ctrl = nose + (nose - (left + right) * 0.5f) * 0.55f;

        var line = new List<Vector3>(Samples);
        for (var i = 0; i < Samples; i++)
        {
            var t = i / (float)(Samples - 1);
            var u = 1f - t;
            line.Add(u * u * left + 2f * u * t * ctrl + t * t * right);
        }

        var verts = new List<Vector3>();
        var tris = new List<int>();

        // thick ribbon: top + bottom faces and the outer walls
        var topStart = verts.Count;
        for (var i = 0; i < line.Count; i++)
        {
            var tangent = (i == 0 ? line[1] - line[0] : line[i] - line[i - 1]).normalized;
            var side = Vector3.Cross(Vector3.up, tangent).normalized * HalfWidth;
            verts.Add(line[i] - side + Vector3.up * Height); // 0: top-left
            verts.Add(line[i] + side + Vector3.up * Height); // 1: top-right
            verts.Add(line[i] - side);                       // 2: bottom-left
            verts.Add(line[i] + side);                       // 3: bottom-right
        }

        for (var i = 0; i < line.Count - 1; i++)
        {
            var a = topStart + i * 4;
            var b = a + 4;
            AddQuad(tris, a + 0, a + 1, b + 1, b + 0);       // top
            AddQuad(tris, a + 3, a + 2, b + 2, b + 3);       // bottom
            AddQuad(tris, a + 2, a + 0, b + 0, b + 2);       // left wall
            AddQuad(tris, a + 1, a + 3, b + 3, b + 1);       // right wall
        }

        // rounded caps at both ends
        AddCap(verts, tris, line[0], (line[0] - line[1]).normalized);
        int last = line.Count - 1;
        AddCap(verts, tris, line[last], (line[last] - line[last - 1]).normalized);

        var mesh = new Mesh { name = "PathChevron" };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void AddCap(List<Vector3> verts, List<int> tris, Vector3 centre, Vector3 outward)
    {
        var side = Vector3.Cross(Vector3.up, outward).normalized;
        var top = verts.Count;
        verts.Add(centre + Vector3.up * Height); // fan centre (top)
        verts.Add(centre);                      // fan centre (bottom)
        for (var i = 0; i <= CapSegments; i++)
        {
            var a = Mathf.PI * i / CapSegments - Mathf.PI * 0.5f;
            var dir = (side * Mathf.Cos(a) + outward * Mathf.Sin(a)) * HalfWidth;
            verts.Add(centre + dir + Vector3.up * Height);
            verts.Add(centre + dir);
        }
        for (var i = 0; i < CapSegments; i++)
        {
            int t0 = top + 2 + i * 2;
            int t1 = t0 + 2;
            tris.Add(top); tris.Add(t0); tris.Add(t1);                    // top fan
            tris.Add(top + 1); tris.Add(t1 + 1); tris.Add(t0 + 1);        // bottom fan
            AddQuad(tris, t0 + 1, t0, t1, t1 + 1);                        // rim
        }
    }

    private static void AddQuad(List<int> tris, int a, int b, int c, int d)
    {
        tris.Add(a); tris.Add(b); tris.Add(c);
        tris.Add(a); tris.Add(c); tris.Add(d);
    }
}
