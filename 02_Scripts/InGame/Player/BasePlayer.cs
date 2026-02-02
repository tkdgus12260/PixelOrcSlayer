using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace PixelSurvival
{
    public class BasePlayerData
    {
        public int MaxHP;
        public int AttackDamage;
        public float MoveSpeed;
        public float AttackSpeed;
    }

    public abstract class BasePlayer : MonoBehaviour
    {
        [SerializeField] public int MaxHP;
        [SerializeField] protected int attackDamage;
        [SerializeField] protected float moveSpeed;
        [SerializeField] protected float attackSpeed;

        
        
        private Coroutine _invulnCoroutine;
        
        [SerializeField] protected List<SpriteRenderer> playerCostumes;
        protected Animator animator;
        protected NavMeshAgent agent;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            agent = GetComponent<NavMeshAgent>();
        }

        public virtual void Init()
        {
            Logger.Log($"{GetType()}:: Inuit");

            MaxHP = 0;
            attackDamage = 0;
            moveSpeed = 0f;
            attackSpeed = 0f;
        }

        public virtual void SetInfo(BasePlayerData enemyData)
        {
            Logger.Log($"{GetType()}:: SetInfo");

            MaxHP = enemyData.MaxHP;
            attackDamage = enemyData.AttackDamage;
            moveSpeed = enemyData.MoveSpeed;
            attackSpeed = enemyData.AttackSpeed;
        }
        
        protected void SetCostumeAlpha(float a)
        {
            foreach (var spriteRenderer in playerCostumes)
            {
                if (spriteRenderer == null) continue;
                if(spriteRenderer.sprite == null) continue;
                
                var color = spriteRenderer.color;
                color.a = a;
                spriteRenderer.color = color;
            }
        }

        public abstract void OnAttack();
    }
}