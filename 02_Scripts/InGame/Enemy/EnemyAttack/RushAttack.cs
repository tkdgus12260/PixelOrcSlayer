using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvival
{
    public class RushAttack : MonoBehaviour, IDamageSource
    {
        public Team SourceTeam { get; private set; }
        public int Damage { get; private set; }

        public void Init(int damage, Team sourceTeam)
        {
            Damage = damage;
            SourceTeam = sourceTeam;
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent<IDamageable>(out var damageable))
            {
                if (damageable.IsInvulnerable) return;
                
                if (damageable.Team == SourceTeam) return;
                damageable.TakeDamage(Damage);
                
                // Damage Text
                Vector3 hitPos = other.bounds.ClosestPoint(transform.position);
                DamageTextPool.Instance.Show(Damage, hitPos, Color.red);
            }
        }
    }
}