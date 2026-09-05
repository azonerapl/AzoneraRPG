using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Azonera.Stats;
using Azonera.Items;
using Azonera.Equipment;

namespace Azonera.Tests.EditMode
{
    /// <summary>Testy ekwipunku: modyfikatory statów po założeniu/zdjęciu, wymóg poziomu.</summary>
    public class EquipmentTests
    {
        private readonly List<Object> _spawned = new List<Object>();

        private (CharacterStats, EquipmentController) NewActor()
        {
            var go = new GameObject("TestActor");
            _spawned.Add(go);
            var stats = go.AddComponent<CharacterStats>();
            var eq = go.AddComponent<EquipmentController>();
            stats.InitializeRaw(level: 1, maxHp: 100f, attack: 10f, defense: 5f);
            return (stats, eq);
        }

        private ItemData MakeWeapon(float attackBonus, int reqLevel = 0)
        {
            var it = ScriptableObject.CreateInstance<ItemData>();
            it.Type = ItemType.Weapon;
            it.Slot = EquipmentSlotType.RightHand;
            it.RequiredLevel = reqLevel;
            it.Modifiers = new List<StatModifier> { new StatModifier(StatType.Attack, attackBonus, ModifierMode.Flat) };
            _spawned.Add(it);
            return it;
        }

        [TearDown]
        public void Cleanup()
        {
            foreach (var o in _spawned) if (o != null) Object.DestroyImmediate(o);
            _spawned.Clear();
        }

        [Test]
        public void Equip_AppliesModifier_Unequip_Reverts()
        {
            var (stats, eq) = NewActor();
            float baseAtk = stats.GetStat(StatType.Attack);

            var sword = MakeWeapon(7f);
            eq.Equip(sword);
            Assert.AreEqual(baseAtk + 7f, stats.GetStat(StatType.Attack), 0.001f);

            eq.Unequip(EquipmentSlotType.RightHand);
            Assert.AreEqual(baseAtk, stats.GetStat(StatType.Attack), 0.001f);
        }

        [Test]
        public void Equip_ReplacingSameSlot_ReturnsPreviousItem()
        {
            var (_, eq) = NewActor();
            var a = MakeWeapon(3f);
            var b = MakeWeapon(9f);

            Assert.IsNull(eq.Equip(a));
            var previous = eq.Equip(b);
            Assert.AreSame(a, previous);
        }

        [Test]
        public void Equip_BelowRequiredLevel_IsRejected()
        {
            var (stats, eq) = NewActor(); // poziom 1
            float baseAtk = stats.GetStat(StatType.Attack);
            var highLevelSword = MakeWeapon(100f, reqLevel: 10);

            var returned = eq.Equip(highLevelSword);

            Assert.AreSame(highLevelSword, returned);                     // oddany, nie założony
            Assert.AreEqual(baseAtk, stats.GetStat(StatType.Attack), 0.001f);
            Assert.IsNull(eq.GetEquipped(EquipmentSlotType.RightHand));
        }
    }
}
