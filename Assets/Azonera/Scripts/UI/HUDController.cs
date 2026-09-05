using UnityEngine;
using UnityEngine.UI;
using Azonera.Stats;
using Azonera.Inventory;

namespace Azonera.UI
{
    /// <summary>
    /// Steruje HUD-em: paski HP/Many/EXP, poziom, profesja, udźwig.
    /// Reaguje na eventy CharacterStats — zero pollingu w Update.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Paski (Image typu Filled)")]
        [SerializeField] private Image _healthFill;
        [SerializeField] private Image _manaFill;
        [SerializeField] private Image _expFill;

        [Header("Teksty")]
        [SerializeField] private Text _healthText;
        [SerializeField] private Text _manaText;
        [SerializeField] private Text _levelText;
        [SerializeField] private Text _classText;
        [SerializeField] private Text _capacityText;

        private CharacterStats _stats;
        private Azonera.Inventory.Inventory _inventory;

        public void Bind(CharacterStats stats, Azonera.Inventory.Inventory inventory)
        {
            Unbind();
            _stats = stats;
            _inventory = inventory;
            if (_stats != null)
            {
                _stats.OnHealthChanged += HandleHealth;
                _stats.OnManaChanged += HandleMana;
                _stats.OnExperienceChanged += HandleExp;
                _stats.OnLevelUp += HandleLevel;
                _stats.OnStatsChanged += HandleStats;
                RefreshAll();
            }
            if (_inventory != null) _inventory.OnInventoryChanged += HandleInventory;
        }

        private void OnDestroy() => Unbind();

        private void Unbind()
        {
            if (_stats != null)
            {
                _stats.OnHealthChanged -= HandleHealth;
                _stats.OnManaChanged -= HandleMana;
                _stats.OnExperienceChanged -= HandleExp;
                _stats.OnLevelUp -= HandleLevel;
                _stats.OnStatsChanged -= HandleStats;
            }
            if (_inventory != null) _inventory.OnInventoryChanged -= HandleInventory;
        }

        private void RefreshAll()
        {
            HandleHealth(_stats.CurrentHealth, _stats.MaxHealth);
            HandleMana(_stats.CurrentMana, _stats.MaxMana);
            HandleExp(_stats.Experience, _stats.ExperienceForNextLevel);
            HandleLevel(_stats.Level);
            HandleStats();
            HandleInventory();
        }

        private void HandleHealth(float cur, float max)
        {
            if (_healthFill != null) _healthFill.fillAmount = max > 0 ? cur / max : 0f;
            if (_healthText != null) _healthText.text = $"{Mathf.CeilToInt(cur)} / {Mathf.CeilToInt(max)}";
        }

        private void HandleMana(float cur, float max)
        {
            if (_manaFill != null) _manaFill.fillAmount = max > 0 ? cur / max : 0f;
            if (_manaText != null) _manaText.text = $"{Mathf.CeilToInt(cur)} / {Mathf.CeilToInt(max)}";
        }

        private void HandleExp(long cur, long next)
        {
            if (_expFill != null) _expFill.fillAmount = next > 0 ? (float)cur / next : 0f;
        }

        private void HandleLevel(int level)
        {
            if (_levelText != null) _levelText.text = $"Poziom {level}";
        }

        private void HandleStats()
        {
            if (_classText != null && _stats.Class != null) _classText.text = _stats.Class.DisplayName;
            if (_capacityText != null && _inventory != null)
                _capacityText.text = $"{_inventory.TotalWeight:0} / {_stats.GetStat(StatType.Capacity):0} oz";
        }

        private void HandleInventory() => HandleStats();
    }
}
