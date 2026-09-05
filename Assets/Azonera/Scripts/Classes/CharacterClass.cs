using System.Collections.Generic;
using UnityEngine;
using Azonera.Stats;

namespace Azonera.Classes
{
    /// <summary>Identyfikator profesji. Dodanie kolejnej = jedna pozycja tutaj + nowy asset ClassData.</summary>
    public enum CharacterClass
    {
        Knight,
        Sorcerer,
        Druid
    }

    /// <summary>
    /// Data-driven definicja profesji. Cała mechanika czyta wartości stąd — nic nie jest
    /// hardcodowane w logice. Rozbudowa balansu = edycja assetu, bez ruszania kodu.
    /// </summary>
    [CreateAssetMenu(fileName = "Class_", menuName = "Azonera/Class Data", order = 0)]
    public class ClassData : ScriptableObject
    {
        [Header("Tożsamość")]
        public CharacterClass Class = CharacterClass.Knight;
        public string DisplayName = "Knight";
        [TextArea] public string Description;
        public Color ThemeColor = Color.white;

        [Header("Statystyki bazowe (poziom 1)")]
        public float BaseHealth = 185f;
        public float BaseMana = 50f;
        public float BaseAttack = 12f;
        public float BaseDefense = 8f;
        public float BaseArmor = 0.05f;
        public float BaseMagicPower = 0f;
        public float BaseMagicResist = 0.02f;
        public float BaseMoveSpeed = 4.5f;
        public float BaseAttackSpeed = 1.1f;
        public float BaseCritChance = 0.05f;
        public float BaseCritDamage = 1.5f;
        public float BaseCapacity = 400f;

        [Header("Przyrost na poziom")]
        public float HealthPerLevel = 15f;
        public float ManaPerLevel = 5f;
        public float AttackPerLevel = 1.5f;
        public float DefensePerLevel = 0.8f;
        public float MagicPowerPerLevel = 0f;
        public float CapacityPerLevel = 10f;

        [Header("Ograniczenia ekwipunku")]
        [Tooltip("Typy broni/zbroi dozwolone dla tej profesji. Pusto = brak ograniczeń.")]
        public List<string> AllowedItemTags = new List<string>();

        [Header("Startowe zaklęcia (identyfikatory)")]
        public List<string> StartingSpells = new List<string>();

        /// <summary>Zwraca bazową wartość statystyki dla danego poziomu.</summary>
        public float GetBaseStat(StatType stat, int level)
        {
            int lv = Mathf.Max(1, level) - 1;
            switch (stat)
            {
                case StatType.MaxHealth:   return BaseHealth + HealthPerLevel * lv;
                case StatType.MaxMana:     return BaseMana + ManaPerLevel * lv;
                case StatType.Attack:      return BaseAttack + AttackPerLevel * lv;
                case StatType.Defense:     return BaseDefense + DefensePerLevel * lv;
                case StatType.Armor:       return BaseArmor;
                case StatType.MagicPower:  return BaseMagicPower + MagicPowerPerLevel * lv;
                case StatType.MagicResist: return BaseMagicResist;
                case StatType.MoveSpeed:   return BaseMoveSpeed;
                case StatType.AttackSpeed: return BaseAttackSpeed;
                case StatType.CritChance:  return BaseCritChance;
                case StatType.CritDamage:  return BaseCritDamage;
                case StatType.Capacity:    return BaseCapacity + CapacityPerLevel * lv;
                default: return 0f;
            }
        }
    }
}
