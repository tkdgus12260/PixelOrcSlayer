using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GlobalDefine
{
    public const string GOOGLE_PLAY_STORE_URL = "https://play.google.com/store/apps/details?id=com.SangHyun.PixelOrcSlayer";
    public const string APPLE_APP_STORE_URL = "https://apps.apple.com/us/app/pixel-orc-slayer/id6758434763";

    public const float THIRD_PARTY_SERVICE_INIT_TIME = 10f;
    
    public const int MAX_CHAPTER = 6;

    public static readonly WaitForSeconds DEATH_ANIMATION_TIME = new WaitForSeconds(0.5f);
    public static readonly WaitForSeconds INVULN_TIME = new WaitForSeconds(1f);
    
    public enum RewardType
    {
        Gold,
        Gem,
    }
    
    public static string DescriptionFormat(string template, IReadOnlyDictionary<string, object> args)
    {
        if (string.IsNullOrEmpty(template) || args == null) return template;

        foreach (var kv in args)
            template = template.Replace("{" + kv.Key + "}", kv.Value?.ToString() ?? string.Empty);

        return template;
    }
    
    
    #region UPGRADE_INFO

    public static int MaxUpgradeLevel = 5;
    public static int LegendaryMaxUpgradeLevel = 10;
        
    public static float[] UpgradeChance =
    {
        0.80f, // 0 -> 1
        0.70f, // 1 -> 2
        0.50f, // 2 -> 3
        0.40f, // 3 -> 4
        0.30f, // 4 -> 5
        0.20f, // 5 -> 6
        0.20f, // 6 -> 7
        0.20f, // 7 -> 8
        0.10f, // 8 -> 9
        0.10f, // 9 -> 10
    };

    public static long[,] UpgradeCost =
    {
        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, 
        { 1000, 1500, 2000, 2500, 3000, 0, 0, 0, 0, 0 }, 
        { 2000, 3000, 4000, 5000, 6000, 0, 0, 0, 0, 0 }, 
        { 3000, 5000, 7000, 9000, 12000, 0, 0, 0, 0, 0 }, 
        { 5000, 8000, 10000, 12000, 15000, 0, 0, 0, 0, 0 }, 
        { 8000, 10000, 12000, 15000, 20000, 30000, 50000, 70000, 90000, 120000 }, 
    };

    #endregion
}
