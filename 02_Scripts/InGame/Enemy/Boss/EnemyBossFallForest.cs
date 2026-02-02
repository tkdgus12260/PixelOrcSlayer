using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace PixelSurvival
{
    public class EnemyBossFallForest : BaseEnemy, IDamageable
    {
        public Team Team => Team.Enemy;
        public bool IsInvulnerable { get; set; }

        private int _currentHP;
        public override int CurrentHp => _currentHP;
        public override float CurrentHpPercent
        {
            get
            {
                if (MaxHP <= 0) return 0f;
                return (float)_currentHP / MaxHP;
            }
        }

        // 일반 공격(투사체) 스폰 위치/타겟 보정
        private readonly Vector3 _spawnCorrection  = new Vector3(0, 1f, 0);
        private readonly Vector3 _targetCorrection = new Vector3(0, 0.5f, 0);
        private readonly Vector2 _projectileSize = new Vector2(2f, 2f);
        
        private float _nextAttackTime;

        [Header("Skill Timings")]
        private readonly int _skillDamage = 20;
        private readonly float _skillInterval   = 7f;
        private readonly float _skillWindup     = 1.3f;
        private readonly float _skillActiveTime = 0.5f;
        private readonly float _skillRecover    = 1.0f;

        [Header("Skill Area")]
        private readonly float _skillRadius = 3.5f;

        [Header("Refs")]
        [SerializeField] private LightningBoltPiece _lightningPiece;
        [SerializeField] private Sprite _projectileSprite;

        private float _nextSkillTime;
        private bool _isCastingSkill;
        private Coroutine _skillCo;

        /// <summary>
        /// 초기화
        /// </summary>
        public override void Init(Transform target)
        {
            base.Init(target);

            gameObject.SetActive(false);

            _isCastingSkill = false;
            _skillCo = null;

            _nextSkillTime = Time.time + _skillInterval;
        }

        /// <summary>
        /// 데이터 설정
        /// </summary>
        public override void SetInfo(BaseEnemyData enemyData)
        {
            base.SetInfo(enemyData);
            gameObject.SetActive(true);

            if (_projectileSprite != null)
                projectileSprite = _projectileSprite;

            _currentHP = MaxHP;
            _nextAttackTime = 0f;

            if (_lightningPiece == null)
                _lightningPiece = GetComponentInChildren<LightningBoltPiece>();

            if (_lightningPiece != null)
            {
                _lightningPiece.transform.SetParent(transform);
                _lightningPiece.gameObject.SetActive(false);
            }
            else
            {
                Logger.LogError("LightningBoltPiece is null.");
            }
        }

        /// <summary>
        /// 행동 업데이트
        /// </summary>
        protected override void Update()
        {
            if (GameManager.Instance.IsPaused) return;
            if (target == null) return;
            if (_isCastingSkill) return;

            base.Update();

            if (Time.time >= _nextSkillTime && _skillCo == null)
            {
                _skillCo = StartCoroutine(LightningBoltCorutine());
            }
        }

        /// <summary>
        /// 기본 공격(투사체 3발 부채꼴)
        /// </summary>
        public override void OnAttack()
        {
            if (_isCastingSkill) return;
            if (target == null) return;
            if (Time.time < _nextAttackTime) return;

            _nextAttackTime = Time.time + attackSpeed;

            base.OnAttack();
            FireProjectiles();
        }

        /// <summary>
        /// 타겟 방향으로 3발 발사. 각도는 spreadDegrees로 부채꼴 처리.
        /// </summary>
        private void FireProjectiles()
        {
            if (projectileSprite == null) return;
            if (target == null) return;

            Vector3 spawnPos = transform.position + _spawnCorrection;

            Vector3 targetPos = target.position + _targetCorrection;
            Vector2 baseDir = new Vector2(targetPos.x - spawnPos.x, targetPos.y - spawnPos.y);

            if (baseDir.sqrMagnitude < 0.0001f)
                baseDir = transform.localScale.x < 0 ? Vector2.right : Vector2.left;

            baseDir.Normalize();

            int count = 3;
            float spreadDegrees = 30f;

            float step  = spreadDegrees / (count - 1);
            float start = -spreadDegrees * 0.5f;

            for (int i = 0; i < count; i++)
            {
                float deg = start + step * i;
                Vector2 dir = Rotate(baseDir, deg).normalized;

                var projectile = PoolManager.Instance.Spawn<Projectile>(PoolType.Projectile, spawnPos, Quaternion.identity);
                if (!projectile) continue;

                projectile.Init(
                    dir,
                    projectileSprite,
                    attackDamage,
                    8,
                    Team,
                    scaleValue: _projectileSize
                );
            }
        }

        /// <summary>
        /// 2D 벡터를 degrees 만큼 회전
        /// </summary>
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

        /// <summary>
        /// 1) 타겟 위치를 중심으로 원형 텔레그래프 표시
        /// 2) 선딜 대기
        /// 3) 일정 시간 동안 이펙트 노출 후
        /// 4) 텔레그래프를 숨기고 폭발 판정으로 데미지
        /// 5) 후딜 후 정상 상태 복귀
        /// </summary>
        private IEnumerator LightningBoltCorutine()
        {
            _isCastingSkill = true;
            OnStop();

            // 타겟 기준으로 스킬 중심점 결정
            Vector3 center = (target != null) ? target.position : transform.position;
            center.z = transform.position.z;

            // 텔레그래프는 선딜+활성시간 동안 표시
            int telegraphId = TelegraphManager.Instance.ShowCircle(center, _skillRadius, _skillWindup + _skillActiveTime);
            animator.SetTrigger("OnSkill");
            
            // 선딜
            yield return new WaitForSeconds(_skillWindup);

            if (_lightningPiece != null)
            {
                // 피스를 중심점에 위치시키고 활성화
                _lightningPiece.transform.position = center;
                _lightningPiece.gameObject.SetActive(true);
                _lightningPiece.Init(Team, _skillDamage, _skillRadius);
                
                yield return new WaitForSeconds(_skillActiveTime);

                // 텔레그래프 숨김 후 실제 데미지 판정
                TelegraphManager.Instance.Hide(telegraphId);

                _lightningPiece.Explode(_skillRadius);
            }

            // 후딜
            yield return new WaitForSeconds(_skillRecover);

            // 다음 스킬 예약 및 상태 복구
            if (_lightningPiece != null)
                _lightningPiece.gameObject.SetActive(false);

            _nextSkillTime = Time.time + Mathf.Max(0.1f, _skillInterval);
            _isCastingSkill = false;
            _skillCo = null;
        }

        /// <summary>
        /// 피해 처리
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (IsInvulnerable) return;

            _currentHP -= damage;
            if (_currentHP <= 0)
            {
                _currentHP = 0;
                OnDie();
            }
        }
    }
}
