using System.Collections;
using System.Collections.Generic;
using Gpm.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace PixelSurvival
{
    public class ChapterScrollItemData : InfiniteScrollData
    {
        public int ChapterNo;
    }

    public class ChapterScrollItem : InfiniteScrollItem
    {
        public GameObject CurrChapter;
        public RawImage CurrChapterBg;
        public Image Dim;
        public Image LockIcon;
        public Image Round;
        public TextMeshProUGUI ComingSoonTxt;

        private ChapterScrollItemData _chapterScrollItemData;

        public override async void UpdateData(InfiniteScrollData scrollData)
        {
            base.UpdateData(scrollData);

            _chapterScrollItemData = scrollData as ChapterScrollItemData;
            if (_chapterScrollItemData == null)
            {
                Logger.LogError("Invalid ChapterScrollItemData");
                return;
            }

            if (_chapterScrollItemData.ChapterNo > GlobalDefine.MAX_CHAPTER)
            {
                CurrChapter.SetActive(false);
                ComingSoonTxt.gameObject.SetActive(true);
            }
            else
            {
                CurrChapter.SetActive(true);
                ComingSoonTxt.gameObject.SetActive(false);

                var userPlayData = UserDataManager.Instance.GetUserData<UserPlayData>();
                if (userPlayData != null)
                {
                    var isLocked = _chapterScrollItemData.ChapterNo > userPlayData.MaxClearedChapter + 1;
                    Dim.gameObject.SetActive(isLocked);
                    LockIcon.gameObject.SetActive(isLocked);
                    Round.color = isLocked ? new Color(0.5f, 0.5f, 0.5f, 1f) : Color.white;
                }

                AsyncOperationHandle<Texture2D> operationHandle =
                    Addressables.LoadAssetAsync<Texture2D>($"ChapterBG{_chapterScrollItemData.ChapterNo.ToString("D3")}");
                await operationHandle.Task;

                if (operationHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    var bgTexture = operationHandle.Result;
                    if (bgTexture != null)
                    {
                        CurrChapterBg.texture = bgTexture;
                    }
                }
            }
        }
    }
}