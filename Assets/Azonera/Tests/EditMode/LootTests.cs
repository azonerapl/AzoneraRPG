using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Azonera.Items;
using Azonera.Loot;

namespace Azonera.Tests.EditMode
{
    /// <summary>Testy tabel lootu: gwarantowany drop, granice liczby, złoto, pusta tabela.</summary>
    public class LootTests
    {
        private readonly List<Object> _spawned = new List<Object>();

        [SetUp]
        public void Seed() => Random.InitState(12345);

        [TearDown]
        public void Cleanup()
        {
            foreach (var o in _spawned) if (o != null) Object.DestroyImmediate(o);
            _spawned.Clear();
        }

        private ItemData MakeItem()
        {
            var it = ScriptableObject.CreateInstance<ItemData>();
            _spawned.Add(it);
            return it;
        }

        [Test]
        public void GuaranteedEntry_AlwaysRolls_WithinCountBounds()
        {
            var item = MakeItem();
            var table = ScriptableObject.CreateInstance<LootTable>();
            _spawned.Add(table);
            table.Entries = new List<LootEntry>
            {
                new LootEntry { Item = item, Chance = 1f, MinCount = 1, MaxCount = 3 }
            };

            for (int i = 0; i < 50; i++)
            {
                var rolls = table.RollLoot();
                Assert.AreEqual(1, rolls.Count);
                Assert.AreSame(item, rolls[0].Item);
                Assert.GreaterOrEqual(rolls[0].Count, 1);
                Assert.LessOrEqual(rolls[0].Count, 3);
            }
        }

        [Test]
        public void Gold_IsWithinConfiguredRange()
        {
            var table = ScriptableObject.CreateInstance<LootTable>();
            _spawned.Add(table);
            table.MinGold = 5; table.MaxGold = 20;

            for (int i = 0; i < 50; i++)
            {
                int gold = table.RollGold();
                Assert.GreaterOrEqual(gold, 5);
                Assert.LessOrEqual(gold, 20);
            }
        }

        [Test]
        public void EmptyTable_ReturnsNothing()
        {
            var table = ScriptableObject.CreateInstance<LootTable>();
            _spawned.Add(table);
            table.Entries = new List<LootEntry>();

            Assert.AreEqual(0, table.RollLoot().Count);
            Assert.AreEqual(0, table.RollGold());
        }
    }
}
