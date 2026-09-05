using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Azonera.Stats;
using Azonera.Inventory;
using Azonera.Items;

namespace Azonera.Save
{
    [Serializable]
    public class ItemSaveEntry
    {
        public string ItemId;
        public int Count;
    }

    [Serializable]
    public class SaveData
    {
        public int SaveVersion = 1;
        public string CharacterClass = "Knight";
        public int Level = 1;
        public long Experience = 0;
        public float CurrentHealth = -1f;
        public float CurrentMana = -1f;
        public float PosX, PosY, PosZ;
        public List<ItemSaveEntry> Inventory = new List<ItemSaveEntry>();
        public string SavedAtUtc;
    }

    /// <summary>
    /// Lokalny zapis/odczyt JSON w persistentDataPath. Architektura oddziela DANE (SaveData)
    /// od gameplayu — ten sam model łatwo wysłać do backendu/MMO w przyszłości.
    /// </summary>
    public static class SaveSystem
    {
        private static string FilePath => Path.Combine(Application.persistentDataPath, "azonera_save.json");

        public static bool HasSave => File.Exists(FilePath);

        public static void Save(Transform player, CharacterStats stats, Azonera.Inventory.Inventory inventory)
        {
            var data = new SaveData();
            if (stats != null)
            {
                data.Level = stats.Level;
                data.Experience = stats.Experience;
                data.CurrentHealth = stats.CurrentHealth;
                data.CurrentMana = stats.CurrentMana;
                if (stats.Class != null) data.CharacterClass = stats.Class.Class.ToString();
            }
            if (player != null)
            {
                data.PosX = player.position.x;
                data.PosY = player.position.y;
                data.PosZ = player.position.z;
            }
            if (inventory != null)
            {
                foreach (var stack in inventory.Stacks)
                {
                    if (stack.Item == null) continue;
                    data.Inventory.Add(new ItemSaveEntry { ItemId = stack.Item.ItemId, Count = stack.Count });
                }
            }
            data.SavedAtUtc = DateTime.UtcNow.ToString("o");

            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
                Debug.Log($"[Save] Zapisano grę: {FilePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] Błąd zapisu: {e.Message}");
            }
        }

        public static SaveData Load()
        {
            if (!HasSave) return null;
            try
            {
                return JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] Błąd odczytu: {e.Message}");
                return null;
            }
        }

        public static void Apply(SaveData data, Transform player, CharacterStats stats, Azonera.Inventory.Inventory inventory)
        {
            if (data == null) return;
            if (player != null)
            {
                player.position = new Vector3(data.PosX, data.PosY, data.PosZ);
                var rb = player.GetComponent<Rigidbody>();
                if (rb != null) rb.linearVelocity = Vector3.zero;
            }
            if (stats != null)
                stats.LoadState(data.Level, data.Experience, data.CurrentHealth, data.CurrentMana);
            if (inventory != null)
            {
                inventory.Clear();
                foreach (var entry in data.Inventory)
                {
                    var item = ItemRegistry.Get(entry.ItemId);
                    if (item != null) inventory.AddItem(item, entry.Count);
                }
            }
            Debug.Log("[Save] Wczytano zapis.");
        }

        public static void Delete()
        {
            if (HasSave) File.Delete(FilePath);
        }
    }
}
