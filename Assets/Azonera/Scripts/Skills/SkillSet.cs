using System;
using System.Collections.Generic;
using UnityEngine;

namespace Azonera.Skills
{
    /// <summary>
    /// Zestaw umiejętności postaci z prawdziwym treningiem i awansem (styl klasycznego MMORPG).
    /// Próg awansu rośnie wykładniczo: required(level) = constant * factor^(level - baseLevel),
    /// a mnożnik trudności zależy od profesji (ustawiany przez VocationDifficulty). Event-driven.
    /// Data-driven i rozszerzalny; combat/magia dodają „tries", HUD słucha eventów.
    /// </summary>
    public class SkillSet : MonoBehaviour
    {
        [Serializable]
        public struct StartLevel
        {
            public SkillType Type;
            public int Level;
        }

        [Header("Poziomy startowe (puste = domyślne 10, Magic 0)")]
        [SerializeField] private List<StartLevel> _startLevels = new List<StartLevel>();

        [Header("Trudność treningu (mnożnik stałej progu; <1 = szybciej)")]
        [Tooltip("Globalny mnożnik trudności dla tej postaci/profesji.")]
        [SerializeField] private float _difficulty = 1f;

        private const float Factor = 1.1f;   // wzrost progu na poziom
        private const int BaseLevel = 10;    // poziom odniesienia formuły

        private readonly Dictionary<SkillType, Skill> _skills = new Dictionary<SkillType, Skill>();
        private bool _initialized;

        /// <summary>(typ, nowy poziom) — po każdym awansie umiejętności.</summary>
        public event Action<SkillType, int> OnSkillAdvanced;
        /// <summary>(typ) — po każdej zmianie punktów/poziomu (do odświeżenia HUD).</summary>
        public event Action<SkillType> OnSkillChanged;

        private void Awake() => EnsureInit();

        private void EnsureInit()
        {
            if (_initialized) return;
            foreach (SkillType t in Enum.GetValues(typeof(SkillType)))
                _skills[t] = new Skill(t, DefaultLevel(t));
            foreach (var s in _startLevels)
                if (_skills.TryGetValue(s.Type, out var sk)) sk.Level = Mathf.Max(0, s.Level);
            _initialized = true;
        }

        private static int DefaultLevel(SkillType t) => t == SkillType.Magic ? 0 : 10;

        /// <summary>Ustawia globalną trudność (np. z profesji). Wywołuj przed użyciem.</summary>
        public void SetDifficulty(float difficulty) => _difficulty = Mathf.Max(0.05f, difficulty);

        public int GetLevel(SkillType type)
        {
            EnsureInit();
            return _skills[type].Level;
        }

        public float GetPoints(SkillType type)
        {
            EnsureInit();
            return _skills[type].Points;
        }

        /// <summary>Liczba tries potrzebna, by z <paramref name="level"/> awansować.</summary>
        public float RequiredTries(SkillType type, int level)
        {
            float constant = BaseConstant(type) * _difficulty;
            return constant * Mathf.Pow(Factor, level - BaseLevel);
        }

        public float ProgressNormalized(SkillType type)
        {
            EnsureInit();
            var s = _skills[type];
            float req = RequiredTries(type, s.Level);
            return req > 0f ? Mathf.Clamp01(s.Points / req) : 0f;
        }

        /// <summary>Dodaje „próby" treningu; obsługuje wielokrotny awans. Zwraca liczbę awansów.</summary>
        public int AddTries(SkillType type, float tries)
        {
            if (tries <= 0f) return 0;
            EnsureInit();
            var s = _skills[type];
            s.Points += tries;
            int advances = 0;
            float req = RequiredTries(type, s.Level);
            while (s.Points >= req)
            {
                s.Points -= req;
                s.Level++;
                advances++;
                OnSkillAdvanced?.Invoke(type, s.Level);
                req = RequiredTries(type, s.Level);
            }
            OnSkillChanged?.Invoke(type);
            return advances;
        }

        public void SetLevel(SkillType type, int level)
        {
            EnsureInit();
            _skills[type].Level = Mathf.Max(0, level);
            _skills[type].Points = 0f;
            OnSkillChanged?.Invoke(type);
        }

        /// <summary>Bazowa stała progu — im wyższa, tym wolniejszy trening danej umiejętności.</summary>
        private static float BaseConstant(SkillType type)
        {
            switch (type)
            {
                case SkillType.Shielding: return 100f;
                case SkillType.Magic:     return 1600f; // trenowana maną (duże wartości)
                case SkillType.Distance:  return 30f;
                case SkillType.Fishing:   return 20f;
                default:                  return 50f;   // bronie do walki wręcz
            }
        }
    }
}
