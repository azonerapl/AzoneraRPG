using System.Collections.Generic;
using UnityEngine;
using Azonera.Monsters;

namespace Azonera.World
{
    /// <summary>
    /// Punkt spawnu potworów z respawnem — podstawowy budulec każdego łowiska Azonery.
    /// Utrzymuje zadaną liczbę żywych potworów w promieniu; po śmierci odlicza czas
    /// i przywraca stwora. Bez tego lokacja „zużywa się" po jednym przejściu.
    ///
    /// Data-driven: gatunek, liczebność, promień i czas respawnu są konfiguracją,
    /// nie kodem. Encje składa <see cref="MonsterFactory"/> — jedna definicja potwora.
    /// </summary>
    public class MonsterSpawner : MonoBehaviour
    {
        [Header("Co spawnować")]
        [SerializeField] private MonsterData _monster;
        [Tooltip("Ilu żywych przedstawicieli utrzymywać jednocześnie.")]
        [SerializeField] private int _count = 2;

        [Header("Gdzie")]
        [Tooltip("Promień losowego rozrzutu wokół tego obiektu.")]
        [SerializeField] private float _radius = 4f;
        [Tooltip("Wysokość, na której osadzany jest potwór.")]
        [SerializeField] private float _groundY = 1f;

        [Header("Respawn")]
        [Tooltip("Sekundy od śmierci do ponownego pojawienia się.")]
        [SerializeField] private float _respawnDelay = 20f;
        [Tooltip("Losowy rozrzut czasu respawnu (±sekundy), by grupy nie wracały równo.")]
        [SerializeField] private float _respawnJitter = 5f;
        [Tooltip("Spawn startowy od razu przy starcie sceny.")]
        [SerializeField] private bool _spawnOnStart = true;

        private readonly List<GameObject> _alive = new List<GameObject>();
        private readonly List<float> _pendingRespawns = new List<float>();

        public MonsterData Monster => _monster;
        public int AliveCount => _alive.Count;

        /// <summary>Konfiguracja z kodu (generatory scen, przyszły edytor świata).</summary>
        public void Configure(MonsterData monster, int count, float radius, float respawnDelay)
        {
            _monster = monster;
            _count = Mathf.Max(1, count);
            _radius = Mathf.Max(0f, radius);
            _respawnDelay = Mathf.Max(1f, respawnDelay);
        }

        private void Start()
        {
            if (!_spawnOnStart || _monster == null) return;
            for (int i = 0; i < _count; i++) SpawnOne();
        }

        private void Update()
        {
            if (_pendingRespawns.Count == 0) return;
            for (int i = _pendingRespawns.Count - 1; i >= 0; i--)
            {
                if (Time.time < _pendingRespawns[i]) continue;
                _pendingRespawns.RemoveAt(i);
                SpawnOne();
            }
        }

        private void SpawnOne()
        {
            if (_monster == null) return;
            if (_alive.Count >= _count) return;

            Vector2 offset = Random.insideUnitCircle * _radius;
            Vector3 pos = new Vector3(transform.position.x + offset.x, _groundY, transform.position.z + offset.y);

            var go = MonsterFactory.Create(_monster, pos, transform);
            if (go == null) return;

            var ai = go.GetComponent<MonsterAI>();
            if (ai != null) ai.OnDespawned += HandleDespawned;

            _alive.Add(go);
        }

        private void HandleDespawned(MonsterAI ai)
        {
            if (ai != null) ai.OnDespawned -= HandleDespawned;

            var go = ai != null ? ai.gameObject : null;
            _alive.Remove(go);
            // Sprzątamy ewentualne martwe wpisy (scena mogła zniszczyć obiekt inaczej).
            _alive.RemoveAll(x => x == null);

            float delay = Mathf.Max(1f, _respawnDelay + Random.Range(-_respawnJitter, _respawnJitter));
            _pendingRespawns.Add(Time.time + delay);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.9f, 0.35f, 0.25f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}
