using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvival
{
    [Serializable]
    public class ProductData
    {
        public string ProductId;
        public ProductType ProductType;
        public string ProductName;
        public PurchaseType PurchaseType;
        public int PurchaseCost;
        public int RewardGem;
        public int RewardGold;
        public int RewardItemId;
    }

    public enum ProductType
    {
        Pack,
        Gem,
        Gold,
        Chest,
    }

    public enum PurchaseType
    {
        IAP,
        Ad,
        Gem,
        Gold,
    }
}
