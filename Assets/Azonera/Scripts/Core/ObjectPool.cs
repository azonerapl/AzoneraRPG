using System.Collections.Generic;
using UnityEngine;

namespace Azonera.Core
{
    /// <summary>
    /// Lekka pula obiektów dla elementów tworzonych masowo w runtime
    /// (liczby obrażeń, efekty trafień, pociski, loot).
    /// Zamiast Instantiate/Destroy co klatkę — recykling gotowych instancji.
    /// </summary>
    /// <typeparam name="T">Komponent na korzeniu obiektu z puli.</typeparam>
    public class ObjectPool<T> where T : Component
    {
        private readonly System.Func<T> _factory;
        private readonly Stack<T> _idle = new Stack<T>();
        private readonly Transform _parent;
        private readonly int _maxIdle;

        public int IdleCount => _idle.Count;

        /// <param name="factory">Tworzy nową instancję, gdy pula jest pusta.</param>
        /// <param name="parent">Rodzic dla uśpionych instancji (porządek w hierarchii).</param>
        /// <param name="prewarm">Ile instancji utworzyć z góry.</param>
        /// <param name="maxIdle">Górny limit uśpionych instancji — nadmiar jest niszczony.</param>
        public ObjectPool(System.Func<T> factory, Transform parent = null, int prewarm = 0, int maxIdle = 64)
        {
            _factory = factory;
            _parent = parent;
            _maxIdle = Mathf.Max(1, maxIdle);
            for (int i = 0; i < prewarm; i++) Release(CreateInstance());
        }

        private T CreateInstance()
        {
            var inst = _factory();
            if (inst != null && _parent != null) inst.transform.SetParent(_parent, false);
            return inst;
        }

        /// <summary>Wyjmuje instancję z puli (lub tworzy nową) i aktywuje ją.</summary>
        public T Get()
        {
            T inst = null;
            // Instancje mogły zostać zniszczone razem ze sceną — pomijamy martwe wpisy.
            while (_idle.Count > 0 && inst == null) inst = _idle.Pop();
            if (inst == null) inst = CreateInstance();
            if (inst != null) inst.gameObject.SetActive(true);
            return inst;
        }

        /// <summary>Zwraca instancję do puli i dezaktywuje ją.</summary>
        public void Release(T instance)
        {
            if (instance == null) return;
            instance.gameObject.SetActive(false);
            if (_idle.Count >= _maxIdle)
            {
                Object.Destroy(instance.gameObject);
                return;
            }
            if (_parent != null) instance.transform.SetParent(_parent, false);
            _idle.Push(instance);
        }

        public void Clear()
        {
            while (_idle.Count > 0)
            {
                var inst = _idle.Pop();
                if (inst != null) Object.Destroy(inst.gameObject);
            }
        }
    }
}
