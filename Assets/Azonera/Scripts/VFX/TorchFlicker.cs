using UnityEngine;

namespace Azonera.VFX
{
    /// <summary>Lekkie migotanie światła pochodni — buduje atmosferę dark fantasy.</summary>
    public class TorchFlicker : MonoBehaviour
    {
        [SerializeField] private Light _light;
        [SerializeField] private float _baseIntensity = 3.2f;
        [SerializeField] private float _amplitude = 0.5f;
        [SerializeField] private float _speed = 7f;

        private float _seed;

        private void Awake()
        {
            if (_light == null) _light = GetComponent<Light>();
            _seed = Random.value * 100f;
        }

        private void Update()
        {
            if (_light == null) return;
            float n = Mathf.PerlinNoise(_seed, Time.time * _speed);
            _light.intensity = _baseIntensity + (n - 0.5f) * 2f * _amplitude;
        }

        public void Configure(Light l, float baseIntensity)
        {
            _light = l;
            _baseIntensity = baseIntensity;
        }
    }
}
