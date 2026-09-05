using UnityEngine;
using Azonera.Core;

namespace Azonera.VFX
{
    /// <summary>
    /// Serwis efektów walki: rozbłyski trafień, wybuchy śmierci, iskry leczenia, aura awansu.
    /// Efekty są budowane proceduralnie (ParticleSystem konfigurowany z kodu) i pulowane —
    /// nie wymagają assetów, a docelowe prefaby VFX można podpiąć bez zmiany wywołań.
    ///
    /// UWAGA (Art Bible): to warstwa TYMCZASOWA/PLACEHOLDER klasy „czytelny feedback".
    /// Docelowo każdy efekt dostaje autorski prefab z teksturami i shaderem Azonery —
    /// API (PlayHit/PlayDeath/PlayHeal/PlayLevelUp) pozostaje bez zmian.
    /// </summary>
    public class CombatVfxService : MonoBehaviour
    {
        private static CombatVfxService _instance;

        private ObjectPool<ParticleSystem> _burstPool;
        private Transform _poolParent;
        private Material _particleMaterial;

        private readonly System.Collections.Generic.List<TrackedBurst> _active =
            new System.Collections.Generic.List<TrackedBurst>(24);

        private struct TrackedBurst
        {
            public ParticleSystem System;
            public float ReleaseAt;
        }

        public static CombatVfxService Instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = FindAnyObjectByType<CombatVfxService>();
                if (_instance == null)
                    _instance = new GameObject("CombatVfxService").AddComponent<CombatVfxService>();
                return _instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _instance = null;

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;

            var poolGo = new GameObject("Pool");
            poolGo.transform.SetParent(transform, false);
            _poolParent = poolGo.transform;

            _particleMaterial = BuildParticleMaterial();
            _burstPool = new ObjectPool<ParticleSystem>(CreateBurst, _poolParent, 6, 32);
        }

        private static Material BuildParticleMaterial()
        {
            // URP-owy shader cząsteczek; fallback na Sprites/Default (Built-in / brak URP).
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader) { name = "M_AzoneraParticle_PLACEHOLDER" };
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);   // Transparent
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 1f);       // Additive
            mat.renderQueue = 3000;
            return mat;
        }

        private ParticleSystem CreateBurst()
        {
            var go = new GameObject("Burst_PLACEHOLDER");
            go.transform.SetParent(_poolParent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.35f;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = _particleMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            return ps;
        }

        private void Emit(Vector3 position, Color color, int count, float speed,
                          float size, float lifetime, float radius, float gravity)
        {
            var ps = _burstPool.Get();
            if (ps == null) return;
            ps.transform.SetParent(transform, false);
            ps.transform.position = position;

            var main = ps.main;
            main.startColor = color;
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.55f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.7f, lifetime);
            main.gravityModifier = gravity;

            var shape = ps.shape;
            shape.radius = radius;

            ps.Clear(true);
            ps.Emit(count);
            ps.Play(true);

            _active.Add(new TrackedBurst { System = ps, ReleaseAt = Time.time + lifetime + 0.35f });
        }

        private void Update()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (Time.time < _active[i].ReleaseAt) continue;
                var ps = _active[i].System;
                _active.RemoveAt(i);
                if (ps == null) continue;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                _burstPool.Release(ps);
            }
        }

        // ---------------------------------------------------------------- API

        /// <summary>Rozbłysk trafienia. Kolor niesie informację o typie obrażeń.</summary>
        public void PlayHit(Vector3 position, Color color, bool critical)
        {
            Emit(position, color,
                 count: critical ? 26 : 12,
                 speed: critical ? 5.5f : 3.2f,
                 size: critical ? 0.26f : 0.17f,
                 lifetime: 0.45f, radius: 0.18f, gravity: 0.5f);
        }

        /// <summary>Wybuch śmierci — mocniejszy, ciemniejszy, z opadaniem.</summary>
        public void PlayDeath(Vector3 position, Color color)
        {
            Emit(position + Vector3.up * 0.6f, color,
                 count: 40, speed: 4.5f, size: 0.3f, lifetime: 0.9f, radius: 0.45f, gravity: 0.8f);
        }

        /// <summary>Iskry leczenia — unoszą się w górę (ujemna grawitacja).</summary>
        public void PlayHeal(Vector3 position)
        {
            Emit(position + Vector3.up * 0.3f, new Color(0.4f, 1f, 0.55f),
                 count: 18, speed: 1.8f, size: 0.2f, lifetime: 0.85f, radius: 0.4f, gravity: -0.35f);
        }

        /// <summary>Złota aura awansu poziomu.</summary>
        public void PlayLevelUp(Vector3 position)
        {
            Emit(position, new Color(1f, 0.85f, 0.35f),
                 count: 60, speed: 3.2f, size: 0.28f, lifetime: 1.3f, radius: 0.7f, gravity: -0.25f);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
