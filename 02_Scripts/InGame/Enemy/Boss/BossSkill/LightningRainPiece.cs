using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvival
{
    public class LightningRainPiece : MonoBehaviour
    {
        private Team _team;
        private int _damage;
        
        private readonly Collider2D[] _hits = new Collider2D[16];
        private readonly float scaleCorrection = 1.15f;
        
        public void Init(Team team, int damage, float explosionRadius)
        {
            _team = team;
            _damage = damage;
            
            transform.localScale =
                new Vector3(explosionRadius * scaleCorrection, explosionRadius * scaleCorrection, 1f);

        }

        public void Explode(float explosionRadius)
        {
            int count = Physics2D.OverlapCircleNonAlloc(transform.position, explosionRadius, _hits);
            for (int i = 0; i < count; i++)
            {
                var col = _hits[i];
                if (!col) continue;

                if (col.TryGetComponent<IDamageable>(out var target))
                {
                    if (target.Team != _team && !target.IsInvulnerable)
                    {
                        target.TakeDamage(_damage);
                        var hitPos = col.bounds.ClosestPoint(transform.position);
                        DamageTextPool.Instance.Show(_damage, hitPos, Color.red);
                    }
                }
            }
        }

    }
}