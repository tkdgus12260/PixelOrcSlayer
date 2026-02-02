using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvival
{
    public class ElementalOrb : MonoBehaviour, ISkill
    {
        [SerializeField] private GameObject _orbPrefab;
        [SerializeField] private Sprite _elementSprite; 

        private readonly float _searchRadius = 8f;
        private readonly int _prewarm = 12;

        private readonly Stack<GameObject> _inactive = new();
        private readonly HashSet<GameObject> _active = new();

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
            InitElementalOrbPiece();
        }

        private void Update()
        {
            if (_skillData == null) return;

            _cooldownTimer -= Time.deltaTime;
            if (_cooldownTimer > 0f) return;

            int shotCount = _skillData.ObjectCount;
            var targets = InGameManager.Instance.FindEnemies(shotCount, _searchRadius);
            
            if (targets.Count > 0)
            {
                FireBurst(targets, shotCount);
                _cooldownTimer = _skillData.Cooldown;
            }
            else
            {
                _cooldownTimer = 0.25f;
            }
        }

        private void InitElementalOrbPiece()
        {
            if (!_orbPrefab)
            {
                Logger.LogError($"{GetType()} :: Orb prefab missing.");
                return;
            }
            for (int i = 0; i < _prewarm; i++)
            {
                var go = Instantiate(_orbPrefab, transform);
                go.SetActive(false);
                _inactive.Push(go);
            }
        }

        private GameObject Spawn()
        {
            var go = _inactive.Count > 0 ? _inactive.Pop() : Instantiate(_orbPrefab, transform);
            _active.Add(go);
            go.transform.SetParent(transform, false);
            go.SetActive(true);
            return go;
        }

        private void Despawn(GameObject go)
        {
            if (!go) return;
            if (_active.Contains(go)) _active.Remove(go);
            go.SetActive(false);
            go.transform.SetParent(transform, false);
            _inactive.Push(go);
        }

        private void FireBurst(List<BaseEnemy> targets, int shotCount)
        {
            Vector2 baseDir = JoystickInput.Instance.MoveInput.sqrMagnitude > 1e-6f
                ? JoystickInput.Instance.MoveInput.normalized
                : Vector2.up;

            int count = Mathf.Max(1, shotCount);
            float spreadDeg = Mathf.Clamp(14f + 3f * (count - 1), 0f, 48f);
            float step  = (count > 1) ? spreadDeg / (count - 1) : 0f;
            float start = -spreadDeg * 0.5f;

            for (int i = 0; i < count; i++)
            {
                var target = targets[i % targets.Count];
                if (!target) continue;

                float deg = start + step * i;
                Vector2 shotDir = Rotate(baseDir, deg).normalized;
                float lateralSign = (i % 2 == 0) ? 1f : -1f;

                SpawnPiece(target, shotDir, lateralSign);
            }
        }

        private void SpawnPiece(BaseEnemy target, Vector2 initialDir, float lateralSign)
        {
            var go = Spawn();
            go.transform.SetParent(null, true);
            go.transform.position = transform.position;

            if (go.TryGetComponent<ElementalOrbPiece>(out var orb))
            {
                orb.InitOrb(new ElementalOrbPiece.ElementalOrbPieceData
                {
                    Team              = _team,
                    Damage            = _damage,
                    Sprite            = _elementSprite,

                    StartPos          = transform.position,
                    TargetPos         = target.transform.position,
                    TargetTransform   = target.transform,
                    InitialDir        = initialDir,
                    LateralSign       = lateralSign,

                    Speed             = Mathf.Max(0.1f, 5),
                    ExplosionRadius   = 0.7f,
                    ExplosionDuration = 0.08f,

                    OnFinish          = () => Despawn(go)
                });
            }
            else
            {
                Logger.LogError($"{GetType()} :: Orb prefab needs ElementalOrbPiece.");
                Despawn(go);
            }
        }

        private Vector2 Rotate(Vector2 v, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }
    }
}
