using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PixelSurvival
{
    public class Player : BasePlayer, IDamageable
    {
        public Team Team => Team.Player;
        public bool IsInvulnerable { get; set; }
        private Coroutine _invulnCoroutine;

        private int _currentHp;
        public int CurrentHp => _currentHp;
        public float CurrentHpPercent => MaxHP <= 0 ? 0f : (float)_currentHp / MaxHP;

        [SerializeField] private Transform skillTransform;

        private readonly List<UserItemData> _equippedItemsCache = new List<UserItemData>(8);

        public override void SetInfo(BasePlayerData enemyData)
        {
            base.SetInfo(enemyData);

            agent.speed = moveSpeed;
            IsInvulnerable = false;

            SetEquippedItems();
            SetMaxHpFromEquipped();
            Logger.Log("KSH Hp : " + MaxHP);
            _currentHp = MaxHP;

            SetupSkillsFromEquipped();
        }

        private void SetEquippedItems()
        {
            _equippedItemsCache.Clear();

            var userInventoryData = UserDataManager.Instance.GetUserData<UserInventoryData>();
            if (userInventoryData == null) return;

            foreach (var equipped in userInventoryData.GetEquippedItemData())
            {
                if (equipped != null)
                    _equippedItemsCache.Add(equipped);
            }
        }

        private void SetMaxHpFromEquipped()
        {
            int totalHp = 0;

            for (int i = 0; i < _equippedItemsCache.Count; i++)
            {
                var userItem = _equippedItemsCache[i];
                if (userItem == null) continue;

                var itemData = DataTableManager.Instance.GetItemData(userItem.ItemId);
                if (itemData == null) continue;

                int baseHp = itemData.Hp;
                int hpIncrease = itemData.ValueIncrease;

                if(baseHp <= 0) continue;
                
                int increaseHp = baseHp + (userItem.UpgradeLevel * hpIncrease);
                totalHp += increaseHp;
            }

            MaxHP += totalHp;
        }

        #region SkillSettings

        private async void SetupSkillsFromEquipped()
        {
            ClearSkills(skillTransform);

            for (int i = 0; i < _equippedItemsCache.Count; i++)
            {
                var itemData = _equippedItemsCache[i];
                if (itemData == null) continue;

                var skillObj = await SkillManager.Instance.SetSkill(itemData, Team);
                if (skillObj != null)
                    skillObj.transform.SetParent(skillTransform);
            }
        }

        private void ClearSkills(Transform root)
        {
            if (!root) return;

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var c = root.GetChild(i);
                if (c) Destroy(c.gameObject);
            }
        }

        #endregion

        public override void OnAttack() { }

        private void OnHit()
        {
            if (_invulnCoroutine != null) StopCoroutine(_invulnCoroutine);
            _invulnCoroutine = StartCoroutine(InvulnerabilityCo());
        }

        private IEnumerator InvulnerabilityCo()
        {
            IsInvulnerable = true;
            SetCostumeAlpha(0.2f);
            yield return GlobalDefine.INVULN_TIME;
            SetCostumeAlpha(1f);
            IsInvulnerable = false;
            _invulnCoroutine = null;
        }

        public void TakeDamage(int damage)
        {
            if (IsInvulnerable) return;

            OnHit();
            _currentHp -= damage;

            if (_currentHp <= 0)
            {
                _currentHp = 0;
                Logger.Log("Player Death");
                InGameManager.Instance.StageFail();
            }
        }
    }
}
