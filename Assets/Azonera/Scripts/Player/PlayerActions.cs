using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Azonera.Combat;
using Azonera.Stats;
using Azonera.UI;

namespace Azonera.Player
{
    /// <summary>
    /// Router akcji gracza: lewy klik wybiera cel / interakcję (raycast spod kursora),
    /// Tab przełącza wrogów, Escape czyści zaznaczenie. Gracz automatycznie atakuje
    /// zaznaczonego wroga w zasięgu (styl ARPG/MMORPG).
    ///
    /// Nie trzyma własnego stanu celu — deleguje do <see cref="TargetSystem"/>,
    /// a zadawanie obrażeń do <see cref="MeleeAttacker"/>. Tu żyje wyłącznie WEJŚCIE.
    /// </summary>
    [RequireComponent(typeof(MeleeAttacker))]
    [RequireComponent(typeof(CharacterStats))]
    public class PlayerActions : MonoBehaviour
    {
        [SerializeField] private LayerMask _clickMask = ~0;

        private MeleeAttacker _attacker;
        private CharacterStats _stats;
        private TargetSystem _targeting;
        private UnityEngine.Camera _cam;

        /// <summary>Aktualny cel (dla zgodności z istniejącym kodem/HUD).</summary>
        public IDamageable CurrentTarget => _targeting != null ? _targeting.Target : null;
        public TargetSystem Targeting => _targeting;

        private void Awake()
        {
            _attacker = GetComponent<MeleeAttacker>();
            _stats = GetComponent<CharacterStats>();
            _cam = UnityEngine.Camera.main;

            // Gwarantujemy system celowania także w scenach zbudowanych wcześniej.
            _targeting = GetComponent<TargetSystem>();
            if (_targeting == null) _targeting = gameObject.AddComponent<TargetSystem>();
        }

        private void Update()
        {
            if (_stats.IsDead) { _targeting.ClearTarget(); return; }
            if (UIState.BlockWorldInput) return;
            if (_cam == null) _cam = UnityEngine.Camera.main;

            HandleMouse();
            HandleKeyboard();
            AutoAttack();
        }

        private void HandleMouse()
        {
            if (Mouse.current == null) return;
            if (!Mouse.current.leftButton.wasPressedThisFrame) return;
            if (IsPointerOverUI()) return;
            HandleLeftClick();
        }

        private void HandleKeyboard()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.tabKey.wasPressedThisFrame) _targeting.CycleTarget();
            if (kb.escapeKey.wasPressedThisFrame) _targeting.ClearTarget();
        }

        private void AutoAttack()
        {
            var target = _targeting.Target;
            if (target == null) return;
            if (!_attacker.InRange(target.transform)) return;
            FaceTarget(target.transform);
            _attacker.TryAttack(target);
        }

        private void HandleLeftClick()
        {
            if (_cam == null) return;
            Ray ray = _cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit, 200f, _clickMask, QueryTriggerInteraction.Collide))
                return;

            // Interakcja (NPC, wejścia) ma priorytet nad walką.
            var interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                float dist = Vector3.Distance(transform.position, hit.collider.transform.position);
                if (dist <= interactable.InteractionRange)
                {
                    interactable.Interact(gameObject);
                    _targeting.ClearTarget();
                    return;
                }
            }

            // Wybór wroga
            var stats = hit.collider.GetComponentInParent<CharacterStats>();
            if (stats != null && stats != _stats && !stats.IsDead)
            {
                _targeting.SetTarget(stats);
                return;
            }

            // Klik w puste miejsce = odznaczenie
            _targeting.ClearTarget();
        }

        private static bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private void FaceTarget(Transform t)
        {
            Vector3 dir = new Vector3(t.position.x - transform.position.x, 0f, t.position.z - transform.position.z);
            if (dir.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 12f * Time.deltaTime);
        }
    }
}
