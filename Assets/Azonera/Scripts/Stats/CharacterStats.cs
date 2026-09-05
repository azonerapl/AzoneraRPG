using System;
using System.Collections.Generic;
using UnityEngine;
using Azonera.Combat;
using Azonera.Classes;

namespace Azonera.Stats
{
    /// <summary>
    /// Centralny komponent statystyk — wspólny dla gracza i potworów.
    /// Trzyma bazę (z profesji lub surową), modyfikatory z ekwipunku/buffów oraz
    /// bieżące HP/Manę. Jedyne miejsce, które liczy obrażenia i level-up.
    /// Zaprojektowany pod przyszłą serwerową autorytatywność (czyste dane + eventy).
    /// </summary>
    public class CharacterStats : MonoBehaviour, IDamageable
    {
        [Header("Poziom / Doświadczenie")]
        [SerializeField] private int _level = 1;
        [SerializeField] private long _experience = 0;

        [Header("Profesja (opcjonalnie — dla gracza)")]
        [SerializeField] private ClassData _classData;

        // Baza policzona dla aktualnego poziomu (StatType -> wartość)
        private readonly Dictionary<StatType, float> _base = new Dictionary<StatType, float>();
        // Modyfikatory z ekwipunku / buffów
        private readonly List<StatModifier> _modifiers = new List<StatModifier>();

        private float _currentHealth;
        private float _currentMana;
        private bool _initialized;

        // ---- Eventy (UI, VFX, AI, przyszła sieć) ----
        public event Action<float, float> OnHealthChanged;   // (current, max)
        public event Action<float, float> OnManaChanged;     // (current, max)
        public event Action<int> OnLevelUp;                  // (newLevel)
        public event Action<long, long> OnExperienceChanged; // (current, requiredForNext)
        public event Action OnStatsChanged;
        public event Action<CharacterStats> OnDied;
        public event Action<DamageInfo> OnDamaged;

        // ---- Właściwości ----
        public int Level => _level;
        public long Experience => _experience;
        public ClassData Class => _classData;
        public float CurrentHealth => _currentHealth;
        public float CurrentMana => _currentMana;
        public float MaxHealth => GetStat(StatType.MaxHealth);
        public float MaxMana => GetStat(StatType.MaxMana);
        public bool IsDead { get; private set; }
        public Transform Transform => transform;

        private void Awake()
        {
            if (!_initialized) EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_classData != null) BuildBaseFromClass();
            _initialized = true;
            _currentHealth = MaxHealth;
            _currentMana = MaxMana;
        }

        /// <summary>Inicjalizuje statystyki z profesji (gracz).</summary>
        public void InitializeFromClass(ClassData data, int level)
        {
            _classData = data;
            _level = Mathf.Max(1, level);
            BuildBaseFromClass();
            _initialized = true;
            _currentHealth = MaxHealth;
            _currentMana = MaxMana;
            IsDead = false;
            RaiseAll();
        }

        /// <summary>Inicjalizuje statystyki wprost (potwory / NPC).</summary>
        public void InitializeRaw(int level, float maxHp, float attack, float defense,
                                  float armor = 0f, float magicResist = 0f,
                                  float moveSpeed = 3.5f, float attackSpeed = 1f,
                                  float critChance = 0.03f, float critDamage = 1.5f)
        {
            _level = Mathf.Max(1, level);
            _base.Clear();
            _base[StatType.MaxHealth] = maxHp;
            _base[StatType.MaxMana] = 0f;
            _base[StatType.Attack] = attack;
            _base[StatType.Defense] = defense;
            _base[StatType.Armor] = armor;
            _base[StatType.MagicPower] = 0f;
            _base[StatType.MagicResist] = magicResist;
            _base[StatType.MoveSpeed] = moveSpeed;
            _base[StatType.AttackSpeed] = attackSpeed;
            _base[StatType.CritChance] = critChance;
            _base[StatType.CritDamage] = critDamage;
            _base[StatType.Capacity] = 0f;
            _initialized = true;
            _currentHealth = MaxHealth;
            _currentMana = MaxMana;
            IsDead = false;
            RaiseAll();
        }

        private void BuildBaseFromClass()
        {
            _base.Clear();
            if (_classData == null) return;
            foreach (StatType stat in Enum.GetValues(typeof(StatType)))
                _base[stat] = _classData.GetBaseStat(stat, _level);
        }

        // ---------- Odczyt statystyk ----------

        /// <summary>Zwraca wartość statystyki po nałożeniu modyfikatorów.</summary>
        public float GetStat(StatType stat)
        {
            if (!_initialized) EnsureInitialized();
            _base.TryGetValue(stat, out float baseVal);
            float flat = 0f, percent = 0f;
            for (int i = 0; i < _modifiers.Count; i++)
            {
                if (_modifiers[i].Stat != stat) continue;
                if (_modifiers[i].Mode == ModifierMode.Flat) flat += _modifiers[i].Value;
                else percent += _modifiers[i].Value;
            }
            return (baseVal + flat) * (1f + percent);
        }

