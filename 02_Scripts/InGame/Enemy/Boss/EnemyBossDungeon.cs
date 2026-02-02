using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace PixelSurvival
{
    public class EnemyBossDungeon : BaseEnemy, IDamageable
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
            Bolt,
            Rain,
            Spiral,
            COUNT,
        }

        private readonly List<SkillType> _readySkills = new((int)SkillType.COUNT);

        // ======= Basic Attack (Wizard) =======
        [Header("Basic Attack (Projectiles)")]
        [SerializeField] private Sprite _projectileSprite;
        private readonly float _projectileSpeed = 10f;

        private readonly Vector3 _spawnCorrection  = new Vector3(0, 1f, 0);
        private readonly Vector3 _targetCorrection = new Vector3(0, 0.5f, 0);
        private readonly Vector2 _projectileSize = new Vector2(2f, 2f);
        
        private float _nextAttackTime;
        private float _defaultSkillTime;
        
        private readonly float _defaultInterval = 5f;
        
        [Header("Lightning Bolt")]
        [SerializeField] private LightningBoltPiece _boltPiece;
        private readonly int _boltDamage = 25;
        private readonly float _boltInterval = 6f; // 스킬 쿨타임
        private readonly float _boltWindup = 1.2f;
        private readonly float _boltActiveTime = 0.25f;
        private readonly float _boltRecover = 0.6f;
        private readonly float _boltRadius = 3.0f;

        [Header("Lightning Rain")]
        [SerializeField] private GameObject _rainPrefab;
        [SerializeField] private Transform _lightningRainRoot;
        private readonly int _lightningRainPoolSize = 100;
        
        // Lightning Rain Pool
        private readonly Stack<LightningRainPiece> _rainInactive = new();
        private readonly HashSet<LightningRainPiece> _rainActive = new();

        private readonly int _rainDamage = 20;
        private readonly float _rainInterval = 10f; // 스킬 쿨타임
        private readonly float _rainDuration = 15f; // 라이트닝 레인 스킬 지속시간
        private readonly float _rainPieceRate = 25f; // 프레임 당 발사 Piece

        private readonly float _rainRainPieceRadius = 1.5f;
        private readonly float _rainWindup = 1.0f;
        private readonly float _rainActiveTime = 0.8f;

        // 번개끼리의 최소 간격은 Piece의 Radius
        private readonly float _rainRecentCheckRadius = 2.5f;
        private readonly int _rainRecentCheckCount = 120;  // 최근 N개만 거리 체크(성능용)
        private readonly int _rainSampleTries = 10;        // 한 발당 샘플링 재시도 횟수
        
        // 최근 번개 위치 버퍼
        private Vector2[] _rainRecentPositions;
        private int _rainRecentCursor;
        private int _rainRecentFilled;

        private bool _isCastingSkill;
        private Coroutine _skillCo;

        [Header("Spiral Shot")]
        private readonly int _spiralDamage = 10;
        private readonly float _spiralInterval = 8f; // 스킬 쿨타임
        private readonly float _spiralDuration = 6f;          // 나선 패턴 지속시간
        private readonly float _spiralShotRate = 45f;         // 초당 발사 수(촘촘할수록 높임)
        private readonly float _spiralAngularSpeed = 720f;    // 초당 회전 각도(deg/s) - 360이면 1회전/초
        private readonly float _spiralRadiusOffset = 0.35f;   // 보스 중심에서 살짝 밖에서 시작(겹침 방지)
        private readonly float _spiralProjectileSpeed = 5f;
        
        private readonly float _spiralWindup = 0.8f;
        private readonly float _spiralRecover = 0.6f;
        
        // 스킬 쿨타임 변수
        private float _nextBoltTime;
        private float _nextRainTime;
        private float _nextSpiralTime;
        
        // Wait 캐싱
        private WaitForSeconds _boltWindupWait;
        private WaitForSeconds _boltActiveWait;
        private WaitForSeconds _boltRecoverWait;

        private WaitForSeconds _rainWindupWait;
        private WaitForSeconds _rainActiveWait;

        private WaitForSeconds _spiralWindupWait;
        private WaitForSeconds _spiralRecoverWait;
        
        // NavMesh Random Sampling Cache
        private Vector3[] _navVerts;
        private int[] _navIndices;
        private float[] _triCdf;
        private int _triCount;
        private float _totalArea;

        [Header("Skill Selection")]
        [SerializeField, Range(0f, 1f)] private float _spiralChance = 0.35f; // 스킬 타이밍에 Spiral 선택 확률
        [SerializeField, Range(0f, 1f)] private float _rainChance = 0.35f;   // 기존 값 유지/조절

        public override void Init(Transform target)
        {
            base.Init(target);

            gameObject.SetActive(false);
        }

        public override void SetInfo(BaseEnemyData enemyData)
        {
            base.SetInfo(enemyData);

            _isCastingSkill = false;
            _skillCo = null;
            
            agent.speed = moveSpeed;
            _currentHP = MaxHP;
            
            _nextAttackTime = 0f;

            float now = Time.time;

            _nextBoltTime   = now + _boltInterval;
            _nextRainTime   = now + _rainInterval;
            _nextSpiralTime = now + _spiralInterval;
            _defaultSkillTime = now + _spiralDuration;

            _boltWindupWait = new WaitForSeconds(_boltWindup);
            _boltActiveWait = new WaitForSeconds(_boltActiveTime);
            _boltRecoverWait = new WaitForSeconds(_boltRecover);

            _rainWindupWait = new WaitForSeconds(_rainWindup);
            _rainActiveWait = new WaitForSeconds(_rainActiveTime);

            _spiralWindupWait = new WaitForSeconds(_spiralWindup);
            _spiralRecoverWait = new WaitForSeconds(_spiralRecover);
            
            if (_boltPiece == null)
                _boltPiece = GetComponentInChildren<LightningBoltPiece>();

            if (_boltPiece != null)
            {
                _boltPiece.transform.SetParent(transform);
                _boltPiece.gameObject.SetActive(false);
            }
            if (_projectileSprite != null)
                projectileSprite = _projectileSprite;
            
            ResetNavMeshSampling();
            ResetLightningRainRecentPos();
            
            PrewarmLightningRainPool();
            
            gameObject.SetActive(true);
        }

        protected override void Update()
        {
            if (GameManager.Instance.IsPaused) return;
            if (target == null) return;
            if (_isCastingSkill) return;

            base.Update();

            if (_skillCo != null) return;
            
            if (GetAnySkillReady())
                _skillCo = StartCoroutine(SkillSelectorCoroutine());
        }

        #region Skill Rotation

        private bool GetAnySkillReady()
        {
            float now = Time.time;

            if (now < _defaultSkillTime) return false;

            if (now >= _nextBoltTime) return true;
            if (_rainPrefab != null && now >= _nextRainTime) return true;
            if (now >= _nextSpiralTime) return true;

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
                case SkillType.Rain:
                    yield return LightningRainCoroutine();
                    break;

                case SkillType.Spiral:
                    yield return SpiralShotCoroutine();
                    break;

                default:
                    yield return LightningBoltCoroutine();
                    break;
            }
        }
        
        private List<SkillType> GetReadySkills()
        {
            _readySkills.Clear();

            float now = Time.time;

            if (now >= _nextBoltTime)
                _readySkills.Add(SkillType.Bolt);

            if (_rainPrefab != null && now >= _nextRainTime)
                _readySkills.Add(SkillType.Rain);

            if (now >= _nextSpiralTime)
                _readySkills.Add(SkillType.Spiral);

            return _readySkills;
        }

        #endregion
        
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
        /// 기본 공격: 마탄 3발 부채꼴 발사
        /// </summary>
        private void FireProjectiles()
        {
            if (projectileSprite == null) return;
            if (target == null) return;

            Vector3 spawnPos = transform.position + _spawnCorrection;

            Vector3 targetPos = target.position + _targetCorrection;
            Vector2 baseDir = new Vector2(targetPos.x - spawnPos.x, targetPos.y - spawnPos.y);

            // 타겟과 겹쳐서 방향이 0이 되는 경우 대비
            if (baseDir.sqrMagnitude < 0.0001f)
                baseDir = (transform.localScale.x < 0f) ? Vector2.left : Vector2.right;

            baseDir.Normalize();

            int count = 3;
            float spreadDegrees = 30f;

            float step = spreadDegrees / (count - 1);
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
                    _projectileSpeed,
                    Team,
                    rotValue: 0,
                    scaleValue: _projectileSize
                );
            }
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

        #region Lightning Bolt Skill
        
        /// <summary>
        /// - 타겟 위치에 원형 텔레그래프 표시
        /// - 선딜 후 폭발 판정
        /// - 후딜 후 복귀
        /// </summary>
        private IEnumerator LightningBoltCoroutine()
        {
            _isCastingSkill = true;
            OnStop();

            Vector3 center = (target != null) ? target.position : transform.position;
            center.z = transform.position.z;

            // 1) 텔레그래프 표시(원)
            int telegraphId = TelegraphManager.Instance.ShowCircle(center, _boltRadius, _boltWindup + _boltActiveTime);
            animator.SetTrigger("OnSkill");

            // 2) 선딜
            yield return _boltWindupWait;

            // 3) 폭발(판정)
            if (_boltPiece != null)
            {
                _boltPiece.transform.position = center;
                _boltPiece.gameObject.SetActive(true);
                _boltPiece.Init(Team, _boltDamage, _boltRadius);

                yield return _boltActiveWait;

                // 텔레그래프 숨김 후 실제 폭발
                TelegraphManager.Instance.Hide(telegraphId);
                _boltPiece.Explode(_boltRadius);
            }

            // 후딜
            yield return _boltRecoverWait;

            // 상태 복구
            if (_boltPiece != null)
                _boltPiece.gameObject.SetActive(false);

            EndSkillCommon(SkillType.Bolt);
        }

        #endregion

        #region Lightning Rain Skill

        /// <summary>
        /// 라이트닝 레인 스킬:
        /// - duration 동안 초당 strikeRate로 계속 번개를 떨어뜨림
        /// - 매 발마다 텔레그래프(원) 표시
        /// - 겹침을 줄이기 위해 "최근 N개" 위치와 최소 간격을 체크
        /// - 번개 프리팹은 풀(100개)로 재활용
        /// </summary>
        private IEnumerator LightningRainCoroutine()
        {
            _isCastingSkill = true;
            OnStop();

            animator.SetBool("IsSkill", true);

            // 네브메쉬 샘플링 캐시 준비
            if (!NavMeshSampling())
            {
                EndSkillCommon(SkillType.Rain);
                yield break;
            }

            // 풀 준비
            PrewarmLightningRainPool();
            if (_rainPrefab == null)
            {
                EndSkillCommon(SkillType.Rain);
                yield break;
            }

            // 최근 위치 버퍼 초기화
            ResetLightningRainRecentPos();
            
            float endTime = Time.time + _rainDuration;

            float accumulator = 0f;

            while (Time.time < endTime)
            {
                if (GameManager.Instance.IsPaused)
                {
                    yield return null;
                    continue;
                }

                // 이번 프레임에 쏴야 하는 발수 계산
                accumulator += _rainPieceRate * Time.deltaTime;

                int strikesThisFrame = Mathf.FloorToInt(accumulator);
                if (strikesThisFrame > 0)
                    accumulator -= strikesThisFrame;
                
                for (int i = 0; i < strikesThisFrame; i++)
                {
                    // 균일한 위치 뽑기
                    if (!TryGetLightningRainSpawnPoint(out Vector3 pos))
                        continue;

                    pos.z = transform.position.z;

                    int telegraphId = TelegraphManager.Instance.ShowCircle(pos, _rainRainPieceRadius, _rainWindup);

                    // 선딜 후 폭발
                    StartCoroutine(LightningRainStrike(pos, telegraphId));
                }

                yield return null;
            }
            
            animator.SetBool("IsSkill", false);
            // 후딜
            yield return _boltRecoverWait;

            EndSkillCommon(SkillType.Rain);
        }
        
        /// <summary>
        /// 라이트닝 레인 "1발" 처리:
        /// - 텔레그래프 선딜 대기
        /// - 텔레그래프 숨김
        /// - 풀에서 번개 오브젝트 꺼내 위치/활성화
        /// - Init + Explode로 판정
        /// - 일정 시간 유지 후 비활성화(풀 반환)
        /// </summary>
        private IEnumerator LightningRainStrike(Vector3 pos, int telegraphId)
        {
            // 선딜
            yield return _rainWindupWait;
            TelegraphManager.Instance.Hide(telegraphId);

            // piece Spawn
            var piece = SpawnLightningRainPiece(pos);
            if (!piece) yield break;

            // 판정
            piece.Init(Team, _rainDamage, _rainRainPieceRadius);
            piece.Explode(_rainRainPieceRadius);

            // 활성 시간
            yield return _rainActiveWait;

            // 풀 반환
            DespawnLightningRainPiece(piece);
        }

        /// <summary>
        /// 라이트닝 레인 풀을 미리 생성(prewarm)한다.
        /// - _lightningRainPoolSize만큼 LightningBoltPiece를 inactive 스택에 채움
        /// - 부족하면 추가 Instantiate
        /// </summary>
        private void PrewarmLightningRainPool()
        {
            if (!_rainPrefab) return;

            if (_lightningRainRoot == null)
            {
                var rootGo = new GameObject("LightningRainRoot(Auto)");
                _lightningRainRoot = rootGo.transform;
                _lightningRainRoot.SetParent(transform, false);
            }

            int target = _lightningRainPoolSize;
            int currentTotal = _rainInactive.Count + _rainActive.Count;
            int need = target - currentTotal;

            for (int i = 0; i < need; i++)
            {
                var go = Instantiate(_rainPrefab, _lightningRainRoot);
                go.name = $"{_rainPrefab.name}_Pool_{(currentTotal + i):D3}";
                go.SetActive(false);

                var piece = go.GetComponentInChildren<LightningRainPiece>();
                if (!piece)
                {
                    Logger.LogError($"{GetType()} :: LightningRain prefab needs LightningBoltPiece.");
                    Destroy(go);
                    continue;
                }

                _rainInactive.Push(piece);
            }
        }

        /// <summary>
        /// 풀에서 LightningBoltPiece 1개를 꺼내 활성화한다.
        /// - inactive 있으면 Pop
        /// - 없으면 즉시 Instantiate(부하 허용)
        /// </summary>
        private LightningRainPiece SpawnLightningRainPiece(Vector3 pos)
        {
            if (!_rainPrefab) return null;

            LightningRainPiece piece = null;

            if (_rainInactive.Count > 0)
            {
                piece = _rainInactive.Pop();
            }
            else
            {
                // 부족하면 즉시 생성
                var go = Instantiate(_rainPrefab, _lightningRainRoot);
                go.name = $"{_rainPrefab.name}_Extra";
                go.SetActive(false);

                piece = go.GetComponentInChildren<LightningRainPiece>();
                if (!piece)
                {
                    Logger.LogError($"{GetType()} :: LightningRain prefab needs LightningRainPiece.");
                    Destroy(go);
                    return null;
                }
            }

            _rainActive.Add(piece);

            // 활성화/배치
            var t = piece.transform;
            t.SetParent(_lightningRainRoot, true);
            t.position = pos;
            t.rotation = Quaternion.identity;

            piece.gameObject.SetActive(true);
            return piece;
        }

        /// <summary>
        /// 사용 끝난 LightningBoltPiece를 비활성화하고 풀로 반환한다.
        /// </summary>
        private void DespawnLightningRainPiece(LightningRainPiece piece)
        {
            if (!piece) return;

            if (_rainActive.Contains(piece))
                _rainActive.Remove(piece);

            piece.gameObject.SetActive(false);
            piece.transform.SetParent(_lightningRainRoot, false);

            _rainInactive.Push(piece);
        }

        /// <summary>
        /// 라이트닝 레인 스폰 포인트를 얻는다.
        /// 네브메쉬 위 랜덤 점을 여러 번 뽑는다
        /// 최근 N개 위치와의 거리 조건을 통과하면 채택
        /// 계속 실패하면 fallback으로 그냥 랜덤 1개를 허용(발사량 유지 목적)
        /// </summary>
        private bool TryGetLightningRainSpawnPoint(out Vector3 pos)
        {
            pos = transform.position;

            float minDist = _rainRecentCheckRadius;
            float minDistSqr = minDist * minDist;

            for (int t = 0; t < _rainSampleTries; t++)
            {
                if (!TryGetRandomPoint(out Vector3 cand))
                {
                    continue;   
                }

                cand.z = transform.position.z;

                // 최근 위치들과 너무 가까우면 버리고 다시 뽑기
                if (minDistSqr > 0f && CheckLightningRainPoint((Vector2)cand, minDistSqr))
                {
                    continue;   
                }

                // 최근 버퍼에 push 후 채택
                pos = cand;
                SaveLightningRainRecent((Vector2)cand);
                return true;
            }

            // 발사량이 줄어드는 걸 막기 위해 랜덤 1개라도 허용
            if (TryGetRandomPoint(out Vector3 fallback))
            {
                fallback.z = transform.position.z;
                pos = fallback;
                SaveLightningRainRecent((Vector2)fallback);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 최근 N개 번개 위치와의 최소 간격 체크
        /// </summary>
        private bool CheckLightningRainPoint(Vector2 p, float minDistSqr)
        {
            int count = Mathf.Min(_rainRecentFilled, _rainRecentPositions.Length);
            for (int i = 0; i < count; i++)
            {
                Vector2 q = _rainRecentPositions[i];
                if ((p - q).sqrMagnitude < minDistSqr)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 최근 번개 위치 버퍼에 저장
        /// </summary>
        private void SaveLightningRainRecent(Vector2 p)
        {
            if (_rainRecentPositions == null || _rainRecentPositions.Length == 0) return;

            _rainRecentPositions[_rainRecentCursor] = p;
            _rainRecentCursor = (_rainRecentCursor + 1) % _rainRecentPositions.Length;
            _rainRecentFilled = Mathf.Min(_rainRecentFilled + 1, _rainRecentPositions.Length);
        }

        /// <summary>
        /// 최근 위치 버퍼를 초기화한다.
        /// </summary>
        private void ResetLightningRainRecentPos()
        {
            int cap = Mathf.Max(16, _rainRecentCheckCount);

            if (_rainRecentPositions == null || _rainRecentPositions.Length != cap)
                _rainRecentPositions = new Vector2[cap];

            _rainRecentCursor = 0;
            _rainRecentFilled = 0;
        }

        /// <summary>
        /// 네브메쉬 샘플링 캐시 초기화
        /// </summary>
        private void ResetNavMeshSampling()
        {
            _navVerts = null;
            _navIndices = null;
            _triCdf = null;
            _triCount = 0;
            _totalArea = 0f;
        }

        /// <summary>
        /// 삼각형별 면적 누적을 만들어서 면적 가중 랜덤 샘플링 가능하게 한다.
        /// </summary>
        private bool NavMeshSampling()
        {
            if (_navVerts != null && _navIndices != null && _triCdf != null && _triCount > 0 && _totalArea > 0.0001f)
                return true;

            var tri = NavMesh.CalculateTriangulation();
            if (tri.vertices == null || tri.vertices.Length < 3) return false;
            if (tri.indices == null || tri.indices.Length < 3) return false;

            _navVerts = tri.vertices;
            _navIndices = tri.indices;

            _triCount = _navIndices.Length / 3;
            _triCdf = new float[_triCount];

            _totalArea = 0f;
            for (int t = 0; t < _triCount; t++)
            {
                int i0 = _navIndices[t * 3 + 0];
                int i1 = _navIndices[t * 3 + 1];
                int i2 = _navIndices[t * 3 + 2];

                Vector3 a = _navVerts[i0];
                Vector3 b = _navVerts[i1];
                Vector3 c = _navVerts[i2];

                // 2D 면적
                float area = Mathf.Abs((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x)) * 0.5f;
                _totalArea += area;
                _triCdf[t] = _totalArea; // 누적합
            }

            return _totalArea > 0.0001f;
        }

        /// <summary>
        /// 네브메쉬 위 랜덤 점을 면적 가중으로 하나 뽑는다.
        /// 큰 삼각형이 더 자주 뽑히게 해서 분포 왜곡을 막는다.
        /// </summary>
        private bool TryGetRandomPoint(out Vector3 point)
        {
            point = transform.position;

            if (!NavMeshSampling())
                return false;

            float r = Random.value * _totalArea;

            // CDF에서 r이 들어가는 삼각형 찾기
            int triIndex = 0;
            for (int i = 0; i < _triCount; i++)
            {
                if (r <= _triCdf[i]) { triIndex = i; break; }
            }

            int i0 = _navIndices[triIndex * 3 + 0];
            int i1 = _navIndices[triIndex * 3 + 1];
            int i2 = _navIndices[triIndex * 3 + 2];

            Vector3 a = _navVerts[i0];
            Vector3 b = _navVerts[i1];
            Vector3 c = _navVerts[i2];

            // 삼각형 내부 균일 샘플링(barycentric)
            float u = Random.value;
            float v = Random.value;
            if (u + v > 1f) { u = 1f - u; v = 1f - v; }

            Vector3 p = a + u * (b - a) + v * (c - a);

            // 미세 오차 대비 스냅
            if (NavMesh.SamplePosition(p, out var hit, 0.25f, NavMesh.AllAreas))
            {
                point = hit.position;
                return true;
            }

            point = p;
            return true;
        }

        #endregion

        #region Spiral Shot Skill

        /// <summary>
        /// - 보스를 네브메쉬 전체 영역의 중앙으로 워프
        /// - 보스 중심 기준으로 나선 패턴으로 투사체를 발사
        /// </summary>
        private IEnumerator SpiralShotCoroutine()
        {
            _isCastingSkill = true;
            OnStop();

            if (!NavMeshSampling())
            {
                EndSkillCommon(SkillType.Spiral);
                yield break;
            }

            yield return _spiralWindupWait;
            
            // 네브메쉬 전체 bounds 기준 중앙으로 워프
            if (TryGetNavMeshBoundsCenter(out Vector3 center))
            {
                center.z = transform.position.z;

                if (agent != null && agent.enabled)
                    agent.Warp(center);
                else
                    transform.position = center;
            }

            animator.SetBool("IsSkill", true);
            
            yield return _spiralWindupWait;
            
            // 나선 발사 루프
            float endTime = Time.time + _spiralDuration;
            float accumulator = 0f;

            // 나선 패턴용 각도
            float angleDeg = 0f;

            while (Time.time < endTime)
            {
                // 이번 프레임에 발사해야 하는 발 수(초당 N발을 프레임으로 분배)
                accumulator += _spiralShotRate * Time.deltaTime;
                int shotsThisFrame = Mathf.FloorToInt(accumulator);
                if (shotsThisFrame > 0) accumulator -= shotsThisFrame;

                for (int i = 0; i < shotsThisFrame; i++)
                {
                    // 4) 현재 각도를 기반으로 방향 벡터 생성(나선의 "회전" 핵심)
                    Vector2 dir = DirFromAngle(angleDeg);

                    // 5) 발사 위치(보스 중심에서 약간 바깥으로)
                    Vector3 spawnPos = transform.position;
                    spawnPos += (Vector3)(dir * _spiralRadiusOffset);
                    spawnPos += _spawnCorrection; // 기존 기본 공격 보정 재사용(원하면 제거 가능)

                    // 6) 투사체 발사(기본 공격과 동일 Init, scaleValue만 1)
                    SpawnSpiralProjectile(spawnPos, dir, _spiralProjectileSpeed);

                    // 7) 다음 샷 각도 업데이트
                    // - angularSpeed(deg/s)를 "샷 간 간격"에 맞춰 증가시키면 일정한 나선이 됨
                    float shotDt = 1f / Mathf.Max(1f, _spiralShotRate);
                    angleDeg += _spiralAngularSpeed * shotDt;
                }

                yield return null;
            }

            animator.SetBool("IsSkill", false);

            // 스킬 후딜(원하면 boltRecover와 분리 가능)
            yield return _spiralRecoverWait;

            EndSkillCommon(SkillType.Spiral);
        }

        /// <summary>
        /// 각도 -> 방향 변환
        /// </summary>
        private Vector2 DirFromAngle(float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        /// <summary>
        /// Spiral Shot에서 실제 Projectile을 스폰
        /// </summary>
        private void SpawnSpiralProjectile(Vector3 spawnPos, Vector2 dir, float projectileSpeed)
        {
            if (projectileSprite == null) return;

            var projectile = PoolManager.Instance.Spawn<Projectile>(PoolType.Projectile, spawnPos, Quaternion.identity);
            if (!projectile) return;

            projectile.Init(
                dir.normalized,
                projectileSprite,
                _spiralDamage,
                projectileSpeed,
                Team,
                rotValue: 0,
                scaleValue: _projectileSize
            );
        }

        /// <summary>
        /// NavMesh 전체 Triangulation vertices로 bounds를 만든 뒤 중앙을 반환
        /// </summary>
        private bool TryGetNavMeshBoundsCenter(out Vector3 center)
        {
            center = transform.position;

            if (_navVerts == null || _navVerts.Length == 0)
                return false;

            Vector3 min = _navVerts[0];
            Vector3 max = _navVerts[0];

            for (int i = 1; i < _navVerts.Length; i++)
            {
                Vector3 v = _navVerts[i];
                if (v.x < min.x) min.x = v.x;
                if (v.y < min.y) min.y = v.y;
                if (v.z < min.z) min.z = v.z;

                if (v.x > max.x) max.x = v.x;
                if (v.y > max.y) max.y = v.y;
                if (v.z > max.z) max.z = v.z;
            }

            Vector3 mid = (min + max) * 0.5f;

            // 중앙이 네브메쉬 밖일 수도 있어서 샘플로 스냅
            if (NavMesh.SamplePosition(mid, out var hit, 2.0f, NavMesh.AllAreas))
            {
                center = hit.position;
                return true;
            }

            center = mid;
            return true;
        }

        #endregion

        
        /// <summary>
        /// 타입별 다음 스킬 예약 및 상태 복구
        /// </summary>
        private void EndSkillCommon(SkillType usedSkill)
        {
            float now = Time.time;

            switch (usedSkill)
            {
                case SkillType.Rain:
                    _nextRainTime = now + _rainInterval;
                    break;

                case SkillType.Spiral:
                    _nextSpiralTime = now + _spiralInterval;
                    break;

                default:
                    _nextBoltTime = now + _boltInterval;
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
