using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace PixelSurvival
{
    public class BaseEnemyData
    {
        public EnemyType EnemyType;
        public int MaxHP;
        public int AttackDamage;
        public float MoveSpeed;
        public float AttackSpeed;
        public float AttackRange;
        public int RewardGold;
    }

    [Serializable]
    public class EnemyCostume
    {
        //Root
        public Transform Root;
        
        // Body
        public SpriteRenderer Head;
        public SpriteRenderer Body;
        public SpriteRenderer Arm_L;
        public SpriteRenderer Arm_R;
        public SpriteRenderer Foot_L;
        public SpriteRenderer Foot_R;

        // Equipment
        public SpriteRenderer Hair;
        public SpriteRenderer Eye_L;
        public SpriteRenderer Pupil_L;
        public SpriteRenderer Eye_R;
        public SpriteRenderer Pupil_R;
        public SpriteRenderer Mouth;
        public SpriteRenderer Helmet;
        public SpriteRenderer Cloth;
        public SpriteRenderer ClothArm_L;
        public SpriteRenderer ClothArm_R;
        public SpriteRenderer ClothShoulder_L;
        public SpriteRenderer ClothShoulder_R;
        public SpriteRenderer Cape;
        public SpriteRenderer Armor;
        public SpriteRenderer Weapon;
        public SpriteRenderer Shield;
        
        public Animator Animator;
    }

    public enum EnemyState
    {
        Move,
        Attack,
        Dead
    } 

    public abstract class BaseEnemy : MonoBehaviour
    {
        [SerializeField] protected EnemyType enemyType;
        [SerializeField] public int MaxHP;
        [SerializeField] protected int attackDamage;
        [SerializeField] protected float moveSpeed;
        [SerializeField] protected float attackSpeed;
        [SerializeField] protected float attackRange = 0f;
        public int RewardGold;

        public virtual int CurrentHp => 1;
        public virtual float CurrentHpPercent => 1f;

        private bool _isMovementLocked = false;
        
        [SerializeField] protected EnemyCostume enemyCostume;
        
        protected Transform target;
        public NavMeshAgent agent;
        protected Animator animator;
        protected Sprite projectileSprite;
        
        [SerializeField] protected BoxCollider2D meleeCollider;
        
        public event Action<BaseEnemy> Died;

        [SerializeField] private EnemyState _state = EnemyState.Move;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            agent = GetComponent<NavMeshAgent>();
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }

        private void OnEnable()
        {
            SetState(EnemyState.Move);
        }

        protected virtual void Update()
        {
            if (GameManager.Instance.IsPaused) return;
            if (target == null) return;
            if (agent == null) return;

            float vx = target.position.x - transform.position.x;
            if (vx > 0) transform.localScale = new Vector2(-1, 1);
            else if (vx < 0) transform.localScale = Vector2.one;

            float sqrDist = (target.position - transform.position).sqrMagnitude;
            float sqrRange = attackRange * attackRange;

            switch (_state)
            {
                case EnemyState.Move:
                    OnMove();

                    if (sqrDist <= sqrRange)
                    {
                        SetState(EnemyState.Attack);
                    }

                    break;

                case EnemyState.Attack:
                    if (sqrDist > sqrRange * 1.1f)
                    {
                        SetState(EnemyState.Move);
                    }
                    else
                    {
                        OnStop();
                        OnAttack();
                    }
                    break;

                case EnemyState.Dead:
                    break;
            }
        }

        private void SetState(EnemyState next)
        {
            if (_state == next) 
                return;

            switch (next)
            {
                case EnemyState.Move:
                    IsMovementLocked(false);
                    break;
                case EnemyState.Attack:
                    IsMovementLocked(true);
                    break;
                case EnemyState.Dead:
                    IsMovementLocked(true);
                    break;
                default:
                    break;
            }
            
            _state = next;
        }

        public virtual void Init(Transform target)
        {
            Logger.Log($"{GetType()}:: Inuit");

            MaxHP = 0;
            attackDamage = 0;
            moveSpeed = 0f;
            attackSpeed = 0f;
            attackRange = 0f;

            this.target = target;
        }

        public virtual void SetInfo(BaseEnemyData enemyData)
        {
            Logger.Log($"{GetType()}:: SetInfo");

            enemyType = enemyData.EnemyType;
            MaxHP = enemyData.MaxHP;
            attackDamage = enemyData.AttackDamage;
            moveSpeed = enemyData.MoveSpeed;
            attackSpeed = enemyData.AttackSpeed;
            attackRange = enemyData.AttackRange;

            RewardGold = enemyData.RewardGold;
        }

        public virtual void OnMove()
        {
            if(_isMovementLocked) return;
            
            agent.SetDestination(target.position);
            animator.SetFloat("Speed", 1);
        }

        protected virtual void OnStop()
        {
            agent.ResetPath(); 
            agent.velocity = Vector3.zero;
            
            if (animator) animator.SetFloat("Speed", 0f);
        }

        public virtual void OnDie()
        {
            if(_state == EnemyState.Dead) return;

            Logger.Log($"{GetType()}:: OnDie");

            SetState(EnemyState.Dead);
            StartCoroutine(OnDead());
        }

        private IEnumerator OnDead()
        {
            OnStop();
            animator.SetTrigger("OnDeath");
            Died?.Invoke(this);

            yield return GlobalDefine.DEATH_ANIMATION_TIME;
            PoolManager.Instance.Despawn(gameObject);
        }

        public virtual void OnAttack()
        {
            animator.SetTrigger("OnAttack");
        }

        private void IsMovementLocked(bool value)
        {
            _isMovementLocked = value;
        }

        #region SET_ENEMTY_COSTUME

        public void SetEnemy(EnemyCostumeData data)
        {
            if (enemyCostume == null) return;
            if (data == null) return;

            enemyType = data.EnemyType;
            enemyCostume.Root.localScale = Vector3.one * data.Scale;

            CostumeApply(enemyCostume.Head, data.Head);
            CostumeApply(enemyCostume.Body, data.Body);
            CostumeApply(enemyCostume.Arm_L, data.Arm_L);
            CostumeApply(enemyCostume.Arm_R, data.Arm_R);
            CostumeApply(enemyCostume.Foot_L, data.Foot_L);
            CostumeApply(enemyCostume.Foot_R, data.Foot_R);

            CostumeApply(enemyCostume.Hair, data.Hair);
            CostumeApply(enemyCostume.Eye_L, data.Eye_L);
            CostumeApply(enemyCostume.Pupil_L, data.Pupil_L);
            CostumeApply(enemyCostume.Eye_R, data.Eye_R);
            CostumeApply(enemyCostume.Pupil_R, data.Pupil_R);
            CostumeApply(enemyCostume.Mouth, data.Mouth);
            CostumeApply(enemyCostume.Helmet, data.Helmet);
            CostumeApply(enemyCostume.Cloth, data.Cloth);
            CostumeApply(enemyCostume.ClothArm_L, data.ClothArm_L);
            CostumeApply(enemyCostume.ClothArm_R, data.ClothArm_R);
            CostumeApply(enemyCostume.ClothShoulder_L, data.ClothShoulder_L);
            CostumeApply(enemyCostume.ClothShoulder_R, data.ClothShoulder_R);
            CostumeApply(enemyCostume.Cape, data.Cape);
            CostumeApply(enemyCostume.Armor, data.Armor);
            CostumeApply(enemyCostume.Weapon, data.Weapon);
            CostumeApply(enemyCostume.Shield, data.Shield);
            
            projectileSprite = data.ProjectileSprite;
            enemyCostume.Animator.runtimeAnimatorController = data.AnimatorController;
        }

        private void CostumeApply(SpriteRenderer root, Sprite costume)
        {
            if (!root)
            {
                Logger.Log("No root");
                return;
            }

            if (costume)
            {
                root.sprite = costume;
                root.enabled = true;
            }
            else
            {
                root.sprite = null;
                root.enabled = false;
            }
        }

        #endregion
    }
}
