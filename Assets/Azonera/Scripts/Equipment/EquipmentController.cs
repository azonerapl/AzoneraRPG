using System;
using System.Collections.Generic;
using UnityEngine;
using Azonera.Items;
using Azonera.Stats;

namespace Azonera.Equipment
{
    /// <summary>
    /// Zarządza założonym ekwipunkiem gracza. Po każdej zmianie automatycznie
    /// przelicza modyfikatory na <see cref="CharacterStats"/> (Stats Recalculation).
    /// </summary>
    [RequireComponent(typeof(CharacterStats))]
    public class EquipmentController : MonoBehaviour
    {
        private readonly Dictionary<EquipmentSlotType, ItemData> _equipped =
            new Dictionary<EquipmentSlotType, ItemData>();

        private CharacterStats _stats;

        public event Action OnEquipmentChanged;

        public IReadOnlyDictionary<EquipmentSlotType, ItemData> Equipped => _equipped;

        private void Awake()
        {
            _stats = GetComponent<CharacterStats>();
        }

        public ItemData GetEquipped(EquipmentSlotType slot)
        {
            _equipped.TryGetValue(slot, out var item);
            return item;
        }

        /// <summary>Zakłada przedmiot. Zwraca przedmiot, który był wcześniej w slocie (do plecaka), lub null.</summary>
        public ItemData Equip(ItemData item)
        {
            if (item == null || !item.IsEquippable) return item;
            if (_stats != null && item.RequiredLevel > _stats.Level)
            {
                Debug.Log($"[Equipment] {item.DisplayName} wymaga poziomu {item.RequiredLevel}.");
                return item; // nie zakładamy — oddajemy z powrotem
            }

            var slot = item.Slot;
            _equipped.TryGetValue(slot, out var previous);
            _equipped[slot] = item;

            RecalculateModifiers();
            OnEquipmentChanged?.Invoke();
            return previous;
        }

        /// <summary>Zdejmuje przedmiot ze slotu. Zwraca zdjęty przedmiot (do plecaka) lub null.</summary>
        public ItemData Unequip(EquipmentSlotType slot)
        {
            if (!_equipped.TryGetValue(slot, out var item)) return null;
            _equipped.Remove(slot);
            RecalculateModifiers();
            OnEquipmentChanged?.Invoke();
            return item;
        }

        /// <summary>Zbiera modyfikatory ze wszystkich założonych itemów i nakłada na statystyki.</summary>
        private void RecalculateModifiers()
        {
            if (_stats == null) return;
            _stats.ClearModifiers();
            foreach (var kvp in _equipped)
            {
                if (kvp.Value == null) continue;
                foreach (var mod in kvp.Value.Modifiers)
                    _stats.AddModifier(mod);
            }
            _stats.RecalculateStats();
        }
    }
}
