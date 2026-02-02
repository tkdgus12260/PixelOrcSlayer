using System.Collections;
using UnityEngine;

namespace PixelSurvival
{
    /// <summary>
    /// 커브 텔레그래프(2개 LineRenderer)
    /// - Base: 알파 0.5 고정, 전체 커브
    /// - Fill: 알파 1.0, duration 동안 커브를 따라 차오름
    /// </summary>
    public class TelegraphCurve : MonoBehaviour
    {
        [SerializeField] private LineRenderer _baseLine;
        [SerializeField] private LineRenderer _fillLine;

        [Range(0f, 1f)][SerializeField] private float _baseAlpha = 0.5f;
        [Range(0f, 1f)][SerializeField] private float _fillAlpha = 1.0f;

        [SerializeField] private float _defaultWidth = 0.25f;

        [Header("2D Z")]
        [SerializeField] private bool _useFixedZ = true;
        [SerializeField] private float _fixedZ = -1f;

        private Vector3[] _points;   // 전체 커브 포인트
        private float _duration;

        private Coroutine _co;

        private void OnDisable()
        {
            StopRunning();
        }

        /// <param name="pointsWorld">커브를 구성하는 월드 포인트들(최소 2개)</param>
        public void Play(Vector3[] pointsWorld, float duration, float width)
        {
            StopRunning();

            if (pointsWorld == null || pointsWorld.Length < 2)
            {
                // 안전 처리
                if (_baseLine) _baseLine.positionCount = 0;
                if (_fillLine) _fillLine.positionCount = 0;
                return;
            }

            _duration = Mathf.Max(0f, duration);

            // 포인트 복사(+Z 고정 옵션)
            _points = new Vector3[pointsWorld.Length];
            for (int i = 0; i < pointsWorld.Length; i++)
            {
                var p = pointsWorld[i];
                if (_useFixedZ) p.z = _fixedZ;
                _points[i] = p;
            }

            float w = (width > 0f) ? width : _defaultWidth;
            ApplyWidth(_baseLine, w);
            ApplyWidth(_fillLine, w);

            // Base: 전체 커브
            SetLine(_baseLine, _points, _baseAlpha);

            // Fill: 시작점만 보이게
            SetLine(_fillLine, new Vector3[] { _points[0], _points[0] }, _fillAlpha);

            if (_duration <= 0f)
            {
                SetLine(_fillLine, _points, _fillAlpha);
                return;
            }

            _co = StartCoroutine(FillCo());
        }

        private IEnumerator FillCo()
        {
            float t = 0f;
            int n = _points.Length;

            while (t < _duration)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / _duration);

                // u에 해당하는 진행 인덱스 계산
                float f = (n - 1) * u;
                int k = Mathf.FloorToInt(f);
                k = Mathf.Clamp(k, 0, n - 2);

                float localU = f - k; // k~k+1 사이 보간값

                // Fill 포인트 구성: 0..k까지 + 마지막 보간점 1개
                int fillCount = (k + 2); // (0..k) + last
                if (fillCount < 2) fillCount = 2;

                var temp = new Vector3[fillCount];
                for (int i = 0; i <= k; i++)
                    temp[i] = _points[i];

                temp[fillCount - 1] = Vector3.Lerp(_points[k], _points[k + 1], localU);

                SetLine(_fillLine, temp, _fillAlpha);

                yield return null;
            }

            // 끝까지 채움
            SetLine(_fillLine, _points, _fillAlpha);
            _co = null;
        }

        private void SetLine(LineRenderer lr, Vector3[] pts, float alpha)
        {
            if (!lr) return;

            lr.useWorldSpace = true;
            lr.positionCount = pts.Length;
            lr.SetPositions(pts);

            var c0 = lr.startColor; c0.a = alpha;
            var c1 = lr.endColor;   c1.a = alpha;
            lr.startColor = c0;
            lr.endColor = c1;
        }

        private void ApplyWidth(LineRenderer lr, float width)
        {
            if (!lr) return;
            lr.startWidth = width;
            lr.endWidth = width;
        }

        public void StopRunning()
        {
            if (_co != null)
            {
                StopCoroutine(_co);
                _co = null;
            }
        }
    }
}
