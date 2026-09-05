using System;
using System.Collections.Generic;
using UnityEngine;
using Azonera.Stats;

namespace Azonera.Combat
{
    /// <summary>
    /// Jedno źródło prawdy o celu gracza. Trzyma aktualny cel, pilnuje jego ważności
    /// (śmierć, zbyt duży dystans, zniszczenie obiektu) i udostępnia listę wrogów
    /// w zasięgu — na tym opierają się Battle List, ramka celu, auto-atak i czary.
    ///
    /// Świadomie oddzielone od <c>PlayerActions</c> (wejście) i <c>MeleeAttacker</c> (obrażenia):
    /// jedna odpowiedzialność, gotowe pod walidację po stronie serwera w wersji MMO.
    /// </summary>
    public class TargetSystem : MonoBehaviour
    {
        [Header("Zasięgi")]
        [Tooltip("Maksymalny dystans, na jakim cel pozostaje zaznaczony.")]
        [SerializeField] private float _maxTargetRange = 14f;
        [Tooltip("Promień skanowania wrogów do Battle List / przełączania Tab.")]
        [SerializeField] private float _scanRadius = 18f;
        [Tooltip("Co ile sekund odświeżać listę wrogów w pobliżu.")]
        [SerializeField] private float _scanInterval = 0.25f;

        private readonly List<CharacterStats> _nearby = new List<CharacterStats>(16);
        private CharacterStats _target;
        private float _nextScan;

        /// <summary>(nowy cel lub null) — zmiana zaznaczenia.</summary>
        public event Action<CharacterStats> OnTargetChanged;
        /// <summary>Odświeżono listę wrogów w pobliżu.</summary>
        public event Action OnNearbyRefreshed;

        /// <summary>Aktualny cel lub null.</summary>
        public CharacterStats Target => _target;
        public bool HasTarget => _target != null && !_target.IsDead;
        /// <summary>Wrogowie w promieniu skanowania, posortowani od najbliższego.</summary>
        public IReadOnlyList<CharacterStats> Nearby => _nearby;
        public float MaxTargetRange => _maxTargetRange;

        private void Update()
        {
            if (Time.time >= _nextScan)
            {
                _nextScan = Time.time + _scanInterval;
                RefreshNearby();
            }
            ValidateTarget();
        }

        // ---------------------------------------------------------------- API

        public void SetTarget(CharacterStats target)
        {
            if (target == _target) return;
            if (target != null && (target.IsDead || target == GetComponent<CharacterStats>())) return;
            _target = target;
            OnTargetChanged?.Invoke(_target);
        }

        public void ClearTarget()
        {
            if (_target == null) return;
            _target = null;
            OnTargetChanged?.Invoke(null);
        }

        /// <summary>Przełącza na kolejnego wroga w pobliżu (Tab). Zawija się do początku listy.</summary>
        public void CycleTarget()
        {
            RefreshNearby();
            if (_nearby.Count == 0) { ClearTarget(); return; }

            int index = _target != null ? _nearby.IndexOf(_target) : -1;
            int next = (index + 1) % _nearby.Count;
            SetTarget(_nearby[next]);
        }

        /// <summary>Zaznacza najbliższego wroga (bez zawijania) — wygodne pod hotkey.</summary>
        public void TargetNearest()
        {
            RefreshNearby();
            SetTarget(_nearby.Count > 0 ? _nearby[0] : null);
        }

        // ---------------------------------------------------------------- wnętrze

        private void ValidateTarget()
        {
            if (_target == null) return;
            bool invalid = _target.IsDead
                        || _target.transform == null
                        || Vector3.Distance(transform.position, _target.transform.position) > _maxTargetRange;
            if (invalid) ClearTarget();
        }

        private void RefreshNearby()
        {
            _nearby.Clear();
            var self = GetComponent<CharacterStats>();

            // Potwory to jedyne encje z MonsterAI — tanie i jednoznaczne kryterium „wróg".
            var candidates = FindObjectsByType<Azonera.Monsters.MonsterAI>(FindObjectsInactive.Exclude);
            for (int i = 0; i < candidates.Length; i++)
            {
                var stats = candidates[i].GetComponent<CharacterStats>();
                if (stats == null || stats == self || stats.IsDead) continue;
                if (Vector3.Distance(transform.position, stats.transform.position) > _scanRadius) continue;
                _nearby.Add(stats);
            }

            Vector3 origin = transform.position;
            _nearby.Sort((a, b) =>
                Vector3.SqrMagnitude(a.transform.position - origin)
                .CompareTo(Vector3.SqrMagnitude(b.transform.position - origin)));

            OnNearbyRefreshed?.Invoke();
        }
    }
}
