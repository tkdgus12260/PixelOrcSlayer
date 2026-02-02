using System;
using System.Collections;
using UnityEngine;

namespace PixelSurvival
{
    public class ElementalOrbPiece : MonoBehaviour, IDamageSource
    {
        [SerializeField] private SpriteRenderer _pieceSpriteRenderer;

        [SerializeField] private GameObject _pieceEffectObj;
        
        [Header("Bezier Curvature")]
        [SerializeField] private float _ctrl1Distance    = 1.2f;
        [SerializeField] private float _ctrl2Distance    = 1.0f;
        [SerializeField] private float _lateralAmplitude = 0.6f;
        
        [Header("Random Curve")]
        [SerializeField] private float _randPerpAngleDeg = 25f;
        [SerializeField] private Vector2 _randAmpMul     = new Vector2(0.7f, 1.3f);
        [SerializeField] private Vector2 _randCtrlMul    = new Vector2(0.85f, 1.2f);

        private float _ampMul;
        private float _ctrl1Mul;
        private float _ctrl2Mul;
        private float _perpAngleDeg;
        
        [Header("Target Follow")]
        [SerializeField] private float _targetFollowLerp = 12f;
        [SerializeField] private float _arriveDistance   = 0.18f;

        [Header("Explosion")]
        [SerializeField] private float _explosionRadius   = 0.6f;
        [SerializeField] private float _explosionDuration = 0.08f;

        public struct ElementalOrbPieceData
        {
            public Team   Team;
            public int    Damage;
            public Sprite Sprite;

            public Vector3   StartPos;
            public Vector3   TargetPos;
            public Transform TargetTransform;
            public Vector2   InitialDir;
            public float     LateralSign;

            public float   Speed;
            public float   ExplosionRadius;
            public float   ExplosionDuration;

            public Action  OnFinish;
        }

        public Team SourceTeam { get; private set; }
        public int  Damage     { get; private set; }

        private Vector3 _p0, _p1, _p2, _p3;
        private float   _t;
        private float   _travelTime;

        private Transform _liveTarget;
        private float     _lateralSign;

        private Action  _onFinish;
        private bool    _exploding;

        private readonly Collider2D[] _hits = new Collider2D[16];
        
        private Vector2 Rotate2D(Vector2 v, float deg)
        {
            float rad = deg * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad);
            float s = Mathf.Sin(rad);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }
        
