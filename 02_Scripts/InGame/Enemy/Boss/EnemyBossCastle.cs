using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace PixelSurvival
{
    public class EnemyBossCastle : BaseEnemy, IDamageable
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
        
        private enum SkillType
        {
            Rush,
            Earthquake,
            COUNT,
        }

        private readonly List<SkillType> _readySkills = new((int)SkillType.COUNT);

        [Header("Melee")]
        [SerializeField] private MeleeAttack _meleeAttack;

        private readonly WaitForSeconds _meleeAttackCorrection = new WaitForSeconds(0.2f);
        private readonly WaitForSeconds _meleeAttackDuration   = new WaitForSeconds(0.3f);
        private float _nextAttackTime;
        private float _meleeRadius;

        [Header("Rush")]
        [SerializeField] private RushAttack _rushAttack;
        [SerializeField] private Collider2D _rushSkillCollider;

        private readonly int _rushDamage = 30;
        private readonly float _rushInterval = 5f;
        private readonly float _rushWindup = 1f;
        private readonly float _rushRecover = 0.5f;
        private readonly int _rushCount = 3;
        private readonly float _rushDistance = 15f;

        [Header("Earthquake")]
        [SerializeField] private EarthquakePiece _earthquakePiecePrefab;
        
        private readonly int _earthquakeDamage = 30;
        private readonly float _earthquakeInterval = 10f;

        private readonly float _earthquakeWindup = 1.5f;
        private readonly float _earthquakeActiveTime = 0.5f;
        private readonly float _earthquakeRecover = 1f;

        private readonly int _earthquakeCount = 3;
        private readonly float _earthquakeStartRadius = 5f;
        private readonly float _earthquakeRadiusStep = 5f;
        private readonly float _earthquakeGap = 1f;


        private bool _isCastingSkill;
        private Coroutine _skillCo;
        private float _defaultInterval = 2f;
        
        private WaitForSeconds _rushWindupWait;
        private WaitForSeconds _rushRecoverWait;

        private WaitForSeconds _earthquakeWindupWait;
        private WaitForSeconds _earthquakeActiveWait;
        private WaitForSeconds _earthquakeRecoverWait;
        private WaitForSeconds _earthquakeGapWait;

        // 스킬별 개별 쿨
        private float _defaultSkillTime;
        private float _nextRushTime;
        private float _nextEarthquakeTime;

        // damage overlap 버퍼(할당 방지)
        private readonly Collider2D[] _hitBuffer = new Collider2D[64];

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

        }

        public override void SetInfo(BaseEnemyData enemyData)
        {
            base.SetInfo(enemyData);
            gameObject.SetActive(true);

            agent.speed = moveSpeed;
            _currentHP = MaxHP;
            _nextAttackTime = 0f;

            _meleeAttack?.Init(attackDamage, Team);
            _rushAttack?.Init(attackDamage, Team);

            if (_rushSkillCollider) _rushSkillCollider.enabled = false;

            _nextRushTime = Time.time + _rushInterval;
            _nextEarthquakeTime = Time.time + _earthquakeInterval;
            
            _rushWindupWait = new WaitForSeconds(_rushWindup);
            _rushRecoverWait = new WaitForSeconds(_rushRecover);

            _earthquakeWindupWait = new WaitForSeconds(_earthquakeWindup);
            _earthquakeActiveWait = new WaitForSeconds(_earthquakeActiveTime);
            _earthquakeRecoverWait = new WaitForSeconds(_earthquakeRecover);
            _earthquakeGapWait = new WaitForSeconds(_earthquakeGap);
            
            _isCastingSkill = false;
            _skillCo = null;
        }

        protected override void Update()
        {
            if (GameManager.Instance.IsPaused) return;
            if (target == null) return;
            if (_isCastingSkill) return;

            base.Update();

            if (GetAnySkillReady())
                _skillCo = StartCoroutine(SkillSelectorCoroutine());
        }
        
        #region Skill Rotation
        private bool GetAnySkillReady()
        {
            float now = Time.time;

            if(now <= _defaultSkillTime) return false;
            if (now >= _nextEarthquakeTime) return true;
            if (now >= _nextRushTime) return true;

            return false;
        }
        
        private IEnumerator SkillSelectorCoroutine()
        {
            var ready = GetReadySkills();
            if (ready.Count == 0)
            {
                _skillCo = null;
                yield break;
            }

            int pick = Random.Range(0, ready.Count);
            SkillType selected = ready[pick];

            switch (selected)
            {
                case SkillType.Rush:
                    yield return RushToTargetCorutine();
                    break;
                default:
                    yield return EarthquakeRoutine();
                    break;
            }
        }
        
        private List<SkillType> GetReadySkills()
        {
            _readySkills.Clear();

            float now = Time.time;

            if (now >= _nextRushTime)
                _readySkills.Add(SkillType.Rush);

            if (now >= _nextEarthquakeTime)
                _readySkills.Add(SkillType.Earthquake);

            return _readySkills;
        }
        #endregion
        
        #region Melee Attack
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
                yield return _meleeAttackCorrection;
                meleeCollider.enabled = true;
                yield return _meleeAttackDuration;
                meleeCollider.enabled = false;
            }
            
            _isCastingSkill = false;
        }

        #endregion
        

        /// <summary>
        /// 1) 타겟 기준 Distance 만큼 돌진 목표점 계산
        /// 2) 직선으로 벽에 막히면 갈 수 있는 최대 지점으로 절삭 후 텔레그래프 생성
        /// 3) 선딜 대기 후 텔레그래프 숨김
        /// 4) 범위 만큼 돌진
        /// 5) 후딜 후 정상 상태 복귀
        /// 6) _rushCount 만큼 반복
        /// </summary>
        private IEnumerator RushToTargetCorutine()
        {
            _isCastingSkill = true;
            OnStop();

            if (meleeCollider) meleeCollider.enabled = false;

            float originalSpeed = agent.speed;
            float rushSpeed = moveSpeed * 2.0f;

            float originalAnimSpeed = animator != null ? animator.speed : 1f;

            for (int i = 0; i < _rushCount; i++)
            {
                if (GameManager.Instance.IsPaused)
                {
                    i--;
                    yield return null;
                    continue;
                }

                if (target == null) break;

                Vector3 startPos = transform.position;
                Vector3 targetPos = target.position;
                targetPos.z = startPos.z;

                Vector3 dir = targetPos - startPos;
                dir.z = 0f;
                if (dir.sqrMagnitude < 0.0001f) dir = Vector3.right;
                dir.Normalize();

                Vector3 desiredPos = startPos + dir * _rushDistance;
                desiredPos.z = startPos.z;

                Vector3 lockedPos = desiredPos;

                if (NavMesh.SamplePosition(desiredPos, out var sampleHit, 8.0f, NavMesh.AllAreas))
                {
                    lockedPos = sampleHit.position;
                    lockedPos.z = startPos.z;
                }
                else
                {
                    if (NavMesh.Raycast(startPos, desiredPos, out var rayHit, NavMesh.AllAreas))
                    {
                        lockedPos = rayHit.position;
                        lockedPos.z = startPos.z;
                    }
                }

                float vx = target.position.x - transform.position.x;
                if (vx > 0) transform.localScale = new Vector2(-1, 1);
                else if (vx < 0) transform.localScale = Vector2.one;
                
                int telegraphId = TelegraphManager.Instance.ShowLine(startPos, lockedPos, _rushWindup, _rushSkillCollider);

                yield return _rushWindupWait;
                TelegraphManager.Instance.Hide(telegraphId);

                if (animator != null)
                {
                    animator.SetFloat("Speed", 1f);
                    animator.speed = 1.5f;
                }

                if (_rushSkillCollider) _rushSkillCollider.enabled = true;

                agent.speed = rushSpeed;

                float dist = Vector3.Distance(startPos, lockedPos);
                float activeTime = (dist / Mathf.Max(0.01f, rushSpeed)) * 1.25f;

                if ((lockedPos - startPos).sqrMagnitude > 0.01f)
                    agent.SetDestination(lockedPos);

                yield return new WaitForSeconds(activeTime);

                agent.speed = originalSpeed;

                if (_rushSkillCollider) _rushSkillCollider.enabled = false;

                if (animator != null)
                {
                    animator.SetFloat("Speed", 0f);
                    animator.speed = originalAnimSpeed;
                }

                yield return _rushRecoverWait;
            }
            
            EndSkillCommon(SkillType.Rush);
        }

        /// <summary>
        /// 지진 스킬
        /// </summary>
        private IEnumerator EarthquakeRoutine()
        {
            _isCastingSkill = true;
            OnStop();

            if (meleeCollider) meleeCollider.enabled = false;
            if (_rushSkillCollider) _rushSkillCollider.enabled = false;

            // 보스를 NavMesh 중앙으로 이동
            if (TryGetNavMeshBoundsCenter(out Vector3 center))
            {
                center.z = transform.position.z;

                if (agent != null && agent.enabled)
                    agent.Warp(center);
                else
                    transform.position = center;
            }
            else
            {
                center = transform.position;
            }

            float radius = _earthquakeStartRadius;

            for (int i = 0; i < _earthquakeCount; i++)
            {
                if (GameManager.Instance.IsPaused)
                {
                    i--;
                    yield return null;
                    continue;
                }

                animator?.SetTrigger("OnSkill");

                // 텔레그래프 표시
                int telegraphId = TelegraphManager.Instance.ShowCircle(center, radius, _earthquakeWindup);

                // 선딜
                yield return _earthquakeWindupWait;

                // 피스 생성
                EarthquakePiece piece = SpawnEarthquakePiece(center);
                if (piece != null)
                {
                    piece.gameObject.SetActive(true);
                    piece.transform.position = center;
                    piece.Init(Team, _earthquakeDamage, radius);
                    piece.Explode(radius);
                }

                // 텔레그래프 숨김
                TelegraphManager.Instance.Hide(telegraphId);

                // 활성시간
                yield return _earthquakeActiveWait;
                
                Destroy(piece.gameObject);

                // 다음 타격
                radius += _earthquakeRadiusStep;
                yield return _earthquakeGapWait;
            }

            // 후딜
            yield return _earthquakeRecoverWait;

            EndSkillCommon(SkillType.Earthquake);
        }

        private EarthquakePiece SpawnEarthquakePiece(Vector3 pos)
        {
            if (_earthquakePiecePrefab == null)
            {
                Logger.LogError("EarthquakePiece prefab is null.");
                return null;
            }

            var piece = Instantiate(_earthquakePiecePrefab, pos, Quaternion.identity);
            return piece;
        }

        /// <summary>
        /// NavMesh 전체 Triangulation vertices로 bounds를 만든 뒤 중앙을 반환
        /// </summary>
        private bool TryGetNavMeshBoundsCenter(out Vector3 center)
        {
            center = transform.position;

            var tri = NavMesh.CalculateTriangulation();
            if (tri.vertices == null || tri.vertices.Length == 0)
                return false;

            Vector3 min = tri.vertices[0];
            Vector3 max = tri.vertices[0];

            for (int i = 1; i < tri.vertices.Length; i++)
            {
                Vector3 v = tri.vertices[i];
                if (v.x < min.x) min.x = v.x;
                if (v.y < min.y) min.y = v.y;
                if (v.z < min.z) min.z = v.z;

                if (v.x > max.x) max.x = v.x;
                if (v.y > max.y) max.y = v.y;
                if (v.z > max.z) max.z = v.z;
            }

            Vector3 mid = (min + max) * 0.5f;

            if (NavMesh.SamplePosition(mid, out var hit, 2.0f, NavMesh.AllAreas))
            {
                center = hit.position;
                return true;
            }

            center = mid;
            return true;
        }

        /// <summary>
        /// 타입별 다음 스킬 예약 및 상태 복구
        /// </summary>
        private void EndSkillCommon(SkillType usedSkill)
        {
            float now = Time.time;

            switch (usedSkill)
            {
                case SkillType.Rush:
                    _nextRushTime = now + _rushInterval;
                    break;
                case SkillType.Earthquake:
                    _nextEarthquakeTime = now + _earthquakeInterval;
                    break;
                default:
                    break;
            }

            _defaultSkillTime = now + _defaultInterval;
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
