using System;
using System.Collections;
using System.Collections.Generic;
using NavMeshPlus.Components;
using UnityEngine;

namespace PixelSurvival
{
    public class NavigationManager : SingletonBehaviour<NavigationManager>
    {
        public NavMeshSurface Surface;

        protected override void Init()
        {
            isDestroyOnLoad = true;
            base.Init();
        }

        public void NavMeshBake()
        {
            Surface.BuildNavMesh();
        }
    }
}