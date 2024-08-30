using HarmonyLib;
using Scopophobia;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[HarmonyPatch(typeof(RoundManager))]
internal class ShyGuySpawnSettings
{
    public static string[] InsideOnly = new string[6] { "Level1Experimentation", "Level2Assurance", "Level3Vow", "Level4March", "Level7Offense", "Level6Adamance" };

    [HarmonyPatch("BeginEnemySpawning")]
    [HarmonyPrefix]
    public static void UpdateSpawnRates(ref SelectableLevel ___currentLevel) //set as public
    {
        if (Config.DisableSpawnRates)
        {
            ScopophobiaPlugin.logger.LogInfo("Scopophobia Spawn Settings are disabled in config. Ignoring...");
            return;
        }
        if (!Config.appears || ScopophobiaPlugin.shyPrefab == null)
        {
            return;
        }
        try
        {
            SpawnableEnemyWithRarity shyEnemy = ScopophobiaPlugin.shyPrefab;
            List<SpawnableEnemyWithRarity> enemies = ___currentLevel.Enemies;
            for (int i = 0; i < ___currentLevel.Enemies.Count; i++)
            {
                SpawnableEnemyWithRarity val2 = ___currentLevel.Enemies[i];
                if (val2.enemyType.enemyName.ToLower() == "shy guy")
                {
                    enemies.Remove(val2);

                }
            }
            ___currentLevel.Enemies = enemies;
            shyEnemy.enemyType.PowerLevel = Config.ShyGuyPowerLevel; //change from int to float
            shyEnemy.rarity = Config.spawnRarity;
            shyEnemy.enemyType.probabilityCurve = new AnimationCurve(new Keyframe(0f, Config.startEnemySpawnCurve), new Keyframe(0.5f, Config.midEnemySpawnCurve), new Keyframe(1f, Config.endEnemySpawnCurve));
            shyEnemy.enemyType.MaxCount = Config.maxSpawnCount;
            shyEnemy.enemyType.isOutsideEnemy = Config.canSpawnOutside;
            if (Config.canSpawnOutside & (!Config.spawnOutsideHardPlanets || !InsideOnly.Contains(___currentLevel.sceneName)))
            {
                ___currentLevel.OutsideEnemies.Add(shyEnemy);
                SelectableLevel obj = ___currentLevel;
                obj.maxOutsideEnemyPowerCount += shyEnemy.enemyType.MaxCount * (int)shyEnemy.enemyType.PowerLevel; //typecast as int to fix PowerLevel, ty MaskedOverhaulFork
                ScopophobiaPlugin.logger.LogInfo("Shy Guy is able to spawn outside, Adding to spawnable entities");
            }
            ___currentLevel.Enemies.Add(shyEnemy);
        }
        catch { }
    }
}