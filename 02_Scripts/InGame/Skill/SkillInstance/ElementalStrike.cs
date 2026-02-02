using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvival
{
    public class ElementalStrike : MonoBehaviour, ISkill
    {
        [SerializeField] private GameObject _elementalPrefab;

        [SerializeField] private int _prewarmCount = 10;
        private readonly Stack<GameObject> _inactive = new();
        private readonly HashSet<GameObject> _active = new();

        private readonly float _searchRadius = 8f;

        private Team _team;
        private SkillData _skillData;
        private int _damage;
        private float _cooldownTimer;

        public void Init(SkillData skillData, int damage, Team team)
        {
            _skillData = skillData;
            _team = team;
            _damage = damage;
            
            _cooldownTimer = 0f;

            InitElementalStrikePiece();
        }

        private void Update()
        {
            if (_skillData == null) return;

            _cooldownTimer -= Time.deltaTime;
            if (_cooldownTimer > 0f) return;

            var target = InGameManager.Instance.FindEnemy(_searchRadius);
            if (target != null)
            {
                SpawnPiece(target.transform.position);
                _cooldownTimer = _skillData.Cooldown;
            }
        }

        private void InitElementalStrikePiece()
        {
            if (_elementalPrefab == null)
            {
                Logger.LogError($"{GetType()} :: Elemental prefab is null.");
                return;
            }

            for (int i = 0; i < _prewarmCount; i++)
            {
                var go = Instantiate(_elementalPrefab, transform);
                go.SetActive(false);
                _inactive.Push(go);
            }
        }

        private void SpawnPiece(Vector3 worldPos)
        {
            AudioManager.Instance.PlaySFX(SFX.fire_strike);

            var go = Spawn();

            go.transform.SetParent(null, true);
            go.transform.position = worldPos;
            go.transform.rotation = Quaternion.identity;
            
            if (go.TryGetComponent<ElementalStrikePiece>(out var piece))
            {
                piece.Init(_team, _damage);
                piece.Explode();
            }
            else
            {
                Logger.LogWarning($"{GetType()} :: ElementalPiece component not found on spawned prefab.");
            }

            StartCoroutine(DespawnCo(go, 1f));
        }

        private IEnumerator DespawnCo(GameObject go, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (go && _active.Contains(go))
            {
                Despawn(go);
            }
        }
        
        private GameObject Spawn()
        {
            var go = _inactive.Count > 0 ? _inactive.Pop() : Instantiate(_elementalPrefab, transform);
            _active.Add(go);
            go.transform.SetParent(transform, false);
            go.SetActive(true);
            return go;
        }

        private void Despawn(GameObject go)
        {
            if (!go) return;

            if (_active.Contains(go))
                _active.Remove(go);

            go.SetActive(false);
            go.transform.SetParent(transform, false);
            _inactive.Push(go);
        }

    }
}
