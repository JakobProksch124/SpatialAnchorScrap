#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class FrostedGlassAutoSetup
{
    private const string ShaderPath = "Assets/Shaders/FrostedGlassURP.shader";
    private const string MatPath = "Assets/Resources/FrostedGlass_Base.mat";

    private const string NoisePngPath = "Assets/Resources/FrostedGlass_NoiseNormal.png";
    private const string NoiseResName = "FrostedGlass_NoiseNormal";

    static FrostedGlassAutoSetup()
    {
        EditorApplication.delayCall += Run;
    }

    private static void Run()
    {
        try
        {
            EnsureFolders();
            EnsureUrpOpaqueTextureEnabled();
            EnsureShaderExists();
            EnsureMaterialExists();
            EnsureNoiseNormalTextureExists();
            EnsureCameraOpaqueTextureRequested();
        }
        catch (Exception e)
        {
            Debug.LogError($"[FrostedGlassAutoSetup] Failed: {e}");
        }
    }

    private static void EnsureFolders()
    {
        if (!Directory.Exists("Assets/Shaders")) Directory.CreateDirectory("Assets/Shaders");
        if (!Directory.Exists("Assets/Resources")) Directory.CreateDirectory("Assets/Resources");
    }

    private static void EnsureCameraOpaqueTextureRequested()
    {
        var cam = Camera.main;
        if (!cam) return;

        var data = cam.GetComponent<UniversalAdditionalCameraData>();
        if (!data) data = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var t = data.GetType();

        // Neuere URP: requiresColorOption = On
        var pOpt = t.GetProperty("requiresColorOption", flags);
        if (pOpt != null && pOpt.CanWrite)
        {
            var enumType = pOpt.PropertyType;              // CameraOverrideOption
            var on = Enum.Parse(enumType, "On");
            pOpt.SetValue(data, on);
            return;
        }

        // Ältere URP: requiresColorTexture = true
        var p = t.GetProperty("requiresColorTexture", flags);
        if (p != null && p.CanWrite)
        {
            p.SetValue(data, true);
            return;
        }

        // Fallback: Serialized field
        var f = t.GetField("m_RequiresColorTexture", flags)
             ?? t.GetField("m_RequireOpaqueTexture", flags)
             ?? t.GetField("m_RequiresOpaqueTexture", flags);
        if (f != null && f.FieldType == typeof(bool))
            f.SetValue(data, true);
    }

    private static void EnsureUrpOpaqueTextureEnabled()
    {
        var rp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (rp == null)
        {
            Debug.LogWarning("[FrostedGlassAutoSetup] currentRenderPipeline is not a UniversalRenderPipelineAsset. (URP not active?)");
            return;
        }

        bool changed = false;
        var t = rp.GetType();

        // supportsCameraOpaqueTexture = true
        changed |= SetBoolProperty(rp, t, "supportsCameraOpaqueTexture", true);

        // opaqueDownsampling = FourXBox (liefert schon „weich/blurred“ Opaque Texture)
        changed |= SetEnumProperty(rp, t, "opaqueDownsampling", "FourXBox");

        // optional (nicht zwingend für dieses Shader-Setup, aber oft nützlich):
        // changed |= SetBoolProperty(rp, t, "supportsCameraDepthTexture", true);

        if (changed)
        {
            EditorUtility.SetDirty(rp);
            AssetDatabase.SaveAssets();
            Debug.Log("[FrostedGlassAutoSetup] URP asset updated (Opaque Texture + Downsampling).");
        }
    }

    private static void EnsureNoiseNormalTextureExists()
    {
        if (File.Exists(NoisePngPath)) return;

        // 1) Normalmap als Texture2D generieren
        Texture2D tex = CreateNoiseNormalTexture2D(size: 256, noiseScale: 12f, strength: 4f, seed: 42);

        // 2) Als PNG in Resources schreiben
        byte[] png = tex.EncodeToPNG();
        File.WriteAllBytes(NoisePngPath, png);
        UnityEngine.Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(NoisePngPath, ImportAssetOptions.ForceSynchronousImport);

        // 3) Importer auf Normal Map stellen
        var importer = (TextureImporter)AssetImporter.GetAtPath(NoisePngPath);
        importer.textureType = TextureImporterType.NormalMap;
        importer.sRGBTexture = false;
        importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Bilinear;

        // keine „Height to Normal“-Konvertierung, weil PNG schon Normal-Daten enthält
        importer.convertToNormalmap = false;

        importer.SaveAndReimport();

        Debug.Log("[FrostedGlassAutoSetup] Created noise normal map: " + NoisePngPath);
    }

    private static Texture2D CreateNoiseNormalTexture2D(int size, float noiseScale, float strength, int seed)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: true, linear: true);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;

        float ox = (seed * 0.1234f) % 1000f;
        float oy = (seed * 0.9876f) % 1000f;

        float Height(int x, int y)
        {
            float u = (x / (float)size) * noiseScale + ox;
            float v = (y / (float)size) * noiseScale + oy;
            return Mathf.PerlinNoise(u, v);
        }

        var pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            int y1 = (y + 1) % size;
            int y0 = (y - 1 + size) % size;

            for (int x = 0; x < size; x++)
            {
                int x1 = (x + 1) % size;
                int x0 = (x - 1 + size) % size;

                float hL = Height(x0, y);
                float hR = Height(x1, y);
                float hD = Height(x, y0);
                float hU = Height(x, y1);

                // Zentraldifferenzen (central differences)
                float dhdx = (hR - hL);
                float dhdy = (hU - hD);

                Vector3 n = new Vector3(-dhdx * strength, -dhdy * strength, 1f).normalized;

                // Encode [-1..1] -> [0..1]
                pixels[y * size + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(updateMipmaps: true, makeNoLongerReadable: false);
        return tex;
    }

    private static bool SetBoolProperty(object obj, Type t, string name, bool value)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        var p = t.GetProperty(name, flags);
        if (p != null && p.PropertyType == typeof(bool) && p.CanWrite)
        {
            bool cur = (bool)p.GetValue(obj);
            if (cur == value) return false;
            p.SetValue(obj, value);
            return true;
        }

        // Fallback-Felder (URP variiert je nach Version)
        string[] fieldCandidates = name == "supportsCameraOpaqueTexture"
            ? new[] { "m_RequireOpaqueTexture", "m_SupportsCameraOpaqueTexture", "m_RequiresOpaqueTexture" }
            : new[] { "m_" + char.ToUpper(name[0]) + name.Substring(1) };

        foreach (var fieldName in fieldCandidates)
        {
            var f = t.GetField(fieldName, flags);
            if (f == null || f.FieldType != typeof(bool)) continue;

            bool cur = (bool)f.GetValue(obj);
            if (cur == value) return false;
            f.SetValue(obj, value);
            return true;
        }

        Debug.LogWarning($"[FrostedGlassAutoSetup] Could not set bool '{name}' in this URP version.");
        return false;
    }

    private static bool SetEnumProperty(object obj, Type t, string name, string enumValueName)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        // 1) Property mit Setter
        var p = t.GetProperty(name, flags);
        if (p != null && p.PropertyType.IsEnum && p.CanWrite)
        {
            object cur = p.GetValue(obj);
            object target;
            try { target = Enum.Parse(p.PropertyType, enumValueName); }
            catch { return false; }

            if (Equals(cur, target)) return false;
            p.SetValue(obj, target);
            return true;
        }

        // 2) Fallback: privates Feld (URP-Versionen haben oft getter-only Property, aber serialized field)
        string[] fieldCandidates = name == "opaqueDownsampling"
            ? new[] { "m_OpaqueDownsampling", "m_OpaqueDownSampling" }
            : new[] { "m_" + char.ToUpper(name[0]) + name.Substring(1) };

        foreach (var fieldName in fieldCandidates)
        {
            var f = t.GetField(fieldName, flags);
            if (f == null || !f.FieldType.IsEnum) continue;

            object cur = f.GetValue(obj);

            // bevorzugt per Enum-Name, sonst Index-Fallback (0=None, 1=2x, 2=4x ist typisch)
            object target = null;
            try { target = Enum.Parse(f.FieldType, enumValueName); } catch { /* ignore */ }

            if (target == null)
            {
                Array vals = Enum.GetValues(f.FieldType);
                if (vals.Length > 2) target = vals.GetValue(2);
            }

            if (target == null || Equals(cur, target)) return false;

            f.SetValue(obj, target);
            return true;
        }

        // nichts gesetzt, aber auch NICHT crashen
        Debug.LogWarning($"[FrostedGlassAutoSetup] Could not set enum '{name}' in this URP version.");
        return false;
    }

    private static void EnsureShaderExists()
    {
        if (File.Exists(ShaderPath)) return;

        File.WriteAllText(ShaderPath, FrostedShaderSource);
        AssetDatabase.Refresh();
        Debug.Log("[FrostedGlassAutoSetup] Created shader: " + ShaderPath);
    }

    private static void EnsureMaterialExists()
    {
        if (File.Exists(MatPath)) return;

        var shader = Shader.Find("Custom/FrostedGlassURP");
        if (shader == null)
        {
            AssetDatabase.Refresh();
            shader = Shader.Find("Custom/FrostedGlassURP");
        }

        if (shader == null)
        {
            Debug.LogError("[FrostedGlassAutoSetup] Shader not found after creation. Check compile errors.");
            return;
        }

        var mat = new Material(shader);
        mat.SetColor("_Tint", Color.white);
        mat.SetFloat("_Alpha", 0.45f);
        mat.SetFloat("_Distortion", 1.2f);
        mat.SetFloat("_BlurRadius", 1.5f);

        var bump = Resources.Load<Texture2D>(NoiseResName);
        if (bump != null) mat.SetTexture("_BumpMap", bump);

        AssetDatabase.CreateAsset(mat, MatPath);
        AssetDatabase.SaveAssets();
        Debug.Log("[FrostedGlassAutoSetup] Created material: " + MatPath);
    }

    // URP screen-space frosted glass:
    // sampelt _CameraOpaqueTexture (mit 4x Box Downsampling schon weich), plus kleiner 5-tap Blur + Normalmap-Distortion.
    private static readonly string FrostedShaderSource = @"
