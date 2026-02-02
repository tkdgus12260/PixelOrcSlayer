using System.Collections;
using System.Collections.Generic;
using Gpm.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PixelSurvival
{
    public class ChapterListUI : BaseUI
    {
        public InfiniteScroll ChapterScrollList;
        public GameObject SelectedChapterName;
        public TextMeshProUGUI SelectedChapterNameTxt;
        public Button SelectBtn;

        private int SelectedChapter;

        public override void SetInfo(BaseUIData uiData)
        {
            base.SetInfo(uiData);

            var userPlayData = UserDataManager.Instance.GetUserData<UserPlayData>();
            if (userPlayData == null)
            {
                return;
            }

            SelectedChapter = userPlayData.SelectedChapter;

            SetSelectedChapter();
            SetChapterScrollList();

            ChapterScrollList.MoveTo(SelectedChapter - 1, InfiniteScroll.MoveToType.MOVE_TO_CENTER);
            ChapterScrollList.OnSnap = (currentSnappedIndex) =>
            {
                var chapterListUI = UIManager.Instance.GetActiveUI<ChapterListUI>() as ChapterListUI;
                if (chapterListUI != null)
                {
                    chapterListUI.OnSnap(currentSnappedIndex + 1);
                }
            };
        }

        private void SetSelectedChapter()
        {
            if (SelectedChapter <= GlobalDefine.MAX_CHAPTER)
            {
                SelectedChapterName.SetActive(true);
                SelectBtn.gameObject.SetActive(true);

                var itemData = DataTableManager.Instance.GetChapterData(SelectedChapter);
                if (itemData != null)
                {
                    SelectedChapterNameTxt.text = itemData.ChapterName;
                }
            }
            else
            {
                SelectedChapterName.SetActive(false);
                SelectBtn.gameObject.SetActive(false);
            }
        }

        private void SetChapterScrollList()
        {
            ChapterScrollList.Clear();

            for (int i = 1; i <= GlobalDefine.MAX_CHAPTER + 1; i++)
            {
                var chapterItemData = new ChapterScrollItemData();
                chapterItemData.ChapterNo = i;
                ChapterScrollList.InsertData(chapterItemData);
            }
        }

        public void OnSnap(int selectedChapter)
        {
            if(SelectedChapter != selectedChapter)
                AudioManager.Instance.PlaySFX(SFX.chapter_scroll);
            
            SelectedChapter = selectedChapter;
            SetSelectedChapter();
        }

        public void OnClickSelect()
        {
            AudioManager.Instance.PlaySFX(SFX.chapter_select);
            
            var userPlayData = UserDataManager.Instance.GetUserData<UserPlayData>();
            if (userPlayData == null)
            {
                Logger.LogError("userPlayData does not exist.");
                return;
            }

            if (SelectedChapter <= userPlayData.MaxClearedChapter + 1)
            {
                userPlayData.SelectedChapter = SelectedChapter;
                LobbyManager.Instance.LobbyUIController.SetCurrChapter();
                CloseUI();
            }
        }
    }
}