using System.Collections;
using UnityEngine;

namespace PixelSurvival
{
    /// <summary>
    /// 원형 텔레그래프(2개 SpriteRenderer)
    /// - Base: 알파 0.5 고정, 최종 radius 크기
    /// - Fill: 알파 1.0, duration 동안 0 -> radius로 커짐(면 채워지는 느낌)
    /// </summary>
    public class TelegraphCircle : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _baseCircle;
        [SerializeField] private SpriteRenderer _fillCircle;

        [Range(0f, 1f)][SerializeField] private float _baseAlpha = 0.5f;
        [Range(0f, 1f)][SerializeField] private float _fillAlpha = 1.0f;

        [Header("2D Z")]
        [SerializeField] private bool _useFixedZ = true;
        [SerializeField] private float _fixedZ = -1f;

        private float _radius;
        private float _duration;

        private Coroutine _co;

        private void OnDisable()
        {
            StopRunning();
        }

        public void Play(Vector3 centerWorld, float radius, float duration)
        {
            StopRunning();

            if (_useFixedZ)
                centerWorld.z = _fixedZ;

            transform.position = centerWorld;

            _radius = Mathf.Max(0f, radius);
            _duration = Mathf.Max(0f, duration);

            if (_baseCircle)
            {
                SetAlpha(_baseCircle, _baseAlpha);
                SetWorldRadius(_baseCircle, _radius);
            }

            if (_fillCircle)
            {
                SetAlpha(_fillCircle, _fillAlpha);
                SetWorldRadius(_fillCircle, 0f);
            }

            if (_duration <= 0f)
            {
                if (_fillCircle) SetWorldRadius(_fillCircle, _radius);
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

                float r = Mathf.Lerp(0f, _radius, u);
                if (_fillCircle) SetWorldRadius(_fillCircle, r);

                yield return null;
            }

            if (_fillCircle) SetWorldRadius(_fillCircle, _radius);
            _co = null;
        }

        private void SetWorldRadius(SpriteRenderer sr, float radius)
        {
            if (!sr || !sr.sprite) return;

            // sprite.bounds.extents.x : 로컬 기준 반지름(유닛)
            float spriteUnitRadius = sr.sprite.bounds.extents.x;
            if (spriteUnitRadius <= 0f) return;

            float scale = radius / spriteUnitRadius;
            sr.transform.localScale = new Vector3(scale, scale, 1f);
        }

        private void SetAlpha(SpriteRenderer sr, float alpha)
        {
            if (!sr) return;
            var c = sr.color;
            c.a = alpha;
            sr.color = c;
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
