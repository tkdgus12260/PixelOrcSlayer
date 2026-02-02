using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvival
{
    public class OrbitShield : MonoBehaviour, ISkill
    {
        [SerializeField] private GameObject _piecePrefab;
        [SerializeField] private List<Sprite> _pieceSprite;
        
        private float radius = 1.2f;
        private float _angle;
        
        private readonly List<GameObject> _spawnedPieces = new();

        private Team _team;
        private SkillData _skillData;
        
        public void Init(SkillData skillData, int damage, Team team)
        {
            _skillData = skillData;
            _team = team;
            
            _angle = 0f;
            InitShieldPiece();
        }

        private void LateUpdate()
        {
            if (_skillData == null) return;

            var parent = transform.parent;
            if (parent)
            {
                var parentLossyScale = parent.lossyScale;
                transform.localScale = new Vector3(
                    Mathf.Approximately(parentLossyScale.x, 0f) ? 1f : 1f / parentLossyScale.x,
                    Mathf.Approximately(parentLossyScale.y, 0f) ? 1f : 1f / parentLossyScale.y,
                    Mathf.Approximately(parentLossyScale.z, 0f) ? 1f : 1f / parentLossyScale.z
                );

                transform.position = parent.position;
            }

            _angle += 50 * Time.deltaTime;
            transform.rotation = Quaternion.AngleAxis(_angle, Vector3.forward);
        }

        
        private void InitShieldPiece()
        {
            int count = Mathf.Max(1, _skillData.ObjectCount);
            float step = 360f / count;

            for (int i = 0; i < count; i++)
            {
                float deg = i * step;
                Vector2 pos2D = Deg2Dir(deg) * radius;

                GameObject pieceGo = CreateOrGetPiece();
                pieceGo.name = $"ShieldPiece_{i}";
                pieceGo.transform.SetParent(transform, false);
                pieceGo.transform.localPosition = new Vector3(pos2D.x, pos2D.y, 0f);
                pieceGo.transform.localRotation = Quaternion.AngleAxis(deg, Vector3.forward);

                if (pieceGo.TryGetComponent<OrbitShieldPiece>(out var piece))
                {
                    piece.Init(_team, _skillData.Cooldown, _pieceSprite[0]);
                }

                _spawnedPieces.Add(pieceGo);
            }
        }

        private GameObject CreateOrGetPiece()
        {
            if (_piecePrefab == null)
            {
                return null;
            }
            
            return Instantiate(_piecePrefab);
        }

        private static Vector2 Deg2Dir(float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }
    }
}
