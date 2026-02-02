using UnityEngine;

namespace PixelSurvival
{
    public class SingletonBehaviour<T> : MonoBehaviour where T : SingletonBehaviour<T>
    {
        // 씬 전환 시 삭제할지 여부
        protected bool isDestroyOnLoad = false;

        // 이 클래스의 스태틱 인스턴스 변수
        protected static T _instance;

        public static T Instance
        {
            get { return _instance; }
        }

        private void Awake()
        {
            Init();
        }

        protected virtual void Init()
        {
            if (_instance == null)
            {
                _instance = (T)this;

                if (!isDestroyOnLoad)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        // 삭제 시 실행되는 함수
        protected virtual void OnDestroy()
        {
            Dispose();
        }

        // 삭제 시 추가로 처리해 주어야할 작업을 여기서 처리
        protected virtual void Dispose()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}