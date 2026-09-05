using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Azonera.Skills;

namespace Azonera.Tests.EditMode
{
    /// <summary>Testy systemu umiejętności: poziomy startowe, trening, awans, trudność, eventy.</summary>
    public class SkillTests
    {
        private readonly List<Object> _spawned = new List<Object>();

        private SkillSet NewSkillSet()
        {
            var go = new GameObject("TestSkills");
            _spawned.Add(go);
            return go.AddComponent<SkillSet>();
        }

        [TearDown]
        public void Cleanup()
        {
            foreach (var o in _spawned) if (o != null) Object.DestroyImmediate(o);
            _spawned.Clear();
        }

        [Test]
        public void DefaultLevels_MeleeTen_MagicZero()
        {
            var s = NewSkillSet();
            Assert.AreEqual(10, s.GetLevel(SkillType.Sword));
            Assert.AreEqual(10, s.GetLevel(SkillType.Shielding));
            Assert.AreEqual(0, s.GetLevel(SkillType.Magic));
        }

        [Test]
        public void RequiredTries_AtBaseLevel_EqualsBaseConstant()
        {
            var s = NewSkillSet();
            // Sword: constant 50, difficulty 1, factor^(10-10)=1 -> 50
            Assert.AreEqual(50f, s.RequiredTries(SkillType.Sword, 10), 0.001f);
            // Shielding: constant 100
            Assert.AreEqual(100f, s.RequiredTries(SkillType.Shielding, 10), 0.001f);
        }

        [Test]
        public void AddTries_Advances_WhenThresholdReached()
        {
            var s = NewSkillSet();
            int advances = s.AddTries(SkillType.Sword, 50f);
            Assert.AreEqual(1, advances);
            Assert.AreEqual(11, s.GetLevel(SkillType.Sword));
        }

        [Test]
        public void AddTries_Partial_KeepsLevel_TracksProgress()
        {
            var s = NewSkillSet();
            s.AddTries(SkillType.Sword, 25f); // połowa z 50
            Assert.AreEqual(10, s.GetLevel(SkillType.Sword));
            Assert.AreEqual(0.5f, s.ProgressNormalized(SkillType.Sword), 0.01f);
        }

        [Test]
        public void Difficulty_ScalesRequiredTries()
        {
            var s = NewSkillSet();
            s.SetDifficulty(0.5f);
            Assert.AreEqual(25f, s.RequiredTries(SkillType.Sword, 10), 0.001f);
        }

        [Test]
        public void AddTries_FiresAdvanceEvent()
        {
            var s = NewSkillSet();
            SkillType? advancedType = null;
            int advancedLevel = 0;
            s.OnSkillAdvanced += (t, lvl) => { advancedType = t; advancedLevel = lvl; };
            s.AddTries(SkillType.Axe, 100f); // >1 poziom
            Assert.AreEqual(SkillType.Axe, advancedType);
            Assert.GreaterOrEqual(advancedLevel, 11);
        }
    }
}
