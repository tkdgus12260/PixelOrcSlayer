using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace PixelSurvival
{
    public class PlayerManager : SingletonBehaviour<PlayerManager>
    {
        public GameObject PlayerContainer;
        public Camera UICamera;
        
        protected override void Init()
        {
            base.Init();
        }

        private async Task<BasePlayer> GetPlayer(string playerName, Transform parent)
        {
            BasePlayer player = null;

            AsyncOperationHandle<GameObject> operationHandle =
                Addressables.InstantiateAsync(playerName);
            await operationHandle.Task;

            if (operationHandle.Status == AsyncOperationStatus.Succeeded)
            {
                var playerObj = operationHandle.Result;
                if (playerObj != null)
                {
                    playerObj.transform.SetParent(parent);
                    player = playerObj.GetComponent<BasePlayer>();
                }
            }

            return player;
        }

        public async Task<BasePlayer> LoadPlayer(string playerName, Transform parent)
        {
            Logger.Log($"{GetType()}::LoadPlayer");

            var player = await GetPlayer(playerName, parent);
            if (!player)
            {
                Logger.LogError($"{playerName} Player does not exist.");
                return null;
            }

            var playerData = DataTableManager.Instance.GetplayerData(playerName);
            if (playerData == null)
            {
                Logger.LogError($"{playerName} PlayerData does not exist.");
                return null;
            }

            var basePlayerData = new BasePlayerData()
            {
                MaxHP = playerData.MaxHP,
                AttackDamage = playerData.AttackDamage,
                MoveSpeed = playerData.MoveSpeed,
                AttackSpeed = playerData.AttackSpeed
            };

            player.Init();
            player.SetInfo(basePlayerData);

            return player;
        }

        public async Task<GameObject> LoadInventoryPlayer(string playerName)
        {
            BasePlayer player = null;

            AsyncOperationHandle<GameObject> operationHandle =
                Addressables.InstantiateAsync(playerName);
            await operationHandle.Task;

            if (operationHandle.Status == AsyncOperationStatus.Succeeded)
            {
                var playerObj = operationHandle.Result;
                if (playerObj != null)
                {
                    playerObj.transform.SetParent(PlayerContainer.transform);
                    playerObj.transform.localPosition = Vector3.zero;
                    playerObj.transform.localRotation = Quaternion.identity;
                    playerObj.transform.localScale = Vector3.one;
                }
                
                return playerObj;
            }
            return null;
        }

        public void ShowInventoryPlayer()
        {
            if (PlayerContainer != null)
            {
                PlayerContainer.gameObject.SetActive(true);
            }
        }
        
        public void HideInventoryPlayer()
        {
            if (PlayerContainer != null)
            {
                PlayerContainer.gameObject.SetActive(false);
            }
        }
        
        public void SetPlayerContainerPos(RectTransform targetUIRect)
        {
            if (PlayerContainer == null)
            {
                Logger.LogError("PlayerContainer is null");
                return;
            }

            if (targetUIRect == null)
            {
                Logger.LogError("targetUIRect is null");
                return;
            }
            
            PlayerContainer.gameObject.SetActive(true);
            
            var playerContainerRT = PlayerContainer.GetComponent<RectTransform>();
            if (playerContainerRT != null)
            {
                playerContainerRT.SetParent(targetUIRect, false);
                playerContainerRT.anchoredPosition3D = Vector3.zero;
                playerContainerRT.localRotation = Quaternion.identity;
                playerContainerRT.localScale = Vector3.one;
                return;
            }

            if (UICamera == null)
            {
                var camObj = GameObject.Find("UICamera");
                if (camObj != null) UICamera = camObj.GetComponent<Camera>();
            }

            if (UICamera == null)
            {
                Logger.LogError("UICamera is null. Assign it in inspector or ensure there is a 'UICamera' object.");
                return;
            }

            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(UICamera, targetUIRect.position);

            float targetZ = 1;

            float distance = Mathf.Abs(targetZ - UICamera.transform.position.z);

            Vector3 worldPos = UICamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, distance));
            worldPos.z = targetZ;

            PlayerContainer.transform.position = worldPos;
        }

        // Character creation without the Navmesh agent will be implemented separately in the future. (For simple creation, do not run SetInfo.)
        // Character customization will be implemented.
    }
}