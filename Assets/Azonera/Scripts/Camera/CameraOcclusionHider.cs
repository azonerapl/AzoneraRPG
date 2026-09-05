using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Azonera.CameraSystem
{
    /// <summary>
    /// Ukrywa geometrię zasłaniającą gracza (ściany, dachy, kolumny) — obowiązkowa mechanika
    /// widoku top-down/izometrycznego: gracz NIGDY nie może zniknąć za bryłą budynku.
    ///
    /// Zasłaniacz nie jest wyłączany, tylko przełączany w tryb „tylko cienie": obiekt przestaje
    /// być rysowany, ale nadal rzuca cień i uczestniczy w oświetleniu — dzięki temu wnętrze
    /// zachowuje spójny nastrój zamiast rozświetlać się dziurą w geometrii.
    ///
    /// Świadomie NIE ruszamy podłoża ani samego gracza. Test jest tani (SphereCastNonAlloc
    /// co kilka klatek), więc koszt nie rośnie z wielkością świata.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class CameraOcclusionHider : MonoBehaviour
    {
        private const int MaxHits = 24;

        [Header("Cel")]
        [SerializeField] private Transform _target;
        [Tooltip("Punkt na postaci, do którego prowadzimy test (mniej więcej klatka piersiowa).")]
        [SerializeField] private float _targetHeight = 1.2f;

        [Header("Test zasłonięcia")]
        [Tooltip("Promień sondy — szerszy niż postać, żeby złapać też krawędzie ścian.")]
        [SerializeField] private float _probeRadius = 0.55f;
        [Tooltip("Warstwy traktowane jako możliwe zasłony.")]
        [SerializeField] private LayerMask _occluderMask = ~0;
        [Tooltip("Co ile sekund sprawdzać zasłonięcie.")]
        [SerializeField] private float _checkInterval = 0.08f;
        [Tooltip("Obiekty o nazwach zawierających te fragmenty nigdy nie są ukrywane.")]
        [SerializeField]
        private string[] _neverHide = { "Floor", "Terrain", "Carpet", "Ground" };

        private readonly RaycastHit[] _hits = new RaycastHit[MaxHits];
        private readonly HashSet<Renderer> _hidden = new HashSet<Renderer>();
        private readonly HashSet<Renderer> _blockingNow = new HashSet<Renderer>();
        private readonly List<Renderer> _toRestore = new List<Renderer>();
        private readonly Dictionary<Renderer, ShadowCastingMode> _originalModes =
            new Dictionary<Renderer, ShadowCastingMode>();

        private float _nextCheck;

        public Transform Target { get => _target; set => _target = value; }

        private void Start()
        {
            if (_target != null) return;
            var player = GameObject.FindWithTag("Player");
            if (player != null) _target = player.transform;
        }

        private void LateUpdate()
        {
            if (_target == null || Time.time < _nextCheck) return;
            _nextCheck = Time.time + _checkInterval;
            UpdateOccluders();
        }

        private void UpdateOccluders()
        {
            _blockingNow.Clear();

            Vector3 origin = transform.position;
            Vector3 focus = _target.position + Vector3.up * _targetHeight;
            Vector3 delta = focus - origin;
            float distance = delta.magnitude;
            if (distance < 0.05f) return;

            int count = Physics.SphereCastNonAlloc(origin, _probeRadius, delta / distance,
                                                   _hits, distance, _occluderMask,
                                                   QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                var col = _hits[i].collider;
                if (col == null || IsExempt(col)) continue;

                var renderers = col.GetComponentsInChildren<Renderer>();
                for (int r = 0; r < renderers.Length; r++)
                {
                    var rend = renderers[r];
                    if (rend == null) continue;
                    _blockingNow.Add(rend);
                    Hide(rend);
                }
            }

            // Przywracamy to, co przestało zasłaniać.
            _toRestore.Clear();
            foreach (var rend in _hidden)
                if (rend == null || !_blockingNow.Contains(rend)) _toRestore.Add(rend);

            for (int i = 0; i < _toRestore.Count; i++) Restore(_toRestore[i]);
        }

        private bool IsExempt(Collider col)
        {
            if (col.transform == _target || col.transform.IsChildOf(_target)) return true;
            if (col.CompareTag("Player")) return true;

            string name = col.gameObject.name;
            for (int i = 0; i < _neverHide.Length; i++)
                if (!string.IsNullOrEmpty(_neverHide[i]) && name.Contains(_neverHide[i])) return true;

            return false;
        }

        private void Hide(Renderer rend)
        {
            if (_hidden.Contains(rend)) return;
            if (!_originalModes.ContainsKey(rend)) _originalModes[rend] = rend.shadowCastingMode;
            rend.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            _hidden.Add(rend);
        }

        private void Restore(Renderer rend)
        {
            _hidden.Remove(rend);
            if (rend == null) { _originalModes.Remove(rend); return; }
            rend.shadowCastingMode = _originalModes.TryGetValue(rend, out var mode)
                ? mode
                : ShadowCastingMode.On;
            _originalModes.Remove(rend);
        }

        private void OnDisable()
        {
            // Nie zostawiamy świata z ukrytą geometrią, gdy komponent gaśnie.
            _toRestore.Clear();
            foreach (var rend in _hidden) _toRestore.Add(rend);
            for (int i = 0; i < _toRestore.Count; i++) Restore(_toRestore[i]);
            _hidden.Clear();
        }
    }
}
