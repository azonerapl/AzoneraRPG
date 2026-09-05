using System.Collections.Generic;
using UnityEngine;
using Azonera.Items;

namespace Azonera.Loot
{
    [System.Serializable]
    public struct LootEntry
    {
        public ItemData Item;
        [Range(0f, 1f)] public float Chance; // szansa 0..1
        public int MinCount;
        public int MaxCount;
    }

    /// <summary>Tabela lootu przypisywana potworom przez MonsterData.</summary>
    [CreateAssetMenu(fileName = "Loot_", menuName = "Azonera/Loot Table", order = 20)]
    public class LootTable : ScriptableObject
    {
        public List<LootEntry> Entries = new List<LootEntry>();

        [Header("Waluta")]
        public int MinGold = 0;
        public int MaxGold = 0;

        public struct Roll
        {
            public ItemData Item;
            public int Count;
        }

        /// <summary>Losuje wynik z tabeli. Każdy wpis rzucany niezależnie.</summary>
        public List<Roll> RollLoot()
        {
            var result = new List<Roll>();
            foreach (var e in Entries)
            {
                if (e.Item == null) continue;
                if (Random.value <= e.Chance)
                {
                    int count = Random.Range(Mathf.Max(1, e.MinCount), Mathf.Max(1, e.MaxCount) + 1);
                    result.Add(new Roll { Item = e.Item, Count = count });
                }
            }
            return result;
        }

        public int RollGold()
        {
            if (MaxGold <= 0) return 0;
            return Random.Range(MinGold, MaxGold + 1);
        }
    }
}
