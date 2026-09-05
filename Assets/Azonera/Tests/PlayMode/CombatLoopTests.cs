using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Azonera.Stats;
using Azonera.Combat;
using Azonera.Monsters;
using Azonera.Items;
using Azonera.Loot;
using InvComp = Azonera.Inventory.Inventory;

namespace Azonera.Tests.PlayMode
{
    /// <summary>
    /// Integracyjny test pętli single-player: gracz atakuje potwora → potwór ginie →
    /// gracz dostaje EXP → loot spawnuje się → gracz podnosi loot do plecaka.
    /// </summary>
    public class CombatLoopTests
    {
        private readonly List<Object> _spawned = new List<Object>();

        [TearDown]
        public void Cleanup()
        {
            foreach (var o in _spawned) if (o != null) Object.Destroy(o);
            _spawned.Clear();
        }

        private GameObject NewPlayer()
        {
            var go = new GameObject("Player");
            go.tag = "Player";
            _spawned.Add(go);
            var stats = go.AddComponent<CharacterStats>();
            go.AddComponent<InvComp>();
            var atk = go.AddComponent<MeleeAttacker>();
            // szybki, mocny gracz — deterministyczne, szybkie zabicie
            stats.InitializeRaw(level: 1, maxHp: 500f, attack: 25f, defense: 2f,
                armor: 0f, magicResist: 0f, moveSpeed: 4.5f, attackSpeed: 3f, critChance: 0f);
            return go;
        }

        private GameObject NewMonster(Vector3 pos, out MonsterData data, ItemData lootItem)
        {
            data = ScriptableObject.CreateInstance<MonsterData>();
            data.DisplayName = "TestDummy";
            data.Level = 1; data.MaxHealth = 20f; data.Damage = 3f; data.Defense = 0f;
            data.MoveSpeed = 0f; data.AttackSpeed = 1f; data.AttackRange = 2f;
            data.AggroRange = 20f; data.DeAggroRange = 30f; data.RoamRadius = 0f;
            data.ExperienceReward = 25; // < 100 (próg lvl 1) → brak level-upa, EXP == 25
            var loot = ScriptableObject.CreateInstance<LootTable>();
            loot.Entries = new List<LootEntry>
            {
                new LootEntry { Item = lootItem, Chance = 1f, MinCount = 1, MaxCount = 1 }
            };
            data.LootTable = loot;
            _spawned.Add(data); _spawned.Add(loot);

            var go = new GameObject("Monster");
            _spawned.Add(go);
            go.transform.position = pos;
            go.AddComponent<CharacterStats>();
            go.AddComponent<MeleeAttacker>();
            var ai = go.AddComponent<MonsterAI>();
            ai.Initialize(data);
            return go;
        }

        [UnityTest]
        public IEnumerator FullLoop_Kill_Exp_Loot_Pickup()
        {
            var lootItem = ScriptableObject.CreateInstance<ItemData>();
            lootItem.ItemId = "test_drop"; lootItem.Stackable = true; lootItem.MaxStack = 10; lootItem.Weight = 1f;
            _spawned.Add(lootItem);

            var player = NewPlayer();
            player.transform.position = Vector3.zero;
            var playerStats = player.GetComponent<CharacterStats>();
            var playerAtk = player.GetComponent<MeleeAttacker>();
            var playerInv = player.GetComponent<InvComp>();

            var monster = NewMonster(new Vector3(0, 0, 1.5f), out _, lootItem);
            var monsterStats = monster.GetComponent<CharacterStats>();

            yield return null; // pozwól Awake/Start wystartować

            Assert.AreEqual(0, playerStats.Experience, "Gracz zaczyna z 0 EXP.");

            // --- Faza 1: atakuj aż potwór zginie (timeout ~8s) ---
            float t = 0f;
            while (!monsterStats.IsDead && t < 8f)
            {
                playerAtk.TryAttack(monsterStats);
                t += Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(monsterStats.IsDead, "Potwór powinien zginąć w wyznaczonym czasie.");
            Assert.AreEqual(25, playerStats.Experience, "Gracz powinien otrzymać EXP za zabicie.");

            // --- Faza 2: loot i podniesienie ---
            // przenieś gracza na miejsce zwłok, by wejść w zasięg auto-pickup
            player.transform.position = new Vector3(0, 0, 1.5f);

            float t2 = 0f;
            while (playerInv.CountOf(lootItem) == 0 && t2 < 4f)
            {
                t2 += Time.deltaTime;
                yield return null;
            }

            Assert.Greater(playerInv.CountOf(lootItem), 0, "Loot powinien trafić do plecaka gracza.");
        }
    }
}
