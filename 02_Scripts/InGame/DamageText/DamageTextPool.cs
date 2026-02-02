using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvival
{
    public class DamageTextPool : SingletonBehaviour<DamageTextPool>
    {
        [SerializeField] private GameObject _prefab;
        [SerializeField] private Transform _parent;
        
        private readonly int _prewarmCount = 50;
        
        private readonly Stack<DamageText> _inactive = new();
        private readonly HashSet<DamageText> _active = new();

        protected override void Init()
        {
            isDestroyOnLoad = true;
            base.Init();

            if (_parent == null)
            {
                var go = new GameObject("DamageTextContainer");
                _parent = go.transform;
            }

            for (int i = 0; i < _prewarmCount; i++)
            {
                var dt = Create();
                dt.gameObject.SetActive(false);
                _inactive.Push(dt);
            }
        }

        private DamageText Create()
        {
            var go = Instantiate(_prefab, _parent);
            var agent = go.GetComponent<DamageText>();
            return agent;
        }

        public void Show(int amount, Vector3 worldPos, Color color)
        {
            var dt = Spawn();
            dt.Play(amount, worldPos, color);
        }

        private DamageText Spawn()
        {
            var dt = _inactive.Count > 0 ? _inactive.Pop() : Create();
            _active.Add(dt);
            dt.transform.SetParent(_parent, true);
            dt.gameObject.SetActive(true);
            return dt;
        }

        public void Despawn(DamageText dt)
        {
            if (!dt) return;
            if (_active.Contains(dt)) _active.Remove(dt);
            dt.gameObject.SetActive(false);
            dt.transform.SetParent(_parent, false);
            _inactive.Push(dt);
        }
    }
}
