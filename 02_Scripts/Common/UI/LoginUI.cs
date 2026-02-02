using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvival
{
    public class LoginUI : BaseUI
    {
        public void OnClickSignInWithGoogle()
        {
            Logger.Log($"{GetType()}::onClickSignInWithGoogle");
            
            FirebaseManager.Instance.SignInWithGoogle();
            CloseUI();
        }
        
        public void OnClickSignInWithApple()
        {
            Logger.Log($"{GetType()}::onClickSignInWithApple");
            
            FirebaseManager.Instance.SignInWithApple();
            CloseUI();
        }
    }   
}
