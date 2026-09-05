#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Azonera.Classes;
using Azonera.Items;
using Azonera.Loot;
using Azonera.Monsters;
using Azonera.Stats;

namespace Azonera.EditorTools
{
    /// <summary>
    /// Generuje bazowe assety danych Azonery (profesje, przedmioty, loot, potwory).
    /// Idempotentne — ponowne uruchomienie aktualizuje istniejące assety zamiast duplikować.
    /// </summary>
    public static class AzoneraDataBuilder
    {
        private const string Root = "Assets/Azonera";
        private const string ClassesPath = Root + "/ScriptableObjects/Classes";
        private const string ItemsPath = Root + "/Resources/Items";     // Resources → ItemRegistry
        private const string LootPath = Root + "/ScriptableObjects/Loot";
        private const string MonstersPath = Root + "/ScriptableObjects/Monsters";

        [MenuItem("Azonera/1. Utwórz dane gry (klasy, itemy, potwory)")]
        public static void BuildDataMenu()
        {
            BuildData();
            EditorUtility.DisplayDialog("Azonera", "Dane gry utworzone / zaktualizowane.", "OK");
        }

        public static void BuildData()
        {
            EnsureFolders();
            BuildClasses();
            var items = BuildItems();
            var loot = BuildLoot(items);
            BuildMonsters(items, loot);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Azonera] Dane gry gotowe.");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Azonera/ScriptableObjects/Loot");
            EnsureFolder("Assets/Azonera/Resources");
            EnsureFolder("Assets/Azonera/Resources/Items");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            string leaf = System.IO.Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var inst = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(inst, path);
            return inst;
        }

        // ---------------- KLASY ----------------
        private static void BuildClasses()
        {
            var knight = GetOrCreate<ClassData>($"{ClassesPath}/Class_Knight.asset");
            knight.Class = CharacterClass.Knight;
            knight.DisplayName = "Rycerz";
            knight.Description = "Wojownik pierwszej linii. Wysokie HP i obrona, walka wręcz.";
            knight.ThemeColor = new Color(0.85f, 0.4f, 0.3f);
            knight.BaseHealth = 200; knight.BaseMana = 40; knight.BaseAttack = 14; knight.BaseDefense = 10;
            knight.BaseArmor = 0.06f; knight.BaseMoveSpeed = 4.6f; knight.BaseAttackSpeed = 1.15f;
            knight.BaseCapacity = 470; knight.HealthPerLevel = 18; knight.AttackPerLevel = 1.8f; knight.DefensePerLevel = 1.0f;
            EditorUtility.SetDirty(knight);

            var sorcerer = GetOrCreate<ClassData>($"{ClassesPath}/Class_Sorcerer.asset");
            sorcerer.Class = CharacterClass.Sorcerer;
            sorcerer.DisplayName = "Czarnoksiężnik";
            sorcerer.Description = "Mistrz niszczycielskiej magii. Wysoka mana i moc zaklęć, kruchy.";
            sorcerer.ThemeColor = new Color(0.4f, 0.5f, 0.95f);
            sorcerer.BaseHealth = 120; sorcerer.BaseMana = 160; sorcerer.BaseAttack = 8; sorcerer.BaseDefense = 4;
            sorcerer.BaseMagicPower = 18; sorcerer.BaseMoveSpeed = 4.4f; sorcerer.BaseAttackSpeed = 1.0f;
            sorcerer.BaseCapacity = 300; sorcerer.HealthPerLevel = 10; sorcerer.ManaPerLevel = 20; sorcerer.MagicPowerPerLevel = 3f;
            sorcerer.StartingSpells = new List<string> { "firebolt", "energy_strike" };
            EditorUtility.SetDirty(sorcerer);

            var druid = GetOrCreate<ClassData>($"{ClassesPath}/Class_Druid.asset");
            druid.Class = CharacterClass.Druid;
            druid.DisplayName = "Druid";
            druid.Description = "Uzdrowiciel i mag natury. Wsparcie, kontrola, magia obszarowa.";
            druid.ThemeColor = new Color(0.4f, 0.8f, 0.5f);
            druid.BaseHealth = 130; druid.BaseMana = 150; druid.BaseAttack = 8; druid.BaseDefense = 5;
            druid.BaseMagicPower = 15; druid.BaseMoveSpeed = 4.5f; druid.BaseAttackSpeed = 1.0f;
            druid.BaseCapacity = 320; druid.HealthPerLevel = 11; druid.ManaPerLevel = 18; druid.MagicPowerPerLevel = 2.6f;
            druid.StartingSpells = new List<string> { "heal", "nature_bolt" };
            EditorUtility.SetDirty(druid);
        }

        // ---------------- ITEMY ----------------
        private static Dictionary<string, ItemData> BuildItems()
        {
            var map = new Dictionary<string, ItemData>();

            var sword = GetOrCreate<ItemData>($"{ItemsPath}/Item_RustedSword.asset");
            sword.ItemId = "rusted_sword"; sword.DisplayName = "Zardzewiały miecz";
            sword.Description = "Prosty miecz. Lepszy niż pięści.";
            sword.Type = ItemType.Weapon; sword.Slot = EquipmentSlotType.RightHand; sword.Rarity = ItemRarity.Common;
            sword.Weight = 38; sword.Value = 25; sword.RequiredLevel = 1;
            sword.Modifiers = new List<StatModifier> { new StatModifier(StatType.Attack, 7f) };
            EditorUtility.SetDirty(sword); map["sword"] = sword;

            var armor = GetOrCreate<ItemData>($"{ItemsPath}/Item_LeatherArmor.asset");
            armor.ItemId = "leather_armor"; armor.DisplayName = "Skórzana zbroja";
            armor.Description = "Znoszona, lecz wciąż chroni.";
            armor.Type = ItemType.Armor; armor.Slot = EquipmentSlotType.Armor; armor.Rarity = ItemRarity.Common;
            armor.Weight = 60; armor.Value = 40; armor.RequiredLevel = 1;
            armor.Modifiers = new List<StatModifier> { new StatModifier(StatType.MaxHealth, 25f), new StatModifier(StatType.Defense, 3f) };
            EditorUtility.SetDirty(armor); map["armor"] = armor;

            var ring = GetOrCreate<ItemData>($"{ItemsPath}/Item_IronRing.asset");
            ring.ItemId = "iron_ring"; ring.DisplayName = "Żelazny pierścień";
            ring.Description = "Cicho szumi mocą.";
            ring.Type = ItemType.Ring; ring.Slot = EquipmentSlotType.Ring; ring.Rarity = ItemRarity.Uncommon;
            ring.Weight = 2; ring.Value = 120; ring.RequiredLevel = 1;
            ring.Modifiers = new List<StatModifier> {
                new StatModifier(StatType.CritChance, 0.04f),
                new StatModifier(StatType.Attack, 2f)
            };
            EditorUtility.SetDirty(ring); map["ring"] = ring;

            var hp = GetOrCreate<ItemData>($"{ItemsPath}/Item_HealthPotion.asset");
            hp.ItemId = "health_potion"; hp.DisplayName = "Mikstura zdrowia";
            hp.Description = "Przywraca 75 punktów życia.";
            hp.Type = ItemType.Potion; hp.Slot = EquipmentSlotType.None; hp.Rarity = ItemRarity.Common;
            hp.Stackable = true; hp.MaxStack = 50; hp.Weight = 0.9f; hp.Value = 12; hp.HealAmount = 75f;
            EditorUtility.SetDirty(hp); map["hp"] = hp;

            var mp = GetOrCreate<ItemData>($"{ItemsPath}/Item_ManaPotion.asset");
            mp.ItemId = "mana_potion"; mp.DisplayName = "Mikstura many";
            mp.Description = "Przywraca 60 punktów many.";
            mp.Type = ItemType.Potion; mp.Rarity = ItemRarity.Common;
            mp.Stackable = true; mp.MaxStack = 50; mp.Weight = 0.9f; mp.Value = 12; mp.ManaAmount = 60f;
            EditorUtility.SetDirty(mp); map["mp"] = mp;

            var tooth = GetOrCreate<ItemData>($"{ItemsPath}/Item_MonsterTooth.asset");
            tooth.ItemId = "monster_tooth"; tooth.DisplayName = "Kieł bestii";
            tooth.Description = "Trofeum. Ktoś może chcieć je kupić.";
            tooth.Type = ItemType.Misc; tooth.Rarity = ItemRarity.Common;
            tooth.Stackable = true; tooth.MaxStack = 100; tooth.Weight = 0.3f; tooth.Value = 4;
            EditorUtility.SetDirty(tooth); map["tooth"] = tooth;

            return map;
        }

        // ---------------- LOOT ----------------
        private static LootTable BuildLoot(Dictionary<string, ItemData> items)
        {
            var loot = GetOrCreate<LootTable>($"{LootPath}/Loot_Common.asset");
            loot.MinGold = 3; loot.MaxGold = 22;
            loot.Entries = new List<LootEntry>
            {
                new LootEntry { Item = items["tooth"], Chance = 0.7f, MinCount = 1, MaxCount = 2 },
                new LootEntry { Item = items["hp"],    Chance = 0.35f, MinCount = 1, MaxCount = 2 },
                new LootEntry { Item = items["mp"],    Chance = 0.2f, MinCount = 1, MaxCount = 1 },
                new LootEntry { Item = items["ring"],  Chance = 0.06f, MinCount = 1, MaxCount = 1 },
                new LootEntry { Item = items["sword"], Chance = 0.08f, MinCount = 1, MaxCount = 1 },
                new LootEntry { Item = items["armor"], Chance = 0.07f, MinCount = 1, MaxCount = 1 },
            };
            EditorUtility.SetDirty(loot);
            return loot;
        }

        // ---------------- POTWORY ----------------
        private static void BuildMonsters(Dictionary<string, ItemData> items, LootTable loot)
        {
            CreateMonster("Monster_MarshSnake", "Bagienny wąż", 1, 30, 6, 1, 2.4f, 15, loot,
                new Color(0.35f, 0.5f, 0.32f), 0.8f);
            CreateMonster("Monster_Wolf", "Wilk", 2, 55, 10, 2, 3.2f, 30, loot,
                new Color(0.45f, 0.42f, 0.4f), 1.1f);
            CreateMonster("Monster_Goblin", "Goblin", 3, 80, 14, 4, 2.9f, 55, loot,
                new Color(0.45f, 0.55f, 0.3f), 1.3f);
            CreateMonster("Monster_Skeleton", "Szkielet", 4, 110, 18, 6, 2.7f, 85, loot,
                new Color(0.82f, 0.8f, 0.72f), 1.7f);
        }

        private static void CreateMonster(string file, string name, int level, float hp, float dmg,
            float def, float speed, long exp, LootTable loot, Color color, float height)
        {
            var m = GetOrCreate<MonsterData>($"{MonstersPath}/{file}.asset");
            m.DisplayName = name; m.Level = level; m.MaxHealth = hp; m.Damage = dmg; m.Defense = def;
            m.MoveSpeed = speed; m.AttackSpeed = 0.8f; m.AttackRange = 2.0f;
            m.AggroRange = 8f; m.DeAggroRange = 15f; m.RoamRadius = 5f;
            m.ExperienceReward = exp; m.LootTable = loot;
            m.PlaceholderColor = color; m.PlaceholderHeight = height;
            EditorUtility.SetDirty(m);
        }
    }
}
#endif
