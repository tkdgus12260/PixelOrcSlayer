using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace PixelSurvival
{
    public class SkillManager : SingletonBehaviour<SkillManager>
    {
        protected override void Init()
        {
            isDestroyOnLoad = true;
            base.Init();
        }

        public async Task<GameObject> SetSkill(UserItemData userItemData, Team team)
        {
            var itemId = userItemData.ItemId;
            var itemData = DataTableManager.Instance.GetItemData(itemId);
            var skillDamage = itemData.Damage + (userItemData.UpgradeLevel * itemData.ValueIncrease);
            var skillData = DataTableManager.Instance.GetSkillData(itemId);
            AsyncOperationHandle<GameObject> operationHandle = Addressables.InstantiateAsync(skillData.SkillName);
            await operationHandle.Task;

            if (operationHandle.Status == AsyncOperationStatus.Succeeded)
            {
                var skillObj = operationHandle.Result;
                if (skillObj != null)
                {
                    if (skillObj.TryGetComponent<ISkill>(out var skill))
                    {
                        skill.Init(skillData, skillDamage, team);
                    }
                    return skillObj;
                }
            }

            return null;
        }
    }
}
