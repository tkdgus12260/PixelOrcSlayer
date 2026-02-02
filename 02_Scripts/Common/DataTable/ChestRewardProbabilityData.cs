using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvival
{
    [Serializable]
    public class ChestRewardProbabilityData
    {
        public int ItemId;
        public string ChestId;
        public int LootProbability;
    }
}