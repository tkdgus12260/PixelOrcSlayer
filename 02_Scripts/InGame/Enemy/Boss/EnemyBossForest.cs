using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace PixelSurvival
{
    public class EnemyBossForest : BaseEnemy, IDamageable
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

        [Header("Melee")]
        [SerializeField] private MeleeAttack _meleeAttack;

        private readonly WaitForSeconds _meleeAttackCorrection = new WaitForSeconds(0.2f);
        private readonly WaitForSeconds _meleeAttackDuration   = new WaitForSeconds(0.3f);

        private float _nextAttackTime;
        private float _meleeRadius;

        [Header("Skill")]
        [SerializeField] private RushAttack _rushAttack;
        [SerializeField] private Collider2D _rushSkillCollider;

        private readonly int _skillDamage = 15;
        private readonly float _skillInterval = 5f;
        private readonly float _skillWindup   = 1f;
        private readonly float _skillRecover  = 0.5f;

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

            if (_meleeAttack)
            {
                if (meleeCollider) meleeCollider.enabled = false;

                float r = _meleeAttack.transform.localPosition.magnitude;
                _meleeRadius = r;
            }

            _isCastingSkill = false;
            _skillCo = null;

            // 첫 스킬 예약
            _nextSkillTime = Time.time + _skillInterval;
        }

        /// <summary>
        /// 데이터 설정
        /// </summary>
        public override void SetInfo(BaseEnemyData enemyData)
        {
            base.SetInfo(enemyData);

            // 세팅 끝났으니 활성화
            gameObject.SetActive(true);

            agent.speed = moveSpeed;
            _currentHP = MaxHP;
            _nextAttackTime = 0f;

            // 근접/스킬 공격 컴포넌트에 데미지/팀 세팅
            _meleeAttack?.Init(attackDamage, Team);
            _rushAttack?.Init(_skillDamage, Team);

            // 스킬 콜라이더 자동 연결(없으면 SkillAttack에서 가져오기)
            if (_rushSkillCollider == null)
            {
                _rushSkillCollider = _rushAttack?.GetComponent<Collider2D>();
                if (_rushSkillCollider) _rushSkillCollider.enabled = false;
            }
        }

        /// <summary>
        /// 행동 업데이트
        /// </summary>
        protected override void Update()
        {
            // 일시정지 / 타겟 없음 / 스킬 중이면 동작 중단
            if (GameManager.Instance.IsPaused) return;
            if (target == null) return;
            if (_isCastingSkill) return;

            // BaseEnemy의 기본 이동/공격 상태 업데이트
            base.Update();

            // 스킬 쿨타임 도달 시 스킬 코루틴 시작
            if (Time.time >= _nextSkillTime && _skillCo == null)
            {
                _skillCo = StartCoroutine(RushToTargetCorutine());
            }
        }

        /// <summary>
        /// 기본 공격
        /// </summary>
        public override void OnAttack()
        {
            if (_isCastingSkill) return;
            if (target == null) return;
            if (Time.time < _nextAttackTime) return;

            _nextAttackTime = Time.time + attackSpeed;
            
            base.OnAttack();

            StartCoroutine(MeleeAttack());
        }

        /// <summary>
        /// 타겟 위치로 근접 콜라이더 재설정 및 Hit처리
        /// </summary>
        private IEnumerator MeleeAttack()
        {
            if (_meleeAttack == null) yield break;
            
            OnStop();
            _isCastingSkill = true;
            
            float r = (_meleeRadius > 0f) ? _meleeRadius : _meleeAttack.transform.localPosition.magnitude;

            Vector2 worldDir = (target.position - transform.position);
            if (worldDir.sqrMagnitude < 0.001f) worldDir = Vector2.right;

            // 로컬 방향으로 변환
            Vector2 localDir = transform.InverseTransformDirection(worldDir).normalized;

            if (transform.localScale.x < 0f)
                localDir.x = -localDir.x;

            localDir.Normalize();

            // 히트박스를 타겟 방향으로 r만큼 배치
            _meleeAttack.transform.localPosition = localDir * r;

            // 지정된 타이밍만큼 대기 후 콜라이더 활성/비활성(짧게만 켜서 히트 처리)
            if (meleeCollider != null)
            {
                yield return _meleeAttackCorrection;
                meleeCollider.enabled = true;
                yield return _meleeAttackDuration;
                meleeCollider.enabled = false;
            }

            _isCastingSkill = false;
        }
        
        /// <summary>
        /// 1) 타겟 기준 Distance 만큼 돌진 목표점 계산
        /// 2) 직선으로 벽에 막히면 갈 수 있는 최대 지점으로 절삭 후 텔레그래프 생성
        /// 3) 선딜 대기 후 텔레그래프 숨김
        /// 4) 범위 만큼 돌진
        /// 5) 후딜 후 정상 상태 복귀
        /// </summary>
        private IEnumerator RushToTargetCorutine()
        {
            _isCastingSkill = true;
            OnStop();

            // 근접 히트박스는 스킬 중 비활성
            if (meleeCollider) meleeCollider.enabled = false;

            // 돌진 목표점 계산
            Vector3 startPos = transform.position;
            Vector3 targetPos = target != null ? target.position : startPos;

            Vector3 dir = targetPos - startPos;
            dir.z = 0f;

            // 타겟과 거의 겹치면 임의 방향으로
            if (dir.sqrMagnitude < 0.0001f)
                dir = Vector3.right;

            dir.Normalize();

            float rushDistance = 15f;
            Vector3 desiredPos = startPos + dir * rushDistance;
            desiredPos.z = startPos.z;

            // 목표점이 NavMesh 위에 있도록 스냅
            Vector3 lockedPos = desiredPos;
            if (NavMesh.SamplePosition(desiredPos, out var sampleHit, 8.0f, NavMesh.AllAreas))
                lockedPos = sampleHit.position;
            else
            {
                if (NavMesh.Raycast(startPos, lockedPos, out var rayHit, NavMesh.AllAreas))
                {
                    lockedPos = rayHit.position;
                }
            }

            float rushSpeed = moveSpeed * 2.0f;
            float originalSpeed = agent.speed;
            float skillActiveTime = (rushDistance / rushSpeed) * 1.25f; // 거리, 속도 기반 시간

            // 텔레그래프
            int telegraphId = TelegraphManager.Instance.ShowLine(startPos, lockedPos, _skillWindup, _rushSkillCollider);

            // 선딜
            yield return new WaitForSeconds(_skillWindup);
            
            TelegraphManager.Instance.Hide(telegraphId);

            // 스킬 발동
            animator.SetFloat("Speed", 1);
            animator.speed = 1.5f; // 걷기보다 빠르게 보이도록 애니 속도 증가

            // 스킬 히트 콜라이더 켜기
            if (_rushSkillCollider) _rushSkillCollider.enabled = true;

            agent.speed = rushSpeed;
            agent.SetDestination(lockedPos); // 발동 시점 목표점으로 1회 이동.
            
            yield return new WaitForSeconds(skillActiveTime);
            
            // 원래 속도로 복구
            agent.speed = originalSpeed;
            // 스킬 종료
            animator.SetFloat("Speed", 0);
            animator.speed = 1.0f;

            if (_rushSkillCollider) _rushSkillCollider.enabled = false;

            // 후딜
            yield return new WaitForSeconds(_skillRecover);

            _nextSkillTime = Time.time + _skillInterval;
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
