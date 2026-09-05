using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Azonera.Stats;
using Azonera.Classes;
using Azonera.Combat;

namespace Azonera.Tests.EditMode
{
    /// <summary>Testy rdzenia statystyk: baza z profesji, obrażenia/mitigacja, śmierć, EXP/level, mana.</summary>
    public class StatsTests
    {
        private readonly List<Object> _spawned = new List<Object>();

        private CharacterStats NewStats()
        {
            var go = new GameObject("TestStats");
            _spawned.Add(go);
            return go.AddComponent<CharacterStats>();
        }

        [TearDown]
        public void Cleanup()
        {
            foreach (var o in _spawned) if (o != null) Object.DestroyImmediate(o);
            _spawned.Clear();
        }

        [Test]
        public void InitializeFromClass_SetsMaxHealth_AndFullCurrent()
        {
            var cd = ScriptableObject.CreateInstance<ClassData>();
            cd.BaseHealth = 200; cd.HealthPerLevel = 15; cd.BaseMana = 40;
            var s = NewStats();
            s.InitializeFromClass(cd, 1);

            Assert.AreEqual(200f, s.MaxHealth, 0.001f);
            Assert.AreEqual(s.MaxHealth, s.CurrentHealth, 0.001f);
            Object.DestroyImmediate(cd);
        }

        [Test]
        public void LevelUp_IncreasesMaxHealth_ByGrowth()
        {
            var cd = ScriptableObject.CreateInstance<ClassData>();
            cd.BaseHealth = 200; cd.HealthPerLevel = 15;
            var s = NewStats();
            s.InitializeFromClass(cd, 1);

            int levelBefore = s.Level;
            s.AddExperience(CharacterStats.RequiredXpForLevel(1)); // dokładnie na 1 poziom

            Assert.AreEqual(levelBefore + 1, s.Level);
            Assert.AreEqual(215f, s.MaxHealth, 0.001f);
            Object.DestroyImmediate(cd);
        }

        [Test]
        public void ExperienceCurve_IsIncreasing()
        {
            Assert.Less(CharacterStats.RequiredXpForLevel(1), CharacterStats.RequiredXpForLevel(2));
            Assert.Less(CharacterStats.RequiredXpForLevel(5), CharacterStats.RequiredXpForLevel(10));
        }

        [Test]
        public void PhysicalDamage_AppliesDefenseThenArmor()
        {
            var s = NewStats();
            s.InitializeRaw(level: 1, maxHp: 100f, attack: 5f, defense: 10f, armor: 0.5f);
            // 50 - 10 (defense) = 40; * (1 - 0.5 armor) = 20
            s.ApplyDamage(new DamageInfo(50f, DamageType.Physical, null));
            Assert.AreEqual(80f, s.CurrentHealth, 0.001f);
        }

        [Test]
        public void TrueDamage_IgnoresMitigation()
        {
            var s = NewStats();
            s.InitializeRaw(1, 100f, 5f, 999f, 0.9f);
            s.ApplyDamage(new DamageInfo(30f, DamageType.True, null));
            Assert.AreEqual(70f, s.CurrentHealth, 0.001f);
        }

        [Test]
        public void LethalDamage_TriggersDeath_AndEvent()
        {
            var s = NewStats();
            s.InitializeRaw(1, 40f, 5f, 0f);
            bool died = false;
            s.OnDied += _ => died = true;

            s.ApplyDamage(new DamageInfo(1000f, DamageType.True, null));

            Assert.IsTrue(s.IsDead);
            Assert.IsTrue(died);
            Assert.AreEqual(0f, s.CurrentHealth, 0.001f);
        }

        [Test]
        public void Heal_ClampsToMax()
        {
            var s = NewStats();
            s.InitializeRaw(1, 100f, 5f, 0f);
            s.ApplyDamage(new DamageInfo(30f, DamageType.True, null)); // 70
            s.Heal(1000f);
            Assert.AreEqual(100f, s.CurrentHealth, 0.001f);
        }

        [Test]
        public void Mana_SpendAndRestore()
        {
            var cd = ScriptableObject.CreateInstance<ClassData>();
            cd.BaseHealth = 100; cd.BaseMana = 50;
            var s = NewStats();
            s.InitializeFromClass(cd, 1);

            Assert.IsTrue(s.TrySpendMana(30f));
            Assert.AreEqual(20f, s.CurrentMana, 0.001f);
            Assert.IsFalse(s.TrySpendMana(999f)); // za mało
            s.RestoreMana(1000f);
            Assert.AreEqual(s.MaxMana, s.CurrentMana, 0.001f);
            Object.DestroyImmediate(cd);
        }
    }
}
