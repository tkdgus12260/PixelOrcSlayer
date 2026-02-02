using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvival
{
    public class AxeProjectile : MonoBehaviour
    {
        [SerializeField] private GameObject _spritePrefab;
        private readonly float _spinSpeed = 2000f;

        private Rigidbody2D _rb;

        private Transform _owner;
        private Team _team;
        private int _damage;

        private float _pickupEnableDelay;
        private float _spawnTime;
        private Func<bool> _canPickupPredicate;
        private Action _onPickedUp;

        private readonly HashSet<IDamageable> _hitOnce = new();

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        public void Init(Transform owner, Team team, int damage, Vector2 initialVelocity, 
            float pickupEnableDelay, Func<bool> canPickupPredicate, Action onPickedUp)
        {
            _owner = owner;
            _team = team;
            _damage = damage;

            _pickupEnableDelay = Mathf.Max(0f, pickupEnableDelay);
            _spawnTime = Time.time;
            _canPickupPredicate = canPickupPredicate;
            _onPickedUp = onPickedUp;

            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rb.bodyType = RigidbodyType2D.Dynamic;

            _rb.velocity = initialVelocity;
        }

        private void Update()
        {
            _spritePrefab.transform.Rotate(0f, 0f, _spinSpeed * Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other) return;
            
            if (other.CompareTag("Wall"))
            {
                Vector2 axePos = _rb ? _rb.position : (Vector2)transform.position;
                Vector2 v = _rb ? _rb.velocity : Vector2.zero;
                if (v.sqrMagnitude < 0.0001f) v = Vector2.right;

                Vector2 wallPoint = other.ClosestPoint(axePos);
                Vector2 normalRaw = axePos - wallPoint;

                Vector2 nrm = normalRaw;
                if (nrm.sqrMagnitude < 0.0001f)
                    nrm = -v.normalized;
                else
                    nrm.Normalize();

                Vector2 reflected = Vector2.Reflect(v, nrm);

                bool timeOk = (Time.time - _spawnTime) >= _pickupEnableDelay;
                bool canPickup = (_canPickupPredicate == null) ? true : _canPickupPredicate.Invoke();
                bool steer = timeOk && canPickup && _owner != null;

                if (steer)
                {
                    Vector2 toOwner = ((Vector2)_owner.position - axePos);
                    if (toOwner.sqrMagnitude > 0.0001f)
                    {
                        Vector2 ownerDir = toOwner.normalized;

                        float speed = reflected.magnitude;

                        Vector2 newDir = Vector2.Lerp(reflected.normalized, ownerDir, speed).normalized;
                        reflected = newDir * speed;
                    }
                }

                _rb.velocity = reflected;
                return;
            }


            if (_owner != null && other.transform == _owner)
            {
                bool timeOk = (Time.time - _spawnTime) >= _pickupEnableDelay;
                bool canPickup = (_canPickupPredicate == null) ? true : _canPickupPredicate.Invoke();

                if (timeOk && canPickup)
                {
                    _onPickedUp?.Invoke();
                }

                return;
            }

            if (other.TryGetComponent<IDamageable>(out var target))
            {
                if (target.Team == _team) return;
                if (target.IsInvulnerable) return;

                if (_hitOnce.Contains(target)) return;
                _hitOnce.Add(target);

                target.TakeDamage(_damage);
                var hitPos = other.bounds.ClosestPoint(transform.position);
                DamageTextPool.Instance.Show(_damage, hitPos, Color.red);
            }
        }

    }
}
