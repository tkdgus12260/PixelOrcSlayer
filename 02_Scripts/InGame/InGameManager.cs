using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace PixelSurvival
{
    public class InGameManager : SingletonBehaviour<InGameManager>
    {
        public InGameUIController InGameUIController { get; private set; }
        public bool IsPaused { get; private set; }

        private int _selectedChapter;
        private int _currentStage;
        private Player _player;
        public Player Player => _player;

        private ChapterData _chapterData;

        [SerializeField] private Transform stageContainer;
        [SerializeField] private Transform enemyContainer;
        [SerializeField] private Transform playerContainer;
        
        [Header("Enemy Spawn")]
        private readonly float _spawnNoSpawnRadius = 8f;       // 플레이어 기준 적 스폰 금지 반경
        private readonly float _spawnMaxRadiusPadding = 1.0f;  // NavMesh 경계 여유
        private readonly int _spawnTryPerEnemy = 20;           // 적 1마리당 후보 시도 횟수
        private readonly float _spawnMinExtraSpacing = 0.25f;  // 적끼리 최소 간격 여유
        private readonly float _spawnRingExtraRadius = 10f;    // 최대 반경 = min + extra, 단 NavMesh로 캡

        private int totalRewardGold = 0;

        [Header("Round Timer")]
        private readonly float _roundDuration = 30f;           // 라운드 시간

        private Coroutine _roundTimerCo;
        private Coroutine _spawnCo;

        private bool _isRoundRunning;

        private bool _isSpawnFinished;

        private readonly Collider2D[] _overlapBuffer = new Collider2D[64];

        public List<BaseEnemy> Enemies = new();

        protected override void Init()
        {
            isDestroyOnLoad = true;

            base.Init();

            GameManager.Instance.Resume();

            InitVariables();
            UIManager.Instance.Fade(Color.black, 1f, 0f, 0.5f, 0f, true);
        }

        private void InitVariables()
        {
            Logger.Log($"{GetType()}::InitVariables");

            _currentStage = 1;

            var userPlayData = UserDataManager.Instance.GetUserData<UserPlayData>();
            if (userPlayData == null)
            {
                Logger.LogError("UserPlayData does not exist.");
                return;
            }

            _selectedChapter = userPlayData.SelectedChapter;

            _chapterData = DataTableManager.Instance.GetChapterData(_selectedChapter);
            if (_chapterData == null)
            {
                Logger.LogError($"{_selectedChapter} chpterInfo does not exist.");
                return;
            }
        }

        private void Start()
        {
            AdsManager.Instance.EnableTopBannerAd(true);

            InGameUIController = FindObjectOfType<InGameUIController>();
            if (InGameUIController == null)
            {
                Logger.LogError("InGameUIController does not exist.");
                return;
            }

            InGameUIController.Init();

            GameSetup();
        }

        private async void GameSetup()
        {
            Logger.Log($"{GetType()}::GameStart");

            AudioManager.Instance.PlayBGM(BGM.in_game);

            var stage = await StageManager.Instance.LoadStage(_chapterData.StageName, stageContainer);
            if (!stage)
            {
                Logger.LogError("stage does not exist.");
                return;
            }

            NavigationManager.Instance.NavMeshBake();

            var player = await PlayerManager.Instance.LoadPlayer("Player", playerContainer);
            if (!player)
            {
                Logger.LogError("player does not exist.");
                return;
            }

            _player = player.GetComponent<Player>();

            CameraManager.Instance.Setup(player.transform, stage.CamRange);

            totalRewardGold = 0;

            StartRound();
        }

        private void StartRound()
        {
            _isRoundRunning = true;
            _isSpawnFinished = false;

            if (_roundTimerCo != null)
            {
                StopCoroutine(_roundTimerCo);
                _roundTimerCo = null;
            }
            _roundTimerCo = StartCoroutine(RoundTimerCo(_roundDuration));

            if (_spawnCo != null)
            {
                StopCoroutine(_spawnCo);
                _spawnCo = null;
            }
            _spawnCo = StartCoroutine(SpawnRoundCo());
        }

        private IEnumerator RoundTimerCo(float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                if (!IsPaused) t += Time.deltaTime;
                yield return null;
            }

            // 맥스 스테이지가 아니면 다음 스테이지로 강제 이동
            if (!IsMaxStage())
            {
                NextRoundOrClear();
            }
            // 맥스 스테이지면 아무것도 안 함 (전멸 시 OnEnemyDead에서 처리)
        }

        /// <summary>
        /// 라운드 시작 시: _roundDuration/3 동안 전체 적을 순차 스폰
        /// </summary>
        private IEnumerator SpawnRoundCo()
        {
            var stageInfos = DataTableManager.Instance.GetStageDatas(_selectedChapter, _currentStage);
            if (stageInfos == null)
            {
                _isSpawnFinished = true;
                _spawnCo = null;
                CheckRoundFinish();
                yield break;
            }

            var spawnQueue = new List<string>(64);
            foreach (var stageInfo in stageInfos)
            {
                for (int i = 0; i < stageInfo.EnemyCount; i++)
                    spawnQueue.Add(stageInfo.EnemyName);
            }

            if (spawnQueue.Count == 0)
            {
                _isSpawnFinished = true;
                _spawnCo = null;
                CheckRoundFinish();
                yield break;
            }

            float spawnSpreadDuration = _roundDuration / 3f;
            float interval = spawnSpreadDuration / spawnQueue.Count;
            var spawnIntervalWait = interval > 0f ? new WaitForSeconds(interval) : null;

            for (int idx = 0; idx < spawnQueue.Count; idx++)
            {
                while (IsPaused) yield return null;

                string enemyAddress = spawnQueue[idx];

                var task = EnemyManager.Instance.LoadEnemy(enemyAddress, _player.transform, enemyContainer);
                while (!task.IsCompleted) yield return null;

                if (!task.IsFaulted && task.Result)
                {
                    var enemy = task.Result;
                    Enemies.Add(enemy);
                    enemy.Died += OnEnemyDied;
                    TryWarpEnemyToRandomSpawn(enemy);
                }
                else if (task.IsFaulted)
                {
                    Logger.LogError($"LoadEnemy failed. {enemyAddress}\n{task.Exception}");
                }

                if (spawnIntervalWait != null && idx < spawnQueue.Count - 1)
                    yield return spawnIntervalWait;
            }

            _isSpawnFinished = true;
            _spawnCo = null;

            CheckRoundFinish();
        }

        /// <summary>
        /// 플레이어 기준 반지름 _spawnNoSpawnRadius 밖으로 워프
        /// NavMesh 위 샘플링
        /// </summary>
        private void TryWarpEnemyToRandomSpawn(BaseEnemy enemy)
        {
            if (!enemy || _player == null) return;

            Vector3 playerPos = _player.transform.position;
            playerPos.z = enemy.transform.position.z;

            float enemyRadius = GetEnemyRadius(enemy);
            float minSpacing = (enemyRadius * 2f) + _spawnMinExtraSpacing;

            float maxR = GetMaxSpawnRadiusFromNavMesh(playerPos);
            float minR = Mathf.Max(_spawnNoSpawnRadius, enemyRadius + 0.1f);
            float ringMax = Mathf.Min(maxR, minR + _spawnRingExtraRadius);
            if (ringMax < minR) ringMax = maxR;

            for (int t = 0; t < _spawnTryPerEnemy; t++)
            {
                Vector3 candidate = GetRandomPointInAnnulus(playerPos, minR, ringMax);

                if (!NavMesh.SamplePosition(candidate, out var hit, 2.0f, NavMesh.AllAreas))
                    continue;

                Vector3 pos = hit.position;
                pos.z = enemy.transform.position.z;

                if (IsTooCloseToOtherEnemies(pos, minSpacing))
                    continue;

                if (enemy.TryGetComponent<NavMeshAgent>(out var agent) && agent && agent.enabled)
                    agent.Warp(pos);
                else
                    enemy.transform.position = pos;

                return;
            }

            Vector3 fallback = playerPos + (Vector3)(Random.insideUnitCircle.normalized * (_spawnNoSpawnRadius + 0.25f));
            if (NavMesh.SamplePosition(fallback, out var fbHit, 4.0f, NavMesh.AllAreas))
            {
                Vector3 pos = fbHit.position;
                pos.z = enemy.transform.position.z;

                if (enemy.TryGetComponent<NavMeshAgent>(out var agent) && agent && agent.enabled)
                    agent.Warp(pos);
                else
                    enemy.transform.position = pos;
            }
        }

        private float GetEnemyRadius(BaseEnemy enemy)
        {
            var a = enemy.agent;
            if (a != null && a.enabled && a.radius > 0.01f)
                return a.radius;

            return 0.5f;
        }

        private Vector3 GetRandomPointInAnnulus(Vector3 center, float minR, float maxR)
        {
            float u = Random.value;
            float r = Mathf.Sqrt(Mathf.Lerp(minR * minR, maxR * maxR, u));
            float a = Random.Range(0f, Mathf.PI * 2f);

            Vector3 p = center + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
            p.z = center.z;
            return p;
        }

        private float GetMaxSpawnRadiusFromNavMesh(Vector3 playerPos)
        {
            var tri = NavMesh.CalculateTriangulation();
            if (tri.vertices == null || tri.vertices.Length == 0)
                return _spawnNoSpawnRadius + _spawnRingExtraRadius;

            float maxD = 0f;
            for (int i = 0; i < tri.vertices.Length; i++)
            {
                Vector3 v = tri.vertices[i];
                v.z = playerPos.z;
                float d = Vector3.Distance(playerPos, v);
                if (d > maxD) maxD = d;
            }

            maxD = Mathf.Max(_spawnNoSpawnRadius + 0.5f, maxD - _spawnMaxRadiusPadding);
            return maxD;
        }

        private bool IsTooCloseToOtherEnemies(Vector3 pos, float minSpacing)
        {
            int count = Physics2D.OverlapCircleNonAlloc(pos, minSpacing, _overlapBuffer);

            for (int i = 0; i < count; i++)
            {
                var c = _overlapBuffer[i];
                if (!c) continue;

                if (c.gameObject.layer == LayerMask.NameToLayer("Enemy"))
                    return true;

                if (c.transform.IsChildOf(enemyContainer))
                    return true;
            }

            return false;
        }

        #region ENEMY

        public BaseEnemy FindEnemy(float radius)
        {
            var enemies = InGameManager.Instance?.Enemies;
            if (enemies == null || enemies.Count == 0) return null;

            Vector3 targetPlayerPosition = _player != null ? _player.transform.position : transform.position;
            BaseEnemy best = null;
            float bestD2 = radius * radius;

            for (int i = 0, n = enemies.Count; i < n; i++)
            {
                var enemy = enemies[i];
                if (!enemy || !enemy.gameObject.activeInHierarchy) continue;

                float d2 = (enemy.transform.position - targetPlayerPosition).sqrMagnitude;
                if (d2 < bestD2)
                {
                    bestD2 = d2;
                    best = enemy;
                }
            }
            return best;
        }

        public List<BaseEnemy> FindEnemies(int maxTargets, float radius)
        {
            var result = new List<BaseEnemy>(maxTargets);
            var enemies = InGameManager.Instance?.Enemies;
            if (enemies == null || enemies.Count == 0) return result;

            Vector3 targetPlayerPosition = _player != null ? _player.transform.position : transform.position;
            for (int pick = 0; pick < maxTargets; pick++)
            {
                float bestD2 = radius * radius;
                int bestIdx = -1;

                for (int i = 0; i < enemies.Count; i++)
                {
                    var enemy = enemies[i];
                    if (!enemy || !enemy.gameObject.activeInHierarchy) continue;
                    if (result.Contains(enemy)) continue;

                    float d2 = (enemy.transform.position - targetPlayerPosition).sqrMagnitude;
                    if (d2 < bestD2) { bestD2 = d2; bestIdx = i; }
                }

                if (bestIdx >= 0) result.Add(enemies[bestIdx]);
                else break;
            }
            return result;
        }

        private void OnEnemyDied(BaseEnemy enemy)
        {
            if (enemy == null) return;

            totalRewardGold += enemy.RewardGold;

            enemy.Died -= OnEnemyDied;
            Enemies.Remove(enemy);

            CheckRoundFinish();
        }

        #endregion

        /// <summary>
        /// 스폰이 끝나기 전 전멸이면 return
        /// 스폰이 끝났고 전멸이면 다음 라운드 or 챕터 클리어
        /// </summary>
        private void CheckRoundFinish()
        {
            if (Enemies.Count > 0) return;
            if (!_isSpawnFinished) return;

            NextRoundOrClear();
        }

        private void NextRoundOrClear()
        {
            if (!_isRoundRunning) return;
            _isRoundRunning = false;

            if (_roundTimerCo != null)
            {
                StopCoroutine(_roundTimerCo);
                _roundTimerCo = null;
            }

            // 타임아웃으로 라운드 넘길 때, 스폰이 아직 남아있을 수 있음 (로드 지연 등)
            if (_spawnCo != null)
            {
                StopCoroutine(_spawnCo);
                _spawnCo = null;
            }

            // 라운드가 강제로 끝났다면 스폰도 끝난 상태로 변경.
            _isSpawnFinished = true;

            if (IsMaxStage())
            {
                ChapterClear();
                return;
            }

            _currentStage++;
            StartRound();
        }

        public void StageFail()
        {
            Logger.Log("Fail!");
            StartCoroutine(ShowStageFailCo());
        }

        private IEnumerator ShowStageFailCo()
        {
            AudioManager.Instance.PlaySFX(SFX.stage_clear);

            var uiData = new BaseUIData();
            UIManager.Instance.OpenUIFromAA<StageFailUI>(uiData);

            yield return new WaitForSeconds(1f);

            var stageFailUI = UIManager.Instance.GetActiveUI<StageFailUI>();
            if (stageFailUI != null)
            {
                SceneLoader.Instance.LoadScene(SceneType.Lobby);
                stageFailUI.CloseUI();
            }
        }

        private bool IsMaxStage()
        {
            return _currentStage == _chapterData.TotalStage;
        }

        private void ChapterClear()
        {
            AudioManager.Instance.PlaySFX(SFX.chapter_clear);
            GameManager.Instance.Pause(false);

            var userPlayData = UserDataManager.Instance.GetUserData<UserPlayData>();
            if (userPlayData == null)
            {
                Logger.LogError("UserPlayData does not exist.");
                return;
            }

            var chapter = DataTableManager.Instance.GetChapterData(_selectedChapter);
            if (chapter == null)
            {
                Logger.LogError($"ChapterData not found for chapterNo: {_selectedChapter}");
                return;
            }

            var rolledDropItems = new List<int>();
            if (chapter.DropItemIds != null && chapter.DropItemIds.Count > 0)
            {
                foreach (var itemId in chapter.DropItemIds)
                {
                    var item = DataTableManager.Instance.GetItemData(itemId);
                    if (item == null) continue;
                    
                    float rate = Mathf.Clamp01(item.DropRate * 0.01f);

                    float randomValue = Random.value;
                    Logger.Log($"{itemId} Drop Rate: {rate}, Value: {randomValue}");
                    if (randomValue <= rate)
                        rolledDropItems.Add(itemId);
                }
            }

            float mul = Random.Range(0.9f, 1.1f);
            int finalGold = Mathf.RoundToInt(totalRewardGold * mul);

            var uiData = new ChapterClearUIData
            {
                Chapter = _selectedChapter,
                EarnReward = _selectedChapter > userPlayData.MaxClearedChapter,
                GoldAmount = finalGold,
                RewardItems = rolledDropItems
            };
            UIManager.Instance.OpenUIFromAA<ChapterClearUI>(uiData);

            if (_selectedChapter > userPlayData.MaxClearedChapter)
            {
                userPlayData.MaxClearedChapter++;
                userPlayData.SelectedChapter = userPlayData.MaxClearedChapter < GlobalDefine.MAX_CHAPTER
                    ? userPlayData.MaxClearedChapter + 1
                    : userPlayData.MaxClearedChapter;

                userPlayData.SaveData();
            }

            var userAchievementData = UserDataManager.Instance.GetUserData<UserAchievementData>();
            if (userAchievementData != null)
            {
                switch (_selectedChapter)
                {
                    case 1: userAchievementData.ProgressAchievement(AchievementType.ClearChapter1, 1); break;
                    case 2: userAchievementData.ProgressAchievement(AchievementType.ClearChapter2, 1); break;
                    case 3: userAchievementData.ProgressAchievement(AchievementType.ClearChapter3, 1); break;
                    case 4: userAchievementData.ProgressAchievement(AchievementType.ClearChapter4, 1); break;
                    case 5: userAchievementData.ProgressAchievement(AchievementType.ClearChapter5, 1); break;
                    case 6: userAchievementData.ProgressAchievement(AchievementType.ClearChapter6, 1); break;
                }
            }
        }

        public void GamePause()
        {
            IsPaused = true;
            GameManager.Instance.Pause(true);
        }

        public void GameResume()
        {
            IsPaused = false;
            GameManager.Instance.Resume();
        }
    }
}
