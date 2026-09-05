using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Azonera.Combat;
using Azonera.Monsters;
using Azonera.Stats;

namespace Azonera.UI
{
    /// <summary>
    /// Nameplate'y nad postaciami: imię, poziom i pasek HP — element wprost z referencji
    /// wizualnej Azonery. Rysowane w screen-space (stała czytelność przy każdym zoomie),
    /// kotwiczone do punktu nad głową encji.
    ///
    /// Jedna warstwa obsługuje wszystkie encje; wpisy są recyklingowane, a nameplate'y poza
    /// ekranem/zasięgiem są wyłączane, nie niszczone. Aktualny cel dostaje złote wyróżnienie.
    /// </summary>
    public class NameplateLayer : MonoBehaviour
    {
        private const float MaxVisibleDistance = 26f;
        private const float RescanInterval = 0.5f;

        private static NameplateLayer _instance;

        private Canvas _canvas;
        private Transform _entriesParent;
        private UnityEngine.Camera _cam;
        private Font _font;
        private TargetSystem _targeting;
        private float _nextScan;

        private readonly Dictionary<CharacterStats, Entry> _entries = new Dictionary<CharacterStats, Entry>();
        private readonly List<CharacterStats> _stale = new List<CharacterStats>(8);

        private class Entry
        {
            public RectTransform Root;
            public Text Label;
            public Image HpBg;
            public Image HpFill;
            public Collider Collider;
        }

        public static NameplateLayer Instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = FindAnyObjectByType<NameplateLayer>();
                if (_instance == null)
                    _instance = new GameObject("NameplateLayer").AddComponent<NameplateLayer>();
                return _instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            // Warstwa ma istnieć w każdej scenie z potworami, bez ręcznej konfiguracji.
            if (FindAnyObjectByType<MonsterAI>() == null) return;
            _ = Instance;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;

            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 400; // pod liczbami obrażeń (500), nad HUD-em

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;

