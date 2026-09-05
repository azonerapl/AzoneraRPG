using UnityEngine;
using UnityEngine.UI;

namespace Azonera.UI
{
    /// <summary>
    /// Pojedyncza liczba obrażeń/leczenia unosząca się nad postacią.
    /// Kotwiczona w świecie, ale rysowana w screen-space — dzięki temu pozostaje
    /// czytelna niezależnie od zoomu kamery (standard prezentacji MMORPG).
    /// Sterowana przez <see cref="CombatTextLayer"/>; instancje pochodzą z puli.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class FloatingCombatText : MonoBehaviour
    {
        private Text _text;
        private RectTransform _rect;
        private UnityEngine.Camera _cam;

        private Vector3 _worldOrigin;
        private Vector3 _drift;          // dryf w przestrzeni świata (rozrzut liczb)
        private float _lifetime;
        private float _elapsed;
        private float _rise;
        private float _baseFontSize;
        private float _punch;            // krótkie „uderzenie" skali na starcie (kryty)
        private Color _color;

        public bool IsPlaying { get; private set; }

        private void Awake()
        {
            _text = GetComponent<Text>();
            _rect = GetComponent<RectTransform>();
        }

        /// <summary>Uruchamia animację liczby nad punktem świata.</summary>
        public void Play(UnityEngine.Camera cam, Vector3 worldPos, string value, Color color,
                         int fontSize, float lifetime, float rise, float punch)
        {
            _cam = cam;
            _worldOrigin = worldPos;
            _color = color;
            _lifetime = Mathf.Max(0.1f, lifetime);
            _rise = rise;
            _punch = punch;
            _elapsed = 0f;
            _baseFontSize = fontSize;

            // Losowy dryf w bok, żeby seria trafień nie nakładała się w jedną kolumnę.
            _drift = new Vector3(Random.Range(-0.55f, 0.55f), 0f, Random.Range(-0.35f, 0.35f));

            _text.text = value;
            _text.fontSize = fontSize;
            _text.color = color;
            IsPlaying = true;
            UpdateVisual(0f);
        }

        private void LateUpdate()
        {
            if (!IsPlaying) return;
            _elapsed += Time.deltaTime;
            float t = _elapsed / _lifetime;
            if (t >= 1f) { IsPlaying = false; return; }
            UpdateVisual(t);
        }

        private void UpdateVisual(float t)
        {
            if (_cam == null) { IsPlaying = false; return; }

            // Ruch w świecie: unoszenie + delikatny łuk na boki.
            Vector3 world = _worldOrigin
                          + Vector3.up * (_rise * EaseOutCubic(t))
                          + _drift * EaseOutCubic(t);

            Vector3 screen = _cam.WorldToScreenPoint(world);
            if (screen.z < 0f) { _rect.anchoredPosition = new Vector2(-9999f, -9999f); return; }
            _rect.position = screen;

            // Skala: krótki „punch" na starcie, potem powrót do 1.
            float scale = 1f + _punch * Mathf.Clamp01(1f - t * 6f);
            _text.fontSize = Mathf.RoundToInt(_baseFontSize * scale);

            // Zanikanie dopiero w drugiej połowie życia — liczba zdąży być przeczytana.
            float alpha = t < 0.55f ? 1f : Mathf.InverseLerp(1f, 0.55f, t);
            var c = _color; c.a = alpha;
            _text.color = c;
        }

        private static float EaseOutCubic(float t)
        {
            float u = 1f - Mathf.Clamp01(t);
            return 1f - u * u * u;
        }
    }
}
