using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace PixelSurvival
{
    public class RewardItemSlotData
    {
        public int ItemId;
        public int GoodsAmount;
        public bool FirstReward;
    }
    
    public class RewardItemSlot : MonoBehaviour
    {
        public Image Background;
        public Image ItemIcon;
        public TextMeshProUGUI AmountTxt;
        public GameObject FirstRewardImage;

        private RewardItemSlotData _rewardItemSlotData;
        
        public async void SetInfo(RewardItemSlotData data)
        {
            _rewardItemSlotData = data;
            
            if (_rewardItemSlotData.ItemId == 1 || _rewardItemSlotData.ItemId == 2)
            {
                AsyncOperationHandle<Texture2D> operationHandle =
                    Addressables.LoadAssetAsync<Texture2D>(_rewardItemSlotData.ItemId.ToString());
                await operationHandle.Task;

                if (operationHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    var itemIconTexture = operationHandle.Result;
                    if (itemIconTexture != null)
                    {
                        ItemIcon.sprite = Sprite.Create(itemIconTexture,
                            new Rect(0, 0, itemIconTexture.width, itemIconTexture.height), new Vector2(1f, 1f));
                    }

                    if (_rewardItemSlotData.GoodsAmount > 0)
                    {
                        AmountTxt.gameObject.SetActive(true);
                        AmountTxt.text = _rewardItemSlotData.GoodsAmount.ToString("N0");
                        Background.gameObject.SetActive(false);
                    }
                    else
                    {
                        AmountTxt.gameObject.SetActive(false);
                        Background.gameObject.SetActive(true);
                    }

                    FirstRewardImage.SetActive(_rewardItemSlotData.FirstReward);
                }
            }
            else
            {
                AmountTxt.gameObject.SetActive(false);
                Background.gameObject.SetActive(true);
                FirstRewardImage.SetActive(_rewardItemSlotData.FirstReward);
                
                StringBuilder sb = new StringBuilder(_rewardItemSlotData.ItemId.ToString());
                sb[1] = '1';
                var itemIconName = sb.ToString();
                
                var address = $"Equipments[{itemIconName}]";
                AsyncOperationHandle<Sprite> operationHandle =
                    Addressables.LoadAssetAsync<Sprite>(address);
                await operationHandle.Task;

                if (operationHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    var itemIconTSprite = operationHandle.Result;
                    if (itemIconTSprite != null)
                    {
                        ItemIcon.sprite = itemIconTSprite;
                    }

                    var itemGrade = (ItemGrade)((_rewardItemSlotData.ItemId / 1000) % 10);
                    var hexColor = string.Empty;
                    switch (itemGrade)
                    {
                        case ItemGrade.Common:    hexColor = "#1AB3FF"; break;
                        case ItemGrade.Uncommon:  hexColor = "#51C52C"; break;
                        case ItemGrade.Rare:      hexColor = "#EA5AFF"; break;
                        case ItemGrade.Epic:      hexColor = "#FF9900"; break;
                        case ItemGrade.Legendary: hexColor = "#F24949"; break;
                        default: break;
                    }

                    Color color;
                    if (ColorUtility.TryParseHtmlString(hexColor, out color))
                    {
                        Background.color = color;
                    }
                }
            }
        }

        public void OnClickItem()
        {
            // 골드 보석일 때 리턴
            if (_rewardItemSlotData.ItemId == 1 || _rewardItemSlotData.ItemId == 2)
                return;
            
            AudioManager.Instance.PlaySFX(SFX.ui_button_click);

            var uiData = new DescriptionUIData();
            uiData.ItemId = _rewardItemSlotData.ItemId;
            
            UIManager.Instance.OpenUIFromAA<DescriptionUI>(uiData);
        }
    }
}