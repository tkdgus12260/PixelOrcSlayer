using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvival
{
    public class GameManager : SingletonBehaviour<GameManager>
    {
        public bool IsPaused { get; private set; }

        public void Pause(bool timePaused)
        {
            IsPaused = true;
            
            if(timePaused)
             Time.timeScale = 0;
        }

        public void Resume()
        {
            IsPaused = false;
            Time.timeScale = 1;
        }
    }
}