        // ---------- Modyfikatory (ekwipunek / buffy) ----------

        public void AddModifier(StatModifier mod)
        {
            _modifiers.Add(mod);
            RecalculateStats();
        }

        public void RemoveAllFromSource(IEnumerable<StatModifier> mods)
        {
            foreach (var m in mods) _modifiers.Remove(m);
            RecalculateStats();
        }

        public void ClearModifiers()
        {
            _modifiers.Clear();
            RecalculateStats();
        }

        /// <summary>Przelicza pochodne po zmianie modyfikatorów; przycina bieżące HP/Manę do nowych maks.</summary>
        public void RecalculateStats()
        {
            _currentHealth = Mathf.Min(_currentHealth, MaxHealth);
            _currentMana = Mathf.Min(_currentMana, MaxMana);
            RaiseAll();
        }

        // ---------- Walka ----------

        public void ApplyDamage(DamageInfo info)
        {
            if (IsDead) return;

            float dmg = info.Amount;
            switch (info.Type)
            {
                case DamageType.Physical:
                    dmg = Mathf.Max(1f, dmg - GetStat(StatType.Defense));
                    dmg *= (1f - Mathf.Clamp01(GetStat(StatType.Armor)));
                    break;
                case DamageType.Magic:
                    dmg *= (1f - Mathf.Clamp01(GetStat(StatType.MagicResist)));
                    break;
                case DamageType.True:
                    break;
            }

            dmg = Mathf.Max(1f, Mathf.Round(dmg));
            _currentHealth -= dmg;
            OnDamaged?.Invoke(new DamageInfo(dmg, info.Type, info.Source, info.IsCritical, info.HitPoint));
            OnHealthChanged?.Invoke(_currentHealth, MaxHealth);

            if (_currentHealth <= 0f)
            {
                _currentHealth = 0f;
                Die(info.Source);
            }
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            _currentHealth = Mathf.Min(MaxHealth, _currentHealth + amount);
            OnHealthChanged?.Invoke(_currentHealth, MaxHealth);
        }

        public bool TrySpendMana(float amount)
        {
            if (_currentMana < amount) return false;
            _currentMana -= amount;
            OnManaChanged?.Invoke(_currentMana, MaxMana);
            return true;
        }

        public void RestoreMana(float amount)
        {
            if (amount <= 0f) return;
            _currentMana = Mathf.Min(MaxMana, _currentMana + amount);
            OnManaChanged?.Invoke(_currentMana, MaxMana);
        }

        private void Die(GameObject killer)
        {
            if (IsDead) return;
            IsDead = true;
            OnDied?.Invoke(this);
        }

        public void ReviveFull()
        {
            IsDead = false;
            _currentHealth = MaxHealth;
            _currentMana = MaxMana;
            RaiseAll();
        }

        // ---------- Doświadczenie / Poziom ----------

        /// <summary>Krzywa doświadczenia — łagodnie rosnące wymagania.</summary>
        public static long RequiredXpForLevel(int level)
        {
            // klasyczny, przewidywalny wzrost; łatwy do przebalansowania
            return (long)(50f * level * level + 50f * level);
        }

        public long ExperienceForNextLevel => RequiredXpForLevel(_level);

        public void AddExperience(long amount)
        {
            if (amount <= 0 || IsDead) return;
            _experience += amount;
            while (_experience >= RequiredXpForLevel(_level))
            {
                _experience -= RequiredXpForLevel(_level);
                LevelUp();
            }
            OnExperienceChanged?.Invoke(_experience, ExperienceForNextLevel);
        }

        private void LevelUp()
        {
            _level++;
            if (_classData != null) BuildBaseFromClass();
            _currentHealth = MaxHealth; // pełne odnowienie przy awansie
            _currentMana = MaxMana;
            OnLevelUp?.Invoke(_level);
            RaiseAll();
        }

        /// <summary>Ustawia poziom bezpośrednio (debug / wczytanie zapisu).</summary>
        public void SetLevel(int level)
        {
            _level = Mathf.Max(1, level);
            if (_classData != null) BuildBaseFromClass();
            _currentHealth = Mathf.Min(_currentHealth, MaxHealth);
            _currentMana = Mathf.Min(_currentMana, MaxMana);
            RaiseAll();
        }

        // Do wczytywania zapisu
        public void LoadState(int level, long experience, float currentHealth, float currentMana)
        {
            _level = Mathf.Max(1, level);
            _experience = experience;
            if (_classData != null) BuildBaseFromClass();
            IsDead = false;
            _currentHealth = currentHealth > 0 ? Mathf.Min(currentHealth, MaxHealth) : MaxHealth;
            _currentMana = Mathf.Min(currentMana, MaxMana);
            RaiseAll();
        }

        private void RaiseAll()
        {
            OnStatsChanged?.Invoke();
            OnHealthChanged?.Invoke(_currentHealth, MaxHealth);
            OnManaChanged?.Invoke(_currentMana, MaxMana);
            OnExperienceChanged?.Invoke(_experience, ExperienceForNextLevel);
        }
    }
}
