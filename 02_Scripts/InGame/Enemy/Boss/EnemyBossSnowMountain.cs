using System.Collections;
using Gpm.LogViewer.Internal;
using UnityEngine;
using UnityEngine.AI;

namespace PixelSurvival
{
    public class EnemyBossSnowMountain : BaseEnemy, IDamageable
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
        [SerializeField] private float _meleeCorrectionDelay = 0.2f;
        [SerializeField] private float _meleeActiveDuration = 0.3f;

        private WaitForSeconds _meleeCorrectionWait;
        private WaitForSeconds _meleeDurationWait;
        private float _nextAttackTime;
        private float _meleeRadius;

        [Header("Skill Axes (Held)")]
        [SerializeField] private GameObject _heldAxeLeft;
        [SerializeField] private GameObject _heldAxeRight;

        private float _axeColSize = 0f;

        // Throw axe skill
        private readonly int _throwDamage = 20;
        private readonly float _skillInterval = 6f;
        private readonly float _throwWindup = 1.0f;
        private readonly float _throwRecover = 0.1f;
        private readonly float _projectileSpeed = 15f;
        private readonly float _throwTelegraphMaxDistance = 20f;
        private readonly Vector3 _spawnCorrection = new Vector3(0f, 1f, 0f);
        private readonly Vector3 _targetCorrection = new Vector3(0f, 0.5f, 0f);

        private readonly float _pickupEnableDelay = 20f;

        // Rush skill
        [SerializeField] private RushAttack _rushAttack;
        [SerializeField] private Collider2D _rushSkillCollider;
        private readonly int _rushDamage = 25;
        private readonly float _rushDistance = 15f;
        private readonly float _rushWindup = 1.0f;
        private readonly float _rushRecover = 0.5f;

        private float _nextSkillTime;
        private bool _isCastingSkill;
        private Coroutine _skillCo;

        private bool _throwRightNext = true;

        [SerializeField] private AxeProjectile _rightProjectile;
        [SerializeField] private AxeProjectile _leftProjectile;

        public override void Init(Transform target)
        {
            base.Init(target);
            gameObject.SetActive(false);

            _meleeCorrectionWait = new WaitForSeconds(_meleeCorrectionDelay);
            _meleeDurationWait = new WaitForSeconds(_meleeActiveDuration);

            if (_meleeAttack)
            {
                if (meleeCollider) meleeCollider.enabled = false;

                float r = _meleeAttack.transform.localPosition.magnitude;
                _meleeRadius = Mathf.Max(r, 0.05f);
            }

            _isCastingSkill = false;
            _skillCo = null;
            _nextSkillTime = Time.time + _skillInterval;

            _throwRightNext = true;

            _axeColSize = GetAxeColliderSize();
        }

        private float GetAxeColliderSize()
        {
            float defaultSize = 0.8f;

            Collider2D col = null;

            if (_rightProjectile != null)
            {
                col = _rightProjectile.GetComponent<Collider2D>();
            }

            if (col is CapsuleCollider2D capsuleCollider)
            {
                float baseSize = Mathf.Max(capsuleCollider.size.x, capsuleCollider.size.y);
                if (baseSize <= 0.0001f)
                {
                    return defaultSize;
                }
                
                Vector3 ls = col.transform.lossyScale;
                float worldX = baseSize * Mathf.Abs(ls.x);
                float worldY = baseSize * Mathf.Abs(ls.y);

                float worldSize = Mathf.Max(worldX, worldY);
                return worldSize;
            }

            return defaultSize;
        }


        public override void SetInfo(BaseEnemyData enemyData)
        {
            base.SetInfo(enemyData);
            gameObject.SetActive(true);

            agent.speed = moveSpeed;
            _currentHP = MaxHP;
            _nextAttackTime = 0f;

            _meleeAttack?.Init(attackDamage, Team);
            _rushAttack?.Init(_rushDamage, Team);
            
            if (_rushSkillCollider) _rushSkillCollider.enabled = false;
        }

