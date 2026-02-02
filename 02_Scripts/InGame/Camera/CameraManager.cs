using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

namespace PixelSurvival
{
    public class CameraManager : SingletonBehaviour<CameraManager>
    {
        [SerializeField] private CinemachineVirtualCamera _virtualCamera;
        private CinemachineConfiner2D _confiner;

        protected override void Init()
        {
            isDestroyOnLoad = true;

            base.Init();

            _confiner = _virtualCamera.GetComponent<CinemachineConfiner2D>();
        }

        public void Setup(Transform target, PolygonCollider2D collider)
        {
            _virtualCamera.Follow = target;
            _confiner.m_BoundingShape2D = collider;
            _confiner.InvalidateCache();
        }
    }
}