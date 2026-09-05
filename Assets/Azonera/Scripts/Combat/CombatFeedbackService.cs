using UnityEngine;
using Azonera.Stats;
using Azonera.UI;
using Azonera.VFX;

namespace Azonera.Combat
{
    /// <summary>
    /// Spina warstwę DANYCH walki z warstwą PREZENTACJI: nasłuchuje globalnych eventów
    /// <see cref="CharacterStats"/> i zamienia je na liczby obrażeń, iskry i rozbłyski.
    ///
    /// Sam się bootstrapuje po wczytaniu sceny — działa w każdej scenie bez konfiguracji
    /// i obejmuje także encje tworzone w runtime (spawny potworów, przywołania).
    /// Gameplay nie wie o istnieniu tej klasy — zależność idzie tylko w jedną stronę.
    /// </summary>
    public class CombatFeedbackService : MonoBehaviour
    {
        // ---- Paleta typów obrażeń (art direction — spójna z Art Bible) ----
        public static readonly Color ColorPhysical = new Color(1f, 0.94f, 0.85f);
        public static readonly Color ColorMagic = new Color(0.55f, 0.72f, 1f);
        public static readonly Color ColorTrue = new Color(1f, 0.45f, 0.95f);
        public static readonly Color ColorCritical = new Color(1f, 0.72f, 0.2f);
        public static readonly Color ColorHeal = new Color(0.45f, 1f, 0.5f);
        public static readonly Color ColorPlayerHurt = new Color(1f, 0.35f, 0.32f);
        public static readonly Color ColorLevelUp = new Color(1f, 0.86f, 0.35f);

        private static CombatFeedbackService _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null) return;
            var go = new GameObject("CombatFeedbackService");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<CombatFeedbackService>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
        }

        private void OnEnable()
        {
            CharacterStats.OnAnyDamaged += HandleDamaged;
            CharacterStats.OnAnyHealed += HandleHealed;
            CharacterStats.OnAnyDied += HandleDied;
            CharacterStats.OnAnyLevelUp += HandleLevelUp;
        }

        private void OnDisable()
        {
            CharacterStats.OnAnyDamaged -= HandleDamaged;
            CharacterStats.OnAnyHealed -= HandleHealed;
            CharacterStats.OnAnyDied -= HandleDied;
            CharacterStats.OnAnyLevelUp -= HandleLevelUp;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        // ---------------------------------------------------------------- handlery

        private void HandleDamaged(CharacterStats victim, DamageInfo info)
        {
            if (victim == null) return;
            Vector3 anchor = AnchorOf(victim);

            bool victimIsPlayer = victim.CompareTag("Player");
            Color color = victimIsPlayer ? ColorPlayerHurt
                        : info.IsCritical ? ColorCritical
                        : ColorOf(info.Type);

            string label = Mathf.RoundToInt(info.Amount).ToString();
            if (info.IsCritical) label += "!";

            CombatTextLayer.Instance.Spawn(
                anchor, label, color,
                fontSize: info.IsCritical ? 34 : 26,
                lifetime: info.IsCritical ? 1.35f : 1.05f,
                rise: info.IsCritical ? 2.1f : 1.6f,
                punch: info.IsCritical ? 0.65f : 0.3f);

            Vector3 hitPoint = info.HitPoint != default ? info.HitPoint : anchor;
            CombatVfxService.Instance.PlayHit(hitPoint, ColorOf(info.Type), info.IsCritical);
        }

        private void HandleHealed(CharacterStats target, float amount)
        {
            if (target == null || amount < 1f) return;
            Vector3 anchor = AnchorOf(target);
            CombatTextLayer.Instance.Spawn(anchor, "+" + Mathf.RoundToInt(amount), ColorHeal,
                fontSize: 24, lifetime: 1.1f, rise: 1.5f, punch: 0.2f);
            CombatVfxService.Instance.PlayHeal(target.transform.position);
        }

        private void HandleDied(CharacterStats victim)
        {
            if (victim == null) return;
            CombatVfxService.Instance.PlayDeath(victim.transform.position, new Color(0.75f, 0.15f, 0.15f));
        }

        private void HandleLevelUp(CharacterStats who, int level)
        {
            if (who == null) return;
            CombatTextLayer.Instance.Spawn(AnchorOf(who) + Vector3.up * 0.4f,
                $"POZIOM {level}!", ColorLevelUp,
                fontSize: 38, lifetime: 2.0f, rise: 1.2f, punch: 0.8f);
            CombatVfxService.Instance.PlayLevelUp(who.transform.position);
        }

        // ---------------------------------------------------------------- pomocnicze

        /// <summary>Punkt nad głową postaci, z którego startuje liczba.</summary>
        private static Vector3 AnchorOf(CharacterStats stats)
        {
            var col = stats.GetComponent<Collider>();
            float top = col != null ? col.bounds.max.y : stats.transform.position.y + 2f;
            var p = stats.transform.position;
            return new Vector3(p.x, top + 0.25f, p.z);
        }

        public static Color ColorOf(DamageType type)
        {
            switch (type)
            {
                case DamageType.Magic: return ColorMagic;
                case DamageType.True: return ColorTrue;
                default: return ColorPhysical;
            }
        }
    }
}
