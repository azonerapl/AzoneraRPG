using UnityEngine;

namespace Azonera.Player
{
    /// <summary>
    /// Umiejętności i witalność w stylu klasycznego MMORPG (Tibia-like), zasilające panel Skills w HUD.
    /// Osobny, data-driven komponent — nie zaśmieca CharacterStats. Wartości rozwijane w przyszłości
    /// (trening skilli, jedzenie, stamina). Na teraz: sensowne wartości bazowe + level scaling.
    /// </summary>
    public class PlayerSkills : MonoBehaviour
    {
        [Header("Witalność (Tibia-like)")]
        public int SoulPoints = 100;
        public int MaxSoulPoints = 200;
        [Tooltip("Stamina w minutach (max 2520 = 42h).")]
        public int StaminaMinutes = 2520;
        public int MaxStaminaMinutes = 2520;
        [Tooltip("Regeneracja jedzenia w sekundach.")]
        public int FoodSeconds = 660;
        [Tooltip("Prędkość ruchu w jednostkach gry (wyświetlana w HUD).")]
        public int Speed = 220;

        [Header("Poziom magiczny i skille broni")]
        public int MagicLevel = 0;
        public int Fist = 10;
        public int Club = 10;
        public int Sword = 12;
        public int Axe = 10;
        public int Distance = 10;
        public int Shielding = 11;
        public int Fishing = 10;

        [Header("Postęp bieżącej umiejętności (0..1) — placeholder paska")]
        [Range(0f, 1f)] public float SkillProgress = 0.3f;

        /// <summary>Stamina jako HH:MM.</summary>
        public string StaminaText => $"{StaminaMinutes / 60:00}:{StaminaMinutes % 60:00}";
        /// <summary>Jedzenie jako MM:SS.</summary>
        public string FoodText => $"{FoodSeconds / 60:00}:{FoodSeconds % 60:00}";
    }
}
