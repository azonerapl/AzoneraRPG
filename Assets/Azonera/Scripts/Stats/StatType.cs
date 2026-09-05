namespace Azonera.Stats
{
    /// <summary>
    /// Wszystkie statystyki postaci. Używane jako klucz w modyfikatorach ekwipunku,
    /// buffach i przyszłej synchronizacji sieciowej.
    /// </summary>
    public enum StatType
    {
        MaxHealth,
        MaxMana,
        Attack,        // bazowa siła ataku fizycznego
        Defense,       // redukcja obrażeń fizycznych (płaska)
        Armor,         // % redukcji obrażeń fizycznych
        MagicPower,    // siła zaklęć
        MagicResist,   // redukcja obrażeń magicznych (%)
        MoveSpeed,
        AttackSpeed,   // ataki na sekundę
        CritChance,    // 0..1
        CritDamage,    // mnożnik, np. 1.5 = +50%
        Capacity       // udźwig
    }

    /// <summary>Sposób nakładania modyfikatora na statystykę bazową.</summary>
    public enum ModifierMode
    {
        Flat,       // dodaj wartość
        PercentAdd  // dodaj procent bazy (0.1 = +10%)
    }

    /// <summary>Pojedynczy modyfikator statystyki pochodzący ze źródła (item, buff).</summary>
    [System.Serializable]
    public struct StatModifier
    {
        public StatType Stat;
        public ModifierMode Mode;
        public float Value;

        public StatModifier(StatType stat, float value, ModifierMode mode = ModifierMode.Flat)
        {
            Stat = stat;
            Value = value;
            Mode = mode;
        }
    }
}