            var parentGo = new GameObject("Entries");
            parentGo.transform.SetParent(transform, false);
            _entriesParent = parentGo.transform;

            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private void Start()
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null) _targeting = player.GetComponent<TargetSystem>();
        }

        private UnityEngine.Camera Cam
        {
            get { if (_cam == null) _cam = UnityEngine.Camera.main; return _cam; }
        }

        private void LateUpdate()
        {
            if (Cam == null) return;

            if (Time.time >= _nextScan)
            {
                _nextScan = Time.time + RescanInterval;
                Rescan();
            }

            Vector3 viewer = Cam.transform.position;
            var target = _targeting != null ? _targeting.Target : null;

            foreach (var kvp in _entries)
            {
                var stats = kvp.Key;
                var entry = kvp.Value;
                if (stats == null) { _stale.Add(kvp.Key); continue; }

                if (stats.IsDead)
                {
                    SetVisible(entry, false);
                    continue;
                }

                Vector3 anchor = AnchorOf(stats, entry);
                float dist = Vector3.Distance(viewer, anchor);
                Vector3 screen = Cam.WorldToScreenPoint(anchor);

                bool visible = screen.z > 0f && dist <= MaxVisibleDistance;
                SetVisible(entry, visible);
                if (!visible) continue;

                entry.Root.position = screen;
                entry.HpFill.fillAmount = stats.MaxHealth > 0f ? stats.CurrentHealth / stats.MaxHealth : 0f;

                bool isTarget = target == stats;
                entry.Label.color = isTarget ? new Color(1f, 0.85f, 0.45f) : new Color(0.9f, 0.88f, 0.84f);
                entry.HpBg.color = isTarget
                    ? new Color(0.4f, 0.32f, 0.08f, 0.95f)
                    : new Color(0.05f, 0.05f, 0.06f, 0.85f);
            }

            if (_stale.Count > 0) PurgeStale();
        }

        private void PurgeStale()
        {
            for (int i = 0; i < _stale.Count; i++)
            {
                if (_entries.TryGetValue(_stale[i], out var entry) && entry.Root != null)
                    Destroy(entry.Root.gameObject);
                _entries.Remove(_stale[i]);
            }
            _stale.Clear();
        }

        private static void SetVisible(Entry entry, bool visible)
        {
            if (entry.Root == null) return;
            if (entry.Root.gameObject.activeSelf != visible) entry.Root.gameObject.SetActive(visible);
        }

        private void Rescan()
        {
            var monsters = FindObjectsByType<MonsterAI>(FindObjectsInactive.Exclude);
            for (int i = 0; i < monsters.Length; i++)
            {
                var stats = monsters[i].GetComponent<CharacterStats>();
                if (stats == null || _entries.ContainsKey(stats)) continue;
                _entries[stats] = BuildEntry(ResolveName(monsters[i], stats), stats);
            }

            foreach (var kvp in _entries)
                if (kvp.Key == null) _stale.Add(kvp.Key);
        }

        private Entry BuildEntry(string name, CharacterStats stats)
        {
            var rootGo = new GameObject($"Nameplate_{name}", typeof(RectTransform));
            rootGo.transform.SetParent(_entriesParent, false);
            var rect = rootGo.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(150f, 30f);

            var labelGo = new GameObject("Name", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelGo.transform.SetParent(rootGo.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, 9f);
            labelRect.sizeDelta = new Vector2(160f, 16f);
            var label = labelGo.GetComponent<Text>();
            label.font = _font;
            label.fontSize = 13;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.text = $"{name}  <{stats.Level}>";
            var outline = labelGo.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(1.1f, -1.1f);

            var hpBgGo = new GameObject("HpBg", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            hpBgGo.transform.SetParent(rootGo.transform, false);
            var hpBgRect = hpBgGo.GetComponent<RectTransform>();
            hpBgRect.anchorMin = hpBgRect.anchorMax = new Vector2(0.5f, 0f);
            hpBgRect.pivot = new Vector2(0.5f, 0f);
            hpBgRect.anchoredPosition = new Vector2(0f, 0f);
            hpBgRect.sizeDelta = new Vector2(76f, 7f);
            var hpBg = hpBgGo.GetComponent<Image>();
            hpBg.color = new Color(0.05f, 0.05f, 0.06f, 0.85f);
            hpBg.raycastTarget = false;

            var hpFillGo = new GameObject("HpFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            hpFillGo.transform.SetParent(hpBgGo.transform, false);
            var hpFillRect = hpFillGo.GetComponent<RectTransform>();
            hpFillRect.anchorMin = new Vector2(0f, 0f);
            hpFillRect.anchorMax = new Vector2(1f, 1f);
            hpFillRect.offsetMin = new Vector2(1f, 1f);
            hpFillRect.offsetMax = new Vector2(-1f, -1f);
            var hpFill = hpFillGo.GetComponent<Image>();
            hpFill.color = new Color(0.68f, 0.16f, 0.16f);
            hpFill.type = Image.Type.Filled;
            hpFill.fillMethod = Image.FillMethod.Horizontal;
            hpFill.raycastTarget = false;

            return new Entry
            {
                Root = rect,
                Label = label,
                HpBg = hpBg,
                HpFill = hpFill,
                Collider = stats.GetComponent<Collider>()
            };
        }

        private static Vector3 AnchorOf(CharacterStats stats, Entry entry)
        {
            float top = entry.Collider != null
                ? entry.Collider.bounds.max.y
                : stats.transform.position.y + 2f;
            var p = stats.transform.position;
            return new Vector3(p.x, top + 0.35f, p.z);
        }

        private static string ResolveName(MonsterAI ai, CharacterStats stats)
        {
            if (ai != null && ai.Data != null && !string.IsNullOrEmpty(ai.Data.DisplayName))
                return ai.Data.DisplayName;
            return stats.gameObject.name.Replace("Monster_", "");
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
