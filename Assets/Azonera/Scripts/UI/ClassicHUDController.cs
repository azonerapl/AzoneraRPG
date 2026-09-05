using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Azonera.Stats;
using Azonera.Player;
using Azonera.Skills;
using InvComp = Azonera.Inventory.Inventory;

namespace Azonera.UI
{
    /// <summary>
    /// HUD klasycznego MMORPG (styl z referencji): portret + paski HP/Many + panel Skills
    /// (Experience, Level, Hit Points, Mana, Soul, Capacity, Speed, Food, Stamina, Magic Level,
    /// skille broni). Sam wyszukuje elementy po nazwie (Val_*, Fill_*), więc builder UI jest minimalny.
    /// Reaguje na eventy CharacterStats; skille/witalność odświeża z PlayerSkills.
    /// </summary>
    public class ClassicHUDController : MonoBehaviour
    {
        private readonly Dictionary<string, Text> _values = new Dictionary<string, Text>();
        private Image _hpFill, _mpFill, _expFill;

        private CharacterStats _stats;
        private PlayerSkills _skills;
        private SkillSet _skillSet;
        private InvComp _inventory;
        private bool _cached;

        private void Awake() => CacheElements();

        private void CacheElements()
        {
            if (_cached) return;
            foreach (var t in GetComponentsInChildren<Text>(true))
            {
                if (t.name.StartsWith("Val_"))
                    _values[t.name.Substring(4)] = t;
            }
            foreach (var img in GetComponentsInChildren<Image>(true))
            {
                if (img.name == "Fill_HP") _hpFill = img;
                else if (img.name == "Fill_MP") _mpFill = img;
                else if (img.name == "Fill_Exp") _expFill = img;
            }
            _cached = true;
        }

        private void Start()
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                _stats = player.GetComponent<CharacterStats>();
                _skills = player.GetComponent<PlayerSkills>();
                _skillSet = player.GetComponent<SkillSet>();
                _inventory = player.GetComponent<InvComp>();
            }
            Bind(_stats, _skills, _inventory);
        }

        public void Bind(CharacterStats stats, PlayerSkills skills, InvComp inventory)
        {
            CacheElements();
            Unbind();
            _stats = stats; _skills = skills; _inventory = inventory;
            if (_stats != null)
            {
                _stats.OnHealthChanged += HandleHealth;
                _stats.OnManaChanged += HandleMana;
                _stats.OnExperienceChanged += HandleExp;
                _stats.OnLevelUp += HandleLevel;
                _stats.OnStatsChanged += RefreshDerived;
            }
            if (_skillSet != null)
            {
                _skillSet.OnSkillChanged += HandleSkillChanged;
                _skillSet.OnSkillAdvanced += HandleSkillAdvanced;
            }
            RefreshAll();
        }

        private void HandleSkillChanged(SkillType t) => RefreshDerived();
        private void HandleSkillAdvanced(SkillType t, int level) => RefreshDerived();

        private void OnDestroy() => Unbind();

        private void Unbind()
        {
            if (_stats == null) return;
            _stats.OnHealthChanged -= HandleHealth;
            _stats.OnManaChanged -= HandleMana;
            _stats.OnExperienceChanged -= HandleExp;
            _stats.OnLevelUp -= HandleLevel;
            _stats.OnStatsChanged -= RefreshDerived;
            if (_skillSet != null)
            {
                _skillSet.OnSkillChanged -= HandleSkillChanged;
                _skillSet.OnSkillAdvanced -= HandleSkillAdvanced;
            }
        }

        private void Set(string key, string text)
        {
            if (_values.TryGetValue(key, out var t) && t != null) t.text = text;
        }

        private void RefreshAll()
        {
            if (_stats == null) return;
            HandleHealth(_stats.CurrentHealth, _stats.MaxHealth);
            HandleMana(_stats.CurrentMana, _stats.MaxMana);
            HandleExp(_stats.Experience, _stats.ExperienceForNextLevel);
            HandleLevel(_stats.Level);
            RefreshDerived();
        }

        private void HandleHealth(float cur, float max)
        {
            if (_hpFill != null) _hpFill.fillAmount = max > 0 ? cur / max : 0f;
            Set("HP", $"{Mathf.CeilToInt(cur)} / {Mathf.CeilToInt(max)}");
            Set("HPBar", $"{Mathf.CeilToInt(cur)} / {Mathf.CeilToInt(max)}");
        }

        private void HandleMana(float cur, float max)
        {
            if (_mpFill != null) _mpFill.fillAmount = max > 0 ? cur / max : 0f;
            Set("Mana", $"{Mathf.CeilToInt(cur)} / {Mathf.CeilToInt(max)}");
            Set("MPBar", $"{Mathf.CeilToInt(cur)} / {Mathf.CeilToInt(max)}");
        }

        private void HandleExp(long cur, long next)
        {
            if (_expFill != null) _expFill.fillAmount = next > 0 ? (float)cur / next : 0f;
            Set("Exp", cur.ToString("N0"));
        }

        private void HandleLevel(int level)
        {
            Set("Level", level.ToString());
            Set("LevelBadge", level.ToString());
        }

        private void RefreshDerived()
        {
            if (_stats != null)
            {
                Set("Cap", Mathf.RoundToInt(_stats.GetStat(StatType.Capacity)).ToString());
            }
            // Witalność klasyczna z PlayerSkills (Soul/Speed/Food/Stamina)
            if (_skills != null)
            {
                Set("Soul", _skills.SoulPoints.ToString());
                Set("Speed", _skills.Speed.ToString());
                Set("Food", _skills.FoodText);
                Set("Stamina", _skills.StaminaText);
            }

            // Skille i Magic Level — preferuj realny SkillSet, fallback do PlayerSkills
            if (_skillSet != null)
            {
                Set("MagicLevel", _skillSet.GetLevel(SkillType.Magic).ToString());
                Set("Fist", _skillSet.GetLevel(SkillType.Fist).ToString());
                Set("Club", _skillSet.GetLevel(SkillType.Club).ToString());
                Set("Sword", _skillSet.GetLevel(SkillType.Sword).ToString());
                Set("Axe", _skillSet.GetLevel(SkillType.Axe).ToString());
                Set("Distance", _skillSet.GetLevel(SkillType.Distance).ToString());
                Set("Shielding", _skillSet.GetLevel(SkillType.Shielding).ToString());
                Set("Fishing", _skillSet.GetLevel(SkillType.Fishing).ToString());
            }
            else if (_skills != null)
            {
                Set("MagicLevel", _skills.MagicLevel.ToString());
                Set("Fist", _skills.Fist.ToString());
                Set("Club", _skills.Club.ToString());
                Set("Sword", _skills.Sword.ToString());
                Set("Axe", _skills.Axe.ToString());
                Set("Distance", _skills.Distance.ToString());
                Set("Shielding", _skills.Shielding.ToString());
                Set("Fishing", _skills.Fishing.ToString());
            }
        }
    }
}