        protected override void Update()
        {
            if (GameManager.Instance.IsPaused) return;
            if (target == null) return;
            if (_isCastingSkill) return;

            base.Update();
            
            if (Time.time >= _nextSkillTime && _skillCo == null)
            {
                bool hasRight = (_heldAxeRight != null && _heldAxeRight.activeSelf);
                bool hasLeft = (_heldAxeLeft != null && _heldAxeLeft.activeSelf);
                
                if (!hasRight && !hasLeft)
                {
                    _skillCo = StartCoroutine(RushToTargetCorutine());
                }
                else
                {
                    _skillCo = StartCoroutine(ThrowAxeCorutine());
                }
            }
        }

        public override void OnAttack()
        {
            if (_isCastingSkill) return;
            if (target == null) return;
            if (Time.time < _nextAttackTime) return;

            _nextAttackTime = Time.time + attackSpeed;

            base.OnAttack();
            
            StartCoroutine(MeleeAttackCo());
        }

        private IEnumerator MeleeAttackCo()
        {
            if (_meleeAttack == null) yield break;
            
            OnStop();
            _isCastingSkill = true;
            
            float r = (_meleeRadius > 0f) ? _meleeRadius : _meleeAttack.transform.localPosition.magnitude;

            Vector2 worldDir = (target.position - transform.position);
            if (worldDir.sqrMagnitude < 0.001f) worldDir = Vector2.right;

            Vector2 localDir = transform.InverseTransformDirection(worldDir).normalized;

            if (transform.localScale.x < 0f)
                localDir.x = -localDir.x;

            localDir.Normalize();
            _meleeAttack.transform.localPosition = localDir * r;

            if (meleeCollider != null)
            {
                yield return _meleeCorrectionWait;
                meleeCollider.enabled = true;
                yield return _meleeDurationWait;
                meleeCollider.enabled = false;
            }
            
            _isCastingSkill = false;
        }

