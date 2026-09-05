using UnityEngine;
using UnityEngine.InputSystem;
using Azonera.Core;
using Azonera.Stats;
using Azonera.Items;
using Azonera.Combat;

namespace Azonera.DevTools
{
    /// <summary>
    /// Panel deweloperski (F1). Aktywny tylko w edytorze / Development Build.
    /// Szybkie testowanie mechanik: EXP, poziom, leczenie, god mode, statystyki, FPS.
    /// </summary>
    public class DebugPanel : MonoBehaviour
    {
        private bool _visible;
        private bool _godMode;
        private float _fps;
        private float _fpsTimer;
        private int _frames;

        private void Update()
        {
#if UNITY_EDITOR || DEBUG
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
                _visible = !_visible;

            _frames++;
            _fpsTimer += Time.unscaledDeltaTime;
            if (_fpsTimer >= 0.5f) { _fps = _frames / _fpsTimer; _frames = 0; _fpsTimer = 0f; }

            if (_godMode)
            {
                var s = Stats;
                if (s != null && !s.IsDead) s.Heal(s.MaxHealth);
            }
#endif
        }

        private CharacterStats Stats => GameManager.Instance != null ? GameManager.Instance.PlayerStats : null;

#if UNITY_EDITOR || DEBUG
        private void OnGUI()
        {
            // HUD wskaźnik FPS zawsze widoczny w dev
            GUI.Label(new Rect(10, Screen.height - 24, 300, 22),
                $"FPS: {_fps:0}   [F1] Panel Dev");

            if (!_visible) return;
            var s = Stats;
            var gm = GameManager.Instance;

            GUILayout.BeginArea(new Rect(10, 10, 240, 420), GUI.skin.box);
            GUILayout.Label("<b>AZONERA — DEV PANEL</b>");

            if (s != null)
            {
                GUILayout.Label($"Poziom: {s.Level}   EXP: {s.Experience}/{s.ExperienceForNextLevel}");
                GUILayout.Label($"HP: {s.CurrentHealth:0}/{s.MaxHealth:0}   MP: {s.CurrentMana:0}/{s.MaxMana:0}");
                GUILayout.Label($"Atak: {s.GetStat(StatType.Attack):0}  Obr: {s.GetStat(StatType.Defense):0}");
                if (gm != null && gm.Player != null)
                {
                    var p = gm.Player.transform.position;
                    GUILayout.Label($"Pozycja: {p.x:0.0}, {p.z:0.0}");
                }

                GUILayout.Space(6);
                if (GUILayout.Button("Give EXP +100")) s.AddExperience(100);
                if (GUILayout.Button("Give EXP +1000")) s.AddExperience(1000);
                if (GUILayout.Button("Level Up")) s.AddExperience(s.ExperienceForNextLevel);
                if (GUILayout.Button("Heal Full")) s.Heal(s.MaxHealth);
                if (GUILayout.Button("Damage self 25"))
                    s.ApplyDamage(new DamageInfo(25f, DamageType.True, gameObject));
                _godMode = GUILayout.Toggle(_godMode, "God Mode");

                if (GUILayout.Button("Give random item"))
                {
                    foreach (var item in ItemRegistry.All)
                    {
                        if (gm != null && gm.PlayerInventory != null) gm.PlayerInventory.AddItem(item, 1);
                        break;
                    }
                }
            }
            else
            {
                GUILayout.Label("Brak gracza w scenie.");
            }

            GUILayout.Space(6);
            if (gm != null)
            {
                if (GUILayout.Button("Save (F5)")) gm.SaveGame();
                if (GUILayout.Button("Load (F9)")) gm.LoadGame();
            }
            GUILayout.EndArea();
        }
#endif
    }
}
