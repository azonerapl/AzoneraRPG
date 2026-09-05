using UnityEngine;
using UnityEngine.InputSystem;

namespace Azonera.CameraSystem
{
    /// <summary>
    /// Profesjonalna kamera izometryczna: płynne śledzenie celu, zoom kółkiem myszy,
    /// obrót Q/E, stały kąt patrzenia z góry. Gotowa pod ograniczenia obszaru mapy.
    /// </summary>
    public class IsometricCameraController : MonoBehaviour
    {
        [Header("Cel")]
        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _targetOffset = new Vector3(0f, 1f, 0f);

        [Header("Kąt")]
        [SerializeField] private float _pitch = 52f;     // nachylenie w dół
        [SerializeField] private float _yaw = 45f;       // obrót poziomy
        [SerializeField] private float _rotateSpeed = 90f;

        [Header("Zoom")]
        [SerializeField] private float _distance = 14f;
        [SerializeField] private float _minDistance = 7f;
        [SerializeField] private float _maxDistance = 24f;
        [SerializeField] private float _zoomSpeed = 4f;

        [Header("Płynność")]
        [SerializeField] private float _followLerp = 10f;
        [SerializeField] private float _zoomLerp = 8f;

        [Header("Ograniczenie obszaru (opcjonalne)")]
        [SerializeField] private bool _useBounds = false;
        [SerializeField] private Vector2 _minXZ = new Vector2(-50f, -50f);
        [SerializeField] private Vector2 _maxXZ = new Vector2(50f, 50f);

        private float _currentDistance;
        private Vector3 _currentFocus;

        public Transform Target { get => _target; set => _target = value; }

        private void Start()
        {
            _currentDistance = _distance;
            if (_target != null) _currentFocus = _target.position + _targetOffset;
        }

        private void LateUpdate()
        {
            HandleInput();

            Vector3 focus = _target != null ? _target.position + _targetOffset : _currentFocus;
            if (_useBounds)
            {
                focus.x = Mathf.Clamp(focus.x, _minXZ.x, _maxXZ.x);
                focus.z = Mathf.Clamp(focus.z, _minXZ.y, _maxXZ.y);
            }

            _currentFocus = Vector3.Lerp(_currentFocus, focus, _followLerp * Time.deltaTime);
            _currentDistance = Mathf.Lerp(_currentDistance, _distance, _zoomLerp * Time.deltaTime);

            Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 position = _currentFocus - (rot * Vector3.forward) * _currentDistance;

            transform.position = position;
            transform.rotation = rot;
        }

        private void HandleInput()
        {
            // Zoom
            if (Mouse.current != null)
            {
                float scroll = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    _distance -= Mathf.Sign(scroll) * _zoomSpeed;
                    _distance = Mathf.Clamp(_distance, _minDistance, _maxDistance);
                }
            }

            // Obrót Q/E
            if (Keyboard.current != null)
            {
                if (Keyboard.current.qKey.isPressed) _yaw -= _rotateSpeed * Time.deltaTime;
                if (Keyboard.current.eKey.isPressed) _yaw += _rotateSpeed * Time.deltaTime;
            }
        }

        public void SetBounds(Vector2 min, Vector2 max)
        {
            _minXZ = min; _maxXZ = max; _useBounds = true;
        }
    }
}
