using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace PixelSurvival
{
    public enum EnemyType
    {
        Warrior,
        Archer,
        Wizard,
        Boss,
    }

    public class EnemyManager : SingletonBehaviour<EnemyManager>
    {
        protected override void Init()
        {
            isDestroyOnLoad = true;

            base.Init();
        }

        public async Task<BaseEnemy> LoadEnemy(string enemyAddress, Transform target, Transform parent)
        {
            Logger.Log($"{GetType()}::LoadEnemy");

            BaseEnemy enemy = null;
            var enemyData = DataTableManager.Instance.GetEnemyData(enemyAddress);

            switch (enemyData.EnemyType)
            {
                case EnemyType.Boss:
                    enemy = await GetBossEnemy(enemyData, target, parent);
                    InGameManager.Instance.InGameUIController.InitBoss(enemy, enemyData.EnemyName, enemyData.EnemyAddress);
                    break;
                default:
                    enemy = GetNormalEnemy(enemyData, target, parent);
                    break;
            }

            return enemy;
        }
        
        private BaseEnemy GetNormalEnemy(EnemyData enemyData, Transform target, Transform parent)
        {
            var enemy = PoolManager.Instance.Spawn<BaseEnemy>(PoolType.Enemy, Vector3.zero, Quaternion.identity, parent);
            
            if (!enemy)
            {
                Logger.LogError($"{enemyData.EnemyName} does not exist.");
                return null;
            }

            var baseEnemyData = new BaseEnemyData()
            {
                EnemyType = enemyData.EnemyType,
                MaxHP = enemyData.MaxHP,
                AttackDamage = enemyData.AttackDamage,
                MoveSpeed = enemyData.MoveSpeed,
                AttackSpeed = enemyData.AttackSpeed,
                AttackRange = enemyData.AttackRange,
                RewardGold = enemyData.RewardGold,
            };

            enemy.Init(target);
            enemy.SetInfo(baseEnemyData);

            var enemyCostumeData = DataTableManager.Instance.GetEnemyCostume(enemyData.EnemyAddress);
            enemy.SetEnemy(enemyCostumeData);

            return enemy;
        }

        private async Task<BaseEnemy> GetBossEnemy(EnemyData enemyData, Transform target, Transform parent)
        {
            AsyncOperationHandle<GameObject> operationHandle = Addressables.InstantiateAsync(enemyData.EnemyAddress);
            await operationHandle.Task;

            if (operationHandle.Status == AsyncOperationStatus.Succeeded)
            {
                var enemyObj = operationHandle.Result;
                var enemy = enemyObj.GetComponent<BaseEnemy>();
                
                if (enemy != null)
                {
                    
                    var baseEnemyData = new BaseEnemyData()
                    {
                        EnemyType = enemyData.EnemyType,
                        MaxHP = enemyData.MaxHP,
                        AttackDamage = enemyData.AttackDamage,
                        MoveSpeed = enemyData.MoveSpeed,
                        AttackSpeed = enemyData.AttackSpeed,
                        AttackRange = enemyData.AttackRange,
                        RewardGold = enemyData.RewardGold,
                    };

                    enemy.Init(target);
                    enemy.SetInfo(baseEnemyData);

                    return enemy;
                }
            }
            
            return null;
        }
    }
}