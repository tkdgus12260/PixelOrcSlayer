using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace PixelSurvival
{
    public enum State
    {
        Move,
    }

    public class PlayerController : MonoBehaviour
    {
        private State State = State.Move;

        private int _attactPower;
        private int _defense;
        private int _speed = 1;

        private Animator _animator;
        private NavMeshAgent _agent;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _agent = GetComponent<NavMeshAgent>();
            _agent.updateRotation = false;
            _agent.updateUpAxis = false;
        }

        private void FixedUpdate()
        {
            if(GameManager.Instance.IsPaused)
                return;

            PlayerMovement();
            PlayerAnimation();
        }

        private void Init()
        {
            var userInventoryData = UserDataManager.Instance.GetUserData<UserInventoryData>();
            if (userInventoryData == null)
            {
                Logger.LogError("UserInventoryData does not exist");
                return;
            }
        }

        private void PlayerMovement()
        {
            switch (State)
            {
                case State.Move:
                    Vector2 nextVec = JoystickInput.Instance.MoveInput * (40 * _speed * Time.fixedDeltaTime);

                    _agent.SetDestination(transform.position + (Vector3)nextVec);
                    break;
                default:
                    break;
            }

            if (JoystickInput.Instance.MoveInput.x > 0) transform.localScale = new Vector2(-1, 1);
            else if (JoystickInput.Instance.MoveInput.x < 0) transform.localScale = Vector2.one;
        }

        private void PlayerAnimation()
        {
            switch (State)
            {
                case State.Move:
                    _animator.SetFloat("Speed", JoystickInput.Instance.MoveInput.magnitude);
                    break;
                default:
                    break;
            }
        }
    }
}