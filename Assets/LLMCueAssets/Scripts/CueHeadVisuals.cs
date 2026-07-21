using System;
using UnityEngine;

public enum CueVisualState { Idle, Available, Listening, Thinking, Speaking }

/// <summary>
/// Drives the 3D living-blob head: per-state wobble/glow/satellite motion on the
/// CueOrb v2 shader (fixed purple gradient + traveling white rim), plus the
/// orbiting satellites kept from the orb design. Exposes the smoothed TTS audio
/// level so CueFaceRig can drive the talking mouth from the real voice.
/// </summary>
public class CueHeadVisuals : MonoBehaviour
{
    [SerializeField] private Renderer orbRenderer;
    [SerializeField] private AudioSource voiceSource;
    [SerializeField] private int orbiterCount = 8;
    [SerializeField] private float transitionSpeed = 5f;

    /// <summary>0..1 smoothed RMS of the currently playing voice audio.</summary>
    public float AudioLevel { get; private set; }

    [Serializable]
    private struct StateLook
    {
        public float glow;
        public float noiseAmp;
        public float noiseSpeed;
        public float rimGlow;
        public float orbiterSpeed;   // deg/s
        public float orbiterRadius;  // relative to head radius
        public float orbiterScale;   // 0 = hidden
    }

    private static readonly StateLook Idle = new()
    { glow = 0.85f, noiseAmp = 0.012f, noiseSpeed = 1.4f, rimGlow = 0.8f, orbiterSpeed = 8f, orbiterRadius = 1.35f, orbiterScale = 0f };

    private static readonly StateLook Available = new()
    { glow = 1.05f, noiseAmp = 0.018f, noiseSpeed = 2.1f, rimGlow = 1.1f, orbiterSpeed = 25f, orbiterRadius = 1.5f, orbiterScale = 0.5f };

    private static readonly StateLook Listening = new()
    { glow = 1.25f, noiseAmp = 0.028f, noiseSpeed = 3.2f, rimGlow = 1.5f, orbiterSpeed = 60f, orbiterRadius = 1.3f, orbiterScale = 0.8f };

    private static readonly StateLook Thinking = new()
    { glow = 1.1f, noiseAmp = 0.04f, noiseSpeed = 2.6f, rimGlow = 1.3f, orbiterSpeed = 220f, orbiterRadius = 1.15f, orbiterScale = 1f };

    private static readonly StateLook Speaking = new()
    { glow = 1.15f, noiseAmp = 0.035f, noiseSpeed = 3.3f, rimGlow = 1.3f, orbiterSpeed = 142f, orbiterRadius = 1.32f, orbiterScale = 1.1f };

    private static readonly int GlowId = Shader.PropertyToID("_Glow");
    private static readonly int NoiseAmpId = Shader.PropertyToID("_NoiseAmp");
    private static readonly int NoiseSpeedId = Shader.PropertyToID("_NoiseSpeed");
    private static readonly int RimGlowId = Shader.PropertyToID("_RimGlow");

    private StateLook _current = Idle;
    private StateLook _target = Idle;
    private CueVisualState _state = CueVisualState.Idle;
    private Vector3 _baseScale;
    private Material _mat;
    private Transform[] _orbiters;
    private float[] _orbiterPhase;
    private Vector3[] _orbiterAxis;
    private float _orbitAngle;
    private readonly float[] _samples = new float[256];

    private void Awake()
    {
        if (!orbRenderer) orbRenderer = GetComponent<Renderer>();
        _baseScale = transform.localScale;
        _mat = orbRenderer.material;
        BuildOrbiters();
        Apply(_current, 0f);
    }

    public void SetState(CueVisualState state)
    {
        _state = state;
        _target = state switch
        {
            CueVisualState.Available => Available,
            CueVisualState.Listening => Listening,
            CueVisualState.Thinking => Thinking,
            CueVisualState.Speaking => Speaking,
            _ => Idle
        };
    }

    private void Update()
    {
        var targetLevel = 0f;
        if (_state == CueVisualState.Speaking && voiceSource && voiceSource.isPlaying)
        {
            voiceSource.GetOutputData(_samples, 0);
            var sum = 0f;
            foreach (var s in _samples) sum += s * s;
            targetLevel = Mathf.Clamp01(Mathf.Sqrt(sum / _samples.Length) * 4f);
        }

        AudioLevel = Mathf.Lerp(AudioLevel, targetLevel, Time.deltaTime * 14f);

        var k = Time.deltaTime * transitionSpeed;
        _current.glow = Mathf.Lerp(_current.glow, _target.glow, k);
        _current.noiseAmp = Mathf.Lerp(_current.noiseAmp, _target.noiseAmp, k);
        _current.noiseSpeed = Mathf.Lerp(_current.noiseSpeed, _target.noiseSpeed, k);
        _current.rimGlow = Mathf.Lerp(_current.rimGlow, _target.rimGlow, k);
        _current.orbiterSpeed = Mathf.Lerp(_current.orbiterSpeed, _target.orbiterSpeed, k);
        _current.orbiterRadius = Mathf.Lerp(_current.orbiterRadius, _target.orbiterRadius, k);
        _current.orbiterScale = Mathf.Lerp(_current.orbiterScale, _target.orbiterScale, k);

        Apply(_current, AudioLevel);
        UpdateOrbiters();

        // breathe (design token 1.07), faster while listening
        var breatheFreq = _state == CueVisualState.Listening ? 4.8f : 2.2f;
        transform.localScale = _baseScale * (1f + 0.035f * (1f + Mathf.Sin(Time.time * breatheFreq)));
    }

    private void Apply(StateLook look, float audio)
    {
        _mat.SetFloat(GlowId, look.glow + audio * 0.5f);
        _mat.SetFloat(NoiseAmpId, look.noiseAmp + audio * 0.03f);
        _mat.SetFloat(NoiseSpeedId, look.noiseSpeed);
        _mat.SetFloat(RimGlowId, look.rimGlow + audio * 0.8f);
    }

    private void BuildOrbiters()
    {
        _orbiters = new Transform[orbiterCount];
        _orbiterPhase = new float[orbiterCount];
        _orbiterAxis = new Vector3[orbiterCount];

        var orbiterMat = new Material(_mat) { name = "CueOrbiterMat" };

        for (var i = 0; i < orbiterCount; i++)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = $"CueOrbiter{i}";
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            go.GetComponent<Renderer>().material = orbiterMat;
            _orbiters[i] = go.transform;
            _orbiterPhase[i] = i * 360f / orbiterCount;
            _orbiterAxis[i] = Quaternion.Euler(
                UnityEngine.Random.Range(-25f, 25f), 0f, UnityEngine.Random.Range(-25f, 25f)) * Vector3.up;
        }
    }

    private void UpdateOrbiters()
    {
        _orbitAngle += _current.orbiterSpeed * Time.deltaTime;
        for (var i = 0; i < _orbiters.Length; i++)
        {
            var angle = _orbitAngle + _orbiterPhase[i];
            var basePos = Quaternion.AngleAxis(angle, _orbiterAxis[i]) * Vector3.forward;
            _orbiters[i].localPosition = basePos * (0.5f * _current.orbiterRadius);
            _orbiters[i].localScale = Vector3.one * (0.055f * _current.orbiterScale * (1f + AudioLevel * 0.6f));
        }
    }
}
