using System.Collections;
using UnityEngine;

namespace PixelSurvival
{
    public class TelegraphLine : MonoBehaviour
    {
        [SerializeField] private LineRenderer _baseLine;
        [SerializeField] private LineRenderer _fillLine;

        [Range(0f, 1f)][SerializeField] private float _baseAlpha = 0.5f;
        [Range(0f, 1f)][SerializeField] private float _fillAlpha = 1.0f;

        [SerializeField] private float _defaultWidth = 0.25f;

        [Header("2D Z")]
        [SerializeField] private bool _useFixedZ = true;
        [SerializeField] private float _fixedZ = -1f;

        private Vector3 _start;
        private Vector3 _end;
        private float _duration;

        private Coroutine _co;

        private void OnDisable()
        {
            StopRunning();
        }

        public void Play(Vector3 startWorld, Vector3 endWorld, float duration, float width)
        {
            StopRunning();

            _start = startWorld;
            _end = endWorld;
            _duration = Mathf.Max(0f, duration);

            if (_useFixedZ)
            {
                _start.z = _fixedZ;
                _end.z = _fixedZ;
            }

            float w = (width > 0f) ? width : _defaultWidth;

            ApplyWidth(_baseLine, w);
            ApplyWidth(_fillLine, w);

            SetLine(_baseLine, _start, _end, _baseAlpha);
            SetLine(_fillLine, _start, _start, _fillAlpha);

            if (_duration <= 0f)
            {
                SetLine(_fillLine, _start, _end, _fillAlpha);
                return;
            }

            _co = StartCoroutine(FillCo());
        }

        private IEnumerator FillCo()
        {
            float t = 0f;

            while (t < _duration)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / _duration);

                Vector3 p = Vector3.Lerp(_start, _end, u);
                SetLine(_fillLine, _start, p, _fillAlpha);

                yield return null;
            }

            SetLine(_fillLine, _start, _end, _fillAlpha);
            _co = null;
        }

        private void SetLine(LineRenderer lr, Vector3 a, Vector3 b, float alpha)
        {
            if (!lr) return;

            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.SetPosition(0, a);
            lr.SetPosition(1, b);

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
