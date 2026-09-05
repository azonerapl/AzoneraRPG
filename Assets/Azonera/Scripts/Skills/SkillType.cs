namespace Azonera.Skills
{
    /// <summary>Umiejętności postaci w stylu klasycznego MMORPG.</summary>
    public enum SkillType
    {
        Fist,
        Club,
        Sword,
        Axe,
        Distance,
        Shielding,
        Magic,      // Magic Level
        Fishing
    }

    /// <summary>
    /// Pojedyncza umiejętność: poziom + zgromadzone „próby" (tries) w stronę następnego poziomu.
    /// Czyste dane (serializowalne) — logika progu awansu żyje w SkillSet (zależy od profesji).
    /// </summary>
    [System.Serializable]
    public class Skill
    {
        public SkillType Type;
        public int Level;
        public float Points;      // zgromadzone tries w stronę Level+1

        public Skill(SkillType type, int level)
        {
            Type = type;
            Level = level;
            Points = 0f;
        }
    }
}
