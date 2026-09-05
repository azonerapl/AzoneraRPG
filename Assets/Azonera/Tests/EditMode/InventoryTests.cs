using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Azonera.Items;
using InvComp = Azonera.Inventory.Inventory;

namespace Azonera.Tests.EditMode
{
    /// <summary>Testy plecaka: stackowanie, sloty niestackowalne, waga, limit slotów.</summary>
    public class InventoryTests
    {
        private readonly List<Object> _spawned = new List<Object>();

        private InvComp NewInventory()
        {
            var go = new GameObject("TestInventory");
            _spawned.Add(go);
            return go.AddComponent<InvComp>();
        }

        private ItemData MakeItem(bool stackable, int maxStack, float weight)
        {
            var it = ScriptableObject.CreateInstance<ItemData>();
            it.Stackable = stackable; it.MaxStack = maxStack; it.Weight = weight;
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
        public void StackableItems_StackUpToMaxStack()
        {
            var inv = NewInventory();
            var potion = MakeItem(stackable: true, maxStack: 10, weight: 0.5f);

            int added = inv.AddItem(potion, 15);

            Assert.AreEqual(15, added);
            Assert.AreEqual(15, inv.CountOf(potion));
            Assert.AreEqual(2, inv.Stacks.Count);           // 10 + 5
            Assert.AreEqual(7.5f, inv.TotalWeight, 0.001f); // 15 * 0.5
        }

        [Test]
        public void NonStackable_TakesSeparateSlots()
        {
            var inv = NewInventory();
            var sword = MakeItem(stackable: false, maxStack: 1, weight: 20f);

            int added = inv.AddItem(sword, 3);

            Assert.AreEqual(3, added);
            Assert.AreEqual(3, inv.Stacks.Count);
            Assert.AreEqual(3, inv.CountOf(sword));
        }

        [Test]
        public void SlotLimit_IsRespected()
        {
            var inv = NewInventory();
            var junk = MakeItem(stackable: false, maxStack: 1, weight: 1f);

            int added = inv.AddItem(junk, inv.SlotCount + 6);

            Assert.AreEqual(inv.SlotCount, added);       // tylko tyle, ile slotów
            Assert.AreEqual(inv.SlotCount, inv.Stacks.Count);
        }

        [Test]
        public void RemoveOne_DecrementsAndRemovesEmptyStack()
        {
            var inv = NewInventory();
            var potion = MakeItem(true, 10, 0.5f);
            inv.AddItem(potion, 2);
            var stack = inv.Stacks[0];

            inv.RemoveOne(stack);
            Assert.AreEqual(1, inv.CountOf(potion));
            inv.RemoveOne(stack);
            Assert.AreEqual(0, inv.CountOf(potion));
            Assert.AreEqual(0, inv.Stacks.Count);
        }
    }
}
