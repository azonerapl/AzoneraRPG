using UnityEngine;
using UnityEngine.InputSystem;
using Azonera.UI;

namespace Azonera.CameraSystem
{
    /// <summary>
    /// Kamera prezentacyjna MMORPG top-down/izometryczna.
    ///
    /// Założenia (świadome, wynikają z kierunku artystycznego Azonery):
    /// • Świat ma STAŁĄ, czytelną orientację — obrót Q/E działa skokowo co 45°, nie płynnym dryfem.
    ///   Dzięki temu gracz zawsze wie, gdzie jest północ, a assety środowiska czyta się tak samo.
    /// • Zoom jest SKOKOWY (poziomy), nie ciągły — powtarzalne kadry, brak przypadkowych ujęć.
    /// • Śledzenie przez SmoothDamp z wyprzedzeniem w kierunku ruchu — gracz nie „ucieka" z kadru.
    /// • Kąt patrzenia lekko rośnie przy oddaleniu — daleki widok pokazuje więcej terenu.
    ///
    /// Zasłanianie gracza przez geometrię obsługuje osobny <see cref="CameraOcclusionHider"/>.
    /// </summary>
    public class IsometricCameraController : MonoBehaviour
    {
        [Header("Cel")]
        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _targetOffset = new Vector3(0f, 1f, 0f);
        [Tooltip("O ile kadr wybiega przed gracza w kierunku ruchu (jednostki świata).")]
        [SerializeField] private float _lookAhead = 1.6f;

        [Header("Kąt")]
        [SerializeField] private float _pitch = 55f;
        [SerializeField] private float _yaw = 45f;
        [Tooltip("Skok obrotu Q/E w stopniach. 45° = klasyczne 8 kierunków izometrii.")]
        [SerializeField] private float _yawStep = 45f;
        [Tooltip("Dodatkowe nachylenie przy maksymalnym oddaleniu (stopnie).")]
        [SerializeField] private float _pitchZoomBoost = 6f;

        [Header("Zoom (poziomy)")]
        [SerializeField] private float _distance = 16f;
        [SerializeField] private float _minDistance = 8f;
        [SerializeField] private float _maxDistance = 26f;
        [Tooltip("Liczba poziomów zoomu między min a max.")]
        [SerializeField] private int _zoomSteps = 5;

        [Header("Płynność")]
        [Tooltip("Czas dojścia kamery do celu (SmoothDamp). Mniej = sztywniej.")]
        [SerializeField] private float _followSmoothTime = 0.12f;
        [SerializeField] private float _zoomLerp = 8f;
        [SerializeField] private float _rotationLerp = 9f;

        [Header("Ograniczenie obszaru (opcjonalne)")]
        [SerializeField] private bool _useBounds = false;
        [SerializeField] private Vector2 _minXZ = new Vector2(-50f, -50f);
        [SerializeField] private Vector2 _maxXZ = new Vector2(50f, 50f);

        private float _currentDistance;
        private float _desiredDistance;
        private float _currentYaw;
        private float _desiredYaw;
        private Vector3 _currentFocus;
        private Vector3 _focusVelocity;
        private Vector3 _lastTargetPos;

        public Transform Target { get => _target; set { _target = value; SnapToTarget(); } }
        /// <summary>Yaw kamery — ruch gracza jest liczony względem niego.</summary>
        public float Yaw => _currentYaw;

        private void Start()
        {
            _desiredDistance = Mathf.Clamp(_distance, _minDistance, _maxDistance);
            _currentDistance = _desiredDistance;
            _desiredYaw = _currentYaw = _yaw;
            SnapToTarget();
        }

        /// <summary>Ustawia kamerę natychmiast na celu (start sceny, teleport, respawn).</summary>
        public void SnapToTarget()
        {
            if (_target == null) return;
            // GameManager może podstawić cel zanim ruszy nasz Start() — dociągamy wartości startowe.
            if (_desiredDistance <= 0f)
            {
                _desiredDistance = Mathf.Clamp(_distance, _minDistance, _maxDistance);
                _currentDistance = _desiredDistance;
                _desiredYaw = _currentYaw = _yaw;
            }
            _currentFocus = _target.position + _targetOffset;
            _lastTargetPos = _target.position;
            _focusVelocity = Vector3.zero;
            ApplyTransform();
        }

        private void LateUpdate()
        {
            HandleInput();

            Vector3 focus = ResolveFocus();
            _currentFocus = Vector3.SmoothDamp(_currentFocus, focus, ref _focusVelocity,
                                               Mathf.Max(0.01f, _followSmoothTime));
            _currentDistance = Mathf.Lerp(_currentDistance, _desiredDistance, _zoomLerp * Time.deltaTime);
            _currentYaw = Mathf.LerpAngle(_currentYaw, _desiredYaw, _rotationLerp * Time.deltaTime);

            ApplyTransform();
        }

        private Vector3 ResolveFocus()
        {
            if (_target == null) return _currentFocus;

            Vector3 focus = _target.position + _targetOffset;

            // Wyprzedzenie w kierunku faktycznego ruchu — kadr „prowadzi" gracza.
            Vector3 motion = _target.position - _lastTargetPos;
            _lastTargetPos = _target.position;
            motion.y = 0f;
            if (motion.sqrMagnitude > 0.000001f && Time.deltaTime > 0f)
            {
                Vector3 dir = motion.normalized;
                float speed = motion.magnitude / Time.deltaTime;
                float weight = Mathf.Clamp01(speed / 6f);
                focus += dir * (_lookAhead * weight);
            }

            if (_useBounds)
            {
                focus.x = Mathf.Clamp(focus.x, _minXZ.x, _maxXZ.x);
                focus.z = Mathf.Clamp(focus.z, _minXZ.y, _maxXZ.y);
            }
            return focus;
        }

        private void ApplyTransform()
        {
            // Im dalej, tym bardziej z góry — daleki kadr pokazuje więcej terenu.
            float zoomT = Mathf.InverseLerp(_minDistance, _maxDistance, _currentDistance);
            float pitch = _pitch + _pitchZoomBoost * zoomT;

            Quaternion rot = Quaternion.Euler(pitch, _currentYaw, 0f);
            transform.position = _currentFocus - (rot * Vector3.forward) * _currentDistance;
            transform.rotation = rot;
        }

        private void HandleInput()
        {
            if (UIState.BlockWorldInput) return;

            if (Mouse.current != null)
            {
                float scroll = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f) StepZoom(scroll > 0f ? -1 : 1);
            }

            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.qKey.wasPressedThisFrame) _desiredYaw -= _yawStep;
            if (kb.eKey.wasPressedThisFrame) _desiredYaw += _yawStep;
        }

        /// <summary>Przesuwa zoom o jeden poziom (+1 = dalej, -1 = bliżej).</summary>
        public void StepZoom(int direction)
        {
            int steps = Mathf.Max(1, _zoomSteps);
            float stepSize = (_maxDistance - _minDistance) / steps;
            _desiredDistance = Mathf.Clamp(_desiredDistance + direction * stepSize, _minDistance, _maxDistance);
        }

        public void SetBounds(Vector2 min, Vector2 max)
        {
            _minXZ = min; _maxXZ = max; _useBounds = true;
        }
    }
}
