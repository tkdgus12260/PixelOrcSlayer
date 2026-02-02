using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvival
{
    public class PauseUI : BaseUI
    {
        public void OnClickResume()
        {
            InGameManager.Instance.GameResume();
            
            CloseUI();
        }
        
        public void OnClickHome()
        {
            SceneLoader.Instance.LoadScene(SceneType.Lobby);
            
            CloseUI();
        }
    }
}