using UnityEngine;
using UnityEngine.UI;
using Azonera.Core;

namespace Azonera.UI
{
    /// <summary>
    /// Warstwa liczb bojowych: własne, dedykowane Canvas (nad HUD-em, bez raycastów)
    /// + pula instancji <see cref="FloatingCombatText"/>.
    /// Tworzy się sama przy pierwszym użyciu — nie wymaga konfiguracji w scenie.
    /// </summary>
    public class CombatTextLayer : MonoBehaviour
    {
        private const int PoolPrewarm = 12;
        private const int PoolMaxIdle = 48;

        private static CombatTextLayer _instance;

        private Canvas _canvas;
        private Transform _poolParent;
        private ObjectPool<FloatingCombatText> _pool;
        private UnityEngine.Camera _cam;
        private Font _font;

        // Aktywne liczby — sprzątane, gdy skończą animację.
        private readonly System.Collections.Generic.List<FloatingCombatText> _active =
            new System.Collections.Generic.List<FloatingCombatText>(32);

        public static CombatTextLayer Instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = FindAnyObjectByType<CombatTextLayer>();
                if (_instance == null)
                {
                    var go = new GameObject("CombatTextLayer");
                    _instance = go.AddComponent<CombatTextLayer>();
                }
                return _instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _instance = null;

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            BuildCanvas();
        }

        private void BuildCanvas()
        {
            _canvas = GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 500; // nad HUD-em, pod oknami modalnymi

            var scaler = gameObject.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;

            // Liczby nie mogą przechwytywać kliknięć — brak GraphicRaycaster jest tu celowy.

            var poolGo = new GameObject("Pool");
            poolGo.transform.SetParent(transform, false);
            _poolParent = poolGo.transform;

            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            _pool = new ObjectPool<FloatingCombatText>(CreateText, _poolParent, PoolPrewarm, PoolMaxIdle);
        }

        private FloatingCombatText CreateText()
        {
            var go = new GameObject("CombatText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(_poolParent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(220f, 40f);

            var text = go.GetComponent<Text>();
            text.font = _font;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            // Cień + obrys: liczby muszą być czytelne na jasnym i ciemnym tle.
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.7f);
            outline.effectDistance = new Vector2(1.1f, -1.1f);

            return go.GetComponent<FloatingCombatText>() ?? go.AddComponent<FloatingCombatText>();
        }

        private UnityEngine.Camera Cam
        {
            get
            {
                if (_cam == null) _cam = UnityEngine.Camera.main;
                return _cam;
            }
        }

        /// <summary>Wyświetla liczbę/etykietę nad punktem świata.</summary>
        public void Spawn(Vector3 worldPos, string value, Color color,
                          int fontSize = 26, float lifetime = 1.1f, float rise = 1.6f, float punch = 0.35f)
        {
            if (Cam == null) return;
            var item = _pool.Get();
            if (item == null) return;
            item.transform.SetParent(transform, false);
            item.Play(Cam, worldPos, value, color, fontSize, lifetime, rise, punch);
            _active.Add(item);
        }

        private void LateUpdate()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var item = _active[i];
                if (item == null) { _active.RemoveAt(i); continue; }
                if (item.IsPlaying) continue;
                _active.RemoveAt(i);
                _pool.Release(item);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
