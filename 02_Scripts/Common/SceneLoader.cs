using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelSurvival
{
    public enum SceneType
    {
        Title,
        Lobby,
        InGame,
    }

    public class SceneLoader : SingletonBehaviour<SceneLoader>
    {
        public void LoadScene(SceneType sceneType)
        {
            Logger.Log($"{sceneType}  scene loading...");

            Time.timeScale = 1;
            SceneManager.LoadScene(sceneType.ToString());
        }

        public void ReloadScene()
        {
            Logger.Log($"{SceneManager.GetActiveScene().name}  reload scene...");

            Time.timeScale = 1;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public AsyncOperation LoadSceneAsync(SceneType sceneType)
        {
            Logger.Log($"{sceneType}  scene loading...");

            Time.timeScale = 1;
            return SceneManager.LoadSceneAsync(sceneType.ToString());
        }

        public SceneType GetCurrentScene()
        {
            return (SceneType)Enum.Parse(typeof(SceneType), SceneManager.GetActiveScene().name);
        }
    }
}