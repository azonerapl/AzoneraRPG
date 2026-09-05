using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Azonera.Stats;
using Azonera.Inventory;
using Azonera.UI;
using Azonera.CameraSystem;
using Azonera.Save;
using Azonera.Items;

namespace Azonera.Core
{
    /// <summary>
    /// Centralny punkt spajający sceny gry w runtime: wiąże gracza z kamerą i HUD-em,
    /// obsługuje zapis (F5) / wczytanie (F9), respawn po śmierci. Pojedyncza instancja.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Referencje (auto-wykrywane, jeśli puste)")]
        [SerializeField] private GameObject _player;
        [SerializeField] private HUDController _hud;
        [SerializeField] private IsometricCameraController _camera;

        [Header("Respawn")]
        [SerializeField] private float _respawnDelay = 3f;

        private CharacterStats _stats;
        private Azonera.Inventory.Inventory _inventory;
        private Vector3 _spawnPoint;

        public GameObject Player => _player;
        public CharacterStats PlayerStats => _stats;
        public Azonera.Inventory.Inventory PlayerInventory => _inventory;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            ItemRegistry.EnsureLoaded();
            ResolveReferences();
            if (_player != null) _spawnPoint = _player.transform.position;

            if (_camera != null && _player != null) _camera.Target = _player.transform;
            if (_hud != null && _stats != null) _hud.Bind(_stats, _inventory);
            if (_stats != null) _stats.OnDied += HandlePlayerDied;

            Debug.Log("[Azonera] GameManager gotowy. F5 = zapis, F9 = wczytaj.");
        }

        private void ResolveReferences()
        {
            if (_player == null) _player = GameObject.FindWithTag("Player");
            if (_player != null)
            {
                _stats = _player.GetComponent<CharacterStats>();
                _inventory = _player.GetComponent<Azonera.Inventory.Inventory>();
            }
            if (_hud == null) _hud = FindFirstObjectByType<HUDController>();
            if (_camera == null) _camera = FindFirstObjectByType<IsometricCameraController>();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.f5Key.wasPressedThisFrame) SaveGame();
            if (kb.f9Key.wasPressedThisFrame) LoadGame();
        }

        public void SaveGame()
        {
            if (_player == null) return;
            SaveSystem.Save(_player.transform, _stats, _inventory);
        }

        public void LoadGame()
        {
            var data = SaveSystem.Load();
            if (data == null) { Debug.Log("[Save] Brak zapisu."); return; }
            SaveSystem.Apply(data, _player.transform, _stats, _inventory);
        }

        private void HandlePlayerDied(CharacterStats s)
        {
            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            Debug.Log("[Azonera] Gracz poległ. Respawn za chwilę...");
            yield return new WaitForSeconds(_respawnDelay);
            if (_player != null)
            {
                _player.transform.position = _spawnPoint;
                var rb = _player.GetComponent<Rigidbody>();
                if (rb != null) rb.linearVelocity = Vector3.zero;
            }
            if (_stats != null) _stats.ReviveFull();
            var pc = _player != null ? _player.GetComponent<Player.PlayerController>() : null;
            if (pc != null) pc.SetMovementLocked(false);
        }
    }
}
