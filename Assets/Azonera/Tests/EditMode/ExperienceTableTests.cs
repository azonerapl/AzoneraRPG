using NUnit.Framework;
using Azonera.Progression;

namespace Azonera.Tests.EditMode
{
    /// <summary>Testy kanonicznej krzywej doświadczenia.</summary>
    public class ExperienceTableTests
    {
        [Test]
        public void TotalXp_Level1_IsZero()
        {
            Assert.AreEqual(0, ExperienceTable.TotalXpForLevel(1));
        }

        [Test]
        public void TotalXp_Level2_Is100()
        {
            Assert.AreEqual(100, ExperienceTable.TotalXpForLevel(2));
        }

        [Test]
        public void XpToNext_Level1_Is100()
        {
            Assert.AreEqual(100, ExperienceTable.XpToNext(1));
        }

        [Test]
        public void TotalXp_IsStrictlyIncreasing()
        {
            for (int l = 1; l < 200; l++)
                Assert.Less(ExperienceTable.TotalXpForLevel(l), ExperienceTable.TotalXpForLevel(l + 1));
        }

        [Test]
        public void LevelForTotalXp_MapsCorrectly()
        {
            Assert.AreEqual(1, ExperienceTable.LevelForTotalXp(0));
            Assert.AreEqual(1, ExperienceTable.LevelForTotalXp(99));
            Assert.AreEqual(2, ExperienceTable.LevelForTotalXp(100));
            Assert.AreEqual(2, ExperienceTable.LevelForTotalXp(ExperienceTable.TotalXpForLevel(3) - 1));
            Assert.AreEqual(3, ExperienceTable.LevelForTotalXp(ExperienceTable.TotalXpForLevel(3)));
        }
    }
}
