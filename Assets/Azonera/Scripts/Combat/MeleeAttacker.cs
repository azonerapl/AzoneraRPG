using UnityEngine;
using Azonera.Stats;

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

        private CharacterStats _stats;
        private float _cooldownTimer;

        public float AttackRange => _attackRange;
        public bool IsReady => _cooldownTimer <= 0f;

        public System.Action<IDamageable, DamageInfo> OnHit;

        private void Awake()
        {
            _stats = GetComponent<CharacterStats>();
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

            float baseAtk = _stats.GetStat(StatType.Attack) * _damageMultiplier;
            if (_damageType == DamageType.Magic)
                baseAtk += _stats.GetStat(StatType.MagicPower);

            bool crit = Random.value < _stats.GetStat(StatType.CritChance);
            float dmg = baseAtk * (crit ? _stats.GetStat(StatType.CritDamage) : 1f);
            // niewielka wariancja, żeby liczby nie były sztywne
            dmg *= Random.Range(0.9f, 1.1f);

            var info = new DamageInfo(dmg, _damageType, gameObject, crit, target.Transform.position);
            target.ApplyDamage(info);
            OnHit?.Invoke(target, info);

            float atkSpeed = Mathf.Max(0.1f, _stats.GetStat(StatType.AttackSpeed));
            _cooldownTimer = 1f / atkSpeed;
            return true;
        }
    }
}
