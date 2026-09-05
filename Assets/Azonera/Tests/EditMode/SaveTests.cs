using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Azonera.Stats;
using Azonera.Save;
using InvComp = Azonera.Inventory.Inventory;

namespace Azonera.Tests.EditMode
{
    /// <summary>Testy zapisu: serializacja JSON round-trip oraz Save/Load/Apply przez SaveSystem.</summary>
    public class SaveTests
    {
        private readonly List<Object> _spawned = new List<Object>();

        [SetUp]
        public void ClearSave() => SaveSystem.Delete();

        [TearDown]
        public void Cleanup()
        {
            SaveSystem.Delete();
            foreach (var o in _spawned) if (o != null) Object.DestroyImmediate(o);
            _spawned.Clear();
        }

        private (Transform, CharacterStats, InvComp) NewActor()
        {
            var go = new GameObject("SaveActor");
            _spawned.Add(go);
            var stats = go.AddComponent<CharacterStats>();
            var inv = go.AddComponent<InvComp>();
            stats.InitializeRaw(level: 1, maxHp: 100f, attack: 10f, defense: 5f);
            return (go.transform, stats, inv);
        }

        [Test]
        public void SaveData_JsonRoundTrip_PreservesFields()
        {
            var data = new SaveData
            {
                Level = 7, Experience = 1234, CurrentHealth = 88, CurrentMana = 12,
                PosX = 3.5f, PosY = 1f, PosZ = -4.25f, CharacterClass = "Druid",
                Inventory = new List<ItemSaveEntry> { new ItemSaveEntry { ItemId = "health_potion", Count = 9 } }
            };

            var json = JsonUtility.ToJson(data);
            var back = JsonUtility.FromJson<SaveData>(json);

            Assert.AreEqual(7, back.Level);
            Assert.AreEqual(1234, back.Experience);
            Assert.AreEqual(-4.25f, back.PosZ, 0.0001f);
            Assert.AreEqual("Druid", back.CharacterClass);
            Assert.AreEqual(1, back.Inventory.Count);
            Assert.AreEqual("health_potion", back.Inventory[0].ItemId);
            Assert.AreEqual(9, back.Inventory[0].Count);
        }

        [Test]
        public void SaveThenLoad_RoundTripsProgression()
        {
            var (tf, stats, inv) = NewActor();
            stats.SetLevel(5);
            tf.position = new Vector3(10f, 1f, -7f);

            SaveSystem.Save(tf, stats, inv);
            Assert.IsTrue(SaveSystem.HasSave);

            var data = SaveSystem.Load();
            Assert.IsNotNull(data);
            Assert.AreEqual(5, data.Level);
            Assert.AreEqual(10f, data.PosX, 0.001f);
            Assert.AreEqual(-7f, data.PosZ, 0.001f);
        }

        [Test]
        public void Apply_RestoresLevelAndPosition()
        {
            var (tf, stats, inv) = NewActor();
            stats.SetLevel(8);
            tf.position = new Vector3(1f, 1f, 2f);
            SaveSystem.Save(tf, stats, inv);
            var data = SaveSystem.Load();

            var (tf2, stats2, inv2) = NewActor(); // świeży aktor na poziomie 1
            SaveSystem.Apply(data, tf2, stats2, inv2);

            Assert.AreEqual(8, stats2.Level);
            Assert.AreEqual(1f, tf2.position.x, 0.001f);
            Assert.AreEqual(2f, tf2.position.z, 0.001f);
        }

        [Test]
        public void Delete_RemovesSave()
        {
            var (tf, stats, inv) = NewActor();
            SaveSystem.Save(tf, stats, inv);
            Assert.IsTrue(SaveSystem.HasSave);
            SaveSystem.Delete();
            Assert.IsFalse(SaveSystem.HasSave);
        }
    }
}
