using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace PixelSurvival
{
    public class EnemyBossDesert : BaseEnemy, IDamageable
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

        private readonly Vector3 _spawnCorrection = new Vector3(0, 1f, 0);
        private readonly Vector3 _targetCorrection = new Vector3(0, 0.5f, 0);
        private readonly Vector2 _projectileSize = new Vector2(2.0f, 2.0f);
        private float _nextAttackTime;

        private Collider2D _skillCollider;
        private readonly int _skillDamage = 20;
        private readonly float _skillInterval = 6f;
        private readonly float _skillWindup = 1.0f;
        private readonly float _skillRecover = 0.5f;

        private float _nextSkillTime;
        private bool _isCastingSkill;
        private Coroutine _skillCo;

        [SerializeField] private Sprite _projectileSprite;
        private readonly Vector2 _projectileColSize = new Vector2(0.5f, 1.5f);

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
                _skillCo = StartCoroutine(SkillRoutine());
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

            FireProjectile();
        }

        /// <summary>
        /// 타겟 방향으로 3발 발사. 각도는 spreadDegrees로 부채꼴 처리.
        /// </summary>
        private void FireProjectile()
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
                    8,
                    Team,
                    rotValue: 45,
                    scaleValue: _projectileSize,
                    colSize: _projectileColSize
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
        /// 1) NavMesh Bounds 계산
        /// 2) spacing 간격으로 세로, 가로 라인 텔레그래프 생성
        /// 3) 선딜 대기 후 텔레그래프 숨김
        /// 4) 격자 라인마다 투사체 일괄 발사
        /// 5) 투사체가 끝까지 이동해서 사라질 시간 동안 정지 유지
        /// 6) 후딜 후 정상 상태 복귀
        /// </summary>
        private IEnumerator SkillRoutine()
        {
            _isCastingSkill = true;
            OnStop();

            float spacing = 2.5f;        // 격자 간격
            float sampleMaxDist = 0.5f;  // 해당 라인이 유효한 네브메쉬 위인지 샘플링 거리
            float projSpeed = 50f;     // 투사체 스피드

            // 네브메쉬의 전체 워크 가능 영역을 사각 Bounds로 계산
            if (!TryGetNavMeshBounds(out var bounds))
            {
                Logger.LogError("[BossDesert] Skill: NavMesh bounds failed");

                _nextSkillTime = Time.time + Mathf.Max(0.1f, _skillInterval);
                _isCastingSkill = false;
                _skillCo = null;
                yield break;
            }

            float width = _projectileColSize.x;

            float leftX = bounds.min.x;
            float rightX = bounds.max.x;
            float botY = bounds.min.y;
            float topY = bounds.max.y;
            float z = transform.position.z;

            List<int> ids = new List<int>(512);
            int vCount = 0, hCount = 0;

            // 위에서 아래 발사 텔레그래프.
            for (float x = leftX; x <= rightX; x += spacing)
            {
                // 해당 x가 네브메쉬 위 유효 통로인지 중앙 y에서 확인
                Vector3 probe = new Vector3(x, (topY + botY) * 0.5f, z);
                if (!NavMesh.SamplePosition(probe, out var hit, sampleMaxDist, NavMesh.AllAreas))
                    continue;

                float hx = hit.position.x;
                Vector3 start = new Vector3(hx, topY, z);
                Vector3 end = new Vector3(hx, botY, z);

                int id = TelegraphManager.Instance.ShowLine(start, end, _skillWindup, width);
                if (id >= 0) { ids.Add(id); vCount++; }
            }

            // 왼쪽에서 오른쪽 발사 텔레그래프.
            for (float y = botY; y <= topY; y += spacing)
            {
                Vector3 probe = new Vector3((leftX + rightX) * 0.5f, y, z);
                if (!NavMesh.SamplePosition(probe, out var hit, sampleMaxDist, NavMesh.AllAreas))
                    continue;

                float hy = hit.position.y;
                Vector3 start = new Vector3(leftX, hy, z);
                Vector3 end = new Vector3(rightX, hy, z);

                int id = TelegraphManager.Instance.ShowLine(start, end, _skillWindup, width);
                if (id >= 0) { ids.Add(id); hCount++; }
            }

            yield return new WaitForSeconds(_skillWindup);

            animator.SetTrigger("OnSkill");
            
            for (int i = 0; i < ids.Count; i++)
                TelegraphManager.Instance.Hide(ids[i]);

            int spawnedV = 0, spawnedH = 0;

            // 화살이 진행방향을 향하므로, y의 절반만큼 안쪽으로 스폰해 벽 충돌 막기.
            float halfForward = _projectileColSize.y * 0.5f;
            float padding = 0.05f;
            float spawnInset = halfForward + padding;

            // 맵 끝까지 실제 이동하는 거리 / 속도 = 시간
            float travelV = (topY - botY) - 2f * spawnInset;
            float travelH = (rightX - leftX) - 2f * spawnInset;

            float invSpeed = 1f / projSpeed;
            float timeV = travelV * invSpeed;
            float timeH = travelH * invSpeed;

            // 세로, 가로 둘 다 쏘면 더 오래 걸리는 쪽이 스킬 지속시간.
            float activeTime = 0f;
            activeTime = timeV > timeH ? timeV : timeH;

            // 위쪽에서 아래로 발사
            float spawnY = topY - spawnInset;

            for (float x = leftX; x <= rightX; x += spacing)
            {
                Vector3 probe = new Vector3(x, (topY + botY) * 0.5f, z);
                if (!NavMesh.SamplePosition(probe, out var hit, sampleMaxDist, NavMesh.AllAreas))
                    continue;

                Vector3 spawnPos = new Vector3(hit.position.x, spawnY, z);

                var p = PoolManager.Instance.Spawn<Projectile>(PoolType.Projectile, spawnPos, Quaternion.identity);
                if (!p) continue;

                p.Init(Vector2.down, projectileSprite, _skillDamage, projSpeed, Team,
                       rotValue: 45, scaleValue: _projectileSize, colSize: _projectileColSize);

                spawnedV++;
            }

            // 왼쪽에서 오른쪽으로 발사
            float spawnX = leftX + spawnInset;

            for (float y = botY; y <= topY; y += spacing)
            {
                Vector3 probe = new Vector3((leftX + rightX) * 0.5f, y, z);
                if (!NavMesh.SamplePosition(probe, out var hit, sampleMaxDist, NavMesh.AllAreas))
                    continue;

                Vector3 spawnPos = new Vector3(spawnX, hit.position.y, z);

                var p = PoolManager.Instance.Spawn<Projectile>(PoolType.Projectile, spawnPos, Quaternion.identity);
                if (!p) continue;

                p.Init(Vector2.right, projectileSprite, _skillDamage, projSpeed, Team,
                       rotValue: 45, scaleValue: _projectileSize, colSize: _projectileColSize);

                spawnedH++;
            }

            // 투사체가 맵 끝까지 도달하는 동안 정지 유지
            yield return new WaitForSeconds(activeTime);

            // 후딜
            yield return new WaitForSeconds(_skillRecover);

            _nextSkillTime = Time.time + Mathf.Max(0.1f, _skillInterval);
            _isCastingSkill = false;
            _skillCo = null;
        }

        /// <summary>
        /// NavMesh 전체 삼각형 꼭짓점으로부터 2D Bounds(사각형 범위)를 계산
        /// 현재 맵 구조가 사각형 워크 영역이므로 Bounds로 커버 가능
        /// </summary>
        private bool TryGetNavMeshBounds(out Bounds bounds)
        {
            bounds = default;

            var tri = NavMesh.CalculateTriangulation();
            if (tri.vertices == null || tri.vertices.Length == 0)
                return false;

            Vector3 v0 = tri.vertices[0];
            float minX = v0.x, maxX = v0.x;
            float minY = v0.y, maxY = v0.y;

            for (int i = 1; i < tri.vertices.Length; i++)
            {
                var v = tri.vertices[i];
                if (v.x < minX) minX = v.x;
                if (v.x > maxX) maxX = v.x;
                if (v.y < minY) minY = v.y;
                if (v.y > maxY) maxY = v.y;
            }

            Vector3 center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, transform.position.z);
            Vector3 size = new Vector3((maxX - minX), (maxY - minY), 0.1f);
            bounds = new Bounds(center, size);
            return true;
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
