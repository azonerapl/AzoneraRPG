using UnityEngine;
using Azonera.Stats;
using Azonera.Combat;
using Azonera.Loot;

namespace Azonera.Monsters
{
    /// <summary>
    /// Maszyna stanów AI potwora: Idle → Roam → Chase → Attack → Dead.
    /// Konfigurowana w całości przez <see cref="MonsterData"/>. Ruch bez NavMesh
    /// (proste podążanie) — łatwe do podmiany na NavMeshAgent w Fazie optymalizacji.
    /// </summary>
    [RequireComponent(typeof(CharacterStats))]
    [RequireComponent(typeof(MeleeAttacker))]
    public class MonsterAI : MonoBehaviour
    {
        private enum State { Idle, Roam, Chase, Attack, Dead }

        [SerializeField] private MonsterData _data;

        private CharacterStats _stats;
        private MeleeAttacker _attacker;
        private Transform _player;
        private CharacterStats _playerStats;

        private State _state = State.Idle;
        private Vector3 _spawnPoint;
        private Vector3 _roamTarget;
        private float _stateTimer;
        private GameObject _lastAttacker;

        public MonsterData Data => _data;

        public void Initialize(MonsterData data)
        {
            _data = data;
            _stats = GetComponent<CharacterStats>();
            _attacker = GetComponent<MeleeAttacker>();
            _stats.InitializeRaw(
                data.Level, data.MaxHealth, data.Damage, data.Defense,
                data.Armor, 0f, data.MoveSpeed, data.AttackSpeed);
            _spawnPoint = transform.position;
            _stats.OnDamaged += HandleDamaged;
            _stats.OnDied += HandleDied;
        }

        private void Awake()
        {
            if (_stats == null) _stats = GetComponent<CharacterStats>();
            if (_attacker == null) _attacker = GetComponent<MeleeAttacker>();
            _spawnPoint = transform.position;
        }

        private void Start()
        {
            var playerGo = GameObject.FindWithTag("Player");
            if (playerGo != null)
            {
                _player = playerGo.transform;
                _playerStats = playerGo.GetComponent<CharacterStats>();
            }
            if (_data != null && _stats != null && _stats.MaxHealth <= 0f)
                Initialize(_data);
        }

        private void Update()
        {
            if (_state == State.Dead || _data == null || _stats == null) return;
            if (_player == null || _playerStats == null || _playerStats.IsDead)
            {
                Tick_NoPlayer();
                return;
            }

            float distToPlayer = Vector3.Distance(transform.position, _player.position);

            switch (_state)
            {
                case State.Idle:  Tick_Idle(distToPlayer); break;
                case State.Roam:  Tick_Roam(distToPlayer); break;
                case State.Chase: Tick_Chase(distToPlayer); break;
                case State.Attack: Tick_Attack(distToPlayer); break;
            }
        }

        private void Tick_NoPlayer()
        {
            if (_state == State.Chase || _state == State.Attack) _state = State.Idle;
            Tick_Idle(999f);
        }

        private void Tick_Idle(float distToPlayer)
        {
            _stateTimer -= Time.deltaTime;
            if (distToPlayer <= _data.AggroRange) { _state = State.Chase; return; }
            if (_stateTimer <= 0f)
            {
                Vector2 r = Random.insideUnitCircle * _data.RoamRadius;
                _roamTarget = _spawnPoint + new Vector3(r.x, 0f, r.y);
                _state = State.Roam;
            }
        }

        private void Tick_Roam(float distToPlayer)
        {
            if (distToPlayer <= _data.AggroRange) { _state = State.Chase; return; }
            MoveTowards(_roamTarget, 0.6f);
            if (Vector3.Distance(transform.position, _roamTarget) < 0.4f)
            {
                _stateTimer = Random.Range(1.5f, 4f);
                _state = State.Idle;
            }
        }

        private void Tick_Chase(float distToPlayer)
        {
            if (distToPlayer > _data.DeAggroRange)
            {
                _stateTimer = Random.Range(1f, 2.5f);
                _state = State.Idle;
                return;
            }
            if (distToPlayer <= _attacker.AttackRange) { _state = State.Attack; return; }
            MoveTowards(_player.position, 1f);
        }

        private void Tick_Attack(float distToPlayer)
        {
            if (distToPlayer > _attacker.AttackRange * 1.1f) { _state = State.Chase; return; }
            FaceTowards(_player.position);
            var dmg = _playerStats as IDamageable;
            _attacker.TryAttack(dmg);
        }

        private void MoveTowards(Vector3 target, float speedScale)
        {
            Vector3 flatTarget = new Vector3(target.x, transform.position.y, target.z);
            Vector3 dir = (flatTarget - transform.position);
            if (dir.sqrMagnitude < 0.0001f) return;
            dir.Normalize();
            float speed = _stats.GetStat(StatType.MoveSpeed) * speedScale;
            transform.position += dir * speed * Time.deltaTime;
            FaceTowards(target);
        }

        private void FaceTowards(Vector3 target)
        {
            Vector3 dir = new Vector3(target.x - transform.position.x, 0f, target.z - transform.position.z);
            if (dir.sqrMagnitude < 0.0001f) return;
            Quaternion rot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, 10f * Time.deltaTime);
        }

        private void HandleDamaged(DamageInfo info)
        {
            if (info.Source != null) _lastAttacker = info.Source;
            if (_state == State.Idle || _state == State.Roam) _state = State.Chase;
        }

        private void HandleDied(CharacterStats stats)
        {
            if (_state == State.Dead) return;
            _state = State.Dead;

            // Nagroda EXP dla zabójcy
            if (_lastAttacker != null)
            {
                var killerStats = _lastAttacker.GetComponent<CharacterStats>();
                if (killerStats != null) killerStats.AddExperience(_data.ExperienceReward);
            }

            // Loot
            if (_data.LootTable != null)
            {
                foreach (var roll in _data.LootTable.RollLoot())
                    ItemPickup.Spawn(roll.Item, roll.Count, transform.position);
            }

            // Efekt śmierci — na razie proste zniknięcie po chwili
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
            Destroy(gameObject, 1.5f);
        }
    }
}
