using UnityEngine;
using Azonera.Stats;
using Azonera.Skills;

namespace Azonera.Combat
{
    /// <summary>
    /// Modularny komponent ataku wręcz. Wspólny dla gracza i potworów.
    /// Liczy obrażenia na podstawie <see cref="CharacterStats"/> atakującego,
    /// obsługuje zasięg, cooldown (z AttackSpeed) i trafienia krytyczne.
    /// Nie zawiera logiki ruchu ani sterowania — czysta odpowiedzialność walki.
    /// </summary>
    [RequireComponent(typeof(CharacterStats))]
    public class MeleeAttacker : MonoBehaviour
    {
        [Header("Parametry ataku")]
        [SerializeField] private float _attackRange = 2.2f;
        [SerializeField] private DamageType _damageType = DamageType.Physical;
        [Tooltip("Mnożnik obrażeń względem statystyki Attack.")]
        [SerializeField] private float _damageMultiplier = 1f;
        [Tooltip("Umiejętność broni trenowana i skalująca obrażenia (dla posiadaczy SkillSet).")]
        [SerializeField] private SkillType _weaponSkill = SkillType.Sword;

        private CharacterStats _stats;
        private SkillSet _skills;   // opcjonalny — gracz ma, potwory zwykle nie
        private float _cooldownTimer;

        public float AttackRange => _attackRange;
        public bool IsReady => _cooldownTimer <= 0f;

        public System.Action<IDamageable, DamageInfo> OnHit;

        private void Awake()
        {
            _stats = GetComponent<CharacterStats>();
        }

        // Leniwe pobranie SkillSet — działa niezależnie od kolejności Awake i gdy dodany w runtime.
        private SkillSet Skills => _skills != null ? _skills : (_skills = GetComponent<SkillSet>());

        /// <summary>Mnożnik obrażeń od poziomu umiejętności broni (10 = neutralny).</summary>
        private float WeaponSkillFactor()
        {
            var sk = Skills;
            if (sk == null) return 1f;
            int lvl = sk.GetLevel(_weaponSkill);
            return Mathf.Max(0.2f, 1f + 0.03f * (lvl - 10));
        }

        private void Update()
        {
            if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;
        }

        public bool InRange(Transform target)
        {
            if (target == null) return false;
            return Vector3.Distance(transform.position, target.position) <= _attackRange;
        }

        /// <summary>Próba wykonania ataku na cel. Zwraca true, jeśli trafienie zaszło.</summary>
        public bool TryAttack(IDamageable target)
        {
            if (target == null || target.IsDead || !IsReady) return false;
            if (!InRange(target.Transform)) return false;

            float baseAtk = _stats.GetStat(StatType.Attack) * _damageMultiplier * WeaponSkillFactor();
            if (_damageType == DamageType.Magic)
                baseAtk += _stats.GetStat(StatType.MagicPower);

            bool crit = Random.value < _stats.GetStat(StatType.CritChance);
            float dmg = baseAtk * (crit ? _stats.GetStat(StatType.CritDamage) : 1f);
            // niewielka wariancja, żeby liczby nie były sztywne
            dmg *= Random.Range(0.9f, 1.1f);

            var info = new DamageInfo(dmg, _damageType, gameObject, crit, target.Transform.position);
            target.ApplyDamage(info);
            OnHit?.Invoke(target, info);

            // Trening umiejętności: atakujący trenuje broń, broniący — Shielding.
            _skills?.AddTries(_weaponSkill, 1f);
            var targetSkills = target.Transform != null ? target.Transform.GetComponent<SkillSet>() : null;
            targetSkills?.AddTries(SkillType.Shielding, 1f);

            float atkSpeed = Mathf.Max(0.1f, _stats.GetStat(StatType.AttackSpeed));
            _cooldownTimer = 1f / atkSpeed;
            return true;
        }
    }
}