        /// <summary>
        /// 1)
        /// </summary>
        private IEnumerator ThrowAxeCorutine()
        {
            OnStop();

            bool canThrowRight = (_heldAxeRight != null && _heldAxeRight.activeSelf);
            bool canThrowLeft = (_heldAxeLeft != null && _heldAxeLeft.activeSelf);
            
            bool throwRight;
            if (_throwRightNext)
                throwRight = canThrowRight ? true : canThrowLeft;
            else
                throwRight = canThrowLeft ? false : canThrowRight;

            if (!canThrowRight && !canThrowLeft)
            {
                EndSkillCommon();
                yield break;
            }
            
            AxeProjectile proj = throwRight ? _rightProjectile : _leftProjectile;
            if (proj == null)
            {
                EndSkillCommon();
                yield break;
            }
            
            // 스폰 위치
            Vector3 bossPos = transform.position;
            Vector3 spawn = bossPos + _spawnCorrection;
            spawn.z = bossPos.z;
            spawn += throwRight ? Vector3.right * 0.25f : Vector3.left * 0.25f;

            // 타겟 방향 고정
            Vector3 aimPos = (target != null) ? (target.position + _targetCorrection) : (spawn + (Vector3)GetForward2D());
            aimPos.z = spawn.z;

            Vector2 aimDir = (aimPos - spawn);
            if (aimDir.sqrMagnitude < 0.0001f)
                aimDir = GetForward2D();
            aimDir.Normalize();

            // 1) 손 도끼 비활성
            if (throwRight) { if (_heldAxeRight) _heldAxeRight.SetActive(false); }
            else { if (_heldAxeLeft) _heldAxeLeft.SetActive(false); }

            Vector3 desiredEnd = spawn + (Vector3)(aimDir * _throwTelegraphMaxDistance);
            desiredEnd.z = spawn.z;

            Vector3 snappedEnd = desiredEnd;

            if (NavMesh.SamplePosition(desiredEnd, out var sampleHit, 8.0f, NavMesh.AllAreas))
            {
                snappedEnd = sampleHit.position;
                snappedEnd.z = spawn.z;
            }
            if (NavMesh.Raycast(spawn, snappedEnd, out var rayHit, NavMesh.AllAreas))
            {
                snappedEnd = rayHit.position;
                snappedEnd.z = spawn.z;
            }

            Vector3 end = snappedEnd;

            int telegraphId = -1;
            if (TelegraphManager.Instance != null)
                telegraphId = TelegraphManager.Instance.ShowLine(spawn, end, _throwWindup, _axeColSize);

            // 선딜
            yield return new WaitForSeconds(_throwWindup);

            if (telegraphId >= 0 && TelegraphManager.Instance != null)
                TelegraphManager.Instance.Hide(telegraphId);

            // 투사체 활성화
            proj.transform.SetParent(null, true);
            proj.transform.position = spawn;
            proj.transform.rotation = Quaternion.identity;
            proj.gameObject.SetActive(true);

            proj.Init(
                owner: transform,
                team: Team,
                damage: _throwDamage,
                initialVelocity: aimDir * _projectileSpeed,
                pickupEnableDelay: _pickupEnableDelay,
                canPickupPredicate: () => !_isCastingSkill,
                onPickedUp: () =>
                {
                    proj.gameObject.SetActive(false);
                    proj.transform.SetParent(transform, false);
                    proj.transform.localPosition = Vector3.zero;
                    proj.transform.localRotation = Quaternion.identity;

                    if (throwRight)
                    {
                        if (_heldAxeRight) _heldAxeRight.SetActive(true);
                    }
                    else
                    {
                        if (_heldAxeLeft) _heldAxeLeft.SetActive(true);
                    }
                }
            );

            _throwRightNext = !_throwRightNext;

            yield return new WaitForSeconds(_throwRecover);
            EndSkillCommon();
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

            if (meleeCollider) meleeCollider.enabled = false;

            Vector3 startPos = transform.position;
            Vector3 targetPos = (target != null) ? target.position : startPos;

            Vector3 dir = targetPos - startPos;
            dir.z = 0f;
            
            if (dir.sqrMagnitude < 0.0001f)
                dir = Vector3.right;
            
            dir.Normalize();

            Vector3 desiredPos = startPos + dir * _rushDistance;
            desiredPos.z = startPos.z;

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
            float skillActiveTime = (_rushDistance / rushSpeed) * 1.25f; // 거리, 속도 기반 시간
            
            int telegraphId = TelegraphManager.Instance.ShowLine(startPos, lockedPos, _rushWindup, _rushSkillCollider);
            
            yield return new WaitForSeconds(_rushWindup);

            TelegraphManager.Instance.Hide(telegraphId);
            
            animator.SetFloat("Speed", 1);
            animator.speed = 1.5f;

            if (_rushSkillCollider) _rushSkillCollider.enabled = true;

            // 목표점이 의미 있을 때만 NavMeshAgent로 이동
            if ((lockedPos - startPos).sqrMagnitude > 0.01f)
            {
                agent.speed = rushSpeed;
                agent.SetDestination(lockedPos); // 발동 시점 목표점으로 1회 이동.
                
                // 벽에 막혀도 돌진 모션 유지
                float t = 0f;
                while (t < skillActiveTime)
                {
                    t += Time.deltaTime;
                    yield return null;
                }
                // 원래 속도로 복구
                agent.speed = originalSpeed;
            }
            else
            {
                // 거의 움직일 수 없는 상황이면 제자리에서 돌진
                float t = 0f;
                while (t < skillActiveTime)
                {
                    t += Time.deltaTime;
                    yield return null;
                }
            }

            // 스킬 종료
            animator.SetFloat("Speed", 0);
            animator.speed = 1.0f;

            if (_rushSkillCollider) _rushSkillCollider.enabled = false;

            // 후딜
            yield return new WaitForSeconds(_rushRecover);

            EndSkillCommon();
        }

        private void EndSkillCommon()
        {
            _nextSkillTime = Time.time + Mathf.Max(0.1f, _skillInterval);
            _isCastingSkill = false;
            _skillCo = null;
        }

        private Vector2 GetForward2D()
        {
            return (transform.localScale.x < 0f) ? Vector2.left : Vector2.right;
        }

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
