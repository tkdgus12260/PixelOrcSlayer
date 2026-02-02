using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace PixelSurvival
{
    public class StageManager : SingletonBehaviour<StageManager>
    {
        private async Task<BaseStage> GetStage(string stageName, Transform parent)
        {
            BaseStage stage = null;
            
            AsyncOperationHandle<GameObject> operationHandle =
                Addressables.InstantiateAsync(stageName);
            await operationHandle.Task;

            if (operationHandle.Status == AsyncOperationStatus.Succeeded)
            {
                var stageObj = operationHandle.Result;
                if (stageObj != null)
                {
                    stageObj.transform.SetParent(parent);
                    stage = stageObj.GetComponent<BaseStage>();
                }
            }

            return stage;
        }

        public async Task<BaseStage> LoadStage(string stageName, Transform parent)
        {
            Logger.Log($"{GetType()}::LoadStage");

            var stage = await GetStage(stageName, parent);
            if (!stage)
            {
                Logger.LogError($"{stageName} Stage does not exist");
                return null;
            }

            return stage;
        }
    }
}