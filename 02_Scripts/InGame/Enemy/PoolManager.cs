using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace PixelSurvival
{
    public enum PoolType
    {
        Enemy,
        Projectile,
    }

    [Serializable]
    public class PoolConfig
    {
        public PoolType Type;
        public string AddressKey;
        [Min(0)] public int InitialCount = 10;
        [Min(0)] public int HardCap = 256;
    }

    public class PoolManager : SingletonBehaviour<PoolManager>
    {
        [Header("Pool Roots")]
        [SerializeField] private Transform enemyPoolRoot;
        [SerializeField] private Transform projectilePoolRoot;

        [Header("Pool Configs")]
        [SerializeField] private List<PoolConfig> poolConfigs = new();

        private readonly Dictionary<PoolType, PoolBase> _pools = new();
        private readonly Dictionary<GameObject, PoolBase> _instanceToPool = new();

        protected override async void Init()
        {
            isDestroyOnLoad = true;
            base.Init();

            if (!enemyPoolRoot || !projectilePoolRoot)
            {
                Logger.LogError($"{GetType()}::Pool roots are not assigned.");
                return;
            }

            foreach (var config in poolConfigs)
            {
                if (string.IsNullOrWhiteSpace(config.AddressKey))
                {
                    Logger.LogError($"{GetType()}::AddressKey is empty for PoolType {config.Type}");
                    continue;
                }
                if (_pools.ContainsKey(config.Type))
                {
                    Logger.LogWarning($"{GetType()}::Duplicate PoolType {config.Type}, skipped.");
                    continue;
                }

                PoolBase pool = null;
                switch (config.Type)
                {
                    case PoolType.Enemy:      pool = new Pool<BaseEnemy>(enemyPoolRoot);     break;
                    case PoolType.Projectile: pool = new Pool<Projectile>(projectilePoolRoot); break;
                    default:
                        Logger.LogError($"{GetType()}::No Pool implementation for {config.Type}");
                        break;
                }

                if (pool != null)
                {
                    var ok = await pool.InitializeAsync(config);
                    if (ok) _pools.Add(config.Type, pool);
                }
            }
        }

        private void OnDestroy()
        {
            foreach (var p in _pools.Values) p.Dispose();
            _pools.Clear();
            _instanceToPool.Clear();
        }
        
        public T Spawn<T>(PoolType type, Vector3 pos, Quaternion rot, Transform parent = null) where T : Component
        {
            if (!_pools.TryGetValue(type, out var pool))
            {
                Logger.LogError($"{GetType()}::No pool for type {type}");
                return null;
            }

            if (pool is Pool<T> typedPool)
            {
                var inst = typedPool.Spawn(pos, rot, parent);
                if (inst != null) _instanceToPool[inst.gameObject] = pool;
                return inst;
            }

            Logger.LogError($"{GetType()}::Pool for {type} is not of type {typeof(T).Name}");
            return null;
        }

        public void Despawn(GameObject go)
        {
            if (!go) return;

            if (_instanceToPool.TryGetValue(go, out var pool))
            {
                pool.Despawn(go);
                _instanceToPool.Remove(go);
            }
            else
            {
                if (go.scene.IsValid())
                {
                    go.SetActive(false);
                    Destroy(go);
                }
            }
        }

        private abstract class PoolBase
        {
            public abstract Task<bool> InitializeAsync(PoolConfig cfg);
            public abstract void Despawn(GameObject go);
            public abstract void Dispose();
        }

        private sealed class Pool<T> : PoolBase where T : Component
        {
            private readonly Transform _root;
            private GameObject _prefab;
            private int _hardCap;

            private readonly Stack<T> _inactive = new();
            private readonly LinkedList<T> _active = new();
            private readonly Dictionary<T, LinkedListNode<T>> _nodeLookup = new();
            private readonly Dictionary<GameObject, T> _goToComp = new();

            public Pool(Transform root) => _root = root;

            public override async Task<bool> InitializeAsync(PoolConfig cfg)
            {
                _hardCap = Mathf.Max(0, cfg.HardCap);
                var operationHandle = Addressables.LoadAssetAsync<GameObject>(cfg.AddressKey);
                await operationHandle.Task;

                if (operationHandle.Status != AsyncOperationStatus.Succeeded || !operationHandle.Result)
                {
                    Logger.LogError($"Pool<{typeof(T).Name}>: Failed to load prefab at '{cfg.AddressKey}'");
                    return false;
                }

                _prefab = operationHandle.Result;

                int prewarm = Mathf.Clamp(cfg.InitialCount, 0, _hardCap == 0 ? int.MaxValue : _hardCap);
                for (int i = 0; i < prewarm; i++) CreateInstance();

                return true;
            }

            private T CreateInstance()
            {
                var go = Instantiate(_prefab, _root);
                var comp = go.GetComponent<T>();
                if (!comp)
                {
                    Logger.LogError($"Pool<{typeof(T).Name}>: Prefab has no {typeof(T).Name}");
                    Destroy(go);
                    return null;
                }

                go.SetActive(false);
                _goToComp[go] = comp;
                _inactive.Push(comp);
                return comp;
            }

            public T Spawn(Vector3 pos, Quaternion rot, Transform parent)
            {
                T inst = null;

                if (_inactive.Count > 0)
                {
                    inst = _inactive.Pop();
                }
                else
                {
                    int total = _inactive.Count + _active.Count;
                    if (_hardCap == 0 || total < _hardCap)
                    {
                        inst = CreateInstance();
                    }
                    else if (_active.Count > 0)
                    {
                        var oldest = _active.First.Value;
                        Despawn(oldest);
                        inst = _inactive.Count > 0 ? _inactive.Pop() : null;
                    }
                }

                if (!inst) return null;

                var t = inst.transform;
                t.SetParent(parent ? parent : _root, false);
                t.SetPositionAndRotation(pos, rot);
                if (!inst.gameObject.activeSelf) inst.gameObject.SetActive(true);

                var node = _active.AddLast(inst);
                _nodeLookup[inst] = node;

                return inst;
            }

            public override void Despawn(GameObject go)
            {
                if (!go) return;

                if (!_goToComp.TryGetValue(go, out var comp) || comp == null)
                {
                    go.SetActive(false);
                    return;
                }

                Despawn(comp);
            }

            private void Despawn(T comp)
            {
                if (comp == null) return;

                if (_nodeLookup.TryGetValue(comp, out var node))
                {
                    _active.Remove(node);
                    _nodeLookup.Remove(comp);
                }

                var go = comp.gameObject;
                if (go.activeSelf) go.SetActive(false);
                go.transform.SetParent(_root, false);
                _inactive.Push(comp);
            }

            public override void Dispose()
            {
                foreach (var live in _active)
                    if (live) live.gameObject.SetActive(false);

                _active.Clear();
                _nodeLookup.Clear();
                _inactive.Clear();
                _goToComp.Clear();

                _prefab = null;
            }
        }
    }
}
