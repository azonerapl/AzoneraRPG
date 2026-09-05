using System.Collections.Generic;
using UnityEngine;
using Azonera.Stats;

namespace Azonera.Items
{
    public enum ItemType
    {
        Weapon, Helmet, Armor, Shield, Legs, Boots, Ring, Amulet,
        Potion, Food, QuestItem, Misc
    }

    public enum ItemRarity
    {
        Common, Uncommon, Rare, Epic, Legendary
    }

    /// <summary>Slot ekwipunku, w który item może wejść. None = nie zakładany.</summary>
    public enum EquipmentSlotType
    {
        None, Head, Amulet, Backpack, Armor, RightHand, LeftHand, Legs, Boots, Ring
    }

    /// <summary>
    /// Data-driven definicja przedmiotu. Wszystkie itemy to ScriptableObjecty —
    /// baza łatwo skalowalna do tysięcy pozycji, gotowa pod serwerowy katalog.
    /// </summary>
    [CreateAssetMenu(fileName = "Item_", menuName = "Azonera/Item Data", order = 10)]
    public class ItemData : ScriptableObject
    {
        [Header("Tożsamość")]
        public string ItemId = System.Guid.NewGuid().ToString();
        public string DisplayName = "Nowy przedmiot";
        [TextArea] public string Description;
        public Sprite Icon;
        public GameObject WorldModel; // model upuszczony na ziemi (opcjonalny)

        [Header("Klasyfikacja")]
        public ItemType Type = ItemType.Misc;
        public ItemRarity Rarity = ItemRarity.Common;
        public EquipmentSlotType Slot = EquipmentSlotType.None;

        [Header("Handel / Waga")]
        public float Weight = 1f;
        public bool Stackable = false;
        public int MaxStack = 1;
        public int Value = 1;

        [Header("Wymagania")]
        public int RequiredLevel = 0;
        public string RequiredClassTag = ""; // pusto = brak ograniczeń

        [Header("Modyfikatory statystyk (ekwipunek)")]
        public List<StatModifier> Modifiers = new List<StatModifier>();

        [Header("Efekt użycia (potion / food)")]
        public float HealAmount = 0f;
        public float ManaAmount = 0f;

        public bool IsEquippable => Slot != EquipmentSlotType.None;
        public bool IsConsumable => Type == ItemType.Potion || Type == ItemType.Food;

        public Color RarityColor
        {
            get
            {
                switch (Rarity)
                {
                    case ItemRarity.Uncommon:  return new Color(0.45f, 0.85f, 0.45f);
                    case ItemRarity.Rare:      return new Color(0.35f, 0.6f, 1f);
                    case ItemRarity.Epic:      return new Color(0.7f, 0.4f, 0.95f);
                    case ItemRarity.Legendary: return new Color(0.95f, 0.75f, 0.25f);
                    default:                   return new Color(0.85f, 0.85f, 0.85f);
                }
            }
        }
    }
}
