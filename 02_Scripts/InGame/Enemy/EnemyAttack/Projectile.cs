using UnityEngine;

namespace PixelSurvival
{
    public enum ProjectileType
    {
        
    }
    
    public class Projectile : MonoBehaviour, IDamageSource
    {
        [SerializeField] private SpriteRenderer _projectileRenderer;
        [SerializeField] private CapsuleCollider2D _projectileCollider;
        private float _speed;
        private float _spinSpeed;
        private readonly float _lifeTime = 5f;

        private Vector3 _direction;
        private float _lifeTimer;
        private bool _isPiercing = false;
        private bool _initialized;
        
        public Team SourceTeam { get; private set; }
        public int Damage { get; private set; }

        public void Init(Vector3 direction, Sprite sprite, int damage, float speed, Team sourceTeam, bool isPiercing = false, float rotValue = 0, Vector2 scaleValue = default, Vector2 colSize = default, float spinSpeed = 0f)
        {
            _direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.up;
            _lifeTimer = 0f;
            _initialized = true;

            if (_projectileRenderer && sprite)
            {
                _projectileRenderer.sprite = sprite;
                _projectileRenderer.gameObject.transform.localRotation = Quaternion.Euler(0f, 0f, rotValue);
                _projectileRenderer.gameObject.transform.localScale = new Vector3(scaleValue == default ? 1f : scaleValue.x, scaleValue == default ? 1f : scaleValue.y, 1f);
            }
            
            if (_projectileCollider)
            {
                if (colSize == default)
                {
                    Vector2 baseSize = sprite.bounds.size;
                    Vector2 s = (scaleValue == default) ? Vector2.one : scaleValue;

                    Vector2 spriteSize = new Vector2(baseSize.x * s.x, baseSize.y * s.y);
                    Vector2 scaledSize = new Vector2(spriteSize.x, spriteSize.y);
                    _projectileCollider.size = spriteSize;

                    _projectileCollider.direction = (scaledSize.x >= scaledSize.y)
                        ? CapsuleDirection2D.Horizontal
                        : CapsuleDirection2D.Vertical;
                }
                else
                {
                    _projectileCollider.size = colSize;
                }
            }

            float angle = Vector2.SignedAngle(Vector2.up, new Vector2(_direction.x, _direction.y));
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            
            _speed = speed;
            _spinSpeed = spinSpeed;
            Damage = damage;
            SourceTeam = sourceTeam;
            _isPiercing = isPiercing;
            
            gameObject.SetActive(true);
        }


        private void OnEnable()
        {
            _lifeTimer = 0f;
        }

        private void Update()
        {
            if (!_initialized) return;

            transform.position += _direction * (_speed * Time.deltaTime);
            if (_spinSpeed > 0f)
            {
                _projectileRenderer.transform.Rotate(0f, 0f, _spinSpeed * Time.deltaTime);
            }

            _lifeTimer += Time.deltaTime;
            if (_lifeTimer >= _lifeTime)
            {
                _initialized = false;
                PoolManager.Instance.Despawn(gameObject);
            }
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent<IDamageable>(out var damageable))
            {
                if (damageable.Team != SourceTeam)
                {
                    if(!_isPiercing)
                        PoolManager.Instance.Despawn(gameObject);
                    
                    if(damageable.IsInvulnerable) return;

                    damageable.TakeDamage(Damage);

                    // Damage Text
                    Vector3 hitPos = other.bounds.ClosestPoint(transform.position);
                    DamageTextPool.Instance.Show(Damage, hitPos, damageable.Team == Team.Player ? Color.red : Color.white);
                }
            }
            
            if(other.CompareTag("Wall"))
            {
                PoolManager.Instance.Despawn(gameObject);
            }
        }
    }
}