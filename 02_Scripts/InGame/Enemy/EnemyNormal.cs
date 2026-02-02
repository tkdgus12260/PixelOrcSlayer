using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace PixelSurvival
{
    public class EnemyNormal : BaseEnemy, IDamageable
    {
        public Team Team => Team.Enemy;
        public bool IsInvulnerable { get; set; }

        private int _currentHP;
        private float _nextAttackTime;
        
        private readonly Vector3 _correctionPos = new Vector3(0, 0.5f, 0);
        private readonly WaitForSeconds _meleeAttackCorrection = new WaitForSeconds(0.2f);
        private readonly WaitForSeconds _meleeAttackDuration = new WaitForSeconds(0.3f);
        
        [SerializeField] private MeleeAttack meleeAttack;
        private Collider2D _meleeCol;
        private float _meleeRadius;

        private Vector2 _arrowColSize = new Vector2(0.25f, 0.75f);

        public override void Init(Transform target)
        {
            base.Init(target);
            
            _meleeCol = meleeAttack.GetComponent<Collider2D>();
            float r = meleeAttack.transform.localPosition.magnitude;
            _meleeRadius = Mathf.Max(r, 0.05f);
        }

        public override void SetInfo(BaseEnemyData enemyData)
        {
            base.SetInfo(enemyData);
            
            _currentHP = MaxHP;
            _nextAttackTime = 0f;
            agent.speed = moveSpeed;
            
            switch (enemyType)
            {
                case EnemyType.Warrior:
                    meleeAttack.gameObject.SetActive(true);
                    meleeAttack?.Init(attackDamage, Team);
                    break;
                case EnemyType.Archer:
                case EnemyType.Wizard:
                default:
                    meleeAttack.gameObject.SetActive(false);
                    break;
            }
        }

        public override void OnAttack()
        {
            if(target == null) return;
            if (Time.time < _nextAttackTime) return;
            _nextAttackTime = Time.time + attackSpeed;
            
            base.OnAttack();
            
            switch (enemyType)
            {
                case EnemyType.Warrior:
                    StartCoroutine(MeleeAttack());
                    break;
                case EnemyType.Archer:
                case EnemyType.Wizard:
                    FireProjectile();
                    break;
                default:
                    break;
            }
        }

        private IEnumerator MeleeAttack()
        {
            if (meleeAttack == null) yield break;

            float r = (_meleeRadius > 0f) ? _meleeRadius : meleeAttack.transform.localPosition.magnitude;

            Vector2 worldDir = (target.position - transform.position);
            if (worldDir.sqrMagnitude < 0.001f) worldDir = Vector2.right;
            Vector2 localDir = transform.InverseTransformDirection(worldDir).normalized;
            
            if (transform.localScale.x < 0f)
                localDir.x = -localDir.x;
            
            localDir.Normalize();

            meleeAttack.transform.localPosition = localDir * r;

            if (_meleeCol != null)
            {
                yield return _meleeAttackCorrection;
                _meleeCol.enabled = true;
                yield return _meleeAttackDuration;
                _meleeCol.enabled = false;
            }
            else
            {
                yield return null;
            }
        }

        private void FireProjectile()
        {
            if(projectileSprite == null) return;
            
            Vector3 spawnPos = transform.position + _correctionPos;
            var projectile = PoolManager.Instance.Spawn<Projectile>(PoolType.Projectile, spawnPos, Quaternion.identity);
            
            if (!projectile) return;
            
            Vector3 targetPos = target.position + _correctionPos;
            Vector3 dir = (targetPos - spawnPos).normalized;
            if (dir.sqrMagnitude < Mathf.Epsilon)
                dir = transform.localScale.x < 0 ? Vector3.right : Vector3.left;

            projectile.Init(
                dir,
                projectileSprite,
                attackDamage,
                6,
                Team,
                rotValue: 45,
                colSize: enemyType == EnemyType.Archer?_arrowColSize : default
                );
        }
        
        public void TakeDamage(int damage)
        {
            _currentHP -= damage;
            
            if (_currentHP <= 0)
            {
                OnDie();
            }
        }
    }
}
