using UnityEngine;

namespace PixelSurvival
{
    [CreateAssetMenu(menuName = "PixelSurvival/Enemy/Costume", fileName = "EnemyCostume_")]
    public class EnemyCostumeData : ScriptableObject
    {
        // Enemt Type
        public EnemyType EnemyType;
        
        // Local Scale
        public float Scale;
        
        // Body
        [Header("Body")]
        public Sprite Head;
        public Sprite Body;
        public Sprite Arm_L;
        public Sprite Arm_R;
        public Sprite Foot_L;
        public Sprite Foot_R;

        // Equipment
        [Header("Equipment")]
        public Sprite Hair;
        public Sprite Eye_L;
        public Sprite Pupil_L;
        public Sprite Eye_R;
        public Sprite Pupil_R;
        public Sprite Mouth;
        public Sprite Helmet;
        public Sprite Cloth;
        public Sprite ClothArm_L;
        public Sprite ClothArm_R;
        public Sprite ClothShoulder_L;
        public Sprite ClothShoulder_R;
        public Sprite Cape;
        public Sprite Armor;
        public Sprite Weapon;
        public Sprite Shield;
        
        // Projectile
        [Header("Projectile")]
        public Sprite ProjectileSprite;
        
        [Header("AnimatorComtroller")]
        public RuntimeAnimatorController AnimatorController;
    }
}
