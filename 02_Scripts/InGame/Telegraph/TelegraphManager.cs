using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvival
{
    public class TelegraphManager : SingletonBehaviour<TelegraphManager>
    {
        [Header("Prefabs")]
        [SerializeField] private TelegraphLine _linePrefab;
        [SerializeField] private TelegraphCircle _circlePrefab;
        [SerializeField] private TelegraphCurve _curvePrefab;

        [Header("Pool")]
        [SerializeField] private int _prewarmLine = 16;
        [SerializeField] private int _prewarmCircle = 16;
        [SerializeField] private int _prewarmCurve = 16;
        [SerializeField] private int _prewarmDonut = 16;

        private readonly Stack<TelegraphLine> _linePool = new();
        private readonly Stack<TelegraphCircle> _circlePool = new();
        private readonly Stack<TelegraphCurve> _curvePool = new();

        private readonly Dictionary<int, MonoBehaviour> _active = new();
        private int _nextId = 1;

        protected override void Init()
        {
            isDestroyOnLoad = true;
            base.Init();
            Prewarm();
        }

        private void Prewarm()
        {
            if (_linePrefab != null)
            {
                for (int i = 0; i < _prewarmLine; i++)
                {
                    var inst = Instantiate(_linePrefab, transform);
                    inst.gameObject.SetActive(false);
                    _linePool.Push(inst);
                }
            }

            if (_circlePrefab != null)
            {
                for (int i = 0; i < _prewarmCircle; i++)
                {
                    var inst = Instantiate(_circlePrefab, transform);
                    inst.gameObject.SetActive(false);
                    _circlePool.Push(inst);
                }
            }

            if (_curvePrefab != null)
            {
                for (int i = 0; i < _prewarmCurve; i++)
                {
                    var inst = Instantiate(_curvePrefab, transform);
                    inst.gameObject.SetActive(false);
                    _curvePool.Push(inst);
                }
            }
        }

        private int NextId()
        {
            if (_nextId == int.MaxValue) _nextId = 1;
            return _nextId++;
        }

        private TelegraphLine GetLine()
        {
            if (_linePrefab == null) return null;
            return _linePool.Count > 0 ? _linePool.Pop() : Instantiate(_linePrefab, transform);
        }

        private TelegraphCircle GetCircle()
        {
            if (_circlePrefab == null) return null;
            return _circlePool.Count > 0 ? _circlePool.Pop() : Instantiate(_circlePrefab, transform);
        }

        private TelegraphCurve GetCurve()
        {
            if (_curvePrefab == null) return null;
            return _curvePool.Count > 0 ? _curvePool.Pop() : Instantiate(_curvePrefab, transform);
        }
        
        public int ShowLine(Vector3 startWorld, Vector3 endWorld, float duration, float width)
        {
            var line = GetLine();
            if (line == null)
            {
                Logger.LogError($"{GetType()} :: Line prefab is null.");
                return -1;
            }

            int id = NextId();
            _active[id] = line;

            line.transform.SetParent(transform, false);
            line.gameObject.SetActive(true);
            line.StopRunning();
            line.Play(startWorld, endWorld, duration, width);

            StartCoroutine(AutoHide(id, duration));
            return id;
        }

        public int ShowLine(Vector3 startWorld, Vector3 endWorld, float duration, Collider2D skillCollider = null)
        {
            float width = GetWidthFromCollider(skillCollider, 0.25f);
            return ShowLine(startWorld, endWorld, duration, width);
        }

        public int ShowCircle(Vector3 centerWorld, float radius, float duration)
        {
            var circle = GetCircle();
            if (circle == null)
            {
                Logger.LogError($"{GetType()} :: Circle prefab is null.");
                return -1;
            }

            int id = NextId();
            _active[id] = circle;

            circle.transform.SetParent(transform, false);
            circle.gameObject.SetActive(true);
            circle.StopRunning();
            circle.Play(centerWorld, radius, duration);

            StartCoroutine(AutoHide(id, duration));
            return id;
        }

        public int ShowCurve(Vector3[] pointsWorld, float duration, float width = 0.25f)
        {
            var curve = GetCurve();
            if (curve == null)
            {
                Logger.LogError($"{GetType()} :: Curve prefab is null.");
                return -1;
            }

            if (pointsWorld == null || pointsWorld.Length < 2)
            {
                Logger.LogWarning($"{GetType()} :: Curve points invalid.");
                return -1;
            }

            int id = NextId();
            _active[id] = curve;

            curve.transform.SetParent(transform, false);
            curve.gameObject.SetActive(true);
            curve.StopRunning();
            curve.Play(pointsWorld, duration, width);

            StartCoroutine(AutoHide(id, duration));
            return id;
        }

        public void Hide(int id)
        {
            if (!_active.TryGetValue(id, out var obj) || obj == null)
                return;

            _active.Remove(id);

            if (obj is TelegraphLine line)
            {
                line.StopRunning();
                line.gameObject.SetActive(false);
                line.transform.SetParent(transform, false);
                _linePool.Push(line);
                return;
            }

            if (obj is TelegraphCircle circle)
            {
                circle.StopRunning();
                circle.gameObject.SetActive(false);
                circle.transform.SetParent(transform, false);
                _circlePool.Push(circle);
                return;
            }

            if (obj is TelegraphCurve curve)
            {
                curve.StopRunning();
                curve.gameObject.SetActive(false);
                curve.transform.SetParent(transform, false);
                _curvePool.Push(curve);
                return;
            }
        }

        private IEnumerator AutoHide(int id, float duration)
        {
            if (duration <= 0f) yield return null;
            else yield return new WaitForSeconds(duration);

            Hide(id);
        }

        private float GetWidthFromCollider(Collider2D col, float defaultWidth)
        {
            if (col == null) return defaultWidth;

            Vector3 s = col.transform.lossyScale;
            float sx = Mathf.Abs(s.x);
            float sy = Mathf.Abs(s.y);

            switch (col)
            {
                case CircleCollider2D c:
                {
                    float diameter = c.radius * 2f;
                    float scale = (sx + sy) * 0.5f;
                    return Mathf.Max(0.01f, diameter * scale);
                }
                case CapsuleCollider2D cap:
                {
                    Vector2 size = cap.size;
                    float w = Mathf.Min(size.x * sx, size.y * sy);
                    return Mathf.Max(0.01f, w);
                }
                case BoxCollider2D box:
                {
                    Vector2 size = box.size;
                    float w = Mathf.Min(size.x * sx, size.y * sy);
                    return Mathf.Max(0.01f, w);
                }
                default:
                {
                    var b = col.bounds.size;
                    float w = Mathf.Min(b.x, b.y);
                    return Mathf.Max(0.01f, w);
                }
            }
        }
    }
}
