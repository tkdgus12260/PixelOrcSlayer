using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvival
{
    public class ElementalStrikePiece : MonoBehaviour
    {
        private Team _team;
        private int _damage;
        
        // [SerializeField] private SpriteRenderer _pieceSpriteRenderer;
        [SerializeField] private float _explosionRadius   = 0.6f;
        private readonly Collider2D[] _hits = new Collider2D[16];
        
        public void Init(Team team, int damage, Sprite pieceSprite = null)
        {
            _team = team;
            _damage = damage;
            // _pieceSpriteRenderer.sprite = pieceSprite;
        }

        public void Explode()
        {
            int count = Physics2D.OverlapCircleNonAlloc(transform.position, _explosionRadius, _hits);
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
                        DamageTextPool.Instance.Show(_damage, hitPos, Color.white);
                    }
                }
            }
        }

    }
}