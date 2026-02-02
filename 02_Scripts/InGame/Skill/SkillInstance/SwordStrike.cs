using System.Collections;
using System.Collections.Generic;
using PixelSurvival;
using UnityEngine;

namespace PixelSurvival
{
    public class SwordStrike : MonoBehaviour, ISkill
    {
        [SerializeField] private List<Sprite> _projectileSprites;
        
        private Team _team;
        private SkillData _skillData;
        private int _damage;
        private float _cooldownTimer;
        
        private readonly float _searchRadius = 8f;
        private readonly Vector2 _projectileSize = new Vector2(2.5f, 1.5f);
        
        public void Init(SkillData skillData, int damage, Team team)
        {
            _skillData = skillData;
            _team = team;
            _damage = damage;
            _cooldownTimer = 0f;
        }
        
        private void Update()
        {
            if (_skillData == null) return;

            _cooldownTimer -= Time.deltaTime;
            if (_cooldownTimer > 0f) return;
            
            var target = InGameManager.Instance.FindEnemy(_searchRadius);
            if (target != null)
            {
                Fire();
                _cooldownTimer = _skillData.Cooldown;
            }
        }

        private void Fire()
        {
            int count = Mathf.Max(1, _skillData.ObjectCount);
            const float spreadDegrees = 15f;
            
            var enemy = InGameManager.Instance.FindEnemy(float.MaxValue);
            if (enemy == null)
                return;
            
            AudioManager.Instance.PlaySFX(SFX.shuriken);
            
            Vector2 baseDir;
            Vector3 toEnemy = enemy.transform.position - transform.position;
            baseDir = new Vector2(toEnemy.x, toEnemy.y);

            if (baseDir.sqrMagnitude < 0.0001f)
                baseDir = Vector2.right;

            baseDir = baseDir.normalized;

            if (count == 1)
            {
                SpawnOne(baseDir);
                return;
            }

            float step = spreadDegrees / (count - 1);
            float start = -spreadDegrees * 0.5f;

            for (int i = 0; i < count; i++)
            {
                float deg = start + step * i;
                Vector2 dir = Rotate(baseDir, deg).normalized;
                SpawnOne(dir);
            }
        }


        private void SpawnOne(Vector2 dir)
        {
            Vector3 spawnPos = transform.position + (Vector3)(dir * 0.2f);

            var proj = PoolManager.Instance.Spawn<Projectile>(PoolType.Projectile, spawnPos, Quaternion.identity);
            if (!proj) return;

            proj.Init(dir, _projectileSprites[0], _damage, 8, _team, true, scaleValue:_projectileSize);
        }

        private Vector2 Rotate(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector2(
                v.x * cos - v.y * sin,
                v.x * sin + v.y * cos
            );
        }
    }
}