Shader ""Custom/FrostedGlassURP""
{
    Properties
    {
        _Tint(""Tint"", Color) = (1,1,1,1)
        _Alpha(""Alpha"", Range(0,1)) = 0.4
        _Distortion(""Distortion"", Range(0,2)) = 0.6
        _BlurRadius(""Blur Radius"", Range(0,3)) = 1.0
        _BumpMap(""Normal Map"", 2D) = ""bump"" {}
    }

    SubShader
    {
        Tags { ""RenderType""=""Transparent"" ""Queue""=""Transparent"" ""RenderPipeline""=""UniversalPipeline"" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include ""Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl""

            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);

            TEXTURE2D_X(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);
            float4 _CameraOpaqueTexture_TexelSize;

            float4 _Tint;
            float _Alpha;
            float _Distortion;
            float _BlurRadius;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 screenPos  : TEXCOORD1;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.screenPos = ComputeScreenPos(o.positionCS);
                return o;
            }

            float4 SampleOpaque(float2 suv)
            {
                #if defined(UNITY_SINGLE_PASS_STEREO)
                    suv = UnityStereoTransformScreenSpaceTex(suv);
                #endif
                return SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, suv);
            }

            float4 frag (Varyings i) : SV_Target
            {
                float2 suv = i.screenPos.xy / i.screenPos.w;

                // Distortion via normal map (klein halten!)
                float3 n = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, i.uv));
                float2 distortion = n.xy * (_Distortion * 0.01);

                float2 texel = _CameraOpaqueTexture_TexelSize.xy * _BlurRadius;

                float4 c0 = SampleOpaque(suv + distortion);
                float4 c1 = SampleOpaque(suv + distortion + float2( texel.x, 0));
                float4 c2 = SampleOpaque(suv + distortion + float2(-texel.x, 0));
                float4 c3 = SampleOpaque(suv + distortion + float2(0,  texel.y));
                float4 c4 = SampleOpaque(suv + distortion + float2(0, -texel.y));

                float4 blurred = (c0 + c1 + c2 + c3 + c4) * 0.2;

                float3 rgb = blurred.rgb * _Tint.rgb;
                return float4(rgb, _Alpha);
            }
            ENDHLSL
        }
    }
}
";
}
#endif