        public void InitOrb(ElementalOrbPieceData data)
        {
            SourceTeam   = data.Team;
            Damage       = data.Damage;
            _onFinish    = data.OnFinish;
            _exploding   = false;
            _liveTarget  = data.TargetTransform;
            _lateralSign = Mathf.Sign(data.LateralSign == 0 ? 1f : data.LateralSign);

            if (_pieceSpriteRenderer && data.Sprite) _pieceSpriteRenderer.sprite = data.Sprite;

            if (data.ExplosionRadius   > 0f) _explosionRadius   = data.ExplosionRadius;
            if (data.ExplosionDuration > 0f) _explosionDuration = data.ExplosionDuration;

            _p0 = data.StartPos;
            _p3 = data.TargetPos;

            Vector2 dir0  = data.InitialDir.sqrMagnitude > 1e-6f ? data.InitialDir.normalized : Vector2.up;
            Vector2 to    = (_p3 - _p0);
            Vector2 dirT  = to.sqrMagnitude > 1e-6f ? to.normalized : dir0;
            Vector2 perp0 = new Vector2(-dir0.y, dir0.x);
            Vector2 perpT = new Vector2(-dirT.y, dirT.x);

            _ampMul       = UnityEngine.Random.Range(_randAmpMul.x, _randAmpMul.y);
            _ctrl1Mul     = UnityEngine.Random.Range(_randCtrlMul.x, _randCtrlMul.y);
            _ctrl2Mul     = UnityEngine.Random.Range(_randCtrlMul.x, _randCtrlMul.y);
            _perpAngleDeg = UnityEngine.Random.Range(-_randPerpAngleDeg, _randPerpAngleDeg);

            Vector2 perp0r = Rotate2D(perp0, _perpAngleDeg);
            Vector2 perpTr = Rotate2D(perpT, _perpAngleDeg);

            float amp = _lateralAmplitude * _ampMul;
            
            _p1 = _p0 + (Vector3)(dir0 * (_ctrl1Distance * _ctrl1Mul) + perp0r * (amp * _lateralSign));
            _p2 = _p3 - (Vector3)(dirT * (_ctrl2Distance * _ctrl2Mul)) + (Vector3)(perpTr * (amp * -_lateralSign));

            float dist = Vector3.Distance(_p0, _p3);
            _travelTime = dist / Mathf.Max(0.01f, data.Speed);

            _t = 0f;
            transform.position = _p0;
            AlignTo(_p1 - _p0);

            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (_exploding) return;
            
            // 적 재생성 시 이전 타겟으로 잡았던 적일 때 따라가는 이슈 해결용 예외처리.
            if (_liveTarget != null && !_liveTarget.gameObject.activeInHierarchy)
            {
                _liveTarget = null;
            }
            
            if (_liveTarget && _liveTarget.gameObject.activeInHierarchy)
            {
                Vector3 desiredP3 = _liveTarget.position;
                _p3 = Vector3.Lerp(_p3, desiredP3, Mathf.Clamp01(_targetFollowLerp * Time.deltaTime));

                Vector2 to   = (_p3 - _p0);
                Vector2 dirT = to.sqrMagnitude > 1e-6f ? to.normalized : Vector2.up;
                Vector2 perpT= new Vector2(-dirT.y, dirT.x);
                Vector2 perpTr = Rotate2D(perpT, _perpAngleDeg);
                float amp = _lateralAmplitude * _ampMul;

                _p2 = _p3 - (Vector3)(dirT * (_ctrl2Distance * _ctrl2Mul)) + (Vector3)(perpTr * (amp * -_lateralSign));

            }

            if ((_p3 - transform.position).sqrMagnitude <= _arriveDistance * _arriveDistance)
            {
                transform.position = _p3;
                Explode();
                return;
            }

            _t += Time.deltaTime / _travelTime;
            _t = Mathf.Min(_t, 1f);

            Vector3 pos = Cubic(_p0, _p1, _p2, _p3, _t);
            Vector3 tan = Tangent(_p0, _p1, _p2, _p3, _t);

            transform.position = pos;
            AlignTo(tan);

            if (_t >= 1f && !_liveTarget) Explode();
        }

        private void Explode()
        {
            _exploding = true;

            int hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, _explosionRadius, _hits);
            for (int i = 0; i < hitCount; i++)
            {
                var col = _hits[i];
                if (!col) continue;

                if (col.TryGetComponent<IDamageable>(out var dmg) && dmg.Team != SourceTeam && !dmg.IsInvulnerable)
                {
                    AudioManager.Instance.PlaySFX(SFX.fire_orb);
                    
                    dmg.TakeDamage(Damage);
                    var hitPos = col.bounds.ClosestPoint(transform.position);
                    DamageTextPool.Instance.Show(Damage, hitPos, Color.white);
                }
            }

            GameObject effectObj = Instantiate(_pieceEffectObj, transform.position, Quaternion.identity);
            Destroy(effectObj, 0.7f);
            
            StartCoroutine(FinishAfter(_explosionDuration));
        }

        private IEnumerator FinishAfter(float time)
        {
            yield return new WaitForSeconds(time);
            _onFinish?.Invoke();
        }

        private static Vector3 Cubic(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float u = 1f - t, uu = u * u, tt = t * t;
            return (uu * u) * p0 + (3f * uu * t) * p1 + (3f * u * tt) * p2 + (tt * t) * p3;
        }

        private static Vector3 Tangent(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float u = 1f - t;
            Vector3 d = 3f * u * u * (p1 - p0) + 6f * u * t * (p2 - p1) + 3f * t * t * (p3 - p2);
            return d.sqrMagnitude > 1e-6f ? d.normalized : Vector3.up;
        }

        private void AlignTo(Vector3 dir)
        {
            if (dir.sqrMagnitude < 1e-6f) return;
            float ang = Vector2.SignedAngle(Vector2.up, new Vector2(dir.x, dir.y));
            transform.rotation = Quaternion.AngleAxis(ang, Vector3.forward);
        }
    }
}
