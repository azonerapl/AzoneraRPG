using System.Collections.Generic;
using UnityEngine;

namespace Azonera.Items
{
    /// <summary>
    /// Rejestr przedmiotów po ID — potrzebny przy wczytywaniu zapisu (JSON trzyma ID, nie referencje).
    /// Ładuje wszystkie ItemData z folderu Resources/Items. Gotowy pod przyszły katalog serwerowy.
    /// </summary>
    public static class ItemRegistry
    {
        private static Dictionary<string, ItemData> _byId;

        public static void EnsureLoaded()
        {
            if (_byId != null) return;
            _byId = new Dictionary<string, ItemData>();
            var all = Resources.LoadAll<ItemData>("Items");
            foreach (var item in all)
            {
                if (item == null || string.IsNullOrEmpty(item.ItemId)) continue;
                _byId[item.ItemId] = item;
            }
            Debug.Log($"[ItemRegistry] Załadowano {_byId.Count} przedmiotów.");
        }

        public static ItemData Get(string id)
        {
            EnsureLoaded();
            return _byId.TryGetValue(id, out var item) ? item : null;
        }

        public static IEnumerable<ItemData> All
        {
            get { EnsureLoaded(); return _byId.Values; }
        }
    }
}
