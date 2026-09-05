namespace Azonera.Progression
{
    /// <summary>
    /// Kanoniczne źródło krzywej doświadczenia postaci (styl klasycznego MMORPG).
    /// Wzór skumulowany: total(L) = (50*L^3 - 150*L^2 + 400*L - 300) / 3.
    /// Dzięki temu przejście 1→2 wymaga 100 EXP, a krzywa rośnie sześciennie.
    /// Jedno miejsce prawdy — CharacterStats, HUD i przyszły serwer korzystają stąd.
    /// </summary>
    public static class ExperienceTable
    {
        /// <summary>Skumulowane EXP potrzebne, by OSIĄGNĄĆ dany poziom (level 1 = 0).</summary>
        public static long TotalXpForLevel(int level)
        {
            if (level < 1) level = 1;
            long L = level;
            return (50L * L * L * L - 150L * L * L + 400L * L - 300L) / 3L;
        }

        /// <summary>EXP potrzebne, by z <paramref name="level"/> awansować na następny.</summary>
        public static long XpToNext(int level)
        {
            if (level < 1) level = 1;
            return TotalXpForLevel(level + 1) - TotalXpForLevel(level);
        }

        /// <summary>Poziom odpowiadający danej ilości skumulowanego EXP.</summary>
        public static int LevelForTotalXp(long totalXp)
        {
            int level = 1;
            while (TotalXpForLevel(level + 1) <= totalXp) level++;
            return level;
        }
    }
}
