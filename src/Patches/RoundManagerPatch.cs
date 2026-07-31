using HarmonyLib;
using Scopophobia;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Diagnostics;

[HarmonyPatch(typeof(RoundManager))]
internal static class RoundManagerPatch
{
    [HarmonyPatch(nameof(RoundManager.LoadNewLevel))]
    [HarmonyPostfix]
    private static void LoadNewLevelPatch()
    {
        EnemyDataManager.SetEnemyDataForCurrentLevel();
    }
}