using UnityEngine;

// Spawns a burst of subtle floating particles around the camera for magical transition effects
public static class TransitionParticleEffect
{
    public static void Spawn(Color color, float duration = 4f, float radius = 1.5f, int particleCount = 80)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        GameObject particleObj = new GameObject("TransitionParticles");
        particleObj.transform.position = cam.transform.position;

        ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // === Main Module ===
        var main = ps.main;
        main.duration = duration;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(duration * 0.5f, duration);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.03f, 0.12f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.006f, 0.025f);
        main.startColor = new Color(color.r, color.g, color.b, 0.9f);
        main.maxParticles = particleCount;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.015f;
        main.stopAction = ParticleSystemStopAction.Destroy;

        // === Emission ===
        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, particleCount)
        });

        // === Shape ===
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = radius;

        // === Color over lifetime ===
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(color, 0f),
                new GradientColorKey(color, 0.5f),
                new GradientColorKey(color, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.85f, 0.12f),
                new GradientAlphaKey(0.6f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        // === Size over lifetime ===
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.5f),
            new Keyframe(0.15f, 1f),
            new Keyframe(1f, 0.2f)
        ));

        // === Noise ===
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.08f;
        noise.frequency = 0.4f;
        noise.scrollSpeed = 0.2f;
        noise.damping = true;

        // === Renderer ===
        var renderer = particleObj.GetComponent<ParticleSystemRenderer>();
        Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (particleShader == null)
            particleShader = Shader.Find("Particles/Standard Unlit");

        Material particleMat = new Material(particleShader);
        particleMat.SetColor("_BaseColor", Color.white);
        particleMat.SetFloat("_Surface", 1f);
        particleMat.renderQueue = 3000;
        particleMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        renderer.material = particleMat;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        ps.Play();
    }
}
