using UnityEngine;
using UnityEngine.InputSystem;
using Azonera.Stats;
using Azonera.Skills;
using Azonera.UI;

namespace Azonera.Player
{
    /// <summary>
    /// Ruch gracza w konwencji izometrycznej: WASD/strzałki względem orientacji kamery,
    /// z przyspieszeniem/hamowaniem i obrotem w kierunku ruchu. Zasila parametry animatora
    /// (jeśli podpięty) — logika gotowa pod podmianę placeholdera na docelowy model 3D.
    /// Walka i interakcja są w osobnych komponentach.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CharacterStats))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Ruch")]
        [SerializeField] private float _acceleration = 40f;
        [SerializeField] private float _deceleration = 50f;
        [SerializeField] private float _rotationSpeed = 14f;
        [Tooltip("Mnożnik biegu (Shift).")]
        [SerializeField] private float _runMultiplier = 1.5f;

        [Header("Odniesienie kamery")]
        [Tooltip("Transform kamery — bazuje na jej yaw. Jeśli pusty, użyje Camera.main.")]
        [SerializeField] private Transform _cameraTransform;

        [Header("Animacja (opcjonalnie)")]
        [SerializeField] private Animator _animator;

        private Rigidbody _rb;
        private CharacterStats _stats;
        private Vector3 _horizontalVelocity;
        private bool _movementLocked;

        public float CurrentSpeed => _horizontalVelocity.magnitude;
        public bool IsRunning { get; private set; }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _stats = GetComponent<CharacterStats>();
            // Blokujemy przewracanie (X/Z), ale zostawiamy obrót w osi Y dla MoveRotation.
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            if (_cameraTransform == null && UnityEngine.Camera.main != null)
                _cameraTransform = UnityEngine.Camera.main.transform;

            EnsureSkills();
        }

        /// <summary>Gwarantuje graczowi SkillSet i ustawia trudność treningu wg profesji.</summary>
        private void EnsureSkills()
        {
            var skills = GetComponent<SkillSet>();
            if (skills == null) skills = gameObject.AddComponent<SkillSet>();
            if (_stats != null && _stats.Class != null)
                skills.SetDifficulty(Mathf.Max(0.05f, _stats.Class.MeleeSkillDifficulty));
        }

        private void OnEnable()
        {
            if (_stats != null) _stats.OnDied += HandleDied;
        }

        private void OnDisable()
        {
            if (_stats != null) _stats.OnDied -= HandleDied;
        }

        private void Update()
        {
            UpdateAnimator();
        }

        private void FixedUpdate()
        {
            if (_movementLocked || _stats == null || _stats.IsDead || UIState.BlockWorldInput)
            {
                Decelerate();
                ApplyVelocity();
                return;
            }

            Vector2 input = ReadMoveInput();
            IsRunning = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;

            Vector3 desiredDir = CameraRelative(input);
            float maxSpeed = _stats.GetStat(StatType.MoveSpeed) * (IsRunning ? _runMultiplier : 1f);
            Vector3 desiredVel = desiredDir * maxSpeed;

            if (desiredDir.sqrMagnitude > 0.01f)
            {
                _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, desiredVel, _acceleration * Time.fixedDeltaTime);
                RotateTowards(desiredDir);
            }
            else
            {
                Decelerate();
            }

            ApplyVelocity();
        }

        private Vector2 ReadMoveInput()
        {
            var kb = Keyboard.current;
            if (kb == null) return Vector2.zero;
            float x = 0f, y = 0f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) y -= 1f;
            return new Vector2(x, y).normalized;
        }

        private Vector3 CameraRelative(Vector2 input)
        {
            if (input.sqrMagnitude < 0.01f) return Vector3.zero;
            Vector3 fwd = Vector3.forward, right = Vector3.right;
            if (_cameraTransform != null)
            {
                fwd = _cameraTransform.forward; fwd.y = 0f; fwd.Normalize();
                right = _cameraTransform.right; right.y = 0f; right.Normalize();
            }
            return (fwd * input.y + right * input.x).normalized;
        }

        private void RotateTowards(Vector3 dir)
        {
            Quaternion target = Quaternion.LookRotation(dir);
            _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, target, _rotationSpeed * Time.fixedDeltaTime));
        }

        private void Decelerate()
        {
            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, Vector3.zero, _deceleration * Time.fixedDeltaTime);
        }

        private void ApplyVelocity()
        {
            Vector3 v = _rb.linearVelocity;
            _rb.linearVelocity = new Vector3(_horizontalVelocity.x, v.y, _horizontalVelocity.z);
        }

        private void UpdateAnimator()
        {
            if (_animator == null) return;
            float maxSpeed = _stats != null ? _stats.GetStat(StatType.MoveSpeed) : 1f;
            float norm = maxSpeed > 0f ? CurrentSpeed / maxSpeed : 0f;
            _animator.SetFloat("Speed", norm);
            _animator.SetBool("IsRunning", IsRunning);
        }

        public void SetMovementLocked(bool locked) => _movementLocked = locked;

        private void HandleDied(CharacterStats s)
        {
            _movementLocked = true;
            if (_animator != null) _animator.SetTrigger("Death");
        }

        public void SetCamera(Transform cam) => _cameraTransform = cam;
        public void SetAnimator(Animator anim) => _animator = anim;
    }
}
