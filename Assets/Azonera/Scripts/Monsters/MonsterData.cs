using UnityEngine;
using Azonera.Loot;

namespace Azonera.Monsters
{
    /// <summary>
    /// Data-driven definicja potwora. Nowy potwór = nowy asset, bez pisania kodu.
    /// Model i animacje podpinane później bez zmiany logiki AI.
    /// </summary>
    [CreateAssetMenu(fileName = "Monster_", menuName = "Azonera/Monster Data", order = 30)]
    public class MonsterData : ScriptableObject
    {
        [Header("Tożsamość")]
        public string DisplayName = "Marsh Snake";
        public int Level = 1;

        [Header("Statystyki bojowe")]
        public float MaxHealth = 40f;
        public float Damage = 8f;
        public float Defense = 2f;
        [Range(0f, 1f)] public float Armor = 0f;
        public float AttackSpeed = 0.8f;
        public float AttackRange = 2.0f;

        [Header("Zachowanie")]
        public float MoveSpeed = 2.6f;
        public float AggroRange = 8f;
        public float DeAggroRange = 14f;
        [Tooltip("Promień losowego patrolu wokół punktu spawnu.")]
        public float RoamRadius = 5f;

        [Header("Nagrody")]
        public long ExperienceReward = 20;
        public LootTable LootTable;

        [Header("Wygląd (opcjonalny placeholder)")]
        public GameObject ModelPrefab;
        public Color PlaceholderColor = new Color(0.4f, 0.5f, 0.3f);
        public float PlaceholderHeight = 1.4f;
    }
}
