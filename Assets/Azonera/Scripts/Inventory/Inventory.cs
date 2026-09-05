using System;
using System.Collections.Generic;
using UnityEngine;
using Azonera.Items;

namespace Azonera.Inventory
{
    /// <summary>Runtime'owy stos przedmiotów w jednym slocie plecaka.</summary>
    [System.Serializable]
    public class InventoryStack
    {
        public ItemData Item;
        public int Count;

        public InventoryStack(ItemData item, int count)
        {
            Item = item;
            Count = count;
        }

        public float Weight => Item != null ? Item.Weight * Count : 0f;
    }

    /// <summary>
    /// Plecak gracza. Trzyma stosy przedmiotów, obsługuje stackowanie, wagę i pojemność.
    /// Czyste dane + eventy — UI tylko rysuje, logika żyje tu (łatwe do przeniesienia na serwer).
    /// </summary>
    public class Inventory : MonoBehaviour
    {
        [SerializeField] private int _slotCount = 24;
        private readonly List<InventoryStack> _stacks = new List<InventoryStack>();

        public event Action OnInventoryChanged;

        public IReadOnlyList<InventoryStack> Stacks => _stacks;
        public int SlotCount => _slotCount;
        public float TotalWeight
        {
            get { float w = 0f; foreach (var s in _stacks) w += s.Weight; return w; }
        }

        /// <summary>Dodaje przedmiot(y). Zwraca ile faktycznie zmieściło się.</summary>
        public int AddItem(ItemData item, int count = 1)
        {
            if (item == null || count <= 0) return 0;
            int remaining = count;

            if (item.Stackable)
            {
                foreach (var s in _stacks)
                {
                    if (s.Item != item || s.Count >= item.MaxStack) continue;
                    int space = item.MaxStack - s.Count;
                    int add = Mathf.Min(space, remaining);
                    s.Count += add;
                    remaining -= add;
                    if (remaining == 0) break;
                }
            }

            while (remaining > 0 && _stacks.Count < _slotCount)
            {
                int add = item.Stackable ? Mathf.Min(item.MaxStack, remaining) : 1;
                _stacks.Add(new InventoryStack(item, add));
                remaining -= add;
            }

            if (remaining != count) OnInventoryChanged?.Invoke();
            return count - remaining;
        }

        public bool RemoveStack(InventoryStack stack)
        {
            bool removed = _stacks.Remove(stack);
            if (removed) OnInventoryChanged?.Invoke();
            return removed;
        }

        public void RemoveOne(InventoryStack stack)
        {
            if (stack == null) return;
            stack.Count--;
            if (stack.Count <= 0) _stacks.Remove(stack);
            OnInventoryChanged?.Invoke();
        }

        public int CountOf(ItemData item)
        {
            int total = 0;
            foreach (var s in _stacks) if (s.Item == item) total += s.Count;
            return total;
        }

        public void Clear()
        {
            _stacks.Clear();
            OnInventoryChanged?.Invoke();
        }

        public void RaiseChanged() => OnInventoryChanged?.Invoke();
    }
}
