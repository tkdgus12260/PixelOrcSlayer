using System.Collections;
using UnityEngine;

namespace PixelSurvival
{
    public class OrbitShieldPiece : MonoBehaviour
    {
        private Team _team;

        [SerializeField] private SpriteRenderer _pieceSprite;
        [SerializeField] private Collider2D _pieceCollider;
        
        // 쉴드 피격 변수
        private readonly float _disableDuration = 10f;
        private WaitForSeconds _disableWait;
        private Coroutine _disableCo;
        
        public void Init(Team team, float cooldown, Sprite pieceSprite)
        {
            _team = team;
            _pieceSprite.sprite = pieceSprite;
            
            _disableWait = new WaitForSeconds(cooldown);
            
            UpdateColliderToSprite();
        }
        
        private void UpdateColliderToSprite()
        {
            if (!_pieceSprite || !_pieceSprite.sprite) return;
            if (!(_pieceCollider is CapsuleCollider2D capsule)) return;

            var localBounds = _pieceSprite.sprite.bounds;

            var s = transform.localScale;
            float sx = Mathf.Approximately(Mathf.Abs(s.x), 0f) ? 1f : Mathf.Abs(s.x);
            float sy = Mathf.Approximately(Mathf.Abs(s.y), 0f) ? 1f : Mathf.Abs(s.y);

            Vector2 size = new Vector2(localBounds.size.x * sx, localBounds.size.y * sy);

            capsule.direction = (size.x >= size.y) ? CapsuleDirection2D.Horizontal : CapsuleDirection2D.Vertical;
            capsule.size = size;

            capsule.offset = localBounds.center;
            capsule.enabled = true;
        }

        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent<Projectile>(out var projectile))
            {
                if (projectile.SourceTeam != _team)
                {
                    AudioManager.Instance.PlaySFX(SFX.shield_piece);
                    
                    PoolManager.Instance?.Despawn(projectile.gameObject);
                    DisableThisPieceForSeconds(_disableDuration);
                }
                return;
            }

            // 쉴드 타격 삭제.
            // if (other.TryGetComponent<IDamageable>(out var target))
            // {
            //     if (target.Team != _team)
            //     {
            //         target.TakeDamage(_damage);
            //
            //         Vector3 hitPos = other.bounds.ClosestPoint(transform.position);
            //         DamageTextPool.Instance.Show(_damage, hitPos, Color.white);
            //     }
            // }
        }

        /// <summary>
        /// 맞은 쉴드 disable -> delay -> enable
        /// </summary>
        private void DisableThisPieceForSeconds(float seconds)
        {
            if (_disableCo != null)
                StopCoroutine(_disableCo);

            _disableCo = StartCoroutine(DisableCoroutine(seconds));
        }

        private IEnumerator DisableCoroutine(float seconds)
        {
            if (_pieceCollider) _pieceCollider.enabled = false;
            if (_pieceSprite) _pieceSprite.enabled = false;

            yield return _disableWait;

            if (_pieceCollider) _pieceCollider.enabled = true;
            if (_pieceSprite) _pieceSprite.enabled = true;

            _disableCo = null;
        }
    }
}