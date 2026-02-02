using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace PixelSurvival
{
    public class BossOffscreenArrow : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Canvas canvas;
        [SerializeField] private Camera uiCamera;
        [SerializeField] private RectTransform arrowRect;

        private readonly string ArrowId = "_Arrow";
        
        [Header("Options")]
        [SerializeField] private float edgePadding = 60f;

        private RectTransform _canvasRect;
        [SerializeField] private RawImage _currentBossBg;
        
        private Transform _target;

        private void Awake()
        {
            if (!worldCamera) worldCamera = Camera.main;
            if (!canvas) canvas = GetComponentInParent<Canvas>();
            if (canvas && !uiCamera) uiCamera = canvas.renderMode == RenderMode.ScreenSpaceCamera ? canvas.worldCamera : null;

            if (!arrowRect) arrowRect = GetComponent<RectTransform>();
            if (canvas) _canvasRect = canvas.transform as RectTransform;

            arrowRect.gameObject.SetActive(false);
        }

        public async void SetInfo(string enemyAddress)
        {
            if (enemyAddress == string.Empty)
            {
                Logger.LogError("Enemy address is empty");
                return;
            }
            
            AsyncOperationHandle<Texture2D> operationHandle =
                Addressables.LoadAssetAsync<Texture2D>(enemyAddress + ArrowId);
            await operationHandle.Task;

            if (operationHandle.Status == AsyncOperationStatus.Succeeded)
            {
                var bossTexture = operationHandle.Result;
                if (bossTexture != null)
                {
                    _currentBossBg.texture = bossTexture;
                }
            }
        }

        public void Show(Transform boss)
        {
            _target = boss;
            arrowRect.gameObject.SetActive(_target != null);
        }

        public void Hide()
        {
            _target = null;
            arrowRect.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (!_target)
            {
                if (arrowRect.gameObject.activeSelf) arrowRect.gameObject.SetActive(false);
                return;
            }

            if (!worldCamera || !arrowRect || !_canvasRect) return;

            UpdateArrow();
        }

        private void UpdateArrow()
        {
            Vector3 vp = worldCamera.WorldToViewportPoint(_target.position);

            if (vp.z < 0f)
            {
                vp.x = 1f - vp.x;
                vp.y = 1f - vp.y;
                vp.z = 0.01f;
            }

            bool onScreen =
                vp.x >= 0f && vp.x <= 1f &&
                vp.y >= 0f && vp.y <= 1f &&
                vp.z > 0f;

            if (onScreen)
            {
                if (arrowRect.gameObject.activeSelf) arrowRect.gameObject.SetActive(false);
                return;
            }

            if (!arrowRect.gameObject.activeSelf) arrowRect.gameObject.SetActive(true);

            // 방향(뷰포트 중심 기준)
            Vector2 fromCenter = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
            if (fromCenter.sqrMagnitude < 0.000001f) fromCenter = Vector2.up;
            fromCenter.Normalize();

            // 뷰포트 경계 교점
            float tX = fromCenter.x != 0f ? (0.5f / Mathf.Abs(fromCenter.x)) : float.PositiveInfinity;
            float tY = fromCenter.y != 0f ? (0.5f / Mathf.Abs(fromCenter.y)) : float.PositiveInfinity;
            float t = Mathf.Min(tX, tY);
            Vector2 edgeVp = new Vector2(0.5f, 0.5f) + fromCenter * t;

            // "스크린 픽셀" 위치
            Vector2 screenPos = new Vector2(edgeVp.x * Screen.width, edgeVp.y * Screen.height);

            // 패딩(스크린 중심 기준으로 안쪽으로)
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 dirPx = (screenPos - screenCenter).normalized;
            screenPos -= dirPx * edgePadding;

            // ✅ Screen 좌표 -> Canvas 로컬 좌표로 변환해서 anchoredPosition에 넣기
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPos, uiCamera, out var localPos))
                arrowRect.anchoredPosition = localPos;

            // 회전
            float angle = Mathf.Atan2(fromCenter.y, fromCenter.x) * Mathf.Rad2Deg;
            arrowRect.localEulerAngles = new Vector3(0f, 0f, angle);
        }
    }
}
