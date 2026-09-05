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
    /// po czym gracz automatycznie atakuje zaznaczonego wroga w zasięgu (styl ARPG).
    /// Deleguje zadawanie obrażeń do <see cref="MeleeAttacker"/> — nie duplikuje logiki walki.
    /// </summary>
    [RequireComponent(typeof(MeleeAttacker))]
    [RequireComponent(typeof(CharacterStats))]
    public class PlayerActions : MonoBehaviour
    {
        [SerializeField] private LayerMask _clickMask = ~0;
        [SerializeField] private float _autoAttackFollowRange = 12f;

        private MeleeAttacker _attacker;
        private CharacterStats _stats;
        private UnityEngine.Camera _cam;
        private IDamageable _target;

        public IDamageable CurrentTarget => _target;

        private void Awake()
        {
            _attacker = GetComponent<MeleeAttacker>();
            _stats = GetComponent<CharacterStats>();
            _cam = UnityEngine.Camera.main;
        }

        private void Update()
        {
            if (_stats.IsDead) { _target = null; return; }
            if (UIState.BlockWorldInput) { return; }
            if (_cam == null) _cam = UnityEngine.Camera.main;

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !IsPointerOverUI())
                HandleLeftClick();

            // czyszczenie martwych/oddalonych celów
            if (_target != null)
            {
                if (_target.IsDead ||
                    Vector3.Distance(transform.position, _target.Transform.position) > _autoAttackFollowRange)
                {
                    _target = null;
                }
            }

            // auto-atak w zasięgu
            if (_target != null && _attacker.InRange(_target.Transform))
            {
                FaceTarget(_target.Transform);
                _attacker.TryAttack(_target);
            }
        }

        private void HandleLeftClick()
        {
            if (_cam == null) return;
            Ray ray = _cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit, 200f, _clickMask, QueryTriggerInteraction.Collide))
                return;

            // Interakcja (NPC, wejścia) ma priorytet
            var interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                float dist = Vector3.Distance(transform.position, hit.collider.transform.position);
                if (dist <= interactable.InteractionRange)
                {
                    interactable.Interact(gameObject);
                    _target = null;
                    return;
                }
            }

            // Wybór wroga
            var dmg = hit.collider.GetComponentInParent<IDamageable>();
            if (dmg != null && dmg != (IDamageable)_stats && !dmg.IsDead)
                _target = dmg;
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
