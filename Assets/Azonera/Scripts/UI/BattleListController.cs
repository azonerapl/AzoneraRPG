using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Azonera.Combat;
using Azonera.Stats;

namespace Azonera.UI
{
    /// <summary>
    /// Działająca Battle List: pokazuje wrogów w pobliżu (nazwa, poziom, pasek HP),
    /// podświetla aktualny cel i pozwala go zmienić kliknięciem — jak w klasycznym MMORPG.
    ///
    /// Wiersze są tworzone raz i recyklingowane (bez Instantiate/Destroy co odświeżenie).
    /// Panel-kontener pochodzi z generatora HUD; kontroler sam się do niego podpina.
    /// </summary>
    public class BattleListController : MonoBehaviour
    {
        private const int MaxRows = 7;
        private const float RowHeight = 24f;

        [SerializeField] private string _panelName = "Battle List";

        private RectTransform _panel;
        private Text _emptyLabel;
        private TargetSystem _targeting;
        private Font _font;

        private readonly List<Row> _rows = new List<Row>(MaxRows);

        private class Row
        {
            public RectTransform Root;
            public Image Background;
            public Image HpFill;
            public Text Label;
            public CharacterStats Bound;
        }

        private void Start()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            if (!ResolvePanel()) { enabled = false; return; }
            BindPlayer();
            BuildRows();
            Refresh();
        }

        private bool ResolvePanel()
        {
            foreach (var rt in GetComponentsInChildren<RectTransform>(true))
            {
                if (rt.name != _panelName) continue;
                _panel = rt;
                break;
            }
            if (_panel == null) return false;

            foreach (var t in _panel.GetComponentsInChildren<Text>(true))
            {
                if (t.name == "BLEmpty") { _emptyLabel = t; break; }
            }
            return true;
        }

        private void BindPlayer()
        {
            var player = GameObject.FindWithTag("Player");
            if (player == null) return;
            _targeting = player.GetComponent<TargetSystem>();
            if (_targeting == null) _targeting = player.AddComponent<TargetSystem>();

            _targeting.OnNearbyRefreshed += Refresh;
            _targeting.OnTargetChanged += HandleTargetChanged;
        }

        private void HandleTargetChanged(CharacterStats _) => Refresh();

        private void OnDestroy()
        {
            if (_targeting == null) return;
            _targeting.OnNearbyRefreshed -= Refresh;
            _targeting.OnTargetChanged -= HandleTargetChanged;
        }

        // ---------------------------------------------------------------- budowa wierszy

        private void BuildRows()
        {
            float y = -28f;
            for (int i = 0; i < MaxRows; i++)
            {
                var row = BuildRow(i, y);
                _rows.Add(row);
                y -= RowHeight + 2f;
            }
        }

        private Row BuildRow(int index, float y)
        {
            var rootGo = new GameObject($"BLRow_{index}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            rootGo.transform.SetParent(_panel, false);
            var rect = rootGo.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(6f, y);
            rect.sizeDelta = new Vector2(_panel.sizeDelta.x - 12f, RowHeight);

            var bg = rootGo.GetComponent<Image>();
            bg.color = new Color(0.12f, 0.11f, 0.10f, 0.85f);

            // Pasek HP wypełniający tło wiersza — czytelny stan wroga bez dodatkowych ikon.
            var fillGo = new GameObject("HpFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillGo.transform.SetParent(rootGo.transform, false);
            var fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = new Vector2(1f, 1f);
            fillRect.offsetMax = new Vector2(-1f, -1f);
            var fill = fillGo.GetComponent<Image>();
            fill.color = new Color(0.55f, 0.13f, 0.13f, 0.75f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.raycastTarget = false;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelGo.transform.SetParent(rootGo.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(8f, 0f);
            labelRect.offsetMax = new Vector2(-8f, 0f);
            var label = labelGo.GetComponent<Text>();
            label.font = _font;
            label.fontSize = 12;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = new Color(0.92f, 0.9f, 0.86f);
            label.raycastTarget = false;

            var row = new Row { Root = rect, Background = bg, HpFill = fill, Label = label };

            // Klik w wiersz = zaznaczenie tego wroga.
            var trigger = rootGo.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener(_ =>
            {
                if (row.Bound != null && !row.Bound.IsDead) _targeting?.SetTarget(row.Bound);
            });
            trigger.triggers.Add(entry);

            rootGo.SetActive(false);
            return row;
        }

        // ---------------------------------------------------------------- odświeżanie

        private void Refresh()
        {
            if (_targeting == null || _panel == null) return;

            var nearby = _targeting.Nearby;
            int shown = Mathf.Min(nearby.Count, MaxRows);

            for (int i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                if (i >= shown)
                {
                    row.Bound = null;
                    if (row.Root.gameObject.activeSelf) row.Root.gameObject.SetActive(false);
                    continue;
                }

                var stats = nearby[i];
                row.Bound = stats;
                if (!row.Root.gameObject.activeSelf) row.Root.gameObject.SetActive(true);

                string name = ResolveName(stats);
                row.Label.text = $"{name}  <lvl {stats.Level}>";
                row.HpFill.fillAmount = stats.MaxHealth > 0f ? stats.CurrentHealth / stats.MaxHealth : 0f;

                bool isTarget = _targeting.Target == stats;
                row.Background.color = isTarget
                    ? new Color(0.35f, 0.28f, 0.10f, 0.95f)   // złota poświata = aktualny cel
                    : new Color(0.12f, 0.11f, 0.10f, 0.85f);
                row.Label.color = isTarget
                    ? new Color(1f, 0.86f, 0.5f)
                    : new Color(0.92f, 0.9f, 0.86f);
            }

            if (_emptyLabel != null) _emptyLabel.enabled = shown == 0;
        }

        private void Update()
        {
            // Paski HP muszą nadążać za walką — odświeżenie listy co 0.25 s to za rzadko.
            for (int i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                if (row.Bound == null || !row.Root.gameObject.activeSelf) continue;
                row.HpFill.fillAmount = row.Bound.MaxHealth > 0f
                    ? row.Bound.CurrentHealth / row.Bound.MaxHealth
                    : 0f;
            }
        }

        private static string ResolveName(CharacterStats stats)
        {
            var ai = stats.GetComponent<Azonera.Monsters.MonsterAI>();
            if (ai != null && ai.Data != null && !string.IsNullOrEmpty(ai.Data.DisplayName))
                return ai.Data.DisplayName;
            return stats.gameObject.name.Replace("Monster_", "");
        }
    }